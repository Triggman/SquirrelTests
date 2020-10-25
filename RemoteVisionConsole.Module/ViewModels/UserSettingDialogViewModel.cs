using Prism.Commands;
using Prism.Services.Dialogs;
using System.Windows.Input;

namespace RemoteVisionConsole.Module.ViewModels
{
    public class UserSettingDialogViewModel : DialogViewModelBase
    {
        private ProcessUnitUserSetting _viewModel;
        public ProcessUnitUserSetting ViewModel
        {
            get { return _viewModel; }
            set { SetProperty(ref _viewModel, value); }
        }

        public ICommand OKCommand { get; }

        public UserSettingDialogViewModel()
        {
            OKCommand = new DelegateCommand(() =>
            {
                var param = new DialogParameters { { "setting", ViewModel } };
                RaiseRequestClose(new DialogResult(ButtonResult.OK, param));
            });
        }


        public override void OnDialogOpened(IDialogParameters parameters)
        {
            ViewModel = parameters.GetValue<ProcessUnitUserSetting>("setting");
            base.OnDialogOpened(parameters);
        }


    }
}
