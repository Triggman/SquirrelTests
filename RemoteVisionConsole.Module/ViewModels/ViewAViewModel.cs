using Prism.Mvvm;
using RemoteVisionConsole.Module.Helper;
using System;

namespace RemoteVisionConsole.Module.ViewModels
{
    public class ViewAViewModel : BindableBase
    {
        private string _message;
        public string Message
        {
            get { return _message; }
            set { SetProperty(ref _message, value); }
        }

        public ViewAViewModel()
        {
            Message = "View A from your Prism Module";
        }

        public static void Test()
        {
        
        }
    }
}
