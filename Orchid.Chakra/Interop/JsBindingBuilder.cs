using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Enklu.Orchid.Logging;

namespace Enklu.Orchid.Chakra.Interop
{
    /// <summary>
    /// A descriptor for a property that can be added to a binding.
    /// </summary>
    public class JsValueDescriptor
    {
        /// <summary>
        /// The name of the JavaScript value.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The host value used to set the js value.
        /// </summary>
        public object Value { get; set; }

        /// <summary>
        /// The host value's type.
        /// </summary>
        public Type ValueType { get; set; }
    }

    /// <summary>
    /// This builder class generates JavaScript bindings from a host object's <see cref="Type"/>.
    /// </summary>
    public class JsBindingBuilder
    {
        // TODO: This class isn't really necessary following some refactors. Look into moving
        // TODO: class method/property/field bindings into JsInterop and remove this.

        // TODO: The 'HostTypeCache' design needs some work, as most of it's original functionality has
        // TODO: been refactored. The remaining method, property, and field objects are just thinned
        // TODO: MethodInfo, PropertyInfo, FieldInfo respectively. We should still cache the reflection,
        // TODO: but we can likely do this with .NET types.

        /// <summary>
        /// Static type cache used to generate and lookup host type information.
        /// </summary>
        private static readonly HostTypeCache TypeCache = new HostTypeCache();

        /// <summary>
        /// Empty parameters for getter methods.
        /// </summary>
        private static object[] EmptyParameters = new object[0];

        private readonly JsContextScope _scope;
        private readonly JsBinder _binder;
        private readonly JsInterop _interop;

        private IHostType _hostType;
        private object _boundTo;
        private IList<JsValueDescriptor> _values;

        /// <summary>
        /// Creates a new <see cref="JsBindingBuilder"/> instance.
        /// </summary>
        public JsBindingBuilder(JsContextScope scope, JsBinder binder, JsInterop interop)
        {
            _scope = scope;
            _binder = binder;
            _interop = interop;
        }

        /// <summary>
        /// Appends a custom value binding to apply after the <see cref="JsBinding"/> is
        /// created.
        /// </summary>
        /// <param name="name">The name of the field to add.</param>
        /// <param name="value">The value to bind.</param>
        public JsBindingBuilder WithValue<T>(string name, T value)
        {
            if (null == _values)
            {
                _values = new List<JsValueDescriptor>();
            }

            _values.Add(new JsValueDescriptor
            {
                Name = name,
                Value = value,
                ValueType = typeof(T)
            });

            return this;
        }

        /// <summary>
        /// Sets the host object to target when creating the binding.
        /// </summary>
        /// <param name="hostObject">The host object to use for the javascript binding.</param>
        public JsBindingBuilder BoundTo<T>(T hostObject) => BoundTo(hostObject, typeof(T));

        /// <summary>
        /// Sets the host object to target when creating the binding.
        /// </summary>
        /// <param name="hostObject">The host object to use for the javascript binding.</param>
        /// <param name="type">The <see cref="Type"/> of the host object.</param>
        public JsBindingBuilder BoundTo(object hostObject, Type type)
        {
            _boundTo = hostObject;
            _hostType = TypeCache.Get(type);

            return this;
        }

        /// <summary>
        /// Creates the <see cref="JsBinding"/> instance representing the new bound JavaScript object.
        /// </summary>
        public JsBinding Build()
        {
            return _scope.Run(() =>
            {
                // Create a host object binding or new JS Object binding
                var jsValue = null != _boundTo
                    ? _binder.BindObject(_boundTo)
                    : JavaScriptValue.CreateObject();

                var binding = new JsBinding(_scope, _binder, _interop, jsValue);

                // Bind Host Object Methods, Properties, and Fields if bound to host object
                if (null != _boundTo && null != _hostType)
                {
                    BindMethods(binding, _hostType, _boundTo);
                    BindProperties(binding, _hostType, _boundTo);
                    BindFields(binding, _hostType, _boundTo);
                }

                // Set custom binding values
                if (null != _values)
                {
                    for (int i = 0; i < _values.Count; ++i)
                    {
                        var valueDescriptor = _values[i];
                        binding.SetValue(valueDescriptor.Name, valueDescriptor.Value, valueDescriptor.ValueType);
                    }
                }

                return binding;
            });
        }

