using System;

namespace Enklu.Orchid.Chakra
{
    /// <summary>
    /// This class is used to initiate execution within the runtime. The runtime is capable
    /// of dealing with multiple context instances, allowing for mutually exclusive global
    /// objects executing in the same runtime. However, only a single context may be active
    /// per runtime.
    ///
    /// Any calls which directly interface with ChakraCore should use the current API for
    /// running commands in a context. <see cref="JsContext"/> provides a reentrant delegate
    /// executor utility as well as <see cref="Enter"/> and <see cref="Exit"/> methods.
    /// </summary>
    public class JsContext : IDisposable
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
        /// The current reentrancy state
        /// </summary>
        private int _contextsHeld = 0;

        /// <summary>
        /// Whether or not this context is the runtime's current.
        /// </summary>
        public bool IsCurrentContext => JavaScriptContext.Current == _context;

        /// <summary>
        /// Creates a new <see cref="JsContext"/> instance.
        /// </summary>
        public JsContext(JavaScriptContext context)
        {
            _context = context;
            _context.AddRef();
        }

        /// <summary>
        /// Enters the execution context.
        /// </summary>
        public void Enter()
        {
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
                JavaScriptContext.Current = Invalid;
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
