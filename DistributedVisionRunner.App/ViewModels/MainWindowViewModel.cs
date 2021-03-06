﻿using CygiaUserClientModule;
using DistributedVisionRunner.Module;
using Prism.Events;
using Prism.Mvvm;
using Prism.Regions;

namespace DistributedVisionRunner.App.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        private string _title = "Distributed vision runner";
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        private bool _loginPageToggle;
        private readonly IRegionManager _rm;

        public bool LoginPageToggle
        {
            get => _loginPageToggle;
            set
            {
                SetProperty(ref _loginPageToggle, value);
                if (value) Navigate("UserManageView");
                else Navigate("VisionProcessUnitTabsHostView");
            }
        }

        private void Navigate(string viewPath)
        {
            _rm.RequestNavigate("VisionRegion", viewPath);
        }

        public MainWindowViewModel(IRegionManager rm, IEventAggregator ea)
        {
            _rm = rm;
            ea.GetEvent<UserRoleEvent>().Subscribe(role =>
            {
                var login = role != "None";
                if (login) LoginPageToggle = false;
                DistributedVisionRunnerModule.UpdateLoginState(login);
            });
        }
    }
}
