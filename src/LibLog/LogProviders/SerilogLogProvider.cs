namespace Common.Log.LogProviders
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq.Expressions;
    using System.Reflection;
    using Common.Log.LogProviders.Loggers;

    [ExcludeFromCodeCoverage]
    internal class SerilogLogProvider : LogProviderBase
    {
        private readonly Func<string, object> _getLoggerByNameDelegate;
        private static bool s_providerIsAvailableOverride = true;

        [SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "Serilog")]
        public SerilogLogProvider()
        {
            if (!IsLoggerAvailable())
            {
                throw new InvalidOperationException("Serilog.Log not found");
            }
            _getLoggerByNameDelegate = GetForContextMethodCall();
        }

        public static bool ProviderIsAvailableOverride
        {
            get { return s_providerIsAvailableOverride; }
            set { s_providerIsAvailableOverride = value; }
        }

        public override Logger GetLogger(string name)
        {
            return new SerilogLogger(_getLoggerByNameDelegate(name)).Log;
        }

        internal static bool IsLoggerAvailable()
        {
            return ProviderIsAvailableOverride && GetLogManagerType() != null;
        }

        protected override OpenNdc GetOpenNdcMethod()
        {
            return message => GetPushProperty()("NDC", message);
        }

        protected override OpenMdc GetOpenMdcMethod()
        {
            return (key, value) => GetPushProperty()(key, value);
        }

        private static Func<string, string, IDisposable> GetPushProperty()
        {
            Type ndcContextType = Type.GetType("Serilog.Context.LogContext, Serilog") ?? 
                                  Type.GetType("Serilog.Context.LogContext, Serilog.FullNetFx");

            MethodInfo pushPropertyMethod = ndcContextType.GetMethodPortable(
                "PushProperty", 
                typeof(string),
                typeof(object),
                typeof(bool));

            ParameterExpression nameParam = Expression.Parameter(typeof(string), "name");
            ParameterExpression valueParam = Expression.Parameter(typeof(object), "value");
            ParameterExpression destructureObjectParam = Expression.Parameter(typeof(bool), "destructureObjects");
            MethodCallExpression pushPropertyMethodCall = Expression
                .Call(null, pushPropertyMethod, nameParam, valueParam, destructureObjectParam);
            var pushProperty = Expression
                .Lambda<Func<string, object, bool, IDisposable>>(
                    pushPropertyMethodCall,
                    nameParam,
                    valueParam,
                    destructureObjectParam)
                .Compile();
            
            return (key, value) => pushProperty(key, value, false);
        }

        private static Type GetLogManagerType()
        {
            return Type.GetType("Serilog.Log, Serilog");
        }

        private static Func<string, object> GetForContextMethodCall()
        {
            Type logManagerType = GetLogManagerType();
            MethodInfo method = logManagerType.GetMethodPortable("ForContext", typeof(string), typeof(object), typeof(bool));
            ParameterExpression propertyNameParam = Expression.Parameter(typeof(string), "propertyName");
            ParameterExpression valueParam = Expression.Parameter(typeof(object), "value");
            ParameterExpression destructureObjectsParam = Expression.Parameter(typeof(bool), "destructureObjects");
            MethodCallExpression methodCall = Expression.Call(null, method, new Expression[]
            {
                propertyNameParam, 
                valueParam,
                destructureObjectsParam
            });
            var func = Expression.Lambda<Func<string, object, bool, object>>(
                methodCall,
                propertyNameParam,
                valueParam,
                destructureObjectsParam)
                .Compile();
            return name => func("SourceContext", name, false);
        }
    }
}