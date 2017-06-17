namespace Common.Log.LogProviders.Loggers
{
    using System;
    using System.Diagnostics.CodeAnalysis;

    [ExcludeFromCodeCoverage]
    internal class NoOpLogger : ILog
    {
        internal static readonly NoOpLogger Instance = new NoOpLogger();

        public bool Log(LogLevel logLevel, Func<string> messageFunc, Exception exception, params object[] formatParameters)
        {
            return false;
        }
    }
}