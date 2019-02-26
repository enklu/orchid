using System.Reflection;

namespace Enklu.Orchid.Chakra.Interop
{
    /// <summary>
    /// A <see cref="FieldInfo"/> cache entry.
    /// </summary>
    public class HostField
    {
        /// <summary>
        /// Contains the <see cref="FieldInfo"/> for a specific type.
        /// </summary>
        public FieldInfo Field { get; set; }
    }
}