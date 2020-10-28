using System;

namespace RemoteVisionConsole.Module.Models
{
    public class TypeSource
    {
        public string AssemblyFilePath { get; set; }
        public string Namespace { get; set; }
        public string TypeName { get; set; }
        public Type Type { get; internal set; }
    }
}
