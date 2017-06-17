﻿namespace LibLog.Logging
{
    using System;
    using Common.Log;
    using Common.Log.LogProviders;
    using Common.Log.LogProviders.Loggers;
    using NLog;
    using NLog.Config;
    using NLog.Targets;
    using Shouldly;
    using Xunit;

    public class LogProviderTests : IDisposable
    {
        [Fact]
        public void When_NLog_is_available_Then_should_get_NLogLogger()
        {
            LogProvider.SetCurrentLogProvider(null);
            NLogLogProvider.ProviderIsAvailableOverride = true;
            Log4NetLogProvider.ProviderIsAvailableOverride = false;
            EntLibLogProvider.ProviderIsAvailableOverride = false;
            SerilogLogProvider.ProviderIsAvailableOverride = false;
            LoupeLogProvider.ProviderIsAvailableOverride = false;

            var logProvider = LogProvider.ResolveLogProvider();
            logProvider.ShouldBeOfType<NLogLogProvider>();
        }

        [Fact]
        public void When_Log4Net_is_available_Then_should_get_Log4NetLogger()
        {
            LogProvider.SetCurrentLogProvider(null);
            NLogLogProvider.ProviderIsAvailableOverride = false;
            Log4NetLogProvider.ProviderIsAvailableOverride = true;
            EntLibLogProvider.ProviderIsAvailableOverride = false;
            SerilogLogProvider.ProviderIsAvailableOverride = false;
            LoupeLogProvider.ProviderIsAvailableOverride = false;

            var logProvider = LogProvider.ResolveLogProvider();
            logProvider.ShouldBeOfType<Log4NetLogProvider>();
        }

        [Fact]
        public void When_EntLibLog_is_available_Then_should_get_EntLibLogger()
        {
            LogProvider.SetCurrentLogProvider(null);
            SerilogLogProvider.ProviderIsAvailableOverride = false;
            NLogLogProvider.ProviderIsAvailableOverride = false;
            Log4NetLogProvider.ProviderIsAvailableOverride = false;
            EntLibLogProvider.ProviderIsAvailableOverride = true;
            LoupeLogProvider.ProviderIsAvailableOverride = false;

            var logProvider = LogProvider.ResolveLogProvider();
            logProvider.ShouldBeOfType<EntLibLogProvider>();
        }

        [Fact]
        public void When_Serilog_is_available_Then_should_get_SeriLogLogger()
        {
            LogProvider.SetCurrentLogProvider(null);
            NLogLogProvider.ProviderIsAvailableOverride = false;
            Log4NetLogProvider.ProviderIsAvailableOverride = false;
            EntLibLogProvider.ProviderIsAvailableOverride = false;
            SerilogLogProvider.ProviderIsAvailableOverride = true;
            LoupeLogProvider.ProviderIsAvailableOverride = false;

            var logProvider = LogProvider.ResolveLogProvider();
            logProvider.ShouldBeOfType<SerilogLogProvider>();
        }

        [Fact]
        public void When_LoupLog_is_available_Then_should_get_LoupeLogger()
        {
            LogProvider.SetCurrentLogProvider(null);
            NLogLogProvider.ProviderIsAvailableOverride = false;
            Log4NetLogProvider.ProviderIsAvailableOverride = false;
            EntLibLogProvider.ProviderIsAvailableOverride = false;
            SerilogLogProvider.ProviderIsAvailableOverride = false;
            LoupeLogProvider.ProviderIsAvailableOverride = true;

            var logProvider = LogProvider.ResolveLogProvider();
            logProvider.ShouldBeOfType<LoupeLogProvider>();
        }

        [Fact]
        public void When_no_logger_is_available_Then_should_get_NoOpLogger()
        {
            LogProvider.SetCurrentLogProvider(null);
            NLogLogProvider.ProviderIsAvailableOverride = false;
            Log4NetLogProvider.ProviderIsAvailableOverride = false;
            EntLibLogProvider.ProviderIsAvailableOverride = false;
            SerilogLogProvider.ProviderIsAvailableOverride = false;
            LoupeLogProvider.ProviderIsAvailableOverride = false;

            ILog logger = LogProvider.For<LogProviderTests>();

            logger.ShouldBeOfType<NoOpLogger>();
        }

        [Fact]
        public void When_set_current_log_provider_then_should_raise_OnCurrentLogProviderSet()
        {
            ILogProvider provider = null;
            LogProvider.OnCurrentLogProviderSet = p => provider = p;

            LogProvider.SetCurrentLogProvider(new NLogLogProvider());

            provider.ShouldNotBeNull();
        }

        [Fact]
        public void When_disable_logging_via_property_then_should_not_log()
        {
            var config = new LoggingConfiguration();
            var target = new MemoryTarget
            {
                Layout = "${level:uppercase=true}|${ndc}|${mdc:item=key}|${message}|${exception}"
            };
            config.AddTarget("memory", target);
            config.LoggingRules.Add(new LoggingRule("*", NLog.LogLevel.Trace, target));
            LogManager.Configuration = config;
            LogProvider.SetCurrentLogProvider(new NLogLogProvider());

            LogProvider.IsDisabled = true;
            var logger = LogProvider.GetLogger("DisableLogging");
            logger.Info("test");

            target.Logs.ShouldBeEmpty();
        }

        [Fact]
        public void When_enable_logging_via_property_then_should_log()
        {
            LogProvider.IsDisabled = true;
            var config = new LoggingConfiguration();
            var target = new MemoryTarget
            {
                Layout = "${level:uppercase=true}|${ndc}|${mdc:item=key}|${message}|${exception}"
            };
            config.AddTarget("memory", target);
            config.LoggingRules.Add(new LoggingRule("*", NLog.LogLevel.Trace, target));
            LogManager.Configuration = config;
            LogProvider.SetCurrentLogProvider(new NLogLogProvider());

            LogProvider.IsDisabled = false;
            var logger = LogProvider.GetLogger("DisableLogging");
            logger.Info("test");

            target.Logs.ShouldNotBeEmpty();
        }

        public void Dispose()
        {
            NLogLogProvider.ProviderIsAvailableOverride = true;
            Log4NetLogProvider.ProviderIsAvailableOverride = true;
            EntLibLogProvider.ProviderIsAvailableOverride = true;
            SerilogLogProvider.ProviderIsAvailableOverride = true;
            LoupeLogProvider.ProviderIsAvailableOverride = true;
            LogProvider.IsDisabled = false;
        }
    }
}
