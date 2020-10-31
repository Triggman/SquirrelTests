namespace DistributedVisionRunner.Module.Models
{
    public class WeightConfigurationConstraint
    {
        public string ProjectFilePath { get; set; }
        public int WeightSetCount { get; set; }
        public string[] WeightNames { get; set; }
        public string[] InputNames { get; set; }
        public string[] OutputNames { get; set; }
    }
}