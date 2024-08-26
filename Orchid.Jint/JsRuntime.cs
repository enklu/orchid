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
        /// Creates a new <see cref="JsRuntime"/> Jint implementation.
        /// </summary>
        public JsRuntime()
        {

        }

        /// <inheritdoc />
        public IJsExecutionContext NewExecutionContext()
        {
            JsExecutionContext jsExecutionContext = null;
            Action<Options> configure = options =>
                {
                    options.AllowClr();
                    options.CatchClrExceptions(exception =>
                    {
                        throw exception;
                    });

                    // Debugging Configuration
                    options.DebugMode(false);
                    options.DebuggerStatementHandling(DebuggerStatementHandling.Ignore);
                    options.SetTypeConverter(e =>
                    {
                        jsExecutionContext = new JsExecutionContext(e);
                        return new OrchidTypeConverter(e, jsExecutionContext);
                    });

                };
            var engine = new Engine(configure);
            return jsExecutionContext;
        }

        /// <inheritdoc />
        public void Dispose()
        {

        }
    }
}