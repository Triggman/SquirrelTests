using LoggingConsole.Interface;
using LoggingConsole.Module;
using LoggingConsole.Module.RollingFileAppender;
using LoggingConsole.Module.Views;
using Prism.Events;
using Prism.Ioc;
using Prism.Modularity;
using Prism.Regions;
using RemoteVisionConsole.App.Views;
using RemoteVisionConsole.Module;
using RemoteVisionConsole.Module.Views;
using System.Collections.Generic;
using System.IO;
using System.Windows;

namespace RemoteVisionConsole.App
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        protected override Window CreateShell()
        {
            var rm = Container.Resolve<IRegionManager>();
            rm.RegisterViewWithRegion("VisionRegion", typeof(VisionProcessUnitTabsHostView));

            // Log messages from RemoteVisionConsoleModule
            var ea = Container.Resolve<IEventAggregator>();
            RemoteVisionConsoleModule.MessageLogged += 
                logItem => 
                ea.GetEvent<LogEvent>().Publish(("General", logItem));

            return Container.Resolve<MainWindow>();
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            
        }

        protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
        {
            moduleCatalog.AddModule<RemoteVisionConsoleModule>();

            LoggingConsoleModule.ConfigureLogging(new List<AppenderParam> { new AppenderParam("General", Path.Combine(Constants.AppDataDir, "Log")) }, "LogRegion");
            moduleCatalog.AddModule<LoggingConsoleModule>();
        }

    }
}
