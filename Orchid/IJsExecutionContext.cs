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
        /// Creates a new <see cref="IJsModule"/> implementation which can be passed to <see cref="RunScript(string)"/>
        /// </summary>
        /// <param name="moduleId"></param>
        /// <returns></returns>
        IJsModule NewModule(string moduleId);

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
        void RunScript(string script);

        /// <summary>
        /// Executes JavaScript in the context of the <c>@this</c> parameter.
        /// </summary>
        /// <param name="@this">The context of execution.</param>
        /// <param name="script">The script to run</param>
        void RunScript(object @this, string script);

        /// <summary>
        /// Executes JavaScript in the context of the <c>@this</c> parameter, and allow exporting
        /// to a specific <see cref="IJsModule"/>.
        /// </summary>
        /// <param name="@this">The context of execution.</param>
        /// <param name="script">The script to run</param>
        /// <param name="module">The module to export any inner properties to.</param>
        void RunScript(object @this, string script, IJsModule module);
    }
}