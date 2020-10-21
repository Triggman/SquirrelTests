using Prism.Events;
using Prism.Mvvm;
using RemoteVisionConsole.Module;
using RemoteVisionConsole.Module.ViewModels;

namespace RemoteVisionConsole.App.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        private string _title = "Prism Application";
        public string Title
        {
            get { return _title; }
            set { SetProperty(ref _title, value); }
        }

        public MainWindowViewModel(IEventAggregator ea)
        {
            var assemblyPath = @"C:\Users\afterbunny\source\repos\RemoteVisionConsole\RemoteVisionModule.Tests\bin\Debug\RemoteVisionModule.Tests.dll";
            var typeSourceAdapter = new TypeSource { AssemblyFilePath = assemblyPath, Namespace = "RemoteVisionModule.Tests.Mocks", TypeName = "VisionAdapterMock" };
            var typeSourceProcessor = new TypeSource { AssemblyFilePath = assemblyPath, Namespace = "RemoteVisionModule.Tests.Mocks", TypeName = "VisionProcessorMock" };

            var unit = new VisionProcessUnit<byte>(ea, typeSourceProcessor, typeSourceAdapter);
        }
    }
}
