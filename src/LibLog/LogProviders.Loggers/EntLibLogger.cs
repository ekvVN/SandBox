namespace Common.Log.LogProviders.Loggers
{
    using System;
    using System.Diagnostics.CodeAnalysis;

    [ExcludeFromCodeCoverage]
    public class EntLibLogger
    {
        private readonly string _loggerName;
        private readonly Action<string, string, int> _writeLog;
        private readonly Func<string, int, bool> _shouldLog;

        public EntLibLogger(string loggerName, Action<string, string, int> writeLog, Func<string, int, bool> shouldLog)
        {
            _loggerName = loggerName;
            _writeLog = writeLog;
            _shouldLog = shouldLog;
        }

        public bool Log(LogLevel logLevel, Func<string> messageFunc, Exception exception, params object[] formatParameters)
        {
            var severity = MapSeverity(logLevel);
            if (messageFunc == null)
            {
                return _shouldLog(_loggerName, severity);
            }


            messageFunc = LogMessageFormatter.SimulateStructuredLogging(messageFunc, formatParameters);
            if (exception != null)
            {
                return LogException(logLevel, messageFunc, exception);
            }
            _writeLog(_loggerName, messageFunc(), severity);
            return true;
        }

        public bool LogException(LogLevel logLevel, Func<string> messageFunc, Exception exception)
        {
            var severity = MapSeverity(logLevel);
            var message = messageFunc() + Environment.NewLine + exception;
            _writeLog(_loggerName, message, severity);
            return true;
        }

        private static int MapSeverity(LogLevel logLevel)
        {
            switch (logLevel)
            {
                case LogLevel.Fatal:
                    return TraceEventTypeValues.Critical;
                case LogLevel.Error:
                    return TraceEventTypeValues.Error;
                case LogLevel.Warn:
                    return TraceEventTypeValues.Warning;
                case LogLevel.Info:
                    return TraceEventTypeValues.Information;
                default:
                    return TraceEventTypeValues.Verbose;
            }
        }
    }
}