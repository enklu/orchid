namespace Enklu.Orchid.Logging
{
    /// <summary>
    /// No-Op log adapter.
    /// </summary>
    public class NoOpLogAdapter : ILogAdapter
    {
        /// <inheritdoc/>
        public void Debug(object caller, object message, params object[] replacements)
        { }

        /// <inheritdoc/>
        public void Info(object caller, object message, params object[] replacements)
        { }

        /// <inheritdoc/>
        public void Warning(object caller, object message, params object[] replacements)
        { }

        /// <inheritdoc/>
        public void Error(object caller, object message, params object[] replacements)
        { }

        /// <inheritdoc/>
        public void Fatal(object caller, object message, params object[] replacements)
        { }
    }

    /// <summary>
    /// Static logging utility which leverages external logging adapters.
    /// </summary>
    public static class Log
    {
        /// <summary>
        /// Default logging adapter to no-op
        /// </summary>
        private static ILogAdapter _log = new NoOpLogAdapter();

        /// <summary>
        /// Updates the logging adapter.
        /// </summary>
        public static void SetAdapter(ILogAdapter adapter)
        {
            if (null == adapter)
            {
                adapter = new NoOpLogAdapter();
            }

            _log = adapter;
        }

        /// <inheritdoc/>
        public static void Debug(object caller, object message, params object[] replacements)
            => _log.Debug(caller, message, replacements);

        /// <inheritdoc/>
        public static void Info(object caller, object message, params object[] replacements)
            => _log.Info(caller, message, replacements);

        /// <inheritdoc/>
        public static void Warning(object caller, object message, params object[] replacements)
            => _log.Warning(caller, message, replacements);

        /// <inheritdoc/>
        public static void Error(object caller, object message, params object[] replacements)
            => _log.Error(caller, message, replacements);

        /// <inheritdoc/>
        public static void Fatal(object caller, object message, params object[] replacements)
            => _log.Fatal(caller, message, replacements);
    }
}