using NetMQ;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using DistributedVisionRunner.Module.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Input;
using System.Xml.Serialization;
using CygiaLog.Module;

namespace DistributedVisionRunner.Module.ViewModels
{
    public class VisionProcessUnitContainerViewModel : BindableBase
    {
        #region private fields

        private readonly IEventAggregator _ea;
        private readonly IDialogService _dialogService;

        #endregion private fields

        #region events

        public event Action<string> Error;

        public event Action<VisionProcessUnitContainerViewModel> Deleted;

        #endregion events

        #region props

        private object _viewModel;

        /// <summary>
        /// Can be type of <see cref="VisionProcessUnitViewModel{TData}"/>
        /// or type of <see cref="VisionProcessUnitConfigurationViewModel"/>
        /// </summary>
        public object ViewModel
        {
            get => _viewModel;
            set => SetProperty(ref _viewModel, value);
        }

        private string _title = "NotConfigured";

        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        public ICommand DeleteMeCommand { get; }

        #endregion props

        #region ctor

        /// <summary>
        /// Construct an instance with <see cref="VisionProcessUnitConfigurationViewModel"/> as ViewModel
        /// </summary>
        public VisionProcessUnitContainerViewModel(IEventAggregator ea, IDialogService dialogService)
        {
            var configViewModel = new VisionProcessUnitConfigurationViewModel();
            configViewModel.Error += Warn;
            configViewModel.ProcessorAndAdapterMatched += OnConfigFinished;
            ViewModel = configViewModel;
            _ea = ea;
            _dialogService = dialogService;

            DeleteMeCommand = new DelegateCommand(DeleteMe);
        }

        /// <summary>
        /// Construct an instance with <see cref="VisionProcessUnitViewModel{TData}"/> as ViewModel
        /// </summary>
        /// <param name="processorType"></param>
        /// <param name="adapterType"></param>
        public VisionProcessUnitContainerViewModel(TypeSource processorType, TypeSource adapterType, Type dataType, IEventAggregator ea, IDialogService dialogService)
        {
            _ea = ea;
            _dialogService = dialogService;
            var (vm, unitName) = CreateUnit(processorType, adapterType, dataType, ea, _dialogService);
            ViewModel = vm;
            Title = unitName;

            DeleteMeCommand = new DelegateCommand(DeleteMe);
        }

        #endregion ctor

        #region impl

        private void DeleteMe()
        {
            // Check for login
            if (!DistributedVisionRunnerModule.UserLogin)
            {
                Warn("此操作需要登录");
                return;
            }

            // Confirm delete
            var prompt = string.Empty;
            if (ViewModel is VisionProcessUnitConfigurationViewModel)
            {
                prompt = "是否删除未配置完成的页面?";
            }
            else
            {
                prompt = $"是否删除页面: {Title} ?";
            }
            _dialogService.ShowDialog("VisionRunnerConfirmDialog", new DialogParameters { { "content", prompt } }, r =>
            {
                if (r.Result == ButtonResult.OK)
                {
                    Log("页面已删除", $"Page({Title}) was deleted");
                    Deleted?.Invoke(this);
                }
            });
        }

        private static (object unit, string unitName) CreateUnit(TypeSource processorType, TypeSource adapterType, Type dataType, IEventAggregator ea, IDialogService dialogService)
        {
            object vm;
            if (dataType == typeof(byte)) vm = new VisionProcessUnitByte(ea, dialogService, processorType, adapterType);
            else if (dataType == typeof(float)) vm = new VisionProcessUnitFloat(ea, dialogService, processorType, adapterType);
            else if (dataType == typeof(short)) vm = new VisionProcessUnitShort(ea, dialogService, processorType, adapterType);
            else if (dataType == typeof(ushort)) vm = new VisionProcessUnitUShort(ea, dialogService, processorType, adapterType);
            else throw new InvalidDataException($"Vision processor of type({dataType}) has not implemented yet");

            var unitName = (string)vm.GetType().GetProperty("Name").GetValue(vm);
            return (vm, unitName);
        }

        private void OnConfigFinished(TypeSource processorTypeSource, TypeSource adapterTypeSource, Type dataType)
        {
            (object vm, string name) unit;
            try
            {
                unit = CreateUnit(processorTypeSource, adapterTypeSource, dataType, _ea, _dialogService);
            }
            catch (AddressAlreadyInUseException ex)
            {
                var instance = Activator.CreateInstance(adapterTypeSource.Type);
                var address = adapterTypeSource.Type.GetProperty("ZeroMQAddress").GetValue(instance);
                Warn($"ZeroMQ服务器地址({address})已被占用, \n请重新选择ZeroMQ服务器地址");
                return;
            }

            // Append config
            List<VisionProcessUnitConfig> configItems;
            if (File.Exists(Constants.ConfigFilePath))
            {
                using (var reader = new StreamReader(Constants.ConfigFilePath))
                {
                    var serializer = new XmlSerializer(typeof(List<VisionProcessUnitConfig>));
                    configItems = (List<VisionProcessUnitConfig>)serializer.Deserialize(reader);
                }
            }
            else configItems = new List<VisionProcessUnitConfig>();

            // Check for duplication
            if (configItems.Any(item => item.UnitName == unit.name))
            {
                Warn($"已存在同名的页面({unit.name}), \n请重新选择Adapter类或修改该Adapter类的Name属性");
                return;
            }

            configItems.Add(new VisionProcessUnitConfig
            {
                AdapterAssemblyPath = adapterTypeSource.AssemblyFilePath,
                AdapterNamespace = adapterTypeSource.Namespace,
                AdapterTypeName = adapterTypeSource.TypeName,
                ProcessorAssemblyPath = processorTypeSource.AssemblyFilePath,
                ProcessorNamespace = processorTypeSource.Namespace,
                ProcessorTypeName = processorTypeSource.TypeName,
                UnitName = unit.name
            });

            using (var writer = new StreamWriter(Constants.ConfigFilePath))
            {
                var serializer = new XmlSerializer(typeof(List<VisionProcessUnitConfig>));
                serializer.Serialize(writer, configItems);
            }

            ViewModel = unit.vm;
            Title = unit.name;
        }

        private void Log(string displayMessage, string saveMessage, LogLevel logLevel = LogLevel.Info)
        {
            DistributedVisionRunnerModule.Log(new LogItem($"({Title}): {displayMessage}", $"VisionPage({Title}): {saveMessage}") { LogLevel = logLevel });
        }

        private void Warn(string message)
        {
            _dialogService.ShowDialog("VisionRunnerNotificationDialog", new DialogParameters { { "message", message } }, r => { });
        }

        #endregion impl
    }
}