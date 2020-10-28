using System.Collections.Generic;

namespace RemoteVisionConsole.Interface
{
    public interface IVisionProcessor<TData>
    {
        /// <summary>
        /// Process data and output statistic and graphical results
        /// </summary>
        /// <param name="data"></param>
        /// <param name="cavityIndex"></param>
        /// <returns></returns>
        ProcessResult<TData> Process(List<TData[]> data, int cavityIndex);

        (string[] floatNames, string[] integerNames, string[] textNames) OutputNames { get; }
        string[] WeightNames { get;  }
    }

    public class ProcessResult<TData>
    {
        public List<TData[]> DisplayData { get; set; }
        public Statistics Statistics { get; set; }
    }

    public class Statistics
    {
        public Dictionary<string, float> FloatResults { get; set; } = new Dictionary<string, float>();
        public Dictionary<string, int> IntegerResults { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, string> TextResults { get; set; } = new Dictionary<string, string>();
    }







}