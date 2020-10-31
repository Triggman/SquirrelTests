using System.IO;

namespace DistributedVisionRunner.Module
{
    public static class Constants
    {
        public static string TabRegionName { get; } = "DistributedVisionRunner.Module.TabRegion";
        public static string ConfigFilePath => Path.Combine(AppDataDir, "DistributedVisionRunner.configuration");
        public const string AppDataDir = "c:/ProgramData/DistributedVisionRunner";

    }
}
