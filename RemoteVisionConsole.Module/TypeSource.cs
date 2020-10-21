using System;
using System.Linq;
using System.Reflection;

namespace RemoteVisionConsole.Module
{
    public class TypeSource
    {
        public string AssemblyFilePath { get; set; }
        public string Namespace { get; set; }
        public string TypeName { get; set; }

        public T CreateTypeInstance<T>()
        {
            var assembly = Assembly.LoadFrom(AssemblyFilePath);
            var allTypes = assembly.GetExportedTypes();

            var typesWithSpecifiedName = allTypes.Where(t => t.Name == TypeName).ToArray();

            var outputType = typesWithSpecifiedName.Single(t => t.Namespace == Namespace);

            var instance = (T)Activator.CreateInstance(outputType);

            return instance;
        }
    }
}
