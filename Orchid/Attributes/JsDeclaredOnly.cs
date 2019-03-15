using System;

namespace Enklu.Orchid
{
    /// <summary>
    /// This attribute type is used to denote JS exposure limited only to the methods, properties, and
    /// fields declared in the class. That is, do not expose any methods, properties, and fields on a
    /// subclass.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
    public class JsDeclaredOnly : Attribute { }
}