namespace Enklu.Orchid.Chakra.Interop
{
    public class JsModule : IJsModule
    {
        private readonly JsContextScope _scope;
        private readonly JsBinder _binder;
        private readonly JsInterop _interop;

        private JsBinding _exports;

        /// <inheritdoc/>
        public string ModuleId { get; }
        
        /// <inheritdoc/>
        public string Name { get; }

        /// <summary>
        /// The module binding specifically for the Chakra implementation.
        /// </summary>
        public JsBinding Module { get; }

        /// <summary>
        /// Creates a new <see cref="JsModule"/> instance.
        /// </summary>
        public JsModule(JsContextScope scope, JsBinder binder, JsInterop interop, string moduleId)
        {
            _scope = scope;
            _binder = binder;
            _interop = interop;

            ModuleId = moduleId;
            Name = moduleId;

            // Create JS Representation
            Module = _scope.Run(() =>
            {
                var jsValue = JavaScriptValue.CreateObject();
                jsValue.AddRef();

                return new JsBinding(_scope, _binder, _interop, jsValue);
            });

        }

        /// <inheritdoc/>
        public T GetExportedValue<T>(string name)
        {
            return _scope.Run(() =>
            {
                if (null == _exports)
                {
                    if (!Module.HasValue("exports"))
                    {
                        return default(T);
                    }

                    var exports = Module.GetValue("exports");
                    _exports = new JsBinding(_scope, _binder, _interop, exports);
                }

                return _exports.GetValue<T>(name);
            });
        }
    }
}