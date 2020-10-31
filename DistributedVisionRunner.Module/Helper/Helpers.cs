using DistributedVisionRunner.Module.ViewModels;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace DistributedVisionRunner.Module.Helper
{
    public static class Helpers
    {
        internal static T CopyObject<T>(T o) where T : class, new()
        {
            var copy = new T();

            var type = copy.GetType();
            var props = type.GetProperties();

            foreach (var prop in props)
            {
                if (prop.CanWrite)
                    prop.SetValue(copy, prop.GetValue(o));
            }

            return copy;

        }

        public static (List<CalculationMethodItemViewModel> loadedMethods, List<string> missingMethods) LoadMethods(string weightProjectDir, string[] outputNames)
        {
            var methodDir = Path.Combine(weightProjectDir, "Methods");
            Directory.CreateDirectory(methodDir);
            var methodFiles = Directory.GetFiles(methodDir).Where(p => p.EndsWith(".calc")).ToArray();

            var loadedMethods = new List<CalculationMethodItemViewModel>();
            var missingMethods = new List<string>();
            foreach (var outputName in outputNames)
            {
                var file = methodFiles.FirstOrDefault(f => Path.GetFileNameWithoutExtension(f) == outputName);
                if (file == null)
                {
                    missingMethods.Add(outputName);
                }
                else
                {
                    loadedMethods.Add(new CalculationMethodItemViewModel()
                    {
                        OutputName = outputName,
                        MethodDefinition = File.ReadAllText(file)
                    });
                }
            }


            return (loadedMethods, missingMethods);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="weightProjectDir"></param>
        /// <param name="weightNames"></param>
        /// <param name="weightSetCount"></param>
        /// <returns>
        /// newlyDefinedWeights: null if no newly defined weights are found
        ///
        /// </returns>
        public static (List<WeightCollectionViewModel> weightCollection, string[] newlyDefinedWeights) LoadWeights(string weightProjectDir, string[] weightNames, int weightSetCount)
        {
            var weightDir = Path.Combine(weightProjectDir, "Weights");
            Directory.CreateDirectory(weightDir);

            var expectedWeightFilePaths = Enumerable.Range(0, weightSetCount)
                .Select(index => Path.Combine(weightDir, $"{index:D3}.weight")).ToArray();

            var output = new List<WeightCollectionViewModel>();
            string[] newlyDefinedWeights = null;
            for (var setIndex = 0; setIndex < expectedWeightFilePaths.Length; setIndex++)
            {
                var filePath = expectedWeightFilePaths[setIndex];
                var fileExists = File.Exists(filePath);
                var (aSetOfWeightItems, newlyAddedWeights) = fileExists
                    ? ReadWeightsFromFile(filePath, weightNames)
                    : (CreateASetOfWeights(weightNames), weightNames);
                if (newlyAddedWeights.Length != 0)
                {
                    newlyDefinedWeights = newlyAddedWeights;
                }
                output.Add(new WeightCollectionViewModel(aSetOfWeightItems)
                { Index = setIndex, NeedToSave = !fileExists });
            }



            return (output, newlyDefinedWeights);
        }

        private static (List<WeightItemViewModel> weightItems, string[] newlyAddWeights) ReadWeightsFromFile(string filePath, string[] weightNames)
        {
            var rawLines = File.ReadAllLines(filePath);
            var spaceFreeLines = rawLines.Select(line => Regex.Replace(line, @"\s+", "")).ToArray();
            // variableName=number
            var pattern = new Regex(@"^([a-zA-Z]\w*)=(-?[\d\.e]+)$");

            var userDefinedHashSet = new HashSet<string>(weightNames);
            var successfulReadWeights = new List<WeightItemViewModel>();
            foreach (var line in spaceFreeLines)
            {
                var matchResult = pattern.Match(line);
                if (!matchResult.Success) continue;
                var weightName = matchResult.Groups[1].Value;
                if (!userDefinedHashSet.Contains(weightName)) continue;
                var weightValueText = matchResult.Groups[2].Value;
                var isNumber = double.TryParse(weightValueText, out var weight);
                if (isNumber) successfulReadWeights.Add(new WeightItemViewModel() { Name = weightName, Weight = weight });
            }

            if (successfulReadWeights.Count == 0)
                return (Helpers.CreateASetOfWeights(weightNames), weightNames);

            // Look for missing weightItems
            // And init missing weightItems with empty weight values
            var loadedHashSet = new HashSet<string>(successfulReadWeights.Select(w => w.Name));
            var missingWeights = weightNames.Where(name => !loadedHashSet.Contains(name))
                .Select(name => new WeightItemViewModel() { Name = name });
            var weightItemViewModels = missingWeights as WeightItemViewModel[] ?? missingWeights.ToArray();
            successfulReadWeights.AddRange(weightItemViewModels);

            var missingWeightNames = weightItemViewModels.Select(w => w.Name).ToArray();
            return (successfulReadWeights.OrderBy(w => w.Name).ToList(), missingWeightNames);
        }


        private static List<WeightItemViewModel> CreateASetOfWeights(string[] weightNames)
        {
            return weightNames.Select(name => new WeightItemViewModel() { Name = name }).ToList();
        }
    }


}
