namespace Common.Log.LogProviders.Loggers
{
    using System;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;

    [ExcludeFromCodeCoverage]
    public class NLogLogger
    {
        private readonly dynamic _logger;

        private static readonly Func<string, object, string, Exception, object> _logEventInfoFact;

        private static readonly object _levelTrace;
        private static readonly object _levelDebug;
        private static readonly object _levelInfo;
        private static readonly object _levelWarn;
        private static readonly object _levelError;
        private static readonly object _levelFatal;

        static NLogLogger()
        {
            try
            {
                var logEventLevelType = Type.GetType("NLog.LogLevel, NLog");
                if (logEventLevelType == null)
                {
                    throw new InvalidOperationException("Type NLog.LogLevel was not found.");
                }

                var levelFields = logEventLevelType.GetFieldsPortable().ToList();
                _levelTrace = levelFields.First(x => x.Name == "Trace").GetValue(null);
                _levelDebug = levelFields.First(x => x.Name == "Debug").GetValue(null);
                _levelInfo = levelFields.First(x => x.Name == "Info").GetValue(null);
                _levelWarn = levelFields.First(x => x.Name == "Warn").GetValue(null);
                _levelError = levelFields.First(x => x.Name == "Error").GetValue(null);
                _levelFatal = levelFields.First(x => x.Name == "Fatal").GetValue(null);

                var logEventInfoType = Type.GetType("NLog.LogEventInfo, NLog");
                if (logEventInfoType == null)
                {
                    throw new InvalidOperationException("Type NLog.LogEventInfo was not found.");
                }
                MethodInfo createLogEventInfoMethodInfo = logEventInfoType.GetMethodPortable("Create",
                    logEventLevelType, typeof(string), typeof(Exception), typeof(IFormatProvider), typeof(string), typeof(object[]));
                ParameterExpression loggerNameParam = Expression.Parameter(typeof(string));
                ParameterExpression levelParam = Expression.Parameter(typeof(object));
                ParameterExpression messageParam = Expression.Parameter(typeof(string));
                ParameterExpression exceptionParam = Expression.Parameter(typeof(Exception));
                UnaryExpression levelCast = Expression.Convert(levelParam, logEventLevelType);
                MethodCallExpression createLogEventInfoMethodCall = Expression.Call(null,
                    createLogEventInfoMethodInfo,
                    levelCast, loggerNameParam, exceptionParam,
                    Expression.Constant(null, typeof(IFormatProvider)), messageParam, Expression.Constant(null, typeof(object[])));
                _logEventInfoFact = Expression.Lambda<Func<string, object, string, Exception, object>>(createLogEventInfoMethodCall,
                    loggerNameParam, levelParam, messageParam, exceptionParam).Compile();
            }
            catch
            {
                // ignored
            }
        }

        public NLogLogger(dynamic logger)
        {
            _logger = logger;
        }

