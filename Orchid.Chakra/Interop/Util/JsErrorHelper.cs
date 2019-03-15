using System;

namespace Enklu.Orchid.Chakra.Interop
{
    /// <summary>
    /// Utility class for working with Chakra JS Exceptions.
    /// </summary>
    public static class JsErrorHelper
    {
        /// <summary>
        /// This method will set a javascript exception on the current context.
        /// </summary>
        /// <param name="message">The message to use in the exception.</param>
        public static void SetJsException(string message)
        {
            var error = JavaScriptValue.CreateError(JavaScriptValue.FromString(message));
            JavaScriptContext.SetException(error);
        }

        /// <summary>
        /// This method will attempt to pass along the any exceptions that bubble through the interop
        /// layers. Assuming that each exception may occur as either a regular C# host exception or
        /// <see cref="JavaScriptScriptException"/>, we check for both and extra the message accordingly.
        /// </summary>
        public static string ExtractErrorMessage(Exception e)
        {
            if (e is JavaScriptScriptException jse)
            {
                var m = jse.Error.GetProperty(JavaScriptPropertyId.FromString("message"));
                return m.ConvertToString().ToString();
            }

            return e.ToString();
        }
    }
}