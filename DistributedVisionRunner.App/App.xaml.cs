﻿using Afterbunny.UI.WPF.Core;
using Afterbunny.Windows.Helpers;
using CygiaUserClientModule;
using CygiaUserClientModule.Views;
using Prism.Events;
using Prism.Ioc;
using Prism.Modularity;
using DistributedVisionRunner.App.Views;
using DistributedVisionRunner.Module;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using CygiaLog.Module;
using CygiaLog.Module.RollingFileAppender;

namespace DistributedVisionRunner.App
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        protected override Window CreateShell()
        {
            return Container.Resolve<MainWindow>();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            FileSystemHelper.SetupUnhandledExceptionLogging(Path.Combine(Constants.AppDataDir, "UnhandledExceptions"));
            base.OnStartup(e);
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterForNavigation<UserManageView>();
        }

        protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
        {
            CygiaLogModule.ConfigModule(new List<AppenderParam> { new AppenderParam("General", Path.Combine(Constants.AppDataDir, "Log")) }, "LogRegion");
            moduleCatalog.AddModule<CygiaLogModule>();

            // Log messages from DistributedVisionRunnerModule
            var ea = Container.Resolve<IEventAggregator>();
            Action<LogItem> logMethod =
                logItem =>
                ea.GetEvent<LogEvent>().Publish(("General", logItem));

            DistributedVisionRunnerModule.ConfigureModule(logMethod, "VisionRegion", true);
            DistributedVisionRunnerModule.SetDefaultImageBackground(Theme.PrimaryColor.R, Theme.PrimaryColor.G, Theme.PrimaryColor.B);
            moduleCatalog.AddModule<DistributedVisionRunnerModule>();

            moduleCatalog.AddModule<CygiaUserClientModuleModule>();
        }
    }
}