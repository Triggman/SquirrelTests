using System;
using System.Collections.Generic;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using UniversalWeightSystem.Framework.SDK;

namespace DistributedVisionRunner.Module.ViewModels
{
    public class PreviewDialogViewModel : BindableBase, IDialogAware
    {
        #region props

        private Dictionary<string, double> _weights;
        private Dictionary<string, double> _inputs;
        private string _weightHeader;
        private List<CalculationResult> _calculationResults;

        #endregion

        #region props

        public Dictionary<string, double> Weights
        {
            get => _weights;
            set => SetProperty(ref _weights, value);
        }

        public Dictionary<string, double> Inputs
        {
            get => _inputs;
            set => SetProperty(ref _inputs, value);
        }
        
        public List<CalculationResult> CalculationResults
        {
            get => _calculationResults;
            set => SetProperty(ref _calculationResults, value);
        }

        public string WeightHeader
        {
            get => _weightHeader;
            set => SetProperty(ref _weightHeader, value);
        }

        #endregion


        #region ctor

        public bool CanCloseDialog()
        {
            return true;
        }

        public void OnDialogClosed()
        {
          
        }

        public void OnDialogOpened(IDialogParameters parameters)
        {
            Inputs = parameters.GetValue<Dictionary<string, double>>("Inputs");
            Weights = parameters.GetValue<Dictionary<string, double>>("Weights");
            var methods = parameters.GetValue<Dictionary<string, string>>("Methods");
            var setIndex = parameters.GetValue<int>("SetIndex");

            WeightHeader = $"Weights: Set {setIndex + 1 }";
            CalculationResults = GetCalculationResults(Inputs, Weights, methods);
        }

        public string Title { get; }
        public event Action<IDialogResult> RequestClose;

        #endregion

        #region impl

        private static List<CalculationResult> GetCalculationResults(Dictionary<string, double> inputs, Dictionary<string, double> weights, Dictionary<string, string> methods)
        {
            var (result, exceptions) = WeightWeaver.Weight(inputs, weights, methods);

            var output = new List<CalculationResult>();
            foreach (var outputName in result.Keys)
            {
                output.Add(new CalculationResult()
                {
                    MethodDefinition = methods[outputName],
                    OutputName = outputName,
                    OutputValue = result[outputName]
                });
            }

            return output;
        }

        #endregion
    }

    public class CalculationResult
    {
        public string OutputName { get; set; }
        public double OutputValue { get; set; }
        public string MethodDefinition { get; set; }
    }
}