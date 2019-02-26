using System;
using System.Reflection;

namespace Enklu.Orchid.Chakra.Interop
{
    /// <summary>
    /// A getter/setter property cache entry.
    /// </summary>
    public class HostProperty
    {
        /// <summary>
        /// The <see cref="MethodInfo"/> representing the getter.
        /// </summary>
        public MethodInfo Getter { get; set; }

        /// <summary>
        /// The <see cref="MethodInfo"/> representing the setter.
        /// </summary>
        public MethodInfo Setter { get; set; }

        /// <summary>
        /// The underlying property <see cref="Type"/>.
        /// </summary>
        public Type PropertyType { get; set; }
    }
}