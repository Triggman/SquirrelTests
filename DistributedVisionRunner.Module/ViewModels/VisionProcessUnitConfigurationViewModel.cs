using Afterbunny.Windows.Helpers;
using DistributedVisionRunner.Interface;
using DistributedVisionRunner.Module.Models;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Input;

namespace DistributedVisionRunner.Module.ViewModels
{
    public class VisionProcessUnitConfigurationViewModel : BindableBase
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
        public VisionProcessUnitConfigurationViewModel()
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
            var path = FileSystemHelper.GetFileFromDialog(Directory.GetCurrentDirectory(), (new[] { "dll" }, "dll"), Path.Combine(Constants.AppDataDir, "Cache/SelectAssembly.RecentFolder"));
            if (string.IsNullOrEmpty(path)) return;

            AdapterAssemblyPath = path;
        }

        private void SelectProcessorAssembly()
        {
            var path = FileSystemHelper.GetFileFromDialog(Directory.GetCurrentDirectory(), (new[] { "dll" }, "dll"), Path.Combine(Constants.AppDataDir, "Cache/SelectAssembly.RecentFolder"));
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
            int typeCount;
            try
            {
                typeCount = PopulateTypeSources(ref _adapterTypeSources, AdapterAssemblyPath,
                       new[] { typeof(IVisionAdapter<byte>), typeof(IVisionAdapter<short>), typeof(IVisionAdapter<ushort>), typeof(IVisionAdapter<float>), });

            }
            catch (FileNotFoundException)
            {
                LogError("请确保该dll的所有依赖dll都存在于软件的运行目录,\n然后重启软件");
                return;
            }
            OnPropertyChanged(new PropertyChangedEventArgs(nameof(AdapterTypeSources)));
            if (typeCount > 0)
            {
                SelectedAdapterTypeSource = AdapterTypeSources.First();
            }
            else
            {
                LogError($"Can not find any adapter type in assembly: {AdapterAssemblyPath}");
            }
        }


        private void OnProcessorAssemblyPathChanged()
        {
            int typeCount;

            try
            {
                typeCount = PopulateTypeSources(ref _processorTypeSources, ProcessorAssemblyPath,
                      new[] { typeof(IVisionProcessor<byte>), typeof(IVisionProcessor<short>), typeof(IVisionProcessor<ushort>), typeof(IVisionProcessor<float>), });

            }
            catch (FileNotFoundException)
            {

                LogError("请确保该dll的所有依赖dll都存在于软件的运行目录,\n然后重启软件");
                return;
            }
            OnPropertyChanged(new PropertyChangedEventArgs(nameof(ProcessorTypeSources)));
            if (typeCount > 0)
            {
                SelectedProcessorTypeSource = ProcessorTypeSources.First();
            }
            else
            {
                LogError($"Can not find any processor type in assembly: {ProcessorAssemblyPath}");
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
