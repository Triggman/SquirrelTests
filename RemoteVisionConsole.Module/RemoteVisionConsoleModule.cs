using Prism.Ioc;
using Prism.Modularity;
using Prism.Regions;
using RemoteVisionConsole.Module.ViewModels;
using RemoteVisionConsole.Module.Views;

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
        }
    }
}