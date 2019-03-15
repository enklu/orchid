using System;
using Enklu.Orchid.Chakra.Interop;

namespace Enklu.Orchid.Chakra
{
    /// <summary>
    /// The main driver for the Chakra JavaScript runtime. This class creates all
    /// of the utilities for binding host objects, a layer of interop, and basic
    /// API for running JS scripts.
    /// </summary>
    public class JsExecutionContext : IJsExecutionContext, IDisposable
    {
        /// <summary>
        /// The Chakra JavaScript runtime
        /// </summary>
        private JavaScriptRuntime _runtime;

        /// <summary>
        /// The context instance internal to this execution context.
        /// </summary>
        private JsContextScope _scope;

        /// <summary>
        /// The interop layer for converting host objects to JS and vice versa.
        /// </summary>
        private JsInterop _interop;

        /// <summary>
        /// Utility for binding host class methods, properties, and fields to javascript
        /// objects.
        /// </summary>
        private JsBinder _binder;

        /// <summary>
        /// Binding representing the global JS object/scope.
        /// </summary>
        private JsBinding _global;

        /// <summary>
        /// The <see cref="JsContextScope"/> used to work with raw <see cref="JavaScriptValue"/>.
        /// </summary>
        public JsContextScope Scope => _scope;

        /// <summary>
        /// Creates a new <see cref="JsExecutionContext"/> instance.
        /// </summary>
        public JsExecutionContext(JavaScriptRuntime runtime)
        {
            _runtime = runtime;
            _scope = new JsContextScope(_runtime.CreateContext());
            _binder = new JsBinder(_scope);
            _interop = new JsInterop(_scope, _binder);

            _scope.Run(() =>
            {
                _global = new JsBinding(_scope, _binder, _interop, JavaScriptValue.GlobalObject);
            });
        }

        /// <summary>
        /// Creates a new <see cref="IJsModule"/> implementation which can be passed to <see cref="RunScript(string)"/>
        /// </summary>
        /// <param name="moduleId"></param>
        /// <returns></returns>
        public IJsModule NewModule(string moduleId)
        {
            return new JsModule(_scope, _binder, _interop, moduleId);
        }

        /// <summary>
        /// Gets a raw <see cref="JavaScriptValue"/> from the global object/scope.
        /// </summary>
        public JavaScriptValue GetValue(string name)
        {
            return _scope.Run(() => _global.GetValue(name));
        }

        /// <summary>
        /// Gets a property from the global object/scope.
        /// </summary>
        public T GetValue<T>(string name)
        {
            return _global.GetValue<T>(name);
        }

        /// <summary>
        /// Sets a property on the global object/scope.
        /// </summary>
        public void SetValue<T>(string name, T value)
        {
            _global.SetValue(name, value);
        }

        /// <summary>
        /// Executes JavaScript code in the context of the global object/scope.
        /// </summary>
        public void RunScript(string script)
        {
            _scope.Run(() =>
            {
                try
                {
                    JavaScriptContext.RunScript(script);
                }
                catch (JavaScriptScriptException e)
                {
                    var error = e.Error;
                    var message = error.GetProperty(JavaScriptPropertyId.FromString("message")).ToString();
                    throw new Exception(message);
                }
            });
        }

        /// <summary>
        /// Executes JavaScript in the context of the <c>@this</c> parameter.
        /// </summary>
        /// <param name="@this">The context of execution.</param>
        /// <param name="script">The script to run</param>
        public void RunScript(object @this, string script)
        {
            _scope.Run(() =>
            {
                try
                {
                    var fnScript = $"(function() {{ {script} }});";
                    var fn = JavaScriptContext.RunScript(fnScript);
                    var jsObject = _interop.ToJsObject(@this, @this.GetType());
                    fn.CallFunction(jsObject);
                }
                catch (JavaScriptScriptException e)
                {
                    var error = e.Error;
                    var message = error.GetProperty(JavaScriptPropertyId.FromString("message")).ToString();
                    throw new Exception(message);
                }
            });
        }

        /// <summary>
        /// Executes JavaScript in the context of the <c>@this</c> parameter, and allow exporting
        /// to a specific <see cref="IJsModule"/>.
        /// </summary>
        /// <param name="@this">The context of execution.</param>
        /// <param name="script">The script to run</param>
        /// <param name="module">The module to export any inner properties to.</param>
        public void RunScript(object @this, string script, IJsModule module)
        {
            _scope.Run(() =>
            {
                try
                {
                    var fnScript = $"(function(module) {{ {script} }});";
                    var fn = JavaScriptContext.RunScript(fnScript);
                    var jsObject = _interop.ToJsObject(@this, @this.GetType());
                    fn.CallFunction(jsObject, ((JsModule) module).Module.Object);
                }
                catch (JavaScriptScriptException e)
                {
                    var error = e.Error;
                    var message = error.GetProperty(JavaScriptPropertyId.FromString("message")).ToString();
                    throw new Exception(message);
                }
            });
        }

        /// <summary>
        /// Creates a new JavaScript object than can be modified directly and passed into <see cref="SetValue{T}"/>
        /// as a property of global.
        /// </summary>
        public JsBinding NewJsObject()
        {
            return _scope.Run(() =>
            {
                var jsValue = JavaScriptValue.CreateObject();

                return new JsBinding(_scope, _binder, _interop, jsValue);
            });
        }

        public void GC()
        {
            _scope.Run(() =>
            {
                _runtime.CollectGarbage();
            });
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _scope.Dispose();
        }
    }
}
