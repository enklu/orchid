using Enklu.Orchid.Jint;
using Jint;
using Jint.Runtime.Interop;
using Jint.Native;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Dynamic;

namespace Enklu.Orchid.Jint
{
  internal class OrchidTypeConverter: DefaultTypeConverter
  {
    public OrchidTypeConverter(Engine engine, JsExecutionContext executionContext) : base(engine)
    {
      RegisterDelegateConversion(typeof(IJsCallback), new JsCallbackConversion(executionContext));
    }

    private readonly Dictionary<Delegate, object> _delegateCache = new Dictionary<Delegate, object>();
    private readonly Dictionary<Type, ICallableConversion> _delegateConversions = new Dictionary<Type, ICallableConversion>();

    private static Expression JsUndefExpr = Expression.Constant(JsValue.Undefined, typeof(JsValue));

    /// <summary>
    /// Registers a conversion operation for a specific target type. When the callable must be adapted to this type,
    /// use the provided conversion operation.
    /// </summary>
    /// <param name="targetType">The <see cref="Type"/> of the object Jint is trying to convert the callable to.</param>
    /// <param name="conversion">The conversion implementation to use for the specific target type</param>
    public void RegisterDelegateConversion(Type targetType, ICallableConversion conversion)
    {
      if (_delegateConversions.ContainsKey(targetType))
      {
        return;
      }

      _delegateConversions[targetType] = conversion;
    }

    public override object Convert(
        object value, Type type, IFormatProvider formatProvider)
    {
      if (value != null && !type.IsInstanceOfType(value) && !type.IsEnum)
      {
        var valueType = value.GetType();
        if (valueType == typeof(Func<JsValue, JsValue[], JsValue>))
        {
          var function = (Func<JsValue, JsValue[], JsValue>)value;

          // Check Cache for existing conversion
          if (_delegateCache.ContainsKey(function))
          {
            return _delegateCache[function];
          }

          // Check for registered callable conversion
          if (_delegateConversions.ContainsKey(type))
          {
            var converted = _delegateConversions[type].Convert(function);
            return Cache(function, converted);
          }
        }
        // Some of the clients expect an actual Dictionary<string, object> where
        // parsing JSON gives us an ExpandoObject, which implements IDictionary<string, object>
        if (valueType.Equals(typeof(ExpandoObject)))
        {
          value = new Dictionary<string, object>(value as IDictionary<string, object>);
        }
      }
      return base.Convert(value, type, formatProvider);
    }

    /// <summary>
    /// Caches the wrapper for a specific callable.
    /// </summary>
    private object Cache(Func<JsValue, JsValue[], JsValue> callable, object target)
    {
      _delegateCache[callable] = target;
      return target;
    }
  }


}
