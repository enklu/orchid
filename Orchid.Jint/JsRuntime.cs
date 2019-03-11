using System;
using Jint;

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
                options.AllowDebuggerStatement(false);
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
                options.DenyInteropAccessWith(typeof(DenyJsAccess));
            };
        }

        /// <inheritdoc />
        public IJsExecutionContext NewExecutionContext()
        {
            var engine = new Engine(_configure);

            engine.ClrTypeConverter.RegisterDelegateConversion(
                typeof(IJsCallback),
                new JsCallbackConversion(engine));

            return new JsExecutionContext(engine);
        }

        /// <inheritdoc />
        public void Dispose()
        {

        }
    }
}