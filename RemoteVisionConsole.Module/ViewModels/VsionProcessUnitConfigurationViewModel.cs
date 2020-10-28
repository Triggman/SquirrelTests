using Prism.Commands;
using Prism.Mvvm;
using RemoteVisionConsole.Interface;
using RemoteVisionConsole.Module.Helper;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Input;
using RemoteVisionConsole.Module.Models;

namespace RemoteVisionConsole.Module.ViewModels
{
    public class VsionProcessUnitConfigurationViewModel : BindableBase
    {

        #region events
        /// <summary>
        /// First type is processor type
        /// Second type is adapter type
        /// Third type is data type
        /// </summary>
        public Action<TypeSource, TypeSource, Type> ProcessorAndAdapterMatched { get; set; }

        public event Action<string> Error;
        #endregion

        #region props
        public ICommand SelectAdapterAssemblyCommand { get; }
        public ICommand SelectProcessorAssemblyCommand { get; }

        private string _adapterAssemblyPath;
        public string AdapterAssemblyPath
        {
            get => _adapterAssemblyPath;
            set
            {
                SetProperty(ref _adapterAssemblyPath, value);
                OnAdapterAssemblyPathChanged();
            }
        }


        private string _processorAssemblyPath;
        public string ProcessorAssemblyPath
        {
            get => _processorAssemblyPath;
            set
            {
                SetProperty(ref _processorAssemblyPath, value);
                OnProcessorAssemblyPathChanged();
            }
        }


        private IEnumerable<TypeSource> _adapterTypeSources;
        public IEnumerable<TypeSource> AdapterTypeSources
        {
            get => _adapterTypeSources;
            set => SetProperty(ref _adapterTypeSources, value);
        }

        private IEnumerable<TypeSource> _processorTypeSources;
        public IEnumerable<TypeSource> ProcessorTypeSources
        {
            get => _processorTypeSources;
            set => SetProperty(ref _processorTypeSources, value);
        }

        private TypeSource _selectedAdapterTypeSource;
        public TypeSource SelectedAdapterTypeSource
        {
            get => _selectedAdapterTypeSource;
            set => SetProperty(ref _selectedAdapterTypeSource, value);
        }

        private TypeSource _selectedProcessorTypeSource;
        public TypeSource SelectedProcessorTypeSource
        {
            get => _selectedProcessorTypeSource;
            set => SetProperty(ref _selectedProcessorTypeSource, value);
        }

        public ICommand MatchProcessorAndAdapterCommand { get; }
        #endregion

        #region ctor
        public VsionProcessUnitConfigurationViewModel()
        {
            MatchProcessorAndAdapterCommand = new DelegateCommand(MatchProcessorAndAdapter, () => SelectedProcessorTypeSource != null && SelectedAdapterTypeSource != null)
                .ObservesProperty(() => SelectedAdapterTypeSource).ObservesProperty(() => SelectedProcessorTypeSource);
            SelectProcessorAssemblyCommand = new DelegateCommand(SelectProcessorAssembly);
            SelectAdapterAssemblyCommand = new DelegateCommand(SelectAdapterAssembly);
        }


        #endregion
        #region impl

        private void SelectAdapterAssembly()
        {
            var path = Helpers.GetFileFromDialog(Directory.GetCurrentDirectory(), (new[] { "dll"}, "dll"));
            if (string.IsNullOrEmpty(path)) return;

            AdapterAssemblyPath = path;
        }

        private void SelectProcessorAssembly()
        {
            var path = Helpers.GetFileFromDialog(Directory.GetCurrentDirectory(), (new[] { "dll" }, "dll"));
            if (string.IsNullOrEmpty(path)) return;

            ProcessorAssemblyPath = path;
        }

        private void MatchProcessorAndAdapter()
        {
            var processorGenericType = SelectedProcessorTypeSource.Type.GetInterfaces().First(t => t.Name.Contains("IVisionProcessor")).GetGenericArguments()[0];
            var adapterGenericType = SelectedAdapterTypeSource.Type.GetInterfaces().First(t => t.Name.Contains("IVisionAdapter")).GetGenericArguments()[0];

            if (processorGenericType == adapterGenericType) ProcessorAndAdapterMatched?.Invoke(SelectedProcessorTypeSource, SelectedAdapterTypeSource, processorGenericType);
            else LogError($"Data type of the processor({processorGenericType}) does not match that({adapterGenericType}) of the adapter");
        }

        private void OnAdapterAssemblyPathChanged()
        {
            var typeCount = PopulateTypeSources(ref _adapterTypeSources, AdapterAssemblyPath,
                new[] { typeof(IVisionAdapter<byte>), typeof(IVisionAdapter<short>), typeof(IVisionAdapter<ushort>), typeof(IVisionAdapter<float>), });
            OnPropertyChanged(new PropertyChangedEventArgs(nameof(AdapterTypeSources)));
            if (typeCount > 0)
            {
                SelectedAdapterTypeSource = AdapterTypeSources.First();
            }
            else
            {
                LogError($"Can not find any adapter type in assmebly: {AdapterAssemblyPath}");
            }
        }


        private void OnProcessorAssemblyPathChanged()
        {
            var typeCount = PopulateTypeSources(ref _processorTypeSources, ProcessorAssemblyPath,
                new[] { typeof(IVisionProcessor<byte>), typeof(IVisionProcessor<short>), typeof(IVisionProcessor<ushort>), typeof(IVisionProcessor<float>), });
            OnPropertyChanged(new PropertyChangedEventArgs(nameof(ProcessorTypeSources)));
            if (typeCount > 0)
            {
                SelectedProcessorTypeSource = ProcessorTypeSources.First();
            }
            else
            {
                LogError($"Can not find any processor type in assmebly: {ProcessorAssemblyPath}");
            }
        }

        private int PopulateTypeSources(ref IEnumerable<TypeSource> output, string assemblyPath, IEnumerable<Type> baseTypes)
        {
            var assembly = Assembly.LoadFrom(assemblyPath);
            var allTypes = assembly.GetExportedTypes().ToArray();
            var matchingTypes = allTypes.Where(t => baseTypes.Any(bt => bt.IsAssignableFrom(t))).ToArray();
            output = matchingTypes.Select(t => new TypeSource { AssemblyFilePath = assemblyPath, Namespace = t.Namespace, TypeName = t.Name, Type = t });

            return output.Count();
        }


        private void LogError(string message)
        {
            Error?.Invoke(message);
        }

        #endregion

    }
}
