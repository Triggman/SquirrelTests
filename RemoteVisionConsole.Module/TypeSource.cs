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
        public Type Type { get; internal set; }
    }
}
