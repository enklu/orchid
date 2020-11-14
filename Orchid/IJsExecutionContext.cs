using System;

namespace Enklu.Orchid
{
    /// <summary>
    /// Abstraction of a JavaScript Execution Context, which contains a global object and interop caches
    /// shared by a specific runtime. Unlike an "Execution Context" described in the ECMA specifications,
    /// this context is exclusive from others, even if they share the same runtime. These objects may only
    /// be active one at a time.
    /// </summary>
    public interface IJsExecutionContext
    {
        /// <summary>
        /// This delegate is invoked just before the current context is disposed of.
        /// </summary>
        Action<IJsExecutionContext> OnExecutionContextDisposing { get; set; }

        /// <summary>
        /// Creates a new <see cref="IJsModule"/> implementation which can be passed to <see cref="RunScript(string)"/>
        /// </summary>
        /// <param name="moduleId">The module's id</param>
        /// <param name="name">The module's friendly name.</param>
        /// <returns></returns>
        IJsModule NewModule(string moduleId, string name = null);

        /// <summary>
        /// Gets a property from the global object/scope.
        /// </summary>
        T GetValue<T>(string name);

        /// <summary>
        /// Sets a property on the global object/scope.
        /// </summary>
        void SetValue<T>(string name, T value);

        /// <summary>
        /// Executes JavaScript code in the context of the global object/scope.
        /// </summary>
        void RunScript(string name, string script);

        /// <summary>
        /// Executes JavaScript in the context of the <c>@this</c> parameter.
        /// </summary>
        /// <param name="name">The name of the context.</param>
        /// <param name="this">The context of execution.</param>
        /// <param name="script">The script to run</param>
        void RunScript(string name, object @this, string script);

        /// <summary>
        /// Executes JavaScript in the context of the <c>@this</c> parameter, and allow exporting
        /// to a specific <see cref="IJsModule"/>.
        /// </summary>
        /// /// <param name="name">The name of the context.</param>
        /// <param name="this">The context of execution.</param>
        /// <param name="script">The script to run</param>
        /// <param name="module">The module to export any inner properties to.</param>
        void RunScript(string name, object @this, string script, IJsModule module);
    }
}