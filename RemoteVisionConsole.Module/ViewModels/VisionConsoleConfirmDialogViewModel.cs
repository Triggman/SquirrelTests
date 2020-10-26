using Prism.Commands;
using Prism.Services.Dialogs;
using System.Windows.Input;

namespace RemoteVisionConsole.Module.ViewModels
{
    public class VisionConsoleConfirmDialogViewModel : DialogViewModelBase
    {
        #region private fields

        private string _content;

        #endregion

        #region props

        public string Content
        {
            get => _content;
            set => SetProperty(ref _content, value);
        }

        public ICommand OkCommand { get; set; }

        public ICommand CancelCommand { get; set; }

        #endregion

        #region ctor

        public VisionConsoleConfirmDialogViewModel()
        {
            OkCommand = new DelegateCommand(() => RaiseRequestClose(new DialogResult(ButtonResult.OK)));
            CancelCommand = new DelegateCommand(() => RaiseRequestClose(new DialogResult(ButtonResult.Cancel)));
        }

        #endregion

        public override void OnDialogOpened(IDialogParameters parameters)
        {
            base.OnDialogOpened(parameters);

            Content = parameters.GetValue<string>("content");
        }
    }
}
