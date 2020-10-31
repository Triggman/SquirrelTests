using System.Collections;
using System.Collections.Generic;

namespace RemoteVisionConsole.Interface
{
    public class StatisticsResults
    {
        public List<DoubleData> DoubleResults { get; set; }
        public List<IntegerData> IntegerResults { get; set; }
        public List<TextData> TextResults { get; set; }

        public ResultType ResultType { get; set; }

        public StatisticsResults()
        {
            DoubleResults = new List<DoubleData>();
            IntegerResults = new List<IntegerData>();
            TextResults = new List<TextData>();
        }

        public StatisticsResults(Dictionary<string, float> doubleDict, Dictionary<string, int> intDict, Dictionary<string, string> textDict)
        {

            DoubleResults = new List<DoubleData>();
            IntegerResults = new List<IntegerData>();
            TextResults = new List<TextData>();

            if (NotNullOrEmpty(doubleDict))
            {
                foreach (var item in doubleDict)
                {
                    DoubleResults.Add(new DoubleData { Name = item.Key, Value = item.Value });
                }
            }

            if (NotNullOrEmpty(intDict))
            {
                foreach (var item in intDict)
                {
                    IntegerResults.Add(new IntegerData { Name = item.Key, Value = item.Value });
                }
            }

            if (NotNullOrEmpty(textDict))
            {
                foreach (var item in textDict)
                {
                    TextResults.Add(new TextData { Name = item.Key, Value = item.Value });
                }
            }
        }

        private bool NotNullOrEmpty(IDictionary dict)
        {
            return dict != null && dict.Count > 0;
        }
    }
}
