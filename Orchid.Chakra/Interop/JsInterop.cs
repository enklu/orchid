using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Enklu.Orchid.Chakra.Interop
{
    /// <summary>
    /// This class is used to to convert types between JavaScript and C#. Most conversions are
    /// deterministic, but dynamic objects and functions take a bit more work to create. The types
    /// used with this utility normally require the backing type to exist in C# before it can exist
    /// in JS and be passed back out to C#.
    /// </summary>
    public class JsInterop
    {
        /// <summary>
        /// The <see cref="ToJsObject"/> method info used for parameter expression generation.
        /// </summary>
        private static MethodInfo JsConvertMethodInfo = typeof(JsInterop).GetMethod("ToJsObject");

        /// <summary>
        /// The <see cref="ToHostObject"/> method info used for result expression generation.
        /// </summary>
        private static MethodInfo ConvertMethodInfo = typeof(JsInterop).GetMethod("ToHostObject");

        /// <summary>
        /// The <see cref="JavaScriptValue.CallFunction"/> method info for converting parameter expressions.
        /// </summary>
        private static MethodInfo JsValueCallFunctionInfo = typeof(JavaScriptValue).GetMethod("CallFunction");

        /// <summary>
        /// The method info for extracting meaningful exception messages from bubbling JS or host exceptions.
        /// </summary>
        private static MethodInfo JsExtractErrorInfo = typeof(JsErrorHelper)
            .GetMethod("ExtractErrorMessage", BindingFlags.Static | BindingFlags.Public);

        /// <summary>
        /// The <see cref="JavaScriptValue.Release"/> method info for releasing a reference to a JS function after it's
        /// been called.
        /// </summary>
        private static MethodInfo JsValueReleaseInfo = typeof(JavaScriptValue).GetMethod("Release");

        /// <summary>
        /// JavaScriptValue.FromString()
        /// </summary>
        private static MethodInfo JsValueFromString = typeof(JavaScriptValue).GetMethod("FromString");

        /// <summary>
        /// JavaScriptValue.CreateError(JavaScriptValue);
        /// </summary>
        private static MethodInfo JsValueCreateError = typeof(JavaScriptValue).GetMethod("CreateError");

        /// <summary>
        /// JavaScriptContext.SetException(JavaScriptValue)
        /// </summary>
        private static MethodInfo JsContextSetException = typeof(JavaScriptContext).GetMethod("SetException");

        /// <summary>
        /// Expression representing <see cref="JavaScriptValue.Undefined"/>.
        /// </summary>
        private static Expression JsValueUndefinedExpression = Expression.Constant(JavaScriptValue.Undefined, typeof(JavaScriptValue));

        /// <summary>
        /// The execution context the interop scope is part of.
        /// </summary>
        private readonly JsExecutionContext _context;

        /// <summary>
        /// The javascript context to use for conversions.
        /// </summary>
        private readonly JsContextScope _scope;

        /// <summary>
        /// The javascript binder used to create and lookup bound host objects.
        /// </summary>
        private readonly JsBinder _binder;

        /// <summary>
        /// Mapping from JS functions to host delegates.
        /// </summary>
        private readonly Dictionary<IntPtr, Delegate> _delegateCache = new Dictionary<IntPtr, Delegate>();

        /// <summary>
        /// Cache containing mappings to orchid core callbacks.
        /// </summary>
        private readonly Dictionary<IntPtr, IJsCallback> _callbackCache = new Dictionary<IntPtr, IJsCallback>();

        /// <summary>
        /// Creates a new <see cref="JsInterop"/> instance.
        /// </summary>
        /// <param name="scope"></param>
        public JsInterop(JsExecutionContext context, JsContextScope scope, JsBinder binder)
        {
            _context = context;
            _scope = scope;
            _binder = binder;
        }

        /// <summary>
        /// Converts a <see cref="JavaScriptValue"/> into a host object of type <see cref="toType"/>
        /// </summary>
        /// <param name="arg">The <see cref="JavaScriptValue"/> to convert.</param>
        /// <param name="toType">The <see cref="Type"/> to convert to.</param>
        public object ToHostObject(JavaScriptValue arg, Type toType)
        {
            // TODO: Could probably turn the converters into a contract and map design
            switch (arg.ValueType)
            {
                case JavaScriptValueType.Undefined: return null;
                case JavaScriptValueType.Null: return null;
                case JavaScriptValueType.Array: return ToHostArray(arg, toType);
                case JavaScriptValueType.TypedArray: return ToHostArray(arg, toType);
                case JavaScriptValueType.Boolean: return ToHostBoolean(arg, toType);
                case JavaScriptValueType.Number: return ToHostNumber(arg, toType);
                case JavaScriptValueType.String: return ToHostString(arg, toType);
                case JavaScriptValueType.Function: return ToHostFunction(arg, toType);
                case JavaScriptValueType.Object: return ToHostBoundObject(arg, toType);
            }

            throw new Exception($"Cannot handle JS value type: {arg.ValueType}");
        }

        /// <summary>
        /// Converts a <see cref="JavaScriptValue"/> object into a host object.
        /// </summary>
        public object ToHostBoundObject(JavaScriptValue arg, Type toType)
        {
            var boundObject = _binder.ObjectLinkedTo(arg);
            if (null == boundObject)
            {
                // Object was constructed in JS and has no host object mapping, so we create one
                boundObject = NewHostObject(arg);
            }

            return boundObject;
        }

        /// <summary>
        /// For object constructed in JS, we need to create a C# representation and bind it to that
        /// object. For this, we'll use a <see cref="Dictionary{TKey,TValue}"/>. Possibly a later addition
        /// could use a custom serialization process and allow the user to control.
        /// </summary>
        private Dictionary<string, object> NewHostObject(JavaScriptValue arg)
        {
            // Create new Dictionary mapping to hold the properties, Create a new bindable JS object to replace
            // the existing one. Note: This could cause issues if these objects are used as keys.
            var d = new Dictionary<string, object>();
            var replacement = _binder.BindObject(d);

            var propNames = (string[]) ToHostArray(arg.GetOwnPropertyNames(), typeof(string[]));
            for (var i = 0; i < propNames.Length; ++i)
            {
                var propName = propNames[i];
                var propId = JavaScriptPropertyId.FromString(propName);
                var jsProp = arg.GetProperty(propId);

                // Copy Properties into Replacement
                replacement.SetProperty(propId, jsProp, true);

                Type propType;
                if (!TryInferType(jsProp, out propType))
                {
                    throw new Exception($"Failed to create Host representation of JS object. Property: {propName}");
                }

                d[propName] = ToHostObject(jsProp, propType);
            }

            return d;
        }

        /// <summary>
        /// Converts a <see cref="JavaScriptValue"/> array into a host array.
        /// </summary>
        public object ToHostArray(JavaScriptValue arg, Type toType)
        {
            if (!toType.IsArray)
            {
                throw new Exception($"Cannot convert javascript array to type: {toType}");
            }

            var elementType = toType.GetElementType();
            var length = arg.GetProperty(JavaScriptPropertyId.FromString("length")).ToInt32();
            var resultArray = Array.CreateInstance(elementType, length);
            for (int i = 0; i < length; ++i)
            {
                var atIndex = arg.GetIndexedProperty(JavaScriptValue.FromInt32(i));
                resultArray.SetValue(ToHostObject(atIndex, elementType), i);
            }

            return resultArray;
        }

        /// <summary>
        /// Converts a <see cref="JavaScriptValue"/> with a bool type to a host value.
        /// </summary>
        public object ToHostBoolean(JavaScriptValue arg, Type toType)
        {
            if (!toType.IsAssignableFrom(typeof(bool)))
            {
                throw new Exception($"Cannot convert javascript boolean to type: {toType}");
            }

            return arg.ToBoolean();
        }

        /// <summary>
        /// Converts a <see cref="JavaScriptValue"/> with a number type to host value.
        /// </summary>
        public object ToHostNumber(JavaScriptValue arg, Type toType)
        {
            // 32-bit Conversions
            if (toType == typeof(byte))
            {
                return (byte) arg.ToInt32();
            }
            if (toType == typeof(sbyte))
            {
                return (sbyte) arg.ToInt32();
            }
            if (toType == typeof(short))
            {
                return (short) arg.ToInt32();
            }
            if (toType == typeof(ushort))
            {
                return (ushort) arg.ToInt32();
            }
            if (toType == typeof(int))
            {
                return arg.ToInt32();
            }
            if (toType == typeof(uint))
            {
                return (uint) arg.ToInt32();
            }

            // 64-bit Conversions
            if (toType == typeof(long))
            {
                return (long)arg.ToDouble();
            }
            if (toType == typeof(ulong))
            {
                return (ulong)arg.ToDouble();
            }
            if (toType == typeof(float))
            {
                return (float) arg.ToDouble();
            }
            if (toType == typeof(double))
            {
                return arg.ToDouble();
            }
            if (toType == typeof(decimal))
            {
                return (decimal)arg.ToDouble();
            }

            // Other Conversions
            if (toType == typeof(string))
            {
                return arg.ConvertToString().ToString();
            }
            if (toType == typeof(bool))
            {
                return arg.ConvertToBoolean().ToBoolean();
            }

            // Last Attempt
            if (toType.IsAssignableFrom(typeof(double)))
            {
                return arg.ToDouble();
            }

            throw new Exception($"Cannot convert javascript number to type: {toType}");
        }

        /// <summary>
        /// Converts a <see cref="JavaScriptValue"/> with a string type to a host value.
        /// </summary>
        private object ToHostString(JavaScriptValue arg, Type toType)
        {
            if (!toType.IsAssignableFrom(typeof(string)))
            {
                throw new Exception($"Cannot convert javascript string to type: {toType}");
            }

            return arg.ToString();
        }

        /// <summary>
        /// Converts a <see cref="JavaScriptValue"/> with a function type to a host value.
        /// </summary>
        public object ToHostFunction(JavaScriptValue arg, Type toType)
        {
            // Orchid IJsCallback
            if (typeof(IJsCallback).IsAssignableFrom(toType))
            {
                return ToJsCallback(arg);
            }

            // Action/Func
            if (typeof(MulticastDelegate).IsAssignableFrom(toType))
            {
                return ToMulticastDelegate(arg, toType);
            }

            throw new Exception("Unable to convert javascript function to delegate/callback type: " + toType);
        }

        /// <summary>
        /// Creates and caches a multicast delegate wrapper which forwards parameters into the JS
        /// callback.
        /// </summary>
        /// <remarks>
        /// This currently handles well defined delegates (with Invoke). For instance:
        /// <see cref="Action{T}"/> and <see cref="Func{TResult}"/> work appropriately.
        /// Some of the parameter handling may need some work (ie: <c>params T[] rest</c>).
        ///
        /// This conversion occurs when executing javascript code needs to pass a callback to
        /// C#. Instead of forcing implementers to use <see cref="JavaScriptNativeFunction"/>,
        /// we dynamically build the callback methods using the <see cref="Expression"/> utility
        /// class. Assuming a callback of type <see cref="Func{int, string, float}"/>, the following
        /// will build an expression tree similar to the following:
        /// <example>
        /// <code>
        /// .Block(JavaScriptValue $returnValue)
        /// {
        ///   .Try
        ///   {
        ///     .Block()
        ///     {
        ///       $returnValue = .Call .Constant[JavaScriptValue](JavaScriptValue).CallFunction(
        ///         .NewArray JavaScriptValue[]
        ///         {
        ///           .Constant[JavaScriptValue](JavaScriptValue),
        ///           .Call .JsInterop.ToJsObject((object)$arg1_0, .Constant[Type](int)),
        ///           .Call .JsInterop.ToJsObject($arg2_1, .Constant[Type](string))
        ///         });
        ///       (float) .Call JsInterop.ToJsObject($returnValue, .Constant[Type](float))
        ///     }
        ///   }
        ///   .Catch (Exception $e)
        ///   {
        ///     .Block()
        ///     {
        ///       .Call JavaScriptContext.SetException(.Call JavaScriptValue.CreateError(.Call JavaScriptValue.FromString($e.Message)));
        ///       .Default(float)
        ///     }
        ///   }
        ///   .Finally
        ///   {
        ///     .Call .Constant[.JavaScriptValue](.JavaScriptValue).Release()
        ///   }
        /// }
        /// </code>
        /// </example>
        /// </remarks>
        private object ToMulticastDelegate(JavaScriptValue arg, Type toType)
        {
            // Get the JS function reference pointer, and determine if we've adapted this function to a host delegate before.
            // If so, return the cached expression. This also ensures that the delegate passed out stays consistent.
            var fnReference = arg.Reference;
            if (_delegateCache.ContainsKey(fnReference))
            {
                return _delegateCache[fnReference];
            }

            var method = toType.GetMethod("Invoke");
            if (null == method)
            {
                throw new Exception($"Delegate/Function type must contain an Invoke method.");
            }

            var parameters = method.GetParameters();
            var funcLength = arg.GetProperty(JavaScriptPropertyId.FromString("length")).ToInt32();
            if (parameters.Length != funcLength)
            {
                throw new Exception($"Host function parameters: {parameters.Length} does not match JS parameters: {funcLength}");
            }

            // Target Return Type
            var returnType = method.ReturnType;

            // Increase reference count to ensure function doesn't get GC'd
            // Expression Body will Release() the reference after calling.
            arg.AddRef();

            // Create Parameter Expressions for Invoke Parameters, then wrap in conversion expressions
            var parameterExpressions = ExpressionHelper.ToParameterExpressions(parameters);
            var convertedParameters = ParametersToJsValueParameters(parameterExpressions);

            // Define the constant expression for the JS Func argument
            var jsFunc = Expression.Constant(arg, typeof(JavaScriptValue));
            var paramsArray = Expression.NewArrayInit(typeof(JavaScriptValue), convertedParameters);

            // Define a JavaScriptValue return value and assign it to the result of calling the JS Function
            var jsReturn = Expression.Variable(typeof(JavaScriptValue), "returnValue");
            var assignJsReturn = Expression.Assign(jsReturn, Expression.Call(jsFunc, JsValueCallFunctionInfo, paramsArray));

            // Call the ToHostObject() method passing in the return value and return type
            // Then, cast the resulting object to the return type
            var convertCall = Expression.Call(Expression.Constant(this),
                ConvertMethodInfo,
                jsReturn,
                Expression.Constant(returnType));
            var conversionExpr = ExpressionHelper.ConvertExpression(convertCall, returnType);

            // Block Expression setting JavaScriptValue variable, then converting to correct host type
            var callAndConvert = Expression.Block(assignJsReturn, conversionExpr);

            // -------------------------------------------------------------------------------

            // Wrap the entire call in a try/catch/finally where we execute the callAndConvert
            // in the try, the function release in the finally, and handle Js Exceptions in Catch

            // Setup Exception.Message extraction
            var exceptionParam = Expression.Parameter(typeof(Exception), "e");
            var messageProp = Expression.Call(JsExtractErrorInfo, exceptionParam);

            // JavaScriptContext.SetException(JavaScriptValue.FromString(e.Message));
            var message = Expression.Call(JsValueFromString, messageProp);
            var fromString = Expression.Call(JsValueCreateError, message);
            var setJsException = Expression.Call(JsContextSetException, fromString);

            var assignAndBody = Expression.Block(new[] { jsReturn },
                Expression.TryCatch(callAndConvert,
                    ExpressionHelper.CatchBlock(exceptionParam, setJsException, returnType)));

            var hostFn = Expression.Lambda(toType, assignAndBody, parameterExpressions).Compile();

            // Add to Delegate Cache for the reference
            // TODO: Look into hooking this into the GC management in JsBinder
            _delegateCache[arg.Reference] = hostFn;

            return hostFn;
        }

        /// <summary>
        /// Wraps the JS function in a callable wrapper object that implements the Orchid core callback interface.
        /// </summary>
        private IJsCallback ToJsCallback(JavaScriptValue arg)
        {
            if (_callbackCache.ContainsKey(arg.Reference))
            {
                return _callbackCache[arg.Reference];
            }

            var jsCallback = new JsCallback(_context, _scope, this, arg);
            _callbackCache[arg.Reference] = jsCallback;
            return jsCallback;
        }

        /// <summary>
        /// When we call a JS function, the parameters from the host delegate must be converted into <see cref="JavaScriptValue"/>
        /// parameters and prepended by the "callee" which we treat as undefined.
        /// </summary>
        private Expression[] ParametersToJsValueParameters(ParameterExpression[] parameters)
        {
            // Callee + Parameters Array
            var convertedParameters = new Expression[parameters.Length + 1];

            // Set Callee
            var index = 0;
            convertedParameters[index++] = JsValueUndefinedExpression;

            // Sanitize Parameters for CallFunction
            for (int i = 0; i < parameters.Length; ++i)
            {
                var paramExpr = parameters[i];

                // Box value types
                var convertedParamExpr = paramExpr.Type.GetTypeInfo().IsValueType
                    ? (Expression)Expression.Convert(paramExpr, typeof(object))
                    : paramExpr;

                // Call ToJsObject on parameters
                convertedParameters[index++] = Expression.Call(
                    Expression.Constant(this),
                    JsConvertMethodInfo,
                    convertedParamExpr,
                    Expression.Constant(paramExpr.Type, typeof(Type)));
            }

            return convertedParameters;
        }

        /// <summary>
        /// Converts a host object to a javascript value.
        /// </summary>
        public JavaScriptValue ToJsObject(object obj, Type type)
        {
            if (null == obj)
            {
                return JavaScriptValue.Null;
            }

            if (JsConversions.IsVoidType(type))
            {
                return ToJsVoid(obj, type);
            }

            if (JsConversions.IsNumberType(type))
            {
                return ToJsNumber(obj, type);
            }

            if (JsConversions.IsStringType(type))
            {
                return ToJsString(obj, type);
            }

            if (JsConversions.IsBoolType(type))
            {
                return ToJsBoolean(obj, type);
            }

            if (JsConversions.IsFunctionType(type))
            {
                return ToJsFunction(obj, type);
            }

            if (type.IsArray)
            {
                var underlyingType = type.GetElementType();
                var hostArray = (Array)obj;
                var newArr = JavaScriptValue.CreateArray((uint)hostArray.Length);

                for (int i = 0; i < hostArray.Length; ++i)
                {
                    var jsValue = ToJsObject(hostArray.GetValue(i), underlyingType);
                    newArr.SetIndexedProperty(JavaScriptValue.FromInt32(i), jsValue);
                }

                return newArr;
            }

            // Attempt to bind the object and return it
            return ToBoundJsObject(obj, type);
        }

        /// <summary>
        /// Creates a <see cref="JavaScriptValue"/> representing the void host type.
        /// </summary>
        /// <remarks>This call requires an active context.</remarks>
        public JavaScriptValue ToJsVoid(object obj, Type type)
        {
            return JavaScriptValue.Invalid;
        }

        /// <summary>
        /// Creates a <see cref="JavaScriptValue"/> representing the string host type.
        /// </summary>
        /// <remarks>This call requires an active context.</remarks>
        public JavaScriptValue ToJsString(object obj, Type type)
        {
            return JavaScriptValue.FromString(obj.ToString());
        }

        /// <summary>
        /// Creates a <see cref="JavaScriptValue"/> representing the bool host type.
        /// </summary>
        /// <remarks>This call requires an active context.</remarks>
        public JavaScriptValue ToJsBoolean(object obj, Type type)
        {
            return JavaScriptValue.FromBoolean((bool)obj);
        }

        /// <summary>
        /// Creates a <see cref="JavaScriptValue"/> representing the specific number host type.
        /// </summary>
        /// <remarks>This call requires an active context.</remarks>
        public JavaScriptValue ToJsNumber(object obj, Type type)
        {
            if (type == typeof(byte))
            {
                return JavaScriptValue.FromInt32((byte)obj);
            }

            if (type == typeof(sbyte))
            {
                return JavaScriptValue.FromInt32((sbyte) obj);
            }

            if (type == typeof(short))
            {
                return JavaScriptValue.FromInt32((short)obj);
            }
            if (type == typeof(ushort))
            {
                return JavaScriptValue.FromInt32((ushort)obj);
            }

            if (type == typeof(int) || type == typeof(uint))
            {
                return JavaScriptValue.FromInt32((int)obj);
            }

            if (type == typeof(decimal))
            {
                return JavaScriptValue.FromDouble((double)(decimal)obj);
            }

            if (type == typeof(float))
            {
                return JavaScriptValue.FromDouble((float)obj);
            }

            if (type == typeof(double))
            {
                return JavaScriptValue.FromDouble((double)obj);
            }

            if (type == typeof(long))
            {
                return JavaScriptValue.FromDouble((long)obj);
            }

            if (type == typeof(ulong))
            {
                return JavaScriptValue.FromDouble((ulong)obj);
            }

            throw new Exception($"Cannot convert type: {type} into JS Number");
        }

        /// <summary>
        /// Creates a <see cref="JavaScriptValue"/> representing the host delegate.
        /// </summary>
        /// <remarks>This call requires an active context.</remarks>
        public JavaScriptValue ToJsFunction(object obj, Type type)
        {
            var invokeInfo = type.GetMethod("Invoke");
            if (null == invokeInfo)
            {
                throw new Exception($"Cannot convert delegate of type: {type} to JavaScript function.");
            }

            JavaScriptNativeFunction func = (v, s, args, argLength, data) =>
            {
                var parameters = invokeInfo.GetParameters();
                var realParams = new object[parameters.Length];
                var argIndex = 1;
                for (int j = 0; j < parameters.Length; ++j)
                {
                    var arg = args[argIndex++];
                    var param = parameters[j];

                    realParams[j] = ToHostObject(arg, param.ParameterType);
                }

                var result = invokeInfo.Invoke(obj, realParams);
                var resultType = invokeInfo.ReturnType;
                if (resultType == typeof(void))
                {
                    return JavaScriptValue.Invalid;
                }

                return ToJsObject(result, JsConversions.TypeFor(result, resultType));
            };

            return _binder.BindFunction(func);
        }

        /// <summary>
        /// Creates a <see cref="JavaScriptValue"/> representing the host object
        /// </summary>
        /// <remarks>This call requires an active context.</remarks>
        public JavaScriptValue ToBoundJsObject(object obj, Type type)
        {
            // Try and lookup linked JSValue to an existing host object.
            // Failures are represented by Invalid
            var linkedValue = _binder.JsObjectLinkedTo(obj);
            if (linkedValue.IsValid)
            {
                return linkedValue;
            }

            var binding = NewBuilder()
                .BoundTo(obj, type)
                .Build();

            return binding.Object;
        }

        /// <summary>
        /// Attempts to infer the host type based on the javscript type.
        /// </summary>
        public bool TryInferType(JavaScriptValue arg, out Type type)
        {
            // TODO: This method should be used as a last line of effort to try and build a host
            // TODO: type of method call from JS objects. Would love to get rid of it altogether.
            type = null;
            switch (arg.ValueType)
            {
                case JavaScriptValueType.Undefined: return true;
                case JavaScriptValueType.Null: return true;
                case JavaScriptValueType.Array:
                case JavaScriptValueType.TypedArray:
                {
                    var length = arg.GetProperty(JavaScriptPropertyId.FromString("length")).ToInt32();
                    if (length <= 0)
                    {
                        type = typeof(object[]);
                        return true;
                    }

                    Type innerType;
                    var firstElement = arg.GetIndexedProperty(JavaScriptValue.FromInt32(0));
                    if (TryInferType(firstElement, out innerType))
                    {
                        type = innerType.MakeArrayType();
                        return true;
                    }

                    type = typeof(object[]);
                    return true;
                }
                case JavaScriptValueType.Boolean:
                {
                    type = typeof(bool);
                    return true;
                }
                case JavaScriptValueType.Number:
                {
                    type = typeof(double);
                    return true;
                }
                case JavaScriptValueType.String:
                {
                    type = typeof(string);
                    return true;
                }
                case JavaScriptValueType.Function:
                {
                    type = typeof(IJsCallback);
                    return true;
                }
                case JavaScriptValueType.Object:
                {
                    try
                    {
                        var hostObject = ToHostBoundObject(arg, typeof(void));
                        type = hostObject.GetType();
                        return true;
                    }
                    catch
                    {
                        type = typeof(object);
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// New JS Binding Builder
        /// </summary>
        private JsBindingBuilder NewBuilder() => new JsBindingBuilder(_scope, _binder, this);

    }
}