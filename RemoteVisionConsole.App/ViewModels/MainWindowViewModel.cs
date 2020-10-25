using Prism.Events;
using Prism.Mvvm;

namespace RemoteVisionConsole.App.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        private string _title = "Remote vision console";
        public string Title
        {
            get { return _title; }
            set { SetProperty(ref _title, value); }
        }

        public MainWindowViewModel(IEventAggregator ea)
        {

        }
    }
}
