﻿using System;
using System.Linq;
using System.Reflection;
using DistributedVisionRunner.Module.Models;

namespace DistributedVisionRunner.Module.ViewModels
{
    public class VisionProcessUnitConfig
    {
        public string AdapterAssemblyPath { get; set; }
        public string AdapterNamespace { get; set; }
        public string AdapterTypeName { get; set; }

        public string ProcessorAssemblyPath { get; set; }
        public string ProcessorNamespace { get; set; }
        public string ProcessorTypeName { get; set; }
        public string UnitName { get; set; }

        public (TypeSource processorTypeSource, TypeSource adapterTypeSource, Type dataType) GetTypes()
        {
            var adapterType = SearchType(AdapterAssemblyPath, AdapterNamespace, AdapterTypeName);
            var processorType = SearchType(ProcessorAssemblyPath, ProcessorNamespace, ProcessorTypeName);
            if (adapterType == null)
            {
                throw new TypeLoadException($"Can not find the required adapter({AdapterTypeName})\n in {AdapterAssemblyPath}");
            }

            if (processorType == null)
            {
                throw new TypeLoadException($"Can not find the required processor type({ProcessorTypeName})\n in {ProcessorAssemblyPath}");
            }

            var genericType = adapterType.GetInterfaces().First(t => t.Name.Contains("IVisionAdapter")).GetGenericArguments()[0];

            return (new TypeSource { AssemblyFilePath = ProcessorAssemblyPath, Namespace = ProcessorNamespace, TypeName = ProcessorTypeName, Type = processorType },
                new TypeSource { AssemblyFilePath = AdapterAssemblyPath, Namespace = AdapterNamespace, TypeName = AdapterTypeName, Type = adapterType }
                , genericType);
        }

        private Type SearchType(string assemblyPath, string ns, string typeName)
        {
            var assembly = Assembly.LoadFrom(assemblyPath);
            return assembly.GetTypes().Where(t => t.Namespace == ns).FirstOrDefault(t => t.Name == typeName);
        }
    }
}