namespace Common.Log.LogProviders
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq.Expressions;
    using System.Reflection;
    using Common.Log.LogProviders.Loggers;

    [ExcludeFromCodeCoverage]
    internal class NLogLogProvider : LogProviderBase
    {
        private readonly Func<string, object> _getLoggerByNameDelegate;
        private static bool s_providerIsAvailableOverride = true;

        [SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "LogManager")]
        [SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "NLog")]
        public NLogLogProvider()
        {
            if (!IsLoggerAvailable())
            {
                throw new InvalidOperationException("NLog.LogManager not found");
            }
            _getLoggerByNameDelegate = GetGetLoggerMethodCall();
        }

        public static bool ProviderIsAvailableOverride
        {
            get { return s_providerIsAvailableOverride; }
            set { s_providerIsAvailableOverride = value; }
        }

        public override Logger GetLogger(string name)
        {
            return new NLogLogger(_getLoggerByNameDelegate(name)).Log;
        }

        public static bool IsLoggerAvailable()
        {
            return ProviderIsAvailableOverride && GetLogManagerType() != null;
        }

        protected override OpenNdc GetOpenNdcMethod()
        {
            Type ndcContextType = Type.GetType("NLog.NestedDiagnosticsContext, NLog");
            MethodInfo pushMethod = ndcContextType.GetMethodPortable("Push", typeof(string));
            ParameterExpression messageParam = Expression.Parameter(typeof(string), "message");
            MethodCallExpression pushMethodCall = Expression.Call(null, pushMethod, messageParam);
            return Expression.Lambda<OpenNdc>(pushMethodCall, messageParam).Compile();
        }

        protected override OpenMdc GetOpenMdcMethod()
        {
            Type mdcContextType = Type.GetType("NLog.MappedDiagnosticsContext, NLog");

            MethodInfo setMethod = mdcContextType.GetMethodPortable("Set", typeof(string), typeof(string));
            MethodInfo removeMethod = mdcContextType.GetMethodPortable("Remove", typeof(string));
            ParameterExpression keyParam = Expression.Parameter(typeof(string), "key");
            ParameterExpression valueParam = Expression.Parameter(typeof(string), "value");

            MethodCallExpression setMethodCall = Expression.Call(null, setMethod, keyParam, valueParam);
            MethodCallExpression removeMethodCall = Expression.Call(null, removeMethod, keyParam);

            Action<string, string> set = Expression
                .Lambda<Action<string, string>>(setMethodCall, keyParam, valueParam)
                .Compile();
            Action<string> remove = Expression
                .Lambda<Action<string>>(removeMethodCall, keyParam)
                .Compile();

            return (key, value) =>
            {
                set(key, value);
                return new DisposableAction(() => remove(key));
            };
        }

        private static Type GetLogManagerType()
        {
            return Type.GetType("NLog.LogManager, NLog");
        }

        private static Func<string, object> GetGetLoggerMethodCall()
        {
            Type logManagerType = GetLogManagerType();
            MethodInfo method = logManagerType.GetMethodPortable("GetLogger", typeof(string));
            ParameterExpression nameParam = Expression.Parameter(typeof(string), "name");
            MethodCallExpression methodCall = Expression.Call(null, method, nameParam);
            return Expression.Lambda<Func<string, object>>(methodCall, nameParam).Compile();
        }
    }
}