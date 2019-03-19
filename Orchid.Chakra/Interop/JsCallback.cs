using System;

namespace Enklu.Orchid.Chakra.Interop
{
    /// <summary>
    /// Implementation of <see cref="Enklu.Orchid.IJsCallback"/> to adapt dynamic callbacks to Chakra.
    /// </summary>
    public class JsCallback : IJsCallback
    {
        private readonly JsContextScope _scope;
        private readonly JsInterop _interop;
        private readonly JavaScriptValue _callback;

        /// <summary>
        /// Creates a new <see cref="JsCallback"/> instance.
        /// </summary>
        public JsCallback(JsContextScope scope, JsInterop interop, JavaScriptValue callback)
        {
            _scope = scope;
            _interop = interop;
            _callback = callback;
            _callback.AddRef();
        }

        /// <inheritdoc />
        public object Apply(object @this, params object[] args)
        {
            return _scope.Run(() =>
            {
                JavaScriptValue[] jsValues = new JavaScriptValue[1 + args.Length];
                jsValues[0] = null != @this ? _interop.ToJsObject(@this, @this.GetType()) : JavaScriptValue.Undefined;
                for (var i = 0; i < args.Length; ++i)
                {
                    var arg = args[i];

                    jsValues[i + 1] = null != arg ? _interop.ToJsObject(arg, arg.GetType()) : JavaScriptValue.Null;
                }

                return TryInvoke(jsValues);
            });
        }

        /// <inheritdoc />
        public object Invoke(params object[] args)
        {
            return _scope.Run(() =>
            {
                JavaScriptValue[] jsValues = new JavaScriptValue[1 + args.Length];
                jsValues[0] = JavaScriptValue.GlobalObject;
                for (var i = 0; i < args.Length; ++i)
                {
                    var arg = args[i];

                    jsValues[i + 1] = null != arg ? _interop.ToJsObject(arg, arg.GetType()) : JavaScriptValue.Null;
                }

                return TryInvoke(jsValues);
            });
        }

        /// <summary>
        /// Attempts to invoke the callback. Any exception will result in flagging a context error.
        /// </summary>
        private object TryInvoke(JavaScriptValue[] values)
        {
            try
            {
                var result = _callback.CallFunction(values);

                if (_interop.TryInferType(result, out var returnType))
                {
                    return _interop.ToHostObject(result, returnType);
                }

                return _interop.ToHostObject(result, typeof(object));
            }
            catch (Exception e)
            {
                if (e is JavaScriptScriptException)
                {
                    var jse = (JavaScriptScriptException)e;
                    throw new Exception(jse.Error.GetProperty(JavaScriptPropertyId.FromString("message")).ToString());
                }

                throw e;
            }
        }
    }
}