using System;

namespace Enklu.Orchid.Chakra
{
    /// <summary>
    /// This class represents the JavaScript runtime implemented by Chakra Core. Each Chakra
    /// runtime has its own independent execution engine, JIT compiler, and garbage collected
    /// heap. As such, each runtime is completely isolated from other runtimes.
    /// </summary>
    public class JsRuntime : IJsRuntime, IDisposable
    {
        /// <summary>
        /// Chakra Core native runtime shim
        /// </summary>
        private JavaScriptRuntime _runtime;

        /// <summary>
        /// Creates a new <see cref="JsRuntime"/> instance.
        /// </summary>
        public JsRuntime()
        {
            _runtime = JavaScriptRuntime.Create();
        }

        /// <inheritdoc />
        public IJsExecutionContext NewExecutionContext()
        {
            return new JsExecutionContext(_runtime);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _runtime.Dispose();
        }
    }
}