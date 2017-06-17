namespace Common.Log.LogProviders
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Linq.Expressions;
    using Common.Log.LogProviders.Loggers;

    [ExcludeFromCodeCoverage]
    public class EntLibLogProvider : LogProviderBase
    {
        private const string TypeTemplate = "Microsoft.Practices.EnterpriseLibrary.Logging.{0}, Microsoft.Practices.EnterpriseLibrary.Logging";
        private static bool s_providerIsAvailableOverride = true;
        private static readonly Type _logEntryType;
        private static readonly Type _loggerType;
        private static readonly Type _traceEventTypeType;
        private static readonly Action<string, string, int> _writeLogEntry;
        private static readonly Func<string, int, bool> _shouldLogEntry;

        [SuppressMessage("Microsoft.Performance", "CA1810:InitializeReferenceTypeStaticFieldsInline")]
        static EntLibLogProvider()
        {
            _logEntryType = Type.GetType(string.Format(CultureInfo.InvariantCulture, TypeTemplate, "LogEntry"));
            _loggerType = Type.GetType(string.Format(CultureInfo.InvariantCulture, TypeTemplate, "Logger"));
            _traceEventTypeType = TraceEventTypeValues.Type;
            if (_logEntryType == null
                || _traceEventTypeType == null
                || _loggerType == null)
            {
                return;
            }
            _writeLogEntry = GetWriteLogEntry();
            _shouldLogEntry = GetShouldLogEntry();
        }

        [SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "EnterpriseLibrary")]
        public EntLibLogProvider()
        {
            if (!IsLoggerAvailable())
            {
                throw new InvalidOperationException("Microsoft.Practices.EnterpriseLibrary.Logging.Logger not found");
            }
        }

        public static bool ProviderIsAvailableOverride
        {
            get { return s_providerIsAvailableOverride; }
            set { s_providerIsAvailableOverride = value; }
        }

        public override Logger GetLogger(string name)
        {
            return new EntLibLogger(name, _writeLogEntry, _shouldLogEntry).Log;
        }

        public static bool IsLoggerAvailable()
        {
            return ProviderIsAvailableOverride
                   && _traceEventTypeType != null
                   && _logEntryType != null;
        }

        private static Action<string, string, int> GetWriteLogEntry()
        {
            // new LogEntry(...)
            var logNameParameter = Expression.Parameter(typeof(string), "logName");
            var messageParameter = Expression.Parameter(typeof(string), "message");
            var severityParameter = Expression.Parameter(typeof(int), "severity");

            var memberInit = GetWriteLogExpression(
                messageParameter,
                Expression.Convert(severityParameter, _traceEventTypeType),
                logNameParameter);

            //Logger.Write(new LogEntry(....));
            var writeLogEntryMethod = _loggerType.GetMethodPortable("Write", _logEntryType);
            var writeLogEntryExpression = Expression.Call(writeLogEntryMethod, memberInit);

            return Expression.Lambda<Action<string, string, int>>(
                writeLogEntryExpression,
                logNameParameter,
                messageParameter,
                severityParameter).Compile();
        }

        private static Func<string, int, bool> GetShouldLogEntry()
        {
            // new LogEntry(...)
            var logNameParameter = Expression.Parameter(typeof(string), "logName");
            var severityParameter = Expression.Parameter(typeof(int), "severity");

            var memberInit = GetWriteLogExpression(
                Expression.Constant("***dummy***"),
                Expression.Convert(severityParameter, _traceEventTypeType),
                logNameParameter);

            //Logger.Write(new LogEntry(....));
            var writeLogEntryMethod = _loggerType.GetMethodPortable("ShouldLog", _logEntryType);
            var writeLogEntryExpression = Expression.Call(writeLogEntryMethod, memberInit);

            return Expression
                .Lambda<Func<string, int, bool>>(
                    writeLogEntryExpression,
                    logNameParameter,
                    severityParameter)
                .Compile();
        }

        private static MemberInitExpression GetWriteLogExpression(Expression message,
            Expression severityParameter, ParameterExpression logNameParameter)
        {
            var entryType = _logEntryType;
            var memberInit = Expression.MemberInit(Expression.New(entryType),
                Expression.Bind(entryType.GetPropertyPortable("Message"), message),
                Expression.Bind(entryType.GetPropertyPortable("Severity"), severityParameter),
                Expression.Bind(
                    entryType.GetPropertyPortable("TimeStamp"),
                    Expression.Property(null, typeof(DateTime).GetPropertyPortable("UtcNow"))),
                Expression.Bind(
                    entryType.GetPropertyPortable("Categories"),
                    Expression.ListInit(
                        Expression.New(typeof(List<string>)),
                        typeof(List<string>).GetMethodPortable("Add", typeof(string)),
                        logNameParameter)));
            return memberInit;
        }
    }
}