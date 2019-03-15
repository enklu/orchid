using System;

namespace Enklu.Orchid
{
    /// <summary>
    /// Restricts method from being called by Js.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field)]
    public class DenyJsAccess : Attribute { }
}