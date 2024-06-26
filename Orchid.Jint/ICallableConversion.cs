using Jint.Native;
using System;

namespace Enklu.Orchid.Jint
{
  /// <summary>
  /// This interface defines an implementation prototype of an object capable of converting a
  /// <see cref="ICallable"/> into an alternative type required by the host.
  /// </summary>
  public interface ICallableConversion
  {
    /// <summary>
    /// Converts a Jint <see cref="ICallable"/> into a object usually defined by type key.
    /// </summary>
    object Convert(Func<JsValue, JsValue[], JsValue> callable);
  }
}