using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Enklu.Orchid.Chakra.Interop
{
    /// <summary>
    /// This class is used to to convert types between JavaScript and C#. While most types are
    /// one to one, there are a few
    /// An interop helper utility for converting host types into javascript types.
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
        /// The javascript context to use for conversions.
        /// </summary>
        private readonly JsContext _context;

        /// <summary>
        /// The javascript binder used to create and lookup bound host objects.
        /// </summary>
        private readonly JsBinder _binder;

        /// <summary>
        /// Creates a new <see cref="JsInterop"/> instance.
        /// </summary>
        /// <param name="context"></param>
        public JsInterop(JsContext context, JsBinder binder)
        {
            _context = context;
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
                throw new Exception("Could not find host bound object for javascript value. Any JS Object types passed to the host must be created on the host");
            }

            return boundObject;
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
            if (toType != typeof(bool))
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
            if (toType == typeof(byte))
            {
                return (byte)arg.ToInt32();
            }

            if (toType == typeof(short))
            {
                return (short)arg.ToInt32();
            }
            if (toType == typeof(int))
            {
                return arg.ToInt32();
            }

            if (toType == typeof(float))
            {
                return (float)arg.ToDouble();
            }

            if (toType == typeof(double))
            {
                return arg.ToDouble();
            }

            if (toType == typeof(decimal))
            {
                return (decimal)arg.ToDouble();
            }

            if (toType == typeof(string))
            {
                return arg.ConvertToString().ToString();
            }

            if (toType == typeof(bool))
            {
                return arg.ConvertToBoolean().ToBoolean();
            }

            throw new Exception($"Cannot convert javascript number to type: {toType}");
        }

        /// <summary>
        /// Converts a <see cref="JavaScriptValue"/> with a string type to a host value.
        /// </summary>
        private object ToHostString(JavaScriptValue arg, Type toType)
        {
            if (typeof(string) != toType)
            {
                throw new Exception($"Cannot convert javascript string to type: {toType}");
            }

            return arg.ToString();
        }

        /// <summary>
        /// Converts a <see cref="JavaScriptValue"/> with a function type to a host value.
        /// </summary>
        /// <remarks>
        /// This currently handles well defined delegates (with Invoke). For instance:
        /// <see cref="Action{T}"/> and <see cref="Func{TResult}"/> work appropriately.
        /// Some of the parameter handling may need some work (ie: <c>params T[] rest</c>).
        /// The <see cref="Delegate"/> type is also not supported currently due to the
        /// <see cref="Delegate.DynamicInvoke"/> method. Since DynamicInvoke and varargs both
        /// require some more Expression building, we should aim to support this soon.
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
        ///           .Call .JsConverter.JsConvert((object)$arg1_0, .Constant[Type](int)),
        ///           .Call .JsConverter.JsConvert($arg2_1, .Constant[Type](string))
        ///         });
        ///       (float) .Call JsConverter.Convert($returnValue, .Constant[Type](float))
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
        public object ToHostFunction(JavaScriptValue arg, Type toType)
        {
            if (!typeof(MulticastDelegate).IsAssignableFrom(toType))
            {
                throw new Exception($"Cannot convert javascript Function to type: {toType}");
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
            var messageProp = Expression.Property(exceptionParam, "Message");

            // JavaScriptContext.SetException(JavaScriptValue.FromString(e.Message));
            var message = Expression.Call(JsValueFromString, messageProp);
            var fromString = Expression.Call(JsValueCreateError, message);
            var setJsException = Expression.Call(JsContextSetException, fromString);

            var assignAndBody = Expression.Block(new[] { jsReturn },
                Expression.TryCatchFinally(callAndConvert,
                    Expression.Call(jsFunc, JsValueReleaseInfo),
                    ExpressionHelper.CatchBlock(exceptionParam, setJsException, returnType)));

            return Expression.Lambda(toType, assignAndBody, parameterExpressions).Compile();
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
                var convertedParamExpr = paramExpr.Type.IsValueType
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
            if (IsVoidType(type))
            {
                return ToJsVoid(obj, type);
            }

            if (IsNumberType(type))
            {
                return ToJsNumber(obj, type);
            }

            if (IsStringType(type))
            {
                return ToJsString(obj, type);
            }

            if (IsBooleanType(type))
            {
                return ToJsBoolean(obj, type);
            }

            if (IsFunctionType(type))
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
            return JavaScriptValue.FromString((string)obj);
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

            if (type == typeof(short))
            {
                return JavaScriptValue.FromInt32((short)obj);
            }

            if (type == typeof(int))
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

                return ToJsObject(result, resultType);
            };

            return _binder.BindFunction(func);
        }

        /// <summary>
        /// Creates a <see cref="JavaScriptValue"/> representing the host object
        /// </summary>
        /// <remarks>This call requires an active context.</remarks>
        public JavaScriptValue ToBoundJsObject(object obj, Type type)
        {
            var binding = NewBuilder()
                .BoundTo(obj, type)
                .Build();

            return binding.Object;
        }

        /// <summary>
        /// New JS Binding Builder
        /// </summary>
        private JsBindingBuilder NewBuilder() => new JsBindingBuilder(_context, _binder, this);

        /// <summary>
        /// Determines if the host type is void.
        /// </summary>
        private static bool IsVoidType(Type type) => type == typeof(void);

        /// <summary>
        /// Determines if the host type is bool.
        /// </summary>
        private static bool IsBooleanType(Type type) => type == typeof(bool);

        /// <summary>
        /// Determines if the host type is string.
        /// </summary>
        private static bool IsStringType(Type type) => type == typeof(string);

        /// <summary>
        /// Determines whether the host type is a function.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        private static bool IsFunctionType(Type type) => typeof(MulticastDelegate).IsAssignableFrom(type);

        /// <summary>
        /// Returns <c>true</c> if the <see cref="Type"/> is convertible to a javascript Number.
        /// </summary>
        private static bool IsNumberType(Type type)
        {
            return type == typeof(byte)
                || type == typeof(short)
                || type == typeof(int)
                || type == typeof(decimal)
                || type == typeof(float)
                || type == typeof(double)
                || type == typeof(long);
        }

    }
}