namespace Common.Log.LogProviders.Loggers
{
    using System;
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// The form of the Loupe Log.Write method we're using
    /// </summary>
    internal delegate void WriteDelegate(
        int severity,
        string logSystem,
        int skipFrames,
        Exception exception,
        bool attributeToException,
        int writeMode,
        string detailsXml,
        string category,
        string caption,
        string description,
        params object[] args
        );

    [ExcludeFromCodeCoverage]
    internal class LoupeLogger
    {
        private const string LogSystem = "LibLog";

        private readonly string _category;
        private readonly WriteDelegate _logWriteDelegate;
        private readonly int _skipLevel;

        internal LoupeLogger(string category, WriteDelegate logWriteDelegate)
        {
            _category = category;
            _logWriteDelegate = logWriteDelegate;
            _skipLevel = 2;
        }

        public bool Log(LogLevel logLevel, Func<string> messageFunc, Exception exception, params object[] formatParameters)
        {
            if (messageFunc == null)
            {
                //nothing to log..
                return true;
            }

            messageFunc = LogMessageFormatter.SimulateStructuredLogging(messageFunc, formatParameters);

            _logWriteDelegate(ToLogMessageSeverity(logLevel), LogSystem, _skipLevel, exception, true, 0, null,
                _category, null, messageFunc.Invoke());

            return true;
        }

        private static int ToLogMessageSeverity(LogLevel logLevel)
        {
            switch (logLevel)
            {
                case LogLevel.Trace:
                    return TraceEventTypeValues.Verbose;
                case LogLevel.Debug:
                    return TraceEventTypeValues.Verbose;
                case LogLevel.Info:
                    return TraceEventTypeValues.Information;
                case LogLevel.Warn:
                    return TraceEventTypeValues.Warning;
                case LogLevel.Error:
                    return TraceEventTypeValues.Error;
                case LogLevel.Fatal:
                    return TraceEventTypeValues.Critical;
                default:
                    throw new ArgumentOutOfRangeException("logLevel");
            }
        }
    }
}