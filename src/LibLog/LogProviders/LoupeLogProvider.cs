namespace Common.Log.LogProviders
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Reflection;
    using Common.Log.LogProviders.Loggers;

    [ExcludeFromCodeCoverage]
    internal class LoupeLogProvider : LogProviderBase
    {
        private static bool s_providerIsAvailableOverride = true;
        private readonly WriteDelegate _logWriteDelegate;

        public LoupeLogProvider()
        {
            if (!IsLoggerAvailable())
            {
                throw new InvalidOperationException("Gibraltar.Agent.Log (Loupe) not found");
            }

            _logWriteDelegate = GetLogWriteDelegate();
        }

        /// <summary>
        /// Gets or sets a value indicating whether [provider is available override]. Used in tests.
        /// </summary>
        /// <value>
        /// <c>true</c> if [provider is available override]; otherwise, <c>false</c>.
        /// </value>
        public static bool ProviderIsAvailableOverride
        {
            get { return s_providerIsAvailableOverride; }
            set { s_providerIsAvailableOverride = value; }
        }

        public override Logger GetLogger(string name)
        {
            return new LoupeLogger(name, _logWriteDelegate).Log;
        }

        public static bool IsLoggerAvailable()
        {
            return ProviderIsAvailableOverride && GetLogManagerType() != null;
        }

        private static Type GetLogManagerType()
        {
            return Type.GetType("Gibraltar.Agent.Log, Gibraltar.Agent");
        }

        private static WriteDelegate GetLogWriteDelegate()
        {
            Type logManagerType = GetLogManagerType();
            Type logMessageSeverityType = Type.GetType("Gibraltar.Agent.LogMessageSeverity, Gibraltar.Agent");
            Type logWriteModeType = Type.GetType("Gibraltar.Agent.LogWriteMode, Gibraltar.Agent");

            MethodInfo method = logManagerType.GetMethodPortable(
                "Write",
                logMessageSeverityType, typeof(string), typeof(int), typeof(Exception), typeof(bool), 
                logWriteModeType, typeof(string), typeof(string), typeof(string), typeof(string), typeof(object[]));

            var callDelegate = (WriteDelegate)method.CreateDelegate(typeof(WriteDelegate));
            return callDelegate;
        }
    }
}