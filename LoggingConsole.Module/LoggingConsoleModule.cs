using LoggingConsole.Module.RollingFileAppender;
using LoggingConsole.Module.Views;
using Prism.Ioc;
using Prism.Modularity;
using Prism.Regions;
using System;
using System.Collections.Generic;

namespace LoggingConsole.Module
{
    public class LoggingConsoleModule : IModule
    {
        private static MessageConsoleInitParam _initParams;
        private static string _regionToAttatch;
        private static bool _loggingConfigured = false;

        public void OnInitialized(IContainerProvider containerProvider)
        {
            var rm = containerProvider.Resolve<IRegionManager>();
            rm.RegisterViewWithRegion(_regionToAttatch, typeof(MessageConsoleView));
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            if (!_loggingConfigured) throw new InvalidOperationException($"ConfigureLogging must be called before RegisterTypes");
            containerRegistry.RegisterInstance(_initParams);
            
        }


        public static void ConfigModule(List<AppenderParam> appenderParams, string regionToAttatch)
        {
            var appenders = RollingFileAppenderManager.Config(appenderParams);
            _initParams = new MessageConsoleInitParam { Loggers = appenders };
            _regionToAttatch = regionToAttatch;

            _loggingConfigured = true;
        }


    }
}