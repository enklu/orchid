using System;
using System.Globalization;
using System.Reflection;
using Enklu.Orchid.Logging;
using Jint;
using Jint.Native;
using Jint.Parser;
using Jint.Runtime;

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
            var obj = value.ToObject();  // mps TODO: This was taken from To<>. Fix to follow DRY
            return (T)_engine.ClrTypeConverter.Convert(obj, typeof(T), CultureInfo.InvariantCulture);
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
        public void RunScript(string name, string script)
        {
            try
            {
              _engine.Execute(script, new ParserOptions
                {
                  Source = name
                });
              }
            catch (JavaScriptException jsError)
            {
                Log.Warning("Scripting", "[{0}:{1}] {2}", name, jsError.Location.Start.Line, jsError.Message);
            }
        }

        /// <inheritdoc />
        public void RunScript(string name, object @this, string script)
        {
            var jsThis = JsValue.FromObject(_engine, @this);
            var jsScript = $"(function() {{ {script} }})";

            var fn = _engine.Execute(jsScript, new ParserOptions { Source = name }).GetCompletionValue();
            try
            {
                _engine.Invoke(fn, jsThis, new object[] { });
            }
            catch (JavaScriptException jsError)
            {
                Log.Warning("Scripting", "[{0}:{1}] {2}", name, jsError.Location.Start.Line, jsError.Message);
            }
        }

        /// <inheritdoc />
        public void RunScript(string name, object @this, string script, IJsModule module)
        {
            var jsThis = JsValue.FromObject(_engine, @this);
            var jsScript = $"(function(module) {{ {script} }})";

            var fn = _engine.Execute(jsScript, new ParserOptions { Source = name }).GetCompletionValue();
            try
            {
                _engine.Invoke(fn, jsThis, new object[] { ((JsModule) module).Module });
            }
            catch (JavaScriptException jsError)
            {
                Log.Warning("Scripting", "[{0}:{1}] {2}", name, jsError.Location.Start.Line, jsError.Message);
            }
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
 //               _engine.Dispose();
            }

            _engine = null;
        }
    }
}
