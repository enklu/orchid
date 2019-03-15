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

        /// <summary>
        /// The total number of optional parameters (not include var args).
        /// </summary>
        public int OptionalParameters { get; set; }

        /// <summary>
        /// Whether or not the parameters have variable length.
        /// </summary>
        public bool IsVarArgs { get; set; }

        /// <summary>
        /// The index of the var arg parameter.
        /// </summary>
        public int VarArgIndex => !IsVarArgs ? -1 : Parameters.Length - 1;

        /// <summary>
        /// The var arg underlying array element type.
        /// </summary>
        public Type VarArgType => !IsVarArgs ? null : Parameters[Parameters.Length - 1].ParameterType.GetElementType();
    }
}