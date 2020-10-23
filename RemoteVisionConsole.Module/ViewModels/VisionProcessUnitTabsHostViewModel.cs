using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Input;
using System.Xml.Serialization;

namespace RemoteVisionConsole.Module.ViewModels
{
    public class VisionProcessUnitTabsHostViewModel : BindableBase
    {

        private readonly IEventAggregator _ea;
        private readonly IDialogService _dialogService;

        public ObservableCollection<VsionProcessUnitContainerViewModel> TabItems { get; set; }

        public ICommand AddTabItemCommand { get; }

        public VisionProcessUnitTabsHostViewModel(IEventAggregator ea, IDialogService dialogService)
        {
            _ea = ea;
            _dialogService = dialogService;
            AddTabItemCommand = new DelegateCommand(AddTabItem, () => TabItems.Count(i => i.ViewModel is VsionProcessUnitConfigurationViewModel) == 0)
                .ObservesProperty(() => TabItems.Count);

            TabItems = LoadSavedTabItems();

        }

        private void AddTabItem()
        {
            TabItems.Add(new VsionProcessUnitContainerViewModel(_ea, _dialogService));
        }

        private ObservableCollection<VsionProcessUnitContainerViewModel> LoadSavedTabItems()
        {
            var output = new ObservableCollection<VsionProcessUnitContainerViewModel>();
            // Load configured tabs if any
            if (File.Exists(Constants.ConfigFilePath))
            {
                List<VisionProcessUnitConfig> configItems;
                using (var reader = new StreamReader(Constants.ConfigFilePath))
                {
                    var serializer = new XmlSerializer(typeof(List<VisionProcessUnitConfig>));
                    configItems = (List<VisionProcessUnitConfig>)serializer.Deserialize(reader);
                }

                foreach (var configItem in configItems)
                {
                    var (processorType, adapterType, dataType) = configItem.GetTypes();
                    output.Add(new VsionProcessUnitContainerViewModel(processorType, adapterType, dataType, _ea, _dialogService));
                }
            }
            // Add a default unconfigured tab
            else
            {
                output.Add(new VsionProcessUnitContainerViewModel(_ea, _dialogService));
            }

            return output;
        }

        private void Warn(string message)
        {
            _dialogService.ShowDialog("VisionConsoleNotificationDialog", new DialogParameters { { "message", message } }, r => { });
        }
    }
}
