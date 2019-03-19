using System;
using System.Reflection;
using Jint;
using Jint.Native;

namespace Enklu.Orchid.Jint
{
    /// <summary>
    /// Jint implementation of <see cref="IJsExecutionContext"/>.
    /// </summary>
    public class JsExecutionContext : IJsExecutionContext, IDisposable
    {
        /// <summary>
        /// Internal Jint execution context
        /// </summary>
        private Engine _engine;

        /// <summary>
        /// The Jint Engine instance for this execution context.
        /// </summary>
        public Engine Engine => _engine;

        /// <summary>
        /// This delegate is invoked just before the current context is disposed of.
        /// </summary>
        public Action<IJsExecutionContext> OnExecutionContextDisposing { get; set; }

        /// <summary>
        /// Creates a new <see cref="JsExecutionContext"/> instance.
        /// </summary>
        public JsExecutionContext(Engine engine)
        {
            _engine = engine;
        }

        /// <inheritdoc />
        public IJsModule NewModule(string moduleId)
        {
            return new JsModule(_engine, moduleId);
        }

        /// <inheritdoc />
        public T GetValue<T>(string name)
        {
            var value = _engine.GetValue(name);
            return value.To<T>(_engine.ClrTypeConverter);
        }

        /// <inheritdoc />
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

        /// <inheritdoc />
        public void RunScript(string script)
        {
            _engine.Execute(script);
        }

        /// <inheritdoc />
        public void RunScript(object @this, string script)
        {
            var jsThis = JsValue.FromObject(_engine, @this);
            var jsScript = string.Format("(function() {{ {0} }})", script);

            var fn = _engine.Execute(jsScript).GetCompletionValue();
            _engine.Invoke(fn, jsThis, new object[] { });
        }

        /// <inheritdoc />
        public void RunScript(object @this, string script, IJsModule module)
        {
            var jsThis = JsValue.FromObject(_engine, @this);
            var jsScript = string.Format("(function(module) {{ {0} }})", script);

            var fn = _engine.Execute(jsScript).GetCompletionValue();
            _engine.Invoke(fn, jsThis, new object[] { ((JsModule) module).Module });
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (null != OnExecutionContextDisposing)
            {
                OnExecutionContextDisposing.Invoke(this);
            }

            if (null != _engine)
            {
                _engine.Destroy();
            }

            _engine = null;
        }
    }
}
