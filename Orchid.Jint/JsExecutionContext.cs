using System;
using Jint;

namespace Enklu.Orchid.Jint
{
    public class JsExecutionContext : IJsExecutionContext
    {
        /// <summary>
        /// Internal Jint execution context
        /// </summary>
        private readonly Engine _engine;

        /// <summary>
        /// Creates a new <see cref="JsExecutionContext"/> instance.
        /// </summary>
        public JsExecutionContext(Engine engine)
        {
            _engine = engine;
        }

        public T GetValue<T>(string name)
        {
            var value = _engine.GetValue(name);
            return value.To<T>();
        }

        public void SetValue<T>(string name, T value)
        {
            var type = typeof(T);
            var objValue = (object) value;

            if (type.IsAssignableFrom(typeof(Delegate)))
            {
                _engine.SetValue(name, (Delegate) objValue);
                return;
            }

            if (type == typeof(bool))
            {
                _engine.SetValue(name, (bool) objValue);
                return;
            }

            if (type.IsAssignableFrom(typeof(double)))
            {
                _engine.SetValue(name, (double) objValue);
                return;
            }

            if (type == typeof(string))
            {
                _engine.SetValue(name, (string) objValue);
                return;
            }

            _engine.SetValue(name, objValue);
        }

        public void RunScript(string script)
        {
            _engine.Execute(script);
        }
    }
}
