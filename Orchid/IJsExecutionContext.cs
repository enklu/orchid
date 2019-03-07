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
    }
}