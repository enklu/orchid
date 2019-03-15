using System;

namespace Enklu.Orchid.Chakra.Interop
{
    /// <summary>
    /// Provides helper utilities for working with Chakra JavaScript function interop.
    /// </summary>
    public static class JsSafeDecorator
    {
        /// <summary>
        /// Creates a safe function wrapper for the native function to ensure we propagate
        /// errors in javascript appropriately.
        /// </summary>
        public static JavaScriptNativeFunction Decorate(JavaScriptNativeFunction fn)
        {
            return (v, s, args, argLength, data) =>
            {
                try
                {
                    return fn(v, s, args, argLength, data);
                }
                catch (Exception e)
                {
                    // Pass back entire stack trace to ensure all information makes it back through
                    var message = e.ToString();

                    var jsException = JavaScriptValue.CreateError(JavaScriptValue.FromString(message));
                    JavaScriptContext.SetException(jsException);

                    return JavaScriptValue.Invalid;
                }
            };
        }
    }
}