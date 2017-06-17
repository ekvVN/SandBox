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
            var logicalThreadContextType = Type.GetType("log4net.LogicalThreadContext, log4net");
            var stacksProperty = logicalThreadContextType.GetPropertyPortable("Stacks");
            var logicalThreadContextStacksType = stacksProperty.PropertyType;
            var stacksIndexerProperty = logicalThreadContextStacksType.GetPropertyPortable("Item");
            var stackType = stacksIndexerProperty.PropertyType;
            var pushMethod = stackType.GetMethodPortable("Push");

            var messageParameter = Expression.Parameter(typeof(string), "message");

            // message => LogicalThreadContext.Stacks.Item["NDC"].Push(message);
            var callPushBody = Expression.Call(
                Expression.Property(Expression.Property(null, stacksProperty),
                    stacksIndexerProperty,
                    Expression.Constant("NDC")),
                pushMethod,
                messageParameter);

            var result = Expression
                .Lambda<OpenNdc>(callPushBody, messageParameter)
                .Compile();

            return result;
        }

        protected override OpenMdc GetOpenMdcMethod()
        {
            var logicalThreadContextType = Type.GetType("log4net.LogicalThreadContext, log4net");
            var propertiesProperty = logicalThreadContextType.GetPropertyPortable("Properties");
            var logicalThreadContextPropertiesType = propertiesProperty.PropertyType;
            var propertiesIndexerProperty = logicalThreadContextPropertiesType.GetPropertyPortable("Item");

            var removeMethod = logicalThreadContextPropertiesType.GetMethodPortable("Remove");

            var keyParam = Expression.Parameter(typeof(string), "key");
            var valueParam = Expression.Parameter(typeof(string), "value");

            var propertiesExpression = Expression.Property(null, propertiesProperty);

            // (key, value) => LogicalThreadContext.Properties.Item[key] = value;
            var setProperties = Expression.Assign(Expression.Property(propertiesExpression, propertiesIndexerProperty, keyParam), valueParam);

            // key => LogicalThreadContext.Properties.Remove(key);
            var removeMethodCall = Expression.Call(propertiesExpression, removeMethod, keyParam);

            var set = Expression
                .Lambda<Action<string, string>>(setProperties, keyParam, valueParam)
                .Compile();

            var remove = Expression
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
            var logManagerType = GetLogManagerType();
            var method = logManagerType.GetMethodPortable("GetLogger", typeof(string));
            var nameParam = Expression.Parameter(typeof(string), "name");
            var methodCall = Expression.Call(null, method, nameParam);
            return Expression
                .Lambda<Func<string, object>>(methodCall, nameParam)
                .Compile();
        }
    }
}