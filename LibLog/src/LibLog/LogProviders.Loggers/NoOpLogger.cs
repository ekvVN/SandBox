namespace Common.Log.LogProviders.Loggers
{
    using System;
    using System.Diagnostics.CodeAnalysis;

    [ExcludeFromCodeCoverage]
    public class NoOpLogger : ILog
    {
        public static readonly NoOpLogger Instance = new NoOpLogger();

        public bool Log(LogLevel logLevel, Func<string> messageFunc, Exception exception, params object[] formatParameters)
        {
            return false;
        }
    }
}