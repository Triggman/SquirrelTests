using LoggingConsole.Interface;
using Prism.Ioc;
using Prism.Modularity;
using RemoteVisionConsole.Module.ViewModels;
using RemoteVisionConsole.Module.Views;
using System;

namespace RemoteVisionConsole.Module
{
    public class RemoteVisionConsoleModule : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {

        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterDialog<VisionConsoleNotificationDialog, VisionConsoleNotificationDialogViewModel>();
            containerRegistry.RegisterDialog<VisionProcessUnitPropertyDialog, VisionProcessUnitPropertyDialogViewModel>();
            containerRegistry.RegisterDialog<UserSettingDialog, UserSettingDialogViewModel>();
        }

        internal static void Log(LoggingMessageItem logItem)
        {
            MessageLogged?.Invoke(logItem);
        }

        public static event Action<LoggingMessageItem> MessageLogged;
    }
}