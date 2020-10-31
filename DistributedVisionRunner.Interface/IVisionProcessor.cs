using System.Collections.Generic;

namespace DistributedVisionRunner.Interface
{
    /// <summary>
    /// Should be implemented by vision engineer
    /// </summary>
    /// <typeparam name="TData">
    /// Data type of the images to be processed, for example, byte for gray scale images
    /// </typeparam>
    public interface IVisionProcessor<TData>
    {
        /// <summary>
        /// Process data and output statistic and graphical results
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        ProcessResult<TData> Process(List<TData[]> data);

        /// <summary>
        /// Equals ProcessResult.Statistics.FloatResults.Keys
        /// </summary>
        string[] OutputNames { get; }

        /// <summary>
        /// Names of weights
        /// Will be used in the weighting system
        /// </summary>
        string[] WeightNames { get; }
    }

    public class ProcessResult<TData>
    {
        /// <summary>
        /// Data for displaying
        /// </summary>
        public List<TData[]> DisplayData { get; set; }

        /// <summary>
        /// statistic output
        /// </summary>
        public Statistics Statistics { get; set; }
    }

    public class Statistics
    {
        public Dictionary<string, float> FloatResults { get; set; } = new Dictionary<string, float>();
        public Dictionary<string, int> IntegerResults { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, string> TextResults { get; set; } = new Dictionary<string, string>();
    }







}