using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Enklu.Orchid.Logging;

namespace Enklu.Orchid.Chakra.Interop
{
    /// <summary>
    /// This static utility class assists with type conversion from JavaScript to C#.
    /// </summary>
    public static class JsConversions
    {
        /// <summary>
        /// Used to build method invocation cache keys
        /// </summary>
        private static StringBuilder _keyBuilder = new StringBuilder();

        /// <summary>
        /// Boolean conversion types
        /// </summary>
        private static readonly ISet<Type> BoolTypes = new HashSet<Type>
        {
            typeof(object),
            typeof(bool)
        };

        /// <summary>
        /// String conversion types
        /// </summary>
        private static readonly ISet<Type> StringTypes = new HashSet<Type>
        {
            typeof(object),
            typeof(char),
            typeof(string)
        };

        /// <summary>
        /// Number conversion types.
        /// </summary>
        private static readonly ISet<Type> NumberTypes = new HashSet<Type>
        {
            typeof(object),
            typeof(sbyte),
            typeof(byte),
            typeof(short),
            typeof(ushort),
            typeof(int),
            typeof(uint),
            typeof(long),
            typeof(ulong),
            typeof(float),
            typeof(double),
            typeof(decimal)
        };

        /// <summary>
        /// Determines if the host type is void.
        /// </summary>
        public static bool IsVoidType(Type type) => type == typeof(void);

        /// <summary>
        /// Determine whether or not the <see cref="Type"/> is a valid JS type for number conversion.
        /// </summary>
        public static bool IsBoolType(Type type) => BoolTypes.Contains(type);

        /// <summary>
        /// Determine whether or not the <see cref="Type"/> is a valid JS type for string conversion.
        /// </summary>
        public static bool IsStringType(Type type) => StringTypes.Contains(type);

        /// <summary>
        /// Determine whether or not the <see cref="Type"/> is a valid JS type for number conversion.
        /// </summary>
        public static bool IsNumberType(Type type) => NumberTypes.Contains(type);

        /// <summary>
        /// Determines whether or not the <see cref="Type"/> is a valid JS type for function conversions.
        /// </summary>
        public static bool IsFunctionType(Type type)
            => typeof(IJsCallback).IsAssignableFrom(type) || typeof(MulticastDelegate).IsAssignableFrom(type);

        /// <summary>
        /// Determines whether or not the <see cref="Type"/> is a valid JS type for array conversions.
        /// </summary>
        private static bool IsArrayType(JavaScriptValue value, JsBinder bindingData, Type type)
        {
            if (!type.IsArray)
            {
                return false;
            }

            // Check length, if empty, we can assume it matches any of the array types
            var length = value.GetProperty(JavaScriptPropertyId.FromString("length")).ToInt32();
            if (length <= 0)
            {
                return true;
            }

            // Look at the first element, type check against the host array element type
            var firstElement = value.GetIndexedProperty(JavaScriptValue.FromInt32(0));
            var elementType = type.GetElementType();

            return IsAssignable(firstElement, bindingData, elementType);
        }

        /// <summary>
        /// This method should be used to perform type checking when the <see cref="JavaScriptValue"/> is of
        /// value type <see cref="JavaScriptValueType.Object"/>. It will use the live binding data to reverse
        /// lookup types bound to JS objects.
        /// </summary>
        private static bool IsObjectAssignable(JavaScriptValue value, JsBinder bindingData, Type toType)
        {
            // Naive Case
            if (typeof(object) == toType)
            {
                return true;
            }

            // Attempt to locate the type from live binding data, determine if assignable
            if (TryGetJsObjectType(value, bindingData, out var objectType))
            {
                return toType.IsAssignableFrom(objectType);
            }

            return false;
        }

        /// <summary>
        /// Attempts to get the <see cref="Type"/> bound to  the JS value as long as the JS value has
        /// a the value type of <see cref="JavaScriptValueType.Object"/>
        /// </summary>
        private static bool TryGetJsObjectType(JavaScriptValue value, JsBinder bindingData, out Type type)
        {
            if (!value.IsValid || value.ValueType != JavaScriptValueType.Object)
            {
                type = typeof(void);
                return false;
            }

            var boundObject = bindingData.ObjectLinkedTo(value);
            if (null == boundObject)
            {
                type = typeof(void);
                return false;
            }

            type = boundObject.GetType();
            return true;
        }

        /// <summary>
        /// Determine if the <see cref="Type"/> provided is suitable to be passed the provided JS value.
        /// </summary>
        public static bool IsAssignable(JavaScriptValue value, JsBinder bindingData, Type toType)
        {
            switch (value.ValueType)
            {
                case JavaScriptValueType.Undefined: return true;
                case JavaScriptValueType.Null: return true;
                case JavaScriptValueType.Boolean: return IsBoolType(toType);
                case JavaScriptValueType.String: return IsStringType(toType);
                case JavaScriptValueType.Number: return IsNumberType(toType);
                case JavaScriptValueType.Function: return IsFunctionType(toType);
                case JavaScriptValueType.Array: return IsArrayType(value, bindingData, toType);
                case JavaScriptValueType.TypedArray: return IsArrayType(value, bindingData, toType);
                case JavaScriptValueType.Object: return IsObjectAssignable(value, bindingData, toType);

                default:
                {
                    Log.Warning(null, "IsAssignable doesn't support the JS Value Type: {0}", value.ValueType);
                    return false;
                }
            }
        }

        /// <summary>
        /// Determines the highest resolution type discernible from the provided data.
        /// </summary>
        public static Type TypeFor(object o, Type hint)
        {
            if (null != o)
            {
                var valueType = o.GetType();
                return valueType.IsAssignableFrom(hint) ? hint : valueType;
            }

            return hint;
        }

        /// <summary>
        /// Creates an invocation key for a specific method name.
        /// </summary>
        public static string ToInvokeKey(string methodName, JavaScriptValue[] args, ushort argCount)
        {
            // NOTE: Navive implementation. Designed to avoid type checking _every_ method call.
            _keyBuilder.Clear();
            _keyBuilder.Append(methodName).Append('_');
            for (int i = 1; i < argCount; ++i)
            {
                _keyBuilder.Append(ToParamKey(args[i].ValueType));
            }

            return _keyBuilder.ToString();
        }

        /// <summary>
        /// Returns a single character key for a JS argument type.
        /// </summary>
        private static char ToParamKey(JavaScriptValueType type)
        {
            switch (type)
            {
                case JavaScriptValueType.Array: return 'A';
                case JavaScriptValueType.TypedArray: return 'A';
                case JavaScriptValueType.Boolean: return 'B';
                case JavaScriptValueType.Function: return 'F';
                case JavaScriptValueType.Object: return 'O';
                case JavaScriptValueType.Null: return 'O';
                case JavaScriptValueType.Undefined: return 'O';
                case JavaScriptValueType.Number: return 'N';
                case JavaScriptValueType.String: return 'S';
                default:
                {
                    return 'O';
                }
            }
        }
    }
}