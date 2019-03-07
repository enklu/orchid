using System;
using Jint;
using Jint.Native;

namespace Enklu.Orchid.Jint
{
    /// <summary>
    /// Orchid callback implementation for Jint. We wrap Jint's <see cref="ICallable"/> and provide
    /// parameter passthrough and conversion from host to Jint types.
    /// </summary>
    public class JsCallback : IJsCallback
    {
        /// <summary>
        /// Jint Engine parameter for creating conversions
        /// </summary>
        private readonly Engine _engine;

        /// <summary>
        /// Jint ICallable function to execute when applying this callback.
        /// </summary>
        private readonly Func<JsValue, JsValue[], JsValue> _callback;

        /// <summary>
        /// Creates a new <see cref="JsCallback"/> instance.
        /// </summary>
        public JsCallback(Engine engine, Func<JsValue, JsValue[], JsValue> callback)
        {
            _engine = engine;
            _callback = callback;
        }

        /// <inheritDoc />
        public object Apply(object @this, params object[] args)
        {
            var jsThis = JsValue.FromObject(_engine, @this);
            var jsArgs = new JsValue[args.Length];

            for (int i = 0; i < args.Length; ++i)
            {
                jsArgs[i] = JsValue.FromObject(_engine, args[i]);
            }

            var result = _callback(jsThis, jsArgs);
            return result.ToObject();
        }

        /// <inheritDoc />
        public object Invoke(params object[] args)
        {
            var jsThis = JsValue.Null;
            var jsArgs = new JsValue[args.Length];

            for (int i = 0; i < args.Length; ++i)
            {
                jsArgs[i] = JsValue.FromObject(_engine, args[i]);
            }

            var result = _callback(jsThis, jsArgs);
            return result.ToObject();
        }
    }
}