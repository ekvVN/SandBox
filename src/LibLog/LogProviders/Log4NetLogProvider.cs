namespace Common.Log.LogProviders
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq.Expressions;
    using System.Reflection;
    using Common.Log.LogProviders.Loggers;

    [ExcludeFromCodeCoverage]
    public class Log4NetLogProvider : LogProviderBase
    {
        private readonly Func<string, object> _getLoggerByNameDelegate;
        private static bool s_providerIsAvailableOverride = true;

        [SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "LogManager")]
        public Log4NetLogProvider()
        {
            if (!IsLoggerAvailable())
            {
                throw new InvalidOperationException("log4net.LogManager not found");
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
            return new Log4NetLogger(_getLoggerByNameDelegate(name)).Log;
        }

        public static bool IsLoggerAvailable()
        {
            return ProviderIsAvailableOverride && GetLogManagerType() != null;
        }

        protected override OpenNdc GetOpenNdcMethod()
        {
            Type logicalThreadContextType = Type.GetType("log4net.LogicalThreadContext, log4net");
            PropertyInfo stacksProperty = logicalThreadContextType.GetPropertyPortable("Stacks");
            Type logicalThreadContextStacksType = stacksProperty.PropertyType;
            PropertyInfo stacksIndexerProperty = logicalThreadContextStacksType.GetPropertyPortable("Item");
            Type stackType = stacksIndexerProperty.PropertyType;
            MethodInfo pushMethod = stackType.GetMethodPortable("Push");

            ParameterExpression messageParameter =
                Expression.Parameter(typeof(string), "message");

            // message => LogicalThreadContext.Stacks.Item["NDC"].Push(message);
            MethodCallExpression callPushBody =
                Expression.Call(
                    Expression.Property(Expression.Property(null, stacksProperty),
                        stacksIndexerProperty,
                        Expression.Constant("NDC")),
                    pushMethod,
                    messageParameter);

            OpenNdc result =
                Expression.Lambda<OpenNdc>(callPushBody, messageParameter)
                    .Compile();

            return result;
        }

        protected override OpenMdc GetOpenMdcMethod()
        {
            Type logicalThreadContextType = Type.GetType("log4net.LogicalThreadContext, log4net");
            PropertyInfo propertiesProperty = logicalThreadContextType.GetPropertyPortable("Properties");
            Type logicalThreadContextPropertiesType = propertiesProperty.PropertyType;
            PropertyInfo propertiesIndexerProperty = logicalThreadContextPropertiesType.GetPropertyPortable("Item");

            MethodInfo removeMethod = logicalThreadContextPropertiesType.GetMethodPortable("Remove");

            ParameterExpression keyParam = Expression.Parameter(typeof(string), "key");
            ParameterExpression valueParam = Expression.Parameter(typeof(string), "value");

            MemberExpression propertiesExpression = Expression.Property(null, propertiesProperty);

            // (key, value) => LogicalThreadContext.Properties.Item[key] = value;
            BinaryExpression setProperties = Expression.Assign(Expression.Property(propertiesExpression, propertiesIndexerProperty, keyParam), valueParam);

            // key => LogicalThreadContext.Properties.Remove(key);
            MethodCallExpression removeMethodCall = Expression.Call(propertiesExpression, removeMethod, keyParam);

            Action<string, string> set = Expression
                .Lambda<Action<string, string>>(setProperties, keyParam, valueParam)
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
            return Type.GetType("log4net.LogManager, log4net");
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