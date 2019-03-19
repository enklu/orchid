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
        private readonly JsExecutionContext _context;

        /// <summary>
        /// Jint ICallable function to execute when applying this callback.
        /// </summary>
        private readonly Func<JsValue, JsValue[], JsValue> _callback;

        /// <inheritDoc />
        public IJsExecutionContext ExecutionContext => _context;

        /// <summary>
        /// Creates a new <see cref="JsCallback"/> instance.
        /// </summary>
        public JsCallback(JsExecutionContext context, Func<JsValue, JsValue[], JsValue> callback)
        {
            _context = context;
            _callback = callback;
        }

        /// <inheritDoc />
        public object Apply(object @this, params object[] args)
        {
            var jsThis = JsValue.FromObject(_context.Engine, @this);
            var jsArgs = new JsValue[args.Length];

            for (int i = 0; i < args.Length; ++i)
            {
                jsArgs[i] = JsValue.FromObject(_context.Engine, args[i]);
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
                jsArgs[i] = JsValue.FromObject(_context.Engine, args[i]);
            }

            var result = _callback(jsThis, jsArgs);
            return result.ToObject();
        }
    }
}