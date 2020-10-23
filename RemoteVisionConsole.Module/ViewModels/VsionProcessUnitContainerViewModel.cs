﻿using Prism.Events;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace RemoteVisionConsole.Module.ViewModels
{
    public class VsionProcessUnitContainerViewModel : BindableBase
    {
        #region private fields
        private readonly IEventAggregator _ea;
        private readonly IDialogService _dialogService;

        #endregion

        #region events
        public event Action<string> Error;

        #endregion

        #region props
        private object _viewModel;

        /// <summary>
        /// Can be type of <see cref="VisionProcessUnitViewModel{TData}"/> 
        /// or type of <see cref="VsionProcessUnitConfigurationViewModel"/>
        /// </summary>
        public object ViewModel
        {
            get { return _viewModel; }
            set { SetProperty(ref _viewModel, value); }
        }

        private string _title = "NotConfigured";
        public string Title
        {
            get { return _title; }
            set { SetProperty(ref _title, value); }
        }
        #endregion

        #region ctor

        /// <summary>
        /// Construct an instance with <see cref="VsionProcessUnitConfigurationViewModel"/> as ViewModel
        /// </summary>
        public VsionProcessUnitContainerViewModel(IEventAggregator ea, IDialogService dialogService)
        {
            var configViewModel = new VsionProcessUnitConfigurationViewModel();
            configViewModel.Error += Warn;
            configViewModel.ProcessorAndAdapterMatched += OnConfigFinished;
            ViewModel = configViewModel;
            _ea = ea;
            _dialogService = dialogService;
        }

        /// <summary>
        /// Construct an instance with <see cref="VisionProcessUnitViewModel{TData}"/> as ViewModel
        /// </summary>
        /// <param name="processorType"></param>
        /// <param name="adapterType"></param>
        public VsionProcessUnitContainerViewModel(TypeSource processorType, TypeSource adapterType, Type dataType, IEventAggregator ea, IDialogService dialogService)
        {
            _ea = ea;
            _dialogService = dialogService;
            var (vm, unitName) = CreateUnit(processorType, adapterType, dataType, ea, _dialogService);
            ViewModel = vm;
            Title = unitName;
        }
        #endregion


        #region impl

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
            // Change view model to VisionProcessUnit
            var (vm, unitName) = CreateUnit(processorTypeSource, adapterTypeSource, dataType, _ea, _dialogService);
            ViewModel = vm;
            Title = unitName;

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

            configItems.Add(new VisionProcessUnitConfig
            {
                AdapterAssemblyPath = adapterTypeSource.AssemblyFilePath,
                AdapterNamespace = adapterTypeSource.Namespace,
                AdapterTypeName = adapterTypeSource.TypeName,
                ProcessorAssemblyPath = processorTypeSource.AssemblyFilePath,
                ProcessorNamespace = processorTypeSource.Namespace,
                ProcessorTypeName = processorTypeSource.TypeName,

            });

            using (var writer = new StreamWriter(Constants.ConfigFilePath))
            {
                var serializer = new XmlSerializer(typeof(List<VisionProcessUnitConfig>));
                serializer.Serialize(writer, configItems);
            }

        }

        private void Warn(string message)
        {
            _dialogService.ShowDialog("VisionConsoleNotificationDialog", new DialogParameters { { "message", message } }, r => { });
        }
        #endregion
    }
}