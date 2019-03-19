using System;
using System.Collections.Generic;
using System.Reflection;

namespace Enklu.Orchid.Chakra.Interop
{
    /// <summary>
    /// Represents an JavaScript object with host bindings.
    /// </summary>
    public class JsBinding
    {
        private readonly JsContextScope _scope;
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
        public JsBinding(JsContextScope scope, JsBinder binder, JsInterop interop, JavaScriptValue value)
        {
            _scope = scope;
            _binder = binder;
            _interop = interop;
            _value = value;
        }

        /// <summary>
        /// This method binds a function to a specific field on a JavaScript object.
        /// </summary>
        /// <param name="name">The name of the field.</param>
        /// <param name="jsFunction">The callback function to execute when the JavaScript function is invoked.</param>
        /// <param name="instanceData">Any instance data that should be passed when the function is executed.</param>
        public void AddInstanceFunction(string name, JavaScriptNativeFunction jsFunction, IntPtr instanceData)
        {
            _scope.Run(() =>
            {
                var jsValue = _binder.BindInstanceFunction(jsFunction, instanceData);

                _value.SetProperty(JavaScriptPropertyId.FromString(name), jsValue, true);
            });
        }

        /// <summary>
        /// This method binds a function to a specific field on a JavaScript object.
        /// </summary>
        /// <param name="name">The name of the field.</param>
        /// <param name="jsFunction">The callback function to execute when the JavaScript function is invoked.</param>
        public void AddFunction(string name, JavaScriptNativeFunction jsFunction)
        {
            _scope.Run(() =>
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
            _scope.Run(() =>
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
        /// Returns the Chakra <see cref="JavaScriptValue"/> for the property name.
        /// </summary>
        public JavaScriptValue GetValue(string name)
        {
            return _scope.Run(() => _value.GetProperty(JavaScriptPropertyId.FromString(name)));
        }

        /// <summary>
        /// Returns <c>true</c> if the current object has a value using the specific property.
        /// </summary>
        public bool HasValue(string name)
        {
            return _scope.Run(() => _value.HasProperty(JavaScriptPropertyId.FromString(name)));
        }

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name"></param>
        /// <returns></returns>
        public T GetValue<T>(string name)
        {
            return _scope.Run(() =>
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
        public void SetValue<T>(string name, T value)
        {
            if (value == null)
            {
                SetValue(name, value, typeof(T));
                return;
            }

            // Resolve highest resolution type
            var valueType = value.GetType();
            var type = valueType.IsAssignableFrom(typeof(T))
                ? typeof(T)
                : valueType;

            SetValue(name, value, type);
        }

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

                _scope.Run(() =>
                {
                    _value.SetProperty(JavaScriptPropertyId.FromString(name), binding.Object, true);
                });
                return;
            }

            _scope.Run(() =>
            {
                var jsValue = _interop.ToJsObject(value, type);

                _value.SetProperty(JavaScriptPropertyId.FromString(name), jsValue, true);
            });
        }
    }
}