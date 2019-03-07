namespace Enklu.Orchid
{
    /// <summary>
    /// JavaScript Runtime Abstraction which supplies execution scoped execution contexts for running and
    /// interacting with the JS runtime.
    /// </summary>
    public interface IJsRuntime
    {
        /// <summary>
        /// Creates a new <see cref="IJsExecutionContext"/> implementation used to
        /// execute JavaScript and interface with host objects.
        /// </summary>
        IJsExecutionContext NewExecutionContext();
    }
}