        [SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        public bool Log(LogLevel logLevel, Func<string> messageFunc, Exception exception, params object[] formatParameters)
        {
            if (messageFunc == null)
            {
                return IsLogLevelEnable(logLevel);
            }
            messageFunc = LogMessageFormatter.SimulateStructuredLogging(messageFunc, formatParameters);

            if (_logEventInfoFact != null)
            {
                if (IsLogLevelEnable(logLevel))
                {
                    var nlogLevel = TranslateLevel(logLevel);

                    StackTrace stack = new StackTrace();
                    Type thisType = GetType();
                    Type knownType0 = typeof(LoggerExecutionWrapper);
                    Type knownType1 = typeof(LogExtensions);
                    //Maybe inline, so we may can't found any LibLog classes in stack
                    Type callerStackBoundaryType = null;
                    for (var i = 0; i < stack.FrameCount; i++)
                    {
                        var declaringType = stack.GetFrame(i).GetMethod().DeclaringType;
                        if (!IsInTypeHierarchy(thisType, declaringType) &&
                            !IsInTypeHierarchy(knownType0, declaringType) &&
                            !IsInTypeHierarchy(knownType1, declaringType))
                        {
                            if (i > 1)
                                callerStackBoundaryType = stack.GetFrame(i - 1).GetMethod().DeclaringType;
                            break;
                        }
                    }

                    if (callerStackBoundaryType != null)
                        _logger.Log(callerStackBoundaryType, _logEventInfoFact(_logger.Name, nlogLevel, messageFunc(), exception));
                    else
                        _logger.Log(_logEventInfoFact(_logger.Name, nlogLevel, messageFunc(), exception));
                    return true;
                }
                return false;
            }

            if (exception != null)
            {
                return LogException(logLevel, messageFunc, exception);
            }
            switch (logLevel)
            {
                case LogLevel.Debug:
                    if (_logger.IsDebugEnabled)
                    {
                        _logger.Debug(messageFunc());
                        return true;
                    }
                    break;
                case LogLevel.Info:
                    if (_logger.IsInfoEnabled)
                    {
                        _logger.Info(messageFunc());
                        return true;
                    }
                    break;
                case LogLevel.Warn:
                    if (_logger.IsWarnEnabled)
                    {
                        _logger.Warn(messageFunc());
                        return true;
                    }
                    break;
                case LogLevel.Error:
                    if (_logger.IsErrorEnabled)
                    {
                        _logger.Error(messageFunc());
                        return true;
                    }
                    break;
                case LogLevel.Fatal:
                    if (_logger.IsFatalEnabled)
                    {
                        _logger.Fatal(messageFunc());
                        return true;
                    }
                    break;
                default:
                    if (_logger.IsTraceEnabled)
                    {
                        _logger.Trace(messageFunc());
                        return true;
                    }
                    break;
            }
            return false;
        }

        private static bool IsInTypeHierarchy(Type currentType, Type checkType)
        {
            while (currentType != null && currentType != typeof(object))
            {
                if (currentType == checkType)
                {
                    return true;
                }
                currentType = currentType.GetBaseTypePortable();
            }
            return false;
        }

        [SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        private bool LogException(LogLevel logLevel, Func<string> messageFunc, Exception exception)
        {
            switch (logLevel)
            {
                case LogLevel.Debug:
                    if (_logger.IsDebugEnabled)
                    {
                        _logger.DebugException(messageFunc(), exception);
                        return true;
                    }
                    break;
                case LogLevel.Info:
                    if (_logger.IsInfoEnabled)
                    {
                        _logger.InfoException(messageFunc(), exception);
                        return true;
                    }
                    break;
                case LogLevel.Warn:
                    if (_logger.IsWarnEnabled)
                    {
                        _logger.WarnException(messageFunc(), exception);
                        return true;
                    }
                    break;
                case LogLevel.Error:
                    if (_logger.IsErrorEnabled)
                    {
                        _logger.ErrorException(messageFunc(), exception);
                        return true;
                    }
                    break;
                case LogLevel.Fatal:
                    if (_logger.IsFatalEnabled)
                    {
                        _logger.FatalException(messageFunc(), exception);
                        return true;
                    }
                    break;
                default:
                    if (_logger.IsTraceEnabled)
                    {
                        _logger.TraceException(messageFunc(), exception);
                        return true;
                    }
                    break;
            }
            return false;
        }

        private bool IsLogLevelEnable(LogLevel logLevel)
        {
            switch (logLevel)
            {
                case LogLevel.Debug:
                    return _logger.IsDebugEnabled;
                case LogLevel.Info:
                    return _logger.IsInfoEnabled;
                case LogLevel.Warn:
                    return _logger.IsWarnEnabled;
                case LogLevel.Error:
                    return _logger.IsErrorEnabled;
                case LogLevel.Fatal:
                    return _logger.IsFatalEnabled;
                default:
                    return _logger.IsTraceEnabled;
            }
        }

        private object TranslateLevel(LogLevel logLevel)
        {
            switch (logLevel)
            {
                case LogLevel.Trace:
                    return _levelTrace;
                case LogLevel.Debug:
                    return _levelDebug;
                case LogLevel.Info:
                    return _levelInfo;
                case LogLevel.Warn:
                    return _levelWarn;
                case LogLevel.Error:
                    return _levelError;
                case LogLevel.Fatal:
                    return _levelFatal;
                default:
                    throw new ArgumentOutOfRangeException("logLevel", logLevel, null);
            }
        }
    }
}