using System;
using Jint;
using Jint.Runtime.Debugger;

namespace Enklu.Orchid.Jint
{
    /// <summary>
    /// Jint implementation of <see cref="IJsRuntime"/>.
    /// </summary>
    public class JsRuntime : IJsRuntime, IDisposable
    {
        /// <summary>
        /// Configuration delegate to pass to each context.
        /// </summary>
        private readonly Action<Options> _configure;

        /// <summary>
        /// Creates a new <see cref="JsRuntime"/> Jint implementation.
        /// </summary>
        public JsRuntime()
            : this(options =>
            {
                options.AllowClr();
                options.CatchClrExceptions(exception =>
                {
                    throw exception;
                });

                // Debugging Configuration
                options.DebugMode(false);
                options.DebuggerStatementHandling(DebuggerStatementHandling.Ignore);
            }) { }

        /// <summary>
        /// Creates a new <see cref="JsRuntime"/> instance.
        /// </summary>
        public JsRuntime(Action<Options> configure)
        {
            _configure = options =>
            {
                configure(options);

                // Enhance existing option setting with Deny Attribute
                // options.DenyInteropAccessWith(typeof(DenyJsAccess)); TODO: Create a new options type
            };
        }

        /// <inheritdoc />
        public IJsExecutionContext NewExecutionContext()
        {
            var engine = new Engine(_configure);
            var executionContext = new JsExecutionContext(engine);

            engine.TypeConverter.RegisterDelegateConversion(
                typeof(IJsCallback),
                new JsCallbackConversion(executionContext));

            return executionContext;
        }

        /// <inheritdoc />
        public void Dispose()
        {

        }
    }
}