        /// <summary>
        /// Adds function bindings for all methods in the <see cref="IHostType"/>.
        /// </summary>
        private void BindMethods(JsBinding binding, IHostType hostType, object instance)
        {
            // TODO: Seems like we can hoist this into JsInterop, which would allow us refactor
            // TODO: out the builder class completely.
            var methods = hostType.MethodNames;
            for (int i = 0; i < methods.Count; ++i)
            {
                var methodName = methods[i];

                binding.AddFunction(
                    methodName,
                    (v, s, args, argLength, data) =>
                    {
                        var totalParameters = argLength - 1;
                        var hostMethods = hostType.MethodsFor(methodName, totalParameters);
                        if (hostMethods.Count == 0)
                        {
                            var message = $"Calling host function that does not exist: [Method: {methodName}, Instance: {instance}]";
                            JsErrorHelper.SetJsException(message);
                            return JavaScriptValue.Invalid;
                        }

                        var hostMethodInfo = FindBestMethod(hostMethods, args, argLength);
                        if (null == hostMethodInfo)
                        {
                            LogMethodSelectionFailure(hostMethods, args, argLength);
                            JsErrorHelper.SetJsException(
                                $"Calling host function that does not exist: [Method: {methodName}, Instance: {instance}]");
                            return JavaScriptValue.Invalid;
                        }

                        try
                        {
                            var realParams = ToParameters(hostMethodInfo, args, argLength);

                            var result = hostMethodInfo.Method.Invoke(instance, realParams);
                            var resultType = hostMethodInfo.ReturnType;
                            if (resultType == typeof(void))
                            {
                                return JavaScriptValue.Invalid;
                            }

                            resultType = JsConversions.TypeFor(result, resultType);
                            return _interop.ToJsObject(result, resultType);
                        }
                        catch (Exception e)
                        {
                            LogMethodInvocationInfo(hostMethodInfo, instance);

                            throw;
                        }
                    });
            }
        }

        /// <summary>
        /// Locates the first compatible method in the list and returns it. If there are no compatible methods,
        /// then <c>null</c> is returned.
        /// </summary>
        private HostMethod FindBestMethod(List<HostMethod> methods, JavaScriptValue[] args, ushort argLength)
        {
            for (int i = 0; i < methods.Count; ++i)
            {
                var method = methods[i];
                if (IsCompatibleMethod(method, args, argLength))
                {
                    return method;
                }
            }

            return null;
        }

