using Prism.Commands;
using Prism.Services.Dialogs;
using System.Windows.Input;
using DistributedVisionRunner.Module.Models;

namespace DistributedVisionRunner.Module.ViewModels
{
    public class VisionRunnerNotificationDialogViewModel
        : DialogViewModelBase
    {

        private string _message;
        public string Message
        {
            get => _message;
            set => SetProperty(ref _message, value);
        }

        public ICommand OKCommand { get; set; }

        public VisionRunnerNotificationDialogViewModel()
        {
            OKCommand = new DelegateCommand(() => { RaiseRequestClose(new DialogResult(ButtonResult.OK)); });
        }

        public override void OnDialogOpened(IDialogParameters parameters)
        {
            Message = parameters.GetValue<string>("message");
        }
    }
}
