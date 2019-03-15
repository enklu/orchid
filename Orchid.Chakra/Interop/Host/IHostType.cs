using System.Collections.Generic;

namespace Enklu.Orchid.Chakra.Interop
{
    /// <summary>
    /// A read-only view for the host type cache.
    /// </summary>
    public interface IHostType
    {
        /// <summary>
        /// Method Names
        /// </summary>
        IList<string> MethodNames { get; }

        /// <summary>
        /// Property Names
        /// </summary>
        IList<string> PropertyNames { get; }

        /// <summary>
        /// Field Names
        /// </summary>
        IList<string> FieldNames { get; }

        /// <summary>
        /// This method returns a list of method available that matches the name and the # of parameters.
        /// </summary>
        List<HostMethod> MethodsFor(string methodName, int totalParams);

        /// <summary>
        /// This method returns a <see cref="HostProperty"/> for the property name provided.
        /// </summary>
        HostProperty PropertyFor(string propertyName);

        /// <summary>
        /// This method returns a <see cref="HostField"/> for the property name provided.
        /// </summary>
        HostField FieldFor(string fieldName);
    }
}