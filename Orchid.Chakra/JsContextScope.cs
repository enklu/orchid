﻿using System;
using System.Collections.Generic;
using Enklu.Orchid.Logging;

namespace Enklu.Orchid.Chakra
{
    /// <summary>
    /// This class is used to initiate execution within the runtime. The runtime is capable
    /// of dealing with multiple context instances, allowing for mutually exclusive global
    /// objects executing in the same runtime. However, only a single context may be active
    /// per runtime.
    ///
    /// Any calls which directly interface with ChakraCore should use the current API for
    /// running commands in a context. <see cref="JsContextScope"/> provides a reentrant delegate
    /// executor utility as well as <see cref="Enter"/> and <see cref="Exit"/> methods.
    /// </summary>
    public class JsContextScope : IDisposable
    {
        /// <summary>
        /// Use the invalid context to determine whether or not entrance is possible
        /// </summary>
        private static JavaScriptContext Invalid = JavaScriptContext.Invalid;

        /// <summary>
        /// The underlying javascript context
        /// </summary>
        private JavaScriptContext _context;

        /// <summary>
        /// The value of the context prior to executing under the new context.
        /// </summary>
        private JavaScriptContext _previousContext;

        /// <summary>
        /// The current reentrancy state
        /// </summary>
        private int _contextsHeld = 0;

        /// <summary>
        /// Queue which C# finalizer threads can add JavaScript values for release, to be
        /// executed on the next context entry.
        /// </summary>
        private Queue<JavaScriptValue> _releaseQueue = new Queue<JavaScriptValue>();

        /// <summary>
        /// Whether or not this context is the runtime's current.
        /// </summary>
        public bool IsCurrentContext => JavaScriptContext.Current == _context;

        /// <summary>
        /// Creates a new <see cref="JsContextScope"/> instance.
        /// </summary>
        public JsContextScope(JavaScriptContext context)
        {
            _context = context;
            _context.AddRef();
        }

        /// <summary>
        /// A thread safe way to queue the release of JavaScript values release if tied to the
        /// finalizer of a C# object.
        /// </summary>
        /// <param name="jsValue"></param>
        public void QueueRelease(JavaScriptValue jsValue)
        {
            lock (_releaseQueue)
            {
                _releaseQueue.Enqueue(jsValue);
            }
        }

        /// <summary>
        /// Releases all of the javascript values in the release queue.
        /// </summary>
        private void DrainQueue()
        {
            lock (_releaseQueue)
            {
                while (_releaseQueue.Count > 0)
                {
                    var jsValue = _releaseQueue.Dequeue();
                    if (jsValue.IsValid)
                    {
                        jsValue.Release();
                    }
                }
            }
        }

        /// <summary>
        /// Enters the execution context.
        /// </summary>
        public void Enter()
        {
            if (!IsCurrentContext)
            {
                _previousContext = JavaScriptContext.Current;
                JavaScriptContext.Current = _context;
            }

            _contextsHeld++;

            // Drain the JsValue.Release() Queue
            DrainQueue();

            /*
            var isCurrent = IsCurrentContext;
            var isContext = JavaScriptContext.Current != Invalid;
            if (isContext && !isCurrent)
            {
                throw new Exception("Already entered different execution context.");
            }

            _contextsHeld++;
            if (!isContext)
            {
                JavaScriptContext.Current = _context;
            }
            */
        }

        /// <summary>
        /// Exits the current context.
        /// </summary>
        public void Exit()
        {
            if (!IsCurrentContext)
            {
                return;
            }

            if (--_contextsHeld == 0)
            {
                JavaScriptContext.Current = _previousContext;
                _previousContext = Invalid;
            }
        }

        /// <summary>
        /// Sets the current execution context, executes the provided action, and returns the
        /// context.
        /// </summary>
        public void Run(Action action)
        {
            Enter();
            try
            {
                action();
            }
            finally
            {
                Exit();
            }
        }

        /// <summary>
        /// Sets the current execution context, executes the provided function, and returns the
        /// context.
        /// </summary>
        public T Run<T>(Func<T> function)
        {
            Enter();
            try
            {
                return function();
            }
            finally
            {
                Exit();
            }
        }

        /// <inheritDoc />
        public void Dispose()
        {
            Exit();

            _context.Release();
        }
    }
}
