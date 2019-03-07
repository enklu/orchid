using System;
using System.Runtime.InteropServices;

namespace Enklu.Orchid.Chakra.Interop
{
    /// <summary>
    /// This class creates non-primitive objects in the JavaScript runtime, and binds them to
    /// host objects. This synchronizes the lifecycle of both the host and JavaScript objects
    /// to avoid memory access issues that can occur if one of the objects is garbage collected.
    ///
    /// For functions, the binding process consists of creating a new <see cref="JavaScriptValue"/>
    /// provided a host function. Then, the two are linked by retaining a <see cref="GCHandle"/>
    /// reference on the host, and using a before collect callback in the JavaScript runtime to
    /// notify the host of collection. See the <see cref="Link{T}"/> method for further details.
    /// </summary>
    public class JsBinder
    {
        private readonly JsContextScope _scope;
        private readonly JavaScriptObjectBeforeCollectCallback _jsGcCollect;

        /// <summary>
        /// Creates a new <see cref="JsBinder"/> instance.
        /// </summary>
        public JsBinder(JsContextScope scope)
        {
            _scope = scope;
            _jsGcCollect = JsGcCollect;
        }

        /// <summary>
        /// This method creates a new <see cref="JavaScriptValue"/> representing a JavaScript function, and
        /// performs the binding of the resulting function with the native host function. This ensures that the
        /// host function will not be garbage collected as long as the JavaScript function has not been garbage
        /// collected.
        /// </summary>
        /// <param name="func">The host function used to create the JavaScript function and binding.</param>
        public JavaScriptValue BindFunction(JavaScriptNativeFunction func)
        {
            return _scope.Run(() =>
            {
                var jsValue = JavaScriptValue.CreateFunction(func);
                Link(jsValue, func);

                return jsValue;
            });
        }

        /// <summary>
        /// This method creates a new <see cref="JavaScriptValue"/> representing a JavaScript object, and
        /// performs the binding of the resulting object with a host object. This ensures that the
        /// host object will not garbage collected as long as the JavaScript object has not been garbage
        /// collected.
        ///
        /// Additionally, this method uses an internal data property on the JavaScript object to identify
        /// the bound host object. A bound host object can be retrieved from a <see cref="JavaScriptValue"/>
        /// by using the <see cref="ObjectLinkedTo"/> method.
        /// </summary>
        /// <param name="instance">The host object instance to bind the JavaScript object to.</param>
        public JavaScriptValue BindObject<T>(T instance)
        {
            return _scope.Run(() =>
            {
                // JS Object which allows external data to be set. In this case, the pointer of our bound host object
                var jsValue = JavaScriptValue.CreateExternalObject(IntPtr.Zero, null);

                // Link the GC Collection of the JS Object to the host object, then set the resulting pointer on the JS Value
                var ptr = Link(jsValue, instance);
                jsValue.ExternalData = ptr;

                return jsValue;
            });
        }

        /// <summary>
        /// This method will lookup a bound host object from a <see cref="JavaScriptValue"/> object. This lookup requires
        /// that the JavaScript object was bound to a host object using the <see cref="BindObject{T}"/> method.
        /// </summary>
        /// <param name="value">The <see cref="JavaScriptValue"/> object to find the bound host object for.</param>
        /// <returns>The bound host object if it exists. Otherwise, <c>null</c></returns>
        public object ObjectLinkedTo(JavaScriptValue value)
        {
            // Must check for external data. Accessing ExternalData on a regular JS object will throw.
            if (!value.HasExternalData)
            {
                return null;
            }

            var externalData = value.ExternalData;
            var handle = GCHandle.FromIntPtr(externalData);
            if (!handle.IsAllocated)
            {
                return null;
            }

            return handle.Target;
        }

        /// <summary>
        /// This method creates a <see cref="GCHandle"/> for a host object, converts it into a pointer, then
        /// adds a 'before collect callback' passing that pointer. This callback will execute whenever the
        /// JavaScript object is going to be garbage collected, which releases the host object GC handle,
        /// allowing it to also be garbage collected should no external references exist.
        /// </summary>
        /// <param name="jsValue">The <see cref="JavaScriptValue"/> to add the before collect callback for.</param>
        /// <param name="instance">The host object to link to the JavaScript object.</param>
        /// <returns>The resulting pointer of the <see cref="GCHandle"/> for the host object.</returns>
        public IntPtr Link<T>(JavaScriptValue jsValue, T instance)
        {
            var gcHandle = GCHandle.Alloc(instance, GCHandleType.Normal);
            var ptr = GCHandle.ToIntPtr(gcHandle);

            _scope.Run(() =>
            {
                JavaScriptContext.SetObjectBeforeCollectCallback(jsValue, ptr, _jsGcCollect);
            });

            return ptr;
        }

        /// <summary>
        /// Triggered before the JavaScript GC collects a JS value. The host object representation
        /// is stored as external data on the object and also passed with the object.
        /// </summary>
        private void JsGcCollect(JavaScriptValue jsValue, IntPtr externalData)
        {
            var handle = GCHandle.FromIntPtr(externalData);
            handle.Free();
        }
    }
}