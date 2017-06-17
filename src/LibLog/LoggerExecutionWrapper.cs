namespace Common.Log
{
    using System;
    using System.Diagnostics.CodeAnalysis;

    [ExcludeFromCodeCoverage]
    public class LoggerExecutionWrapper : ILog
    {
        private readonly Logger _logger;
        private readonly Func<bool> _getIsDisabled;
        public const string FailedToGenerateLogMessage = "Failed to generate log message";

        public LoggerExecutionWrapper(Logger logger, Func<bool> getIsDisabled = null)
        {
            _logger = logger;
            _getIsDisabled = getIsDisabled ?? (() => false);
        }

        public Logger WrappedLogger
        {
            get { return _logger; }
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        public bool Log(LogLevel logLevel, Func<string> messageFunc, Exception exception = null, params object[] formatParameters)
        {
            if (_getIsDisabled())
            {
                return false;
            }
            if (messageFunc == null)
            {
                return _logger(logLevel, null);
            }

            Func<string> wrappedMessageFunc = () =>
            {
                try
                {
                    return messageFunc();
                }
                catch (Exception ex)
                {
                    Log(LogLevel.Error, () => FailedToGenerateLogMessage, ex);
                }
                return null;
            };
            return _logger(logLevel, wrappedMessageFunc, exception, formatParameters);
        }
    }
}