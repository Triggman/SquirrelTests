using Prism.Commands;
using Prism.Services.Dialogs;
using System.Windows.Input;
using RemoteVisionConsole.Module.Models;

namespace RemoteVisionConsole.Module.ViewModels
{
    public class VisionConsoleNotificationDialogViewModel
        : DialogViewModelBase
    {

        private string _message;
        public string Message
        {
            get => _message;
            set => SetProperty(ref _message, value);
        }

        public ICommand OKCommand { get; set; }

        public VisionConsoleNotificationDialogViewModel()
        {
            OKCommand = new DelegateCommand(() => { RaiseRequestClose(new DialogResult(ButtonResult.OK)); });
        }

        public override void OnDialogOpened(IDialogParameters parameters)
        {
            Message = parameters.GetValue<string>("message");
        }
    }
}
