using LoggingConsole.Interface;
using Prism.Ioc;
using Prism.Modularity;
using Prism.Regions;
using RemoteVisionConsole.Module.ViewModels;
using RemoteVisionConsole.Module.Views;
using System;

namespace RemoteVisionConsole.Module
{
    public class RemoteVisionConsoleModule : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            if (!_configured) throw new InvalidOperationException("RemoteVisionConsoleModule.ConfigureModule must be called before");
            var rm = containerProvider.Resolve<IRegionManager>();
            rm.RegisterViewWithRegion(_regionToAttatch, typeof(VisionProcessUnitTabsHostView));
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterDialog<VisionConsoleNotificationDialog, VisionConsoleNotificationDialogViewModel>();
            containerRegistry.RegisterDialog<VisionProcessUnitPropertyDialog, VisionProcessUnitPropertyDialogViewModel>();
            containerRegistry.RegisterDialog<UserSettingDialog, UserSettingDialogViewModel>();
            containerRegistry.RegisterDialog<VisionConsoleConfirmDialog, VisionConsoleConfirmDialogViewModel>();
        }

        public static void UpdateLoginState(bool login)
        {
            UserLogin = login;
        }

        internal static void Log(LoggingMessageItem logItem)
        {
            MessageLogged?.Invoke(logItem);
        }

        public static void ConfigureModule(Action<LoggingMessageItem> logMethod, string regionToAttach, bool requireLogin)
        {
            MessageLogged = logMethod;
            _regionToAttatch = regionToAttach;

            _requireLogin = requireLogin;
            UserLogin = !requireLogin;

            _configured = true;
        }

        internal static bool UserLogin { get; private set; }
        private static bool _configured = false;
        private static bool _requireLogin;
        private static Action<LoggingMessageItem> MessageLogged { get; set; }
        private static string _regionToAttatch;
    }
}