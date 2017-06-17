namespace LibLog.Logging.LogProviders
{
    using System;
    using Common.Log;
    using Shouldly;

    internal static class LogExtensions
    {
        public static void AssertCanCheckLogLevelsEnabled(this ILog logger)
        {
            var loglevelEnabledActions = new Action[]
            {
                () => logger.IsTraceEnabled(),
                () => logger.IsDebugEnabled(),
                () => logger.IsInfoEnabled(),
                () => logger.IsWarnEnabled(),
                () => logger.IsErrorEnabled(),
                () => logger.IsFatalEnabled(),
            };

            foreach (var isLogLevelEnabled in loglevelEnabledActions)
            {
                isLogLevelEnabled.ShouldNotThrow();
            }
        }
    }
}