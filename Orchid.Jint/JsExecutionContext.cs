using System;
using Jint;
using Jint.Native;

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
            return value.To<T>(_engine.ClrTypeConverter);
        }

        public void SetValue<T>(string name, T value)
        {
            // Ensure that we're getting type information from the
            // highest resolution type.
            var valueType = value.GetType();
            var type = valueType.IsAssignableFrom(typeof(T))
                ? typeof(T)
                : valueType;
            var objValue = (object) value;

            if (typeof(Delegate).IsAssignableFrom(type))
            {
                _engine.SetValue(name, (Delegate) objValue);
                return;
            }

            if (typeof(bool).IsAssignableFrom(type))
            {
                _engine.SetValue(name, (bool) objValue);
                return;
            }

            if (typeof(double).IsAssignableFrom(type))
            {
                _engine.SetValue(name, (double) objValue);
                return;
            }

            if (typeof(string).IsAssignableFrom(type))
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

        public void RunScript(object @this, string script)
        {
            var jsThis = JsValue.FromObject(_engine, @this);
            var currentThis = _engine.ExecutionContext.ThisBinding;
            _engine.ExecutionContext.ThisBinding = jsThis;

            _engine.Execute(script);

            _engine.ExecutionContext.ThisBinding = currentThis;
        }
    }
}
