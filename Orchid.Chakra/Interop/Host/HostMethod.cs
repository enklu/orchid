using System;
using System.Reflection;

namespace Enklu.Orchid.Chakra.Interop
{
    /// <summary>
    /// A <see cref="MethodInfo"/> cache entry.
    /// </summary>
    public class HostMethod
    {
        /// <summary>
        /// The <see cref="MethodInfo"/> instance that can be used to invoke with an instance.
        /// </summary>
        public MethodInfo Method { get; set; }

        /// <summary>
        /// The <see cref="ParameterInfo"/> array used to call the method.
        /// </summary>
        public ParameterInfo[] Parameters { get; set; }

        /// <summary>
        /// The return type for the method invocation.
        /// </summary>
        public Type ReturnType { get; set; }
    }
}