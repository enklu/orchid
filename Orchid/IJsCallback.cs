﻿namespace Enklu.Orchid
{
    /// <summary>
    /// This interface defines an implementation prototype for a callback function inside of the javascript
    /// runtime.
    /// </summary>
    public interface IJsCallback
    {
        /// <summary>
        /// This method invokes the javascript callback with the provided arguments. Invocation works identically
        /// to a function call in javascript. That is, you may pass fewer or more arguments than the function length.
        /// </summary>
        /// <param name="@this">The context in which the callback function is executed.</param>
        /// <param name="args">The objects to pass to the javascript function.</param>
        /// <returns>JavaScript functions with undefined returns will translate to null. Otherwise, a host object converted
        /// from the javascript callback return value.</returns>
        object Apply(object @this, params object[] args);

        /// <summary>
        /// This method invokes the javascript callback with the provided arguments. Invocation works identically
        /// to a function call in javascript. That is, you may pass fewer or more arguments than the function length.
        /// </summary>
        /// <param name="args">The objects to pass to the javascript function.</param>
        /// <returns>JavaScript functions with undefined returns will translate to null. Otherwise, a host object converted
        /// from the javascript callback return value.</returns>
        object Invoke(params object[] args);
    }
}