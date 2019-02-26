using System;

namespace Enklu.Orchid.Chakra.Interop
{
    /// <summary>
    /// Represents an JavaScript object with host bindings.
    /// </summary>
    public class JsBinding
    {
        private readonly JsContext _context;
        private readonly JsBinder _binder;
        private readonly JsInterop _interop;
        private readonly JavaScriptValue _value;

        /// <summary>
        /// The javascript object in this binding.
        /// </summary>
        public JavaScriptValue Object => _value;

        /// <summary>
        /// Creates a new <see cref="JsBinding"/> instance.
        /// </summary>
        public JsBinding(JsContext context, JsBinder binder, JsInterop interop, JavaScriptValue value)
        {
            _context = context;
            _binder = binder;
            _interop = interop;
            _value = value;
        }

        /// <summary>
        /// This method binds a function to a specific field on a JavaScript object.
        /// </summary>
        /// <param name="name">The name of the field.</param>
        /// <param name="jsFunction">The callback function to execute when the JavaScript function is invoked.</param>
        public void AddFunction(string name, JavaScriptNativeFunction jsFunction)
        {
            _context.Run(() =>
            {
                var jsValue = _binder.BindFunction(jsFunction);

                _value.SetProperty(JavaScriptPropertyId.FromString(name), jsValue, true);
            });
        }

        /// <summary>
        /// This method binds a getter and setter function to a specific field on a JavaScript object.
        /// </summary>
        /// <param name="name">The name of the field.</param>
        /// <param name="getter">The function to invoke when the field is read.</param>
        /// <param name="setter">The function to invoke when the field is written to.</param>
        public void AddProperty(string name, JavaScriptNativeFunction getter, JavaScriptNativeFunction setter)
        {
            _context.Run(() =>
            {
                var get = _binder.BindFunction(getter);
                var set = _binder.BindFunction(setter);

                var descriptor = JavaScriptValue.CreateObject();
                descriptor.SetProperty(JavaScriptPropertyId.FromString("get"), get, true);
                descriptor.SetProperty(JavaScriptPropertyId.FromString("set"), set, true);

                _value.DefineProperty(JavaScriptPropertyId.FromString(name), descriptor);
            });
        }

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name"></param>
        /// <returns></returns>
        public T GetValue<T>(string name)
        {
            return _context.Run(() =>
            {
                var property = _value.GetProperty(JavaScriptPropertyId.FromString(name));
                return (T) _interop.ToHostObject(property, typeof(T));
            });
        }

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public void SetValue<T>(string name, T value) => SetValue(name, value, typeof(T));

        /// <summary>
        ///
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <param name="type"></param>
        public void SetValue(string name, object value, Type type)
        {
            // TODO: Manage these cases in JsInterop? Possibly another "Binding Filter" to strip down the
            // TODO: core and wrapper instances into pure js objects
            if (typeof(JavaScriptNativeFunction) == type)
            {
                AddFunction(name, (JavaScriptNativeFunction) value);
                return;
            }

            if (typeof(JsBinding) == type)
            {
                var binding = (JsBinding) value;

                _context.Run(() =>
                {
                    _value.SetProperty(JavaScriptPropertyId.FromString(name), binding.Object, true);
                });
                return;
            }

            _context.Run(() =>
            {
                var jsValue = _interop.ToJsObject(value, type);

                _value.SetProperty(JavaScriptPropertyId.FromString(name), jsValue, true);
            });
        }

    }
}