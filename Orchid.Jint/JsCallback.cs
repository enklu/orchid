﻿using System;
using Enklu.Orchid.Logging;
using Jint;
using Jint.Native;
using Jint.Runtime;

namespace Enklu.Orchid.Jint
{
    /// <summary>
    /// Orchid callback implementation for Jint. We wrap Jint's <see cref="ICallable"/> and provide
    /// parameter passthrough and conversion from host to Jint types.
    /// </summary>
    public class JsCallback : IJsCallback
    {
        /// <summary>
        /// Jint Engine parameter for creating conversions
        /// </summary>
        private readonly JsExecutionContext _context;

        /// <summary>
        /// Jint ICallable function to execute when applying this callback.
        /// </summary>
        private readonly Func<JsValue, JsValue[], JsValue> _callback;

        /// <summary>
        /// The @this binding for callback execution
        /// </summary>
        private JsValue _binding;

        /// <inheritDoc />
        public IJsExecutionContext ExecutionContext => _context;

        /// <inheritDoc />
        public Exception ExecutionError { get; private set; }

        /// <summary>
        /// Creates a new <see cref="JsCallback"/> instance.
        /// </summary>
        public JsCallback(JsExecutionContext context, Func<JsValue, JsValue[], JsValue> callback)
        {
            _context = context;
            _callback = callback;
        }

        /// <inheritDoc />
        public object Apply(object @this, params object[] args)
        {
            var jsThis = _binding == null
                ? JsValue.FromObject(_context.Engine, @this)
                : _binding;
            
            var argsLength = args?.Length ?? 0;
            var jsArgs = new JsValue[argsLength];

            for (int i = 0; i < argsLength; ++i)
            {
                jsArgs[i] = JsValue.FromObject(_context.Engine, args[i]);
            }

            try
            {
                var result = _callback(jsThis, jsArgs);
                return result.ToObject();
            }
            catch (JavaScriptException jsError)
            {
                Log.Warning("Scripting", "[{0}:{1}] {2}", jsError.Location.SourceFile, jsError.Location.Start.Line, jsError.Message);
                ExecutionError = jsError;
            }
            catch (Exception exception)
            {
                // TODO: Most recent js stack trace?
                Log.Warning("Scripting", "An unknown error has occured: {0}", exception);
                ExecutionError = exception;
            }
            return null;
        }

        /// <inheritDoc />
        public object Invoke(params object[] args)
        {
            var jsThis = _binding == null ? JsValue.Null : _binding;
            
            var argsLength = args?.Length ?? 0;
            var jsArgs = new JsValue[argsLength];

            for (int i = 0; i < argsLength; ++i)
            {
                jsArgs[i] = JsValue.FromObject(_context.Engine, args[i]);
            }

            try
            {
                var result = _callback(jsThis, jsArgs);
                return result.ToObject();
            }
            catch (JavaScriptException jsError)
            {
                Log.Warning("Scripting", "[{0}:{1}] {2}", jsError.Location.SourceFile, jsError.Location.Start.Line, jsError.Message);
                ExecutionError = jsError;
            }
            catch (Exception exception)
            {
                // TODO: Most recent js stack trace?
                Log.Warning("Scripting", "An unknown error has occured: {0}", exception);
                ExecutionError = exception;
            }
            return null;
        }

        /// <inheritDoc />
        public void Bind(object @this)
        {
            if (null != _binding)
            {
                return;
            }

            _binding = JsValue.FromObject(_context.Engine, @this);
        }
    }
}