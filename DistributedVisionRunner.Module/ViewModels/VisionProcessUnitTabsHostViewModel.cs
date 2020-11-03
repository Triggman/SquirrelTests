using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using System;
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
        private readonly string _selectedTabCacheFile = "RecentSelectedTab";

        private ObservableCollection<VisionProcessUnitContainerViewModel> _tabItems;
        public ObservableCollection<VisionProcessUnitContainerViewModel> TabItems
        {
            get => _tabItems;
            set => SetProperty(ref _tabItems, value);
        }

        private VisionProcessUnitContainerViewModel _selectedTab;
        public VisionProcessUnitContainerViewModel SelectedTab
        {
            get => _selectedTab;
            set
            {
                SetProperty(ref _selectedTab, value);
                // Remember recent selected tab
                File.WriteAllText(_selectedTabCacheFile, value.Title);
            }

        }

        public ICommand AddTabItemCommand { get; }

        public VisionProcessUnitTabsHostViewModel(IEventAggregator ea, IDialogService dialogService)
        {
            _ea = ea;
            _dialogService = dialogService;
            AddTabItemCommand = new DelegateCommand(AddTabItem);

            Directory.CreateDirectory(Constants.AppDataDir);
            TabItems = LoadSavedTabItems();
            if (TabItems.Any())
            {
                SelectedTab = GetRecentSelectedTab();
            }
        }

        private VisionProcessUnitContainerViewModel GetRecentSelectedTab()
        {
            if (!File.Exists(_selectedTabCacheFile)) return TabItems[0];
            var tabName = File.ReadAllText(_selectedTabCacheFile);
            var matchingTab = TabItems.FirstOrDefault(t => t.Title == tabName);
            return matchingTab ?? TabItems[0];
        }

        private void AddTabItem()
        {
            var item = new VisionProcessUnitContainerViewModel(_ea, _dialogService);
            AttatchDeleteEvent(item);
            TabItems.Add(item);
            SelectedTab = item;
        }

        private ObservableCollection<VisionProcessUnitContainerViewModel> LoadSavedTabItems()
        {
            var output = new ObservableCollection<VisionProcessUnitContainerViewModel>();
            // Load configured tabs if any
            if (File.Exists(Constants.ConfigFilePath))
            {
                var configItems = ReadUnitConfigs();

                var errorItems = new List<VisionProcessUnitConfig>();
                foreach (var configItem in configItems)
                {
                    try
                    {
                        var (processorType, adapterType, dataType) = configItem.GetTypes();
                        output.Add(new VisionProcessUnitContainerViewModel(processorType, adapterType, dataType, _ea,
                            _dialogService));
                    }
                    catch (FileNotFoundException e)
                    {
                        var prompt = $"dll文件({e.FileName})丢失, \n页面{configItem.UnitName}无法加载";
                        Warn(prompt, $"dll file lost: {e.FileName}");
                        errorItems.Add(configItem);
                    }
                    catch (TypeLoadException ex)
                    {
                        Warn(ex.Message, ex.Message);
                        errorItems.Add(configItem);
                    }
                }


                if (errorItems.Any())
                {
                    foreach (var errorItem in errorItems)
                    {
                        configItems.Remove(errorItem);
                    }
                    SaveConfigs(configItems);
                }

            }
            // Add a default not-configured tab
            else
            {
                output.Add(new VisionProcessUnitContainerViewModel(_ea, _dialogService));
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

        private void AttatchDeleteEvent(VisionProcessUnitContainerViewModel item)
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

                        // Remove relating folders
                        try
                        {
                            Directory.Delete($"{Constants.AppDataDir}/Changes/{unitName}");
                            Directory.Delete($"{Constants.AppDataDir}/WeightSettings/{unitName}");
                        }
                        catch
                        {
                            // Ignore this
                        }
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
