using System;
using Enklu.Orchid.Chakra.Interop;

namespace Enklu.Orchid.Chakra
{
    /// <summary>
    /// The main driver for the Chakra JavaScript runtime. This class creates all
    /// of the utilities for binding host objects, a layer of interop, and basic
    /// API for running JS scripts.
    /// </summary>
    public class JsEngine : IDisposable
    {
        /// <summary>
        /// The Chakra JavaScript runtime
        /// </summary>
        private JavaScriptRuntime _runtime;

        /// <summary>
        /// The main runtime context.
        /// </summary>
        private JsContext _context;

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
        /// Creates a new <see cref="JsEngine"/> instance.
        /// </summary>
        public JsEngine()
        {
            _runtime = JavaScriptRuntime.Create();
            _context = new JsContext(_runtime.CreateContext());
            _binder = new JsBinder(_context);
            _interop = new JsInterop(_context, _binder);

            _context.Run(() =>
            {
                _global = new JsBinding(_context, _binder, _interop, JavaScriptValue.GlobalObject);
            });
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
        public JavaScriptValue RunScript(string script)
        {
            return _context.Run(() =>
            {
                JavaScriptValue result = JavaScriptValue.Invalid;
                try
                {
                    result = JavaScriptContext.RunScript(script);
                }
                catch (JavaScriptScriptException e)
                {
                    var message = GetExceptionMessage(e.Error);
                    throw new Exception(message);
                }

                return result;
            });
        }

        /// <summary>
        /// Creates a new JavaScript object than can be modified directly and passed into <see cref="SetValue{T}"/>
        /// as a property of global.
        /// </summary>
        public JsBinding NewJsObject()
        {
            return _context.Run(() =>
            {
                var jsValue = JavaScriptValue.CreateObject();

                return new JsBinding(_context, _binder, _interop, jsValue);
            });
        }

        /// <summary>
        /// Prints error message.
        /// </summary>
        /// <param name="exception"></param>
        private static string GetExceptionMessage(JavaScriptValue exception)
        {
            return exception.GetProperty(JavaScriptPropertyId.FromString("message")).ToString();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _context.Dispose();
            _runtime.Dispose();
        }

        /// <summary>
        /// Creates a new binding builder.
        /// </summary>
        private JsBindingBuilder NewBuilder() => new JsBindingBuilder(_context, _binder, _interop);
    }
}
