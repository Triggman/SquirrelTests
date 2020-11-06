using Prism.Ioc;
using Prism.Modularity;
using Prism.Regions;
using DistributedVisionRunner.Module.ViewModels;
using DistributedVisionRunner.Module.Views;
using System;
using Afterbunny.Windows.Helpers;
using CygiaLog.Module;

namespace DistributedVisionRunner.Module
{
    public class DistributedVisionRunnerModule : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            if (!_configured) throw new InvalidOperationException("DistributedVisionRunnerModule.ConfigureModule must be called before");
            var rm = containerProvider.Resolve<IRegionManager>();
            rm.RegisterViewWithRegion(_regionToAttatch, typeof(VisionProcessUnitTabsHostView));
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterDialog<VisionRunnerNotificationDialog, VisionRunnerNotificationDialogViewModel>();
            containerRegistry.RegisterDialog<VisionProcessUnitPropertyDialog, VisionProcessUnitPropertyDialogViewModel>();
            containerRegistry.RegisterDialog<UserSettingDialog, UserSettingDialogViewModel>();
            containerRegistry.RegisterDialog<VisionRunnerConfirmDialog, VisionRunnerConfirmDialogViewModel>();
            containerRegistry.RegisterDialog<WeightEditorDialog, WeightEditorDialogViewModel>();
            containerRegistry.RegisterDialog<FillPreviewInputsDialog, FillPreviewInputsDialogViewModel>();
            containerRegistry.RegisterDialog<PreviewDialog, PreviewDialogViewModel>();
            containerRegistry.RegisterDialog<CommitViewerDialog, CommitViewerDialogViewModel>();
        }

        public static void UpdateLoginState(bool login)
        {
            UserLogin = login;
        }

        internal static void Log(LogItem logItem)
        {
            MessageLogged?.Invoke(logItem);
        }

        public static void ConfigureModule(Action<LogItem> logMethod, string regionToAttach, bool requireLogin)
        {
            MessageLogged = logMethod;
            _regionToAttatch = regionToAttach;

            _requireLogin = requireLogin;
            UserLogin = !requireLogin;

            _configured = true;
        }

        public static void SetDefaultImageBackground(string hexColor)
        {
            DefaultImageBackground = WPFHelper.HexadecimalToRGB(hexColor);
        }

        public static void SetDefaultImageBackground(byte r, byte g, byte b)
        {
            DefaultImageBackground = (r, g, b);
        }

        internal static (byte r, byte g, byte b) DefaultImageBackground { get; set; } = WPFHelper.HexadecimalToRGB("4CAB6F");
        internal static bool UserLogin { get; private set; }
        private static bool _configured = false;
        private static bool _requireLogin;
        private static Action<LogItem> MessageLogged { get; set; }
        private static string _regionToAttatch;
    }
}