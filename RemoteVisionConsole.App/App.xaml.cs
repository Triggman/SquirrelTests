using Prism.Ioc;
using Prism.Modularity;
using Prism.Regions;
using RemoteVisionConsole.App.Views;
using RemoteVisionConsole.Module;
using RemoteVisionConsole.Module.Views;
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
            rm.RegisterViewWithRegion("MainRegion", typeof(VisionProcessUnitTabsHostView));

            return Container.Resolve<MainWindow>();
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
        }

        protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
        {
            moduleCatalog.AddModule<RemoteVisionConsoleModule>();
        }

    }
}
