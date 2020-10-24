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
        ProcessResult<TData> Process(TData[] data, int cavityIndex);


    }

    public class ProcessResult<TData>
    {
        public TData[] DisplayData { get; set; }
        public Statistics Statistics { get; set; }
    }

    public class Statistics
    {
        public Dictionary<string, float> FloatResults { get; set; }
        public Dictionary<string, int> IntegerResults { get; set; }
        public Dictionary<string, string> TextResults { get; set; }
    }

  





}