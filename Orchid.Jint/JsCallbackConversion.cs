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
        /// Jint executionContext.
        /// </summary>
        private readonly JsExecutionContext _executionContext;

        /// <summary>
        /// Creates a new <see cref="JsCallbackConversion"/> instance.
        /// </summary>
        public JsCallbackConversion(JsExecutionContext executionContext)
        {
            _executionContext = executionContext;
        }

        /// <summary>
        /// Wraps the callable in a <see cref="JsCallback"/> to adapt to Orchid
        /// callback design.
        /// </summary>
        public object Convert(Func<JsValue, JsValue[], JsValue> callable)
        {
            return new JsCallback(_executionContext, callable);
        }
    }
}