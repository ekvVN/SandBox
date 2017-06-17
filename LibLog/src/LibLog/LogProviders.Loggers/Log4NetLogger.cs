namespace Common.Log.LogProviders.Loggers
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;

    [ExcludeFromCodeCoverage]
    public class Log4NetLogger
    {
        private readonly dynamic _logger;
        private static Type s_callerStackBoundaryType;
        private static readonly object _callerStackBoundaryTypeSync = new object();

        private readonly object _levelDebug;
        private readonly object _levelInfo;
        private readonly object _levelWarn;
        private readonly object _levelError;
        private readonly object _levelFatal;
        private readonly Func<object, object, bool> _isEnabledForDelegate;
        private readonly Action<object, object> _logDelegate;
        private readonly Func<object, Type, object, string, Exception, object> _createLoggingEvent;
        private readonly Action<object, string, object> _loggingEventPropertySetter;

        [SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "ILogger")]
        public Log4NetLogger(dynamic logger)
        {
            _logger = logger.Logger;

            var logEventLevelType = Type.GetType("log4net.Core.Level, log4net");
            if (logEventLevelType == null)
            {
                throw new InvalidOperationException("Type log4net.Core.Level was not found.");
            }

            var levelFields = logEventLevelType.GetFieldsPortable().ToList();
            _levelDebug = levelFields.First(x => x.Name == "Debug").GetValue(null);
            _levelInfo = levelFields.First(x => x.Name == "Info").GetValue(null);
            _levelWarn = levelFields.First(x => x.Name == "Warn").GetValue(null);
            _levelError = levelFields.First(x => x.Name == "Error").GetValue(null);
            _levelFatal = levelFields.First(x => x.Name == "Fatal").GetValue(null);

            // Func<object, object, bool> isEnabledFor = (logger, level) => { return ((log4net.Core.ILogger)logger).IsEnabled(level); }
            var loggerType = Type.GetType("log4net.Core.ILogger, log4net");
            if (loggerType == null)
            {
                throw new InvalidOperationException("Type log4net.Core.ILogger, was not found.");
            }
            var instanceParam = Expression.Parameter(typeof(object));
            var instanceCast = Expression.Convert(instanceParam, loggerType);
            var levelParam = Expression.Parameter(typeof(object));
            var levelCast = Expression.Convert(levelParam, logEventLevelType);
            _isEnabledForDelegate = GetIsEnabledFor(loggerType, logEventLevelType, instanceCast, levelCast, instanceParam, levelParam);

            var loggingEventType = Type.GetType("log4net.Core.LoggingEvent, log4net");

            _createLoggingEvent = GetCreateLoggingEvent(instanceParam, instanceCast, levelParam, levelCast, loggingEventType);

            _logDelegate = GetLogDelegate(loggerType, loggingEventType, instanceCast, instanceParam);

            _loggingEventPropertySetter = GetLoggingEventPropertySetter(loggingEventType);
        }

        private static Action<object, object> GetLogDelegate(Type loggerType, Type loggingEventType, UnaryExpression instanceCast,
            ParameterExpression instanceParam)
        {
            //Action<object, object, string, Exception> Log =
            //(logger, callerStackBoundaryDeclaringType, level, message, exception) => { ((ILogger)logger).Log(new LoggingEvent(callerStackBoundaryDeclaringType, logger.Repository, logger.Name, level, message, exception)); }
            var writeExceptionMethodInfo = loggerType.GetMethodPortable("Log",
                loggingEventType);

            var loggingEventParameter = Expression.Parameter(typeof(object), "loggingEvent");
            var loggingEventCasted = Expression.Convert(loggingEventParameter, loggingEventType);

            var writeMethodExp = Expression.Call(
                instanceCast,
                writeExceptionMethodInfo,
                loggingEventCasted);

            var logDelegate = Expression.Lambda<Action<object, object>>(
                writeMethodExp,
                instanceParam,
                loggingEventParameter).Compile();

            return logDelegate;
        }

        private static Func<object, Type, object, string, Exception, object> GetCreateLoggingEvent(ParameterExpression instanceParam,
            UnaryExpression instanceCast, ParameterExpression levelParam, UnaryExpression levelCast, Type loggingEventType)
        {
            var callerStackBoundaryDeclaringTypeParam = Expression.Parameter(typeof(Type));
            var messageParam = Expression.Parameter(typeof(string));
            var exceptionParam = Expression.Parameter(typeof(Exception));

            var repositoryProperty = loggingEventType.GetPropertyPortable("Repository");
            var levelProperty = loggingEventType.GetPropertyPortable("Level");

            var loggingEventConstructor = loggingEventType.GetConstructorPortable(typeof(Type), repositoryProperty.PropertyType,
                typeof(string), levelProperty.PropertyType, typeof(object), typeof(Exception));

            //Func<object, object, string, Exception, object> Log =
            //(logger, callerStackBoundaryDeclaringType, level, message, exception) => new LoggingEvent(callerStackBoundaryDeclaringType, ((ILogger)logger).Repository, ((ILogger)logger).Name, (Level)level, message, exception); }
            var newLoggingEventExpression = Expression
                .New(loggingEventConstructor,
                    callerStackBoundaryDeclaringTypeParam,
                    Expression.Property(instanceCast, "Repository"),
                    Expression.Property(instanceCast, "Name"),
                    levelCast,
                    messageParam,
                    exceptionParam);

            var createLoggingEvent = Expression
                .Lambda<Func<object, Type, object, string, Exception, object>>(
                    newLoggingEventExpression,
                    instanceParam,
                    callerStackBoundaryDeclaringTypeParam,
                    levelParam,
                    messageParam,
                    exceptionParam)
                .Compile();

            return createLoggingEvent;
        }

        private static Func<object, object, bool> GetIsEnabledFor(Type loggerType, Type logEventLevelType,
            UnaryExpression instanceCast,
            UnaryExpression levelCast,
            ParameterExpression instanceParam,
            ParameterExpression levelParam)
        {
            var isEnabledMethodInfo = loggerType.GetMethodPortable("IsEnabledFor", logEventLevelType);
            var isEnabledMethodCall = Expression.Call(instanceCast, isEnabledMethodInfo, levelCast);

            var result = Expression
                .Lambda<Func<object, object, bool>>(isEnabledMethodCall, instanceParam, levelParam)
                .Compile();

            return result;
        }

        private static Action<object, string, object> GetLoggingEventPropertySetter(Type loggingEventType)
        {
            var loggingEventParameter = Expression.Parameter(typeof(object), "loggingEvent");
            var keyParameter = Expression.Parameter(typeof(string), "key");
            var valueParameter = Expression.Parameter(typeof(object), "value");

            var propertiesProperty = loggingEventType.GetPropertyPortable("Properties");
            var item = propertiesProperty.PropertyType.GetPropertyPortable("Item");

            // ((LoggingEvent)loggingEvent).Properties[key] = value;
            var body =
                Expression.Assign(
                    Expression.Property(
                        Expression.Property(Expression.Convert(loggingEventParameter, loggingEventType),
                            propertiesProperty), item, keyParameter), valueParameter);

            var result = Expression
                .Lambda<Action<object, string, object>>(body, loggingEventParameter, keyParameter, valueParameter)
                .Compile();

            return result;
        }

        public bool Log(LogLevel logLevel, Func<string> messageFunc, Exception exception, params object[] formatParameters)
        {
            if (messageFunc == null)
            {
                return IsLogLevelEnable(logLevel);
            }

            if (!IsLogLevelEnable(logLevel))
            {
                return false;
            }

            var message = messageFunc();

            IEnumerable<string> patternMatches;

            var formattedMessage = LogMessageFormatter.FormatStructuredMessage(message, formatParameters, out patternMatches);

            // determine correct caller - this might change due to jit optimizations with method inlining
            if (s_callerStackBoundaryType == null)
            {
                lock (_callerStackBoundaryTypeSync)
                {
                    var stack = new StackTrace();
                    var thisType = GetType();
                    s_callerStackBoundaryType = Type.GetType("LoggerExecutionWrapper");
                    for (var i = 1; i < stack.FrameCount; i++)
                    {
                        if (!IsInTypeHierarchy(thisType, stack.GetFrame(i).GetMethod().DeclaringType))
                        {
                            s_callerStackBoundaryType = stack.GetFrame(i - 1).GetMethod().DeclaringType;
                            break;
                        }
                    }
                }
            }

            var translatedLevel = TranslateLevel(logLevel);

            object loggingEvent = _createLoggingEvent(_logger, s_callerStackBoundaryType, translatedLevel, formattedMessage, exception);

            PopulateProperties(loggingEvent, patternMatches, formatParameters);

            _logDelegate(_logger, loggingEvent);

            return true;
        }

        private void PopulateProperties(object loggingEvent, IEnumerable<string> patternMatches, object[] formatParameters)
        {
            var keyToValue = patternMatches.Zip(formatParameters,
                (key, value) => new KeyValuePair<string, object>(key, value));

            foreach (KeyValuePair<string, object> keyValuePair in keyToValue)
            {
                _loggingEventPropertySetter(loggingEvent, keyValuePair.Key, keyValuePair.Value);
            }
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

        private bool IsLogLevelEnable(LogLevel logLevel)
        {
            var level = TranslateLevel(logLevel);
            return _isEnabledForDelegate(_logger, level);
        }

        private object TranslateLevel(LogLevel logLevel)
        {
            switch (logLevel)
            {
                case LogLevel.Trace:
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