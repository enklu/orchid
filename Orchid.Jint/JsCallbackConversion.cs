using System;
using Jint;
using Jint.Native;
using Jint.Runtime.Interop;

namespace Enklu.Orchid.Jint
{
    /// <summary>
    /// A <see cref="ICallableConversion"/> implementation to adapt Jint's <see cref="Func{T1, T2, TResult}"/> to
    /// Orchid <see cref="IJsCallback"/>.
    /// </summary>
    public class JsCallbackConversion : ICallableConversion
    {
        /// <summary>
        /// Jint engine.
        /// </summary>
        private readonly Engine _engine;

        /// <summary>
        /// Creates a new <see cref="JsCallbackConversion"/> instance.
        /// </summary>
        public JsCallbackConversion(Engine engine)
        {
            _engine = engine;
        }

        /// <summary>
        /// Wraps the callable in a <see cref="JsCallback"/> to adapt to Orchid
        /// callback design.
        /// </summary>
        public object Convert(Func<JsValue, JsValue[], JsValue> callable)
        {
            return new JsCallback(_engine, callable);
        }
    }
}