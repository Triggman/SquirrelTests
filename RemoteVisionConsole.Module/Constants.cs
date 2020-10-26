using System.IO;

namespace RemoteVisionConsole.Module
{
    public static class Constants
    {
        public static string TabRegionName { get; } = "RemoteVisionConsole.Module.TabRegion";
        public static string ConfigFilePath => Path.Combine(AppDataDir, "RemoteVisionConsole.configuration");
        public const string AppDataDir = "c:/ProgramData/RemoteVisionConsole";

    }
}
