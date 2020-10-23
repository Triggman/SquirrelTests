using Prism.Mvvm;
using Prism.Services.Dialogs;
using System;

namespace RemoteVisionConsole.Module.ViewModels
{
    public class VisionConsoleNotificationDialogViewModel
        : BindableBase, IDialogAware
    {
        public string Title { get; } = "Warning";

        private string _message;
        public string Message
        {
            get { return _message; }
            set { SetProperty(ref _message, value); }
        }

        public event Action<IDialogResult> RequestClose;

        public bool CanCloseDialog()
        {
            return true;
        }

        public void OnDialogClosed()
        {
        }

        public void OnDialogOpened(IDialogParameters parameters)
        {
            Message = parameters.GetValue<string>("message");
        }
    }
}
