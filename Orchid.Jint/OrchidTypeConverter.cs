using Enklu.Orchid.Jint;
using Jint;
using Jint.Runtime.Interop;
using Jint.Native;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;


namespace Orchid.Jint
{
  internal class OrchidTypeConverter: DefaultTypeConverter
  {
    public OrchidTypeConverter(Engine engine) : base(engine) { }

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
    public override object? Convert(
        object? value, Type type, IFormatProvider formatProvider)
    {
      return null;
    }
  }


}
