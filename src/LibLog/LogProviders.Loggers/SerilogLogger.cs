namespace Common.Log.LogProviders.Loggers
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq.Expressions;
    using System.Reflection;

    [ExcludeFromCodeCoverage]
    public class SerilogLogger
    {
        private readonly object _logger;
        private static readonly object _debugLevel;
        private static readonly object _errorLevel;
        private static readonly object _fatalLevel;
        private static readonly object _informationLevel;
        private static readonly object _verboseLevel;
        private static readonly object _warningLevel;
        private static readonly Func<object, object, bool> _isEnabled;
        private static readonly Action<object, object, string, object[]> _write;
        private static readonly Action<object, object, Exception, string, object[]> _writeException;

        [SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations")]
        [SuppressMessage("Microsoft.Performance", "CA1810:InitializeReferenceTypeStaticFieldsInline")]
        [SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "ILogger")]
        [SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "LogEventLevel")]
        [SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "Serilog")]
        static SerilogLogger()
        {
            var logEventLevelType = Type.GetType("Serilog.Events.LogEventLevel, Serilog");
            if (logEventLevelType == null)
            {
                throw new InvalidOperationException("Type Serilog.Events.LogEventLevel was not found.");
            }
            _debugLevel = Enum.Parse(logEventLevelType, "Debug", false);
            _errorLevel = Enum.Parse(logEventLevelType, "Error", false);
            _fatalLevel = Enum.Parse(logEventLevelType, "Fatal", false);
            _informationLevel = Enum.Parse(logEventLevelType, "Information", false);
            _verboseLevel = Enum.Parse(logEventLevelType, "Verbose", false);
            _warningLevel = Enum.Parse(logEventLevelType, "Warning", false);

            // Func<object, object, bool> isEnabled = (logger, level) => { return ((SeriLog.ILogger)logger).IsEnabled(level); }
            var loggerType = Type.GetType("Serilog.ILogger, Serilog");
            if (loggerType == null)
            {
                throw new InvalidOperationException("Type Serilog.ILogger was not found.");
            }
            MethodInfo isEnabledMethodInfo = loggerType.GetMethodPortable("IsEnabled", logEventLevelType);
            ParameterExpression instanceParam = Expression.Parameter(typeof(object));
            UnaryExpression instanceCast = Expression.Convert(instanceParam, loggerType);
            ParameterExpression levelParam = Expression.Parameter(typeof(object));
            UnaryExpression levelCast = Expression.Convert(levelParam, logEventLevelType);
            MethodCallExpression isEnabledMethodCall = Expression.Call(instanceCast, isEnabledMethodInfo, levelCast);
            _isEnabled = Expression.Lambda<Func<object, object, bool>>(isEnabledMethodCall, instanceParam, levelParam).Compile();

            // Action<object, object, string> Write =
            // (logger, level, message, params) => { ((SeriLog.ILoggerILogger)logger).Write(level, message, params); }
            MethodInfo writeMethodInfo = loggerType.GetMethodPortable("Write", logEventLevelType, typeof(string), typeof(object[]));
            ParameterExpression messageParam = Expression.Parameter(typeof(string));
            ParameterExpression propertyValuesParam = Expression.Parameter(typeof(object[]));
            MethodCallExpression writeMethodExp = Expression.Call(
                instanceCast,
                writeMethodInfo,
                levelCast,
                messageParam,
                propertyValuesParam);
            var expression = Expression.Lambda<Action<object, object, string, object[]>>(
                writeMethodExp,
                instanceParam,
                levelParam,
                messageParam,
                propertyValuesParam);
            _write = expression.Compile();

            // Action<object, object, string, Exception> WriteException =
            // (logger, level, exception, message) => { ((ILogger)logger).Write(level, exception, message, new object[]); }
            MethodInfo writeExceptionMethodInfo = loggerType.GetMethodPortable("Write",
                logEventLevelType,
                typeof(Exception),
                typeof(string),
                typeof(object[]));
            ParameterExpression exceptionParam = Expression.Parameter(typeof(Exception));
            writeMethodExp = Expression.Call(
                instanceCast,
                writeExceptionMethodInfo,
                levelCast,
                exceptionParam,
                messageParam,
                propertyValuesParam);
            _writeException = Expression.Lambda<Action<object, object, Exception, string, object[]>>(
                writeMethodExp,
                instanceParam,
                levelParam,
                exceptionParam,
                messageParam,
                propertyValuesParam).Compile();
        }

        public SerilogLogger(object logger)
        {
            _logger = logger;
        }

        public bool Log(LogLevel logLevel, Func<string> messageFunc, Exception exception, params object[] formatParameters)
        {
            var translatedLevel = TranslateLevel(logLevel);
            if (messageFunc == null)
            {
                return _isEnabled(_logger, translatedLevel);
            }

            if (!_isEnabled(_logger, translatedLevel))
            {
                return false;
            }

            if (exception != null)
            {
                LogException(translatedLevel, messageFunc, exception, formatParameters);
            }
            else
            {
                LogMessage(translatedLevel, messageFunc, formatParameters);
            }

            return true;
        }

        private void LogMessage(object translatedLevel, Func<string> messageFunc, object[] formatParameters)
        {
            _write(_logger, translatedLevel, messageFunc(), formatParameters);
        }

        private void LogException(object logLevel, Func<string> messageFunc, Exception exception, object[] formatParams)
        {
            _writeException(_logger, logLevel, exception, messageFunc(), formatParams);
        }

        private static object TranslateLevel(LogLevel logLevel)
        {
            switch (logLevel)
            {
                case LogLevel.Fatal:
                    return _fatalLevel;
                case LogLevel.Error:
                    return _errorLevel;
                case LogLevel.Warn:
                    return _warningLevel;
                case LogLevel.Info:
                    return _informationLevel;
                case LogLevel.Trace:
                    return _verboseLevel;
                default:
                    return _debugLevel;
            }
        }
    }
}