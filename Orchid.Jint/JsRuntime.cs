using System;
using Jint;

namespace Enklu.Orchid.Jint
{
    public class JsRuntime : IJsRuntime, IDisposable
    {
        public JsRuntime()
        {

        }

        public IJsExecutionContext NewExecutionContext()
        {
            var engine = new Engine(options =>
            {
                options.AllowClr();
                options.CatchClrExceptions(exception =>
                {
                    throw exception;
                });

                // Debugging Configuration
                options.DebugMode(false);
                options.AllowDebuggerStatement(false);
            });

            engine.ClrTypeConverter.RegisterDelegateConversion(
                typeof(IJsCallback),
                new JsCallbackConversion(engine));

            return new JsExecutionContext(engine);
        }

        public void Dispose()
        {

        }
    }
}