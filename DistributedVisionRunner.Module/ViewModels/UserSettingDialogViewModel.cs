using Prism.Commands;
using Prism.Services.Dialogs;
using DistributedVisionRunner.Module.Helper;
using System.Windows.Input;
using Afterbunny.Windows.Helpers;
using DistributedVisionRunner.Module.Models;

namespace DistributedVisionRunner.Module.ViewModels
{
    public class UserSettingDialogViewModel : DialogViewModelBase
    {
        private ProcessUnitUserSetting _viewModel;
        public ProcessUnitUserSetting ViewModel
        {
            get => _viewModel;
            set => SetProperty(ref _viewModel, value);
        }

        private bool _userLogin;
        public bool UserLogin
        {
            get => _userLogin;
            set => SetProperty(ref _userLogin, value);
        }

        public ICommand OKCommand { get; }
        public ICommand SelectImageSaveMainFolderCommand { get; }

        public UserSettingDialogViewModel()
        {
            OKCommand = new DelegateCommand(() =>
            {
                var param = new DialogParameters { { "setting", ViewModel } };
                RaiseRequestClose(new DialogResult(ButtonResult.OK, param));
            });

            SelectImageSaveMainFolderCommand = new DelegateCommand(SelectImageSaveMainFolder);
        }

        public override void OnDialogOpened(IDialogParameters parameters)
        {
            ViewModel = parameters.GetValue<ProcessUnitUserSetting>("setting");
            UserLogin = parameters.GetValue<bool>("login");
            base.OnDialogOpened(parameters);
        }
        private void SelectImageSaveMainFolder()
        {
            var selectedDir = FileSystemHelper.GetDirFromDialog();
            if (string.IsNullOrEmpty(selectedDir)) return;
            ViewModel.ImageSaveMainFolder = selectedDir;
        }

    }



}

