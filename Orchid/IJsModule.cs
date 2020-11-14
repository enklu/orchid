namespace Enklu.Orchid
{
    /// <summary>
    /// Represents a JavaScript module extraction for binding exported methods and properties.
    /// </summary>
    public interface IJsModule
    {
        /// <summary>
        /// Unique module identifier to prevent overlap across a global namespace.
        /// </summary>
        string ModuleId { get; }
        
        /// <summary>
        /// Non-unique name for the module. Commonly the script's name.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets an exported value from the module.
        /// </summary>
        /// <param name="name">The name of the exported value.</param>
        T GetExportedValue<T>(string name);
    }
}