        /// <summary>
        /// This method type checks each parameter passed by javascript to determine if the method is a suitable
        /// match for execution. At this point, we can assume that the provided arguments qualify for the method
        /// signature in terms of number of parameters, accounting for optional parameters and var args.
        /// </summary>
        private bool IsCompatibleMethod(HostMethod method, JavaScriptValue[] args, ushort argLength)
        {
            var parameters = method.Parameters;
            var isVarArg = method.IsVarArgs;

            // Parameter Index Pointers - JS parameters include the callee at the first index, so we start after that
            var argIndex = 1;
            var pIndex = 0;

            // The maximum number of iterations we'll have to run to complete type checking each parameter
            // This is typically parameters.Length unless there are optional parameters or var args
            var iterations = Math.Max(parameters.Length, argLength - 1);

            // Loop Max Argument Count
            while (iterations-- > 0)
            {
                // Case 1. We've Type Checked All JS Parameters Against C# Parameters
                if (argIndex >= argLength)
                {
                    // For a Var Arg C# Parameter Method, ensure we've indexed into the var arg index
                    if (isVarArg)
                    {
                        // If we haven't, then we ensure that we have an optional parameter. Otherwise,
                        // we're short JS parameters, so we can't call this method.
                        if (pIndex != method.VarArgIndex)
                        {
                            return parameters[pIndex].IsOptional;
                        }

                        return true;
                    }

                    // For methods without var args, we simply ensure we've typed checked against all required
                    // parameters.
                    if (pIndex < parameters.Length)
                    {
                        return parameters[pIndex].IsOptional;
                    }

                    return true;
                }

                // Case 2. We reach the end of C# Method Parameters, but still have JS Parameters left to check
                if (pIndex >= parameters.Length)
                {
                    // This case is only possible with var args (since the params []) counts as a single index
                    if (!method.IsVarArgs)
                    {
                        return false;
                    }

                    // Ensure that pIndex stays at the var arg index for type checking the element type
                    pIndex = method.VarArgIndex;
                }

                // Case 3. TypeCheck JS Argument Against C# Parameter
                var arg = args[argIndex];
                var parameter = parameters[pIndex];

                // For Var Args, we need to ensure that we use the var arg type to check against the JS arg.
                var paramType = (isVarArg && pIndex >= method.VarArgIndex)
                    ? method.VarArgType
                    : parameter.ParameterType;

                // Increment index pointers
                argIndex++;
                pIndex++;

                // Run type conversion checking, early return on failure
                if (!JsConversions.IsAssignable(arg, _binder, paramType))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Generates a parameter array based on the target method parameters.
        /// </summary>
        private object[] ToParameters(HostMethod method, JavaScriptValue[] args, ushort argCount)
        {
            // Build the parameters up to params, then build params
            if (method.IsVarArgs)
            {
                return ToVarArgParams(method, args, argCount);
            }

            return ToSerialParameters(method, args, argCount);
        }

        /// <summary>
        /// Regular serial parameter passing.
        /// </summary>
        private object[] ToSerialParameters(HostMethod method, JavaScriptValue[] args, ushort argCount)
        {
            var totalParameters = argCount - 1;
            var parameters = method.Parameters;
            var realParams = new object[parameters.Length];

            var argIndex = 1;
            for (int j = 0; j < parameters.Length; ++j)
            {
                var param = parameters[j];

                // If we run out of JS parameters passed in compared to the total number of host parameters,
                // we have either picked the wrong method or the host method has optional parameters with defaults.
                if (argIndex >= argCount)
                {
                    if (!param.IsOptional)
                    {
                        Log.Warning(this, "Interop chose the wrong method to execute. Too few JS parameters, no optional C# parameters.");
                        break;
                    }

                    realParams[j] = param.DefaultValue;
                }
                else
                {
                    var arg = args[argIndex++];
                    realParams[j] = _interop.ToHostObject(arg, param.ParameterType);
                }
            }

            return realParams;
        }

        /// <summary>
        /// Handles the scenario with variable arguments in the host method via <c>params</c>.
        /// </summary>
        private object[] ToVarArgParams(HostMethod method, JavaScriptValue[] args, ushort argCount)
        {
            if (!method.IsVarArgs)
            {
                throw new Exception("Not a variable argument parameter set");
            }

            var totalParameters = argCount - 1;
            var parameters = method.Parameters;
            var realParams = new object[parameters.Length];

            var vaIndex = method.VarArgIndex;
            var vaType = method.VarArgType;

            // Non VarArg Parameters
            var argIndex = 1;
            var paramIndex = 0;
            for (int j = 0; j < vaIndex; ++j)
            {
                var param = parameters[j];

                // If we run out of JS parameters passed in compared to the total number of host parameters,
                // we have either picked the wrong method or the host method has optional parameters with defaults.
                if (argIndex >= argCount)
                {
                    if (!param.IsOptional)
                    {
                        Log.Warning(this, "Interop chose the wrong method to execute. Too few JS parameters, no optional C# parameters.");
                        break;
                    }

                    realParams[paramIndex++] = param.DefaultValue;
                }
                else
                {
                    var arg = args[argIndex++];
                    realParams[paramIndex++] = _interop.ToHostObject(arg, param.ParameterType);
                }
            }

            // Determine if we have enough JS parameters to put into var args
            if (vaIndex > totalParameters)
            {
                realParams[paramIndex++] = Array.CreateInstance(vaType, 0);

                return realParams;
            }

            // Put remaining JS args into var args
            var remainingLength = totalParameters - vaIndex;
            var paramsArray = Array.CreateInstance(vaType, remainingLength);

            for (var j = 0; j < remainingLength; ++j)
            {
                var arg = args[argIndex++];
                paramsArray.SetValue(_interop.ToHostObject(arg, vaType), j);
            }

            // Set last parameter to var arg array
            realParams[paramIndex++] = paramsArray;

            return realParams;
        }

        /// <summary>
        /// Binds all public properties for the instance.
        /// </summary>
        private void BindProperties(JsBinding binding, IHostType hostType, object instance)
        {
            // TODO: Seems like we can hoist this into JsInterop, which would allow us refactor
            // TODO: out the builder class completely.
            var properties = hostType.PropertyNames;
            for (int i = 0; i < properties.Count; ++i)
            {
                var propertyName = properties[i];

                binding.AddProperty(
                    propertyName,
                    (v, s, args, argLength, data) =>
                    {
                        var get = hostType.PropertyFor(propertyName).Getter;
                        var result = get.Invoke(instance, EmptyParameters);

                        var returnType = JsConversions.TypeFor(result, get.ReturnType);
                        return _interop.ToJsObject(result, returnType);
                    },
                    (v, s, args, argLength, data) =>
                    {
                        var hostProperty = hostType.PropertyFor(propertyName);
                        var propType = hostProperty.PropertyType;
                        var set = hostProperty.Setter;

                        var value = _interop.ToHostObject(args[1], propType);
                        set.Invoke(instance, new[] { value });
                        return JavaScriptValue.Invalid;
                    });
            }
        }

        /// <summary>
        /// Binds all the fields for the instance.
        /// </summary>
        private void BindFields(JsBinding binding, IHostType hostType, object instance)
        {
            // TODO: Seems like we can hoist this into JsInterop, which would allow us refactor
            // TODO: out the builder class completely.
            var fields = hostType.FieldNames;
            for (int i = 0; i < fields.Count; ++i)
            {
                var fieldName = fields[i];

                binding.AddProperty(
                    fieldName,
                    (v, s, args, argLength, data) =>
                    {
                        var fieldInfo = hostType.FieldFor(fieldName).Field;
                        var result = fieldInfo.GetValue(instance);

                        var returnType = JsConversions.TypeFor(result, fieldInfo.FieldType);
                        return _interop.ToJsObject(result, returnType);
                    },
                    (v, s, args, argLength, data) =>
                    {
                        var fieldInfo = hostType.FieldFor(fieldName).Field;
                        var fieldType = fieldInfo.FieldType;

                        var value = _interop.ToHostObject(args[1], fieldType);
                        fieldInfo.SetValue(instance, value);

                        return JavaScriptValue.Invalid;
                    });
            }
        }

        /// <summary>
        /// Logs method invocation information
        /// </summary>
        private void LogMethodInvocationInfo(HostMethod method, object instance)
        {
            var parameters = method.Parameters;

            var str = new StringBuilder();
            str.Append($"Exception Invoking: [Method: {method.Method.Name}]");
            str.Append("\n[Parameters]\n");
            for (var ii = 0; ii < parameters.Length; ++ii)
            {
                var p = parameters[ii];
                str.Append($"  [Name: {p.Name}, Type: {p.ParameterType}, Optional: {p.IsOptional}");

                if (p.IsOptional)
                {
                    str.Append($", Default: {p.DefaultValue}");
                }

                str.Append("]\n");
            }

            str.Append("[Object Instance: ").Append(instance).Append("]");

            Log.Info(this, str.ToString());
        }

        private void LogMethodSelectionFailure(List<HostMethod> methods, JavaScriptValue[] args, ushort argsLength)
        {
            var str = new StringBuilder();
            str.Append("Failed to find function for method invocation.\n");
            str.Append("[JavaScript Parameters]:      (");
            for (var i = 1; i < argsLength; ++i)
            {
                var arg = args[i];
                str.Append(arg.ValueType);
                if (i != argsLength - 1)
                {
                    str.Append(", ");
                }
            }

            str.Append(")\n");
            for (int i = 0; i < methods.Count; ++i)
            {
                str.Append("[Possible Method Parameters]: (");
                var method = methods[i];
                var parameters = method.Parameters;
                var isVarArg = method.IsVarArgs;

                for (int j = 0; j < parameters.Length; ++j)
                {
                    var p = parameters[j];
                    if (isVarArg && j == method.VarArgIndex)
                    {
                        str.Append("params ");
                    }
                    str.Append($"{p.ParameterType.Name} {p.Name}");
                    if (p.IsOptional)
                    {
                        str.Append($" = {p.DefaultValue}");
                    }

                    if (j != parameters.Length - 1)
                    {
                        str.Append(", ");
                    }
                }

                str.Append(")\n");
            }

            Log.Info(this, str.ToString());
        }
    }
}