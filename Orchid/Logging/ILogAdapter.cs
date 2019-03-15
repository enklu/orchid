namespace Enklu.Orchid.Logging
{
    /// <summary>
    /// Orchid log adapter contract which provides a way to hook in external logging support.
    /// </summary>
    public interface ILogAdapter
    {
        /// <summary>
        /// Debug log.
        /// </summary>
        void Debug(object caller, object message, params object[] replacements);

        /// <summary>
        /// Info log.
        /// </summary>
        void Info(object caller, object message, params object[] replacements);

        /// <summary>
        /// Warning log.
        /// </summary>
        void Warning(object caller, object message, params object[] replacements);

        /// <summary>
        /// Error log.
        /// </summary>
        void Error(object caller, object message, params object[] replacements);

        /// <summary>
        /// Fatal log.
        /// </summary>
        void Fatal(object caller, object message, params object[] replacements);
    }
}