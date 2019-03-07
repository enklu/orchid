using System;
using System.Collections;
using System.Collections.Generic;

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
                        var hostMethodInfo = hostType.MethodFor(methodName, totalParameters);
                        if (null == hostMethodInfo)
                        {
                            return JavaScriptValue.Invalid;
                        }

                        var parameters = hostMethodInfo.Parameters;
                        var realParams = new object[parameters.Length];
                        var argIndex = 1;
                        for (int j = 0; j < parameters.Length; ++j)
                        {
                            var arg = args[argIndex++];
                            var param = parameters[j];

                            realParams[j] = _interop.ToHostObject(arg, param.ParameterType);
                        }

                        var result = hostMethodInfo.Method.Invoke(instance, realParams);
                        var resultType = hostMethodInfo.ReturnType;
                        if (resultType == typeof(void))
                        {
                            return JavaScriptValue.Invalid;
                        }

                        return _interop.ToJsObject(result, resultType);
                    });
            }
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
                        var returnType = get.ReturnType;
                        var result = get.Invoke(instance, EmptyParameters);

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
                        var returnType = fieldInfo.FieldType;
                        var result = fieldInfo.GetValue(instance);

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
    }
}