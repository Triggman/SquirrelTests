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

namespace DistributedVisionRunner.Module.ViewModels
{
    public class VisionProcessUnitTabsHostViewModel : BindableBase
    {

        private readonly IEventAggregator _ea;
        private readonly IDialogService _dialogService;

        private ObservableCollection<VsionProcessUnitContainerViewModel> _tabItems;
        public ObservableCollection<VsionProcessUnitContainerViewModel> TabItems
        {
            get => _tabItems;
            set => SetProperty(ref _tabItems, value);
        }

        private VsionProcessUnitContainerViewModel _selectedTab;
        public VsionProcessUnitContainerViewModel SelectedTab
        {
            get => _selectedTab;
            set => SetProperty(ref _selectedTab, value);
        }

        public ICommand AddTabItemCommand { get; }

        public VisionProcessUnitTabsHostViewModel(IEventAggregator ea, IDialogService dialogService)
        {
            _ea = ea;
            _dialogService = dialogService;
            AddTabItemCommand = new DelegateCommand(AddTabItem);

            Directory.CreateDirectory(Constants.AppDataDir);
            TabItems = LoadSavedTabItems();

        }

        private void AddTabItem()
        {
            var item = new VsionProcessUnitContainerViewModel(_ea, _dialogService);
            AttatchDeleteEvent(item);
            TabItems.Add(item);
            SelectedTab = item;
        }

        private ObservableCollection<VsionProcessUnitContainerViewModel> LoadSavedTabItems()
        {
            var output = new ObservableCollection<VsionProcessUnitContainerViewModel>();
            // Load configured tabs if any
            if (File.Exists(Constants.ConfigFilePath))
            {
                var configItems = ReadUnitConfigs();

                for (var index = 0; index < configItems.Count; index++)
                {
                    var configItem = configItems[index];
                    try
                    {
                        var (processorType, adapterType, dataType) = configItem.GetTypes();
                        output.Add(new VsionProcessUnitContainerViewModel(processorType, adapterType, dataType, _ea,
                            _dialogService));
                    }
                    catch (FileNotFoundException e)
                    {
                        var prompt = $"dll文件({e.FileName})丢失, \n页面{configItem.UnitName}无法加载";
                        Warn(prompt, $"dll file lost: {e.FileName}");

                        // Remove invalid config from file
                        configItems.Remove(configItem);
                        SaveConfigs(configItems);
                    }
                }
            }
            // Add a default not-configured tab
            else
            {
                output.Add(new VsionProcessUnitContainerViewModel(_ea, _dialogService));
            }

            foreach (var item in output)
            {
                AttatchDeleteEvent(item);
            }

            return output;
        }

        private static List<VisionProcessUnitConfig> ReadUnitConfigs()
        {
            List<VisionProcessUnitConfig> configItems;
            using (var reader = new StreamReader(Constants.ConfigFilePath))
            {
                var serializer = new XmlSerializer(typeof(List<VisionProcessUnitConfig>));
                configItems = (List<VisionProcessUnitConfig>)serializer.Deserialize(reader);
            }

            return configItems;
        }

        private void AttatchDeleteEvent(VsionProcessUnitContainerViewModel item)
        {
            item.Deleted += i =>
            {
                TabItems.Remove(i);

                // Remove setting
                if (!(i.ViewModel is VisionProcessUnitConfigurationViewModel))
                {
                    var unitName = i.Title;
                    var configs = ReadUnitConfigs();
                    var configToRemove = configs.FirstOrDefault(c => c.UnitName == unitName);
                    if (configToRemove != null)
                    {
                        configs.Remove(configToRemove);
                        SaveConfigs(configs);
                    }
                }

                // Switch to last tab remaining
                var lastTab = TabItems.LastOrDefault();
                if (lastTab != null) SelectedTab = lastTab;
            };
        }

        private static void SaveConfigs(List<VisionProcessUnitConfig> configs)
        {
            using (var writer = new StreamWriter(Constants.ConfigFilePath))
            {
                var serializer = new XmlSerializer(typeof(List<VisionProcessUnitConfig>));
                serializer.Serialize(writer, configs);
            }
        }

        private void Warn(string message, string saveMessage)
        {
            _dialogService.ShowDialog("VisionRunnerNotificationDialog", new DialogParameters { { "message", message } }, r => { });
            DistributedVisionRunnerModule.Log(new LoggingConsole.Interface.LoggingMessageItem(message, saveMessage) { LogLevel = LoggingConsole.Interface.LogLevel.Fatal });
        }
    }
}
