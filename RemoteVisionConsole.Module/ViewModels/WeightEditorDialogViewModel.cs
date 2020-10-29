using Prism.Commands;
using Prism.Services.Dialogs;
using RemoteVisionConsole.Module.Helper;
using RemoteVisionConsole.Module.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Input;
using System.Xml.Serialization;
using UniversalWeightSystem.Framework.SDK;

namespace RemoteVisionConsole.Module.ViewModels
{
    public class WeightEditorDialogViewModel : DialogViewModelBase
    {
        #region private fields

        private string _weightDir;
        private List<WeightCollectionViewModel> _weights;
        private readonly IDialogService _dialogService;
        private string _methodHint;
        private string _methodDir;
        private string _projectFilePath;
        private WeightConfigurationConstraint _constraint;
        private string _weightNamesCsv;
        private string _inputNamesCsv;
        private bool _canModify;

        #endregion

        #region props

        public List<WeightCollectionViewModel> Weights
        {
            get => _weights;
            set => SetProperty(ref _weights, value);
        }


        public ICommand SaveProjectCommand { get; }


        public string MethodHint
        {
            get => _methodHint;
            set => SetProperty(ref _methodHint, value);
        }

        public string WeightNamesCsv
        {
            get => _weightNamesCsv;
            set => SetProperty(ref _weightNamesCsv, value);
        }

        public string InputNamesCsv
        {
            get => _inputNamesCsv;
            set => SetProperty(ref _inputNamesCsv, value);
        }

        public bool CanModify
        {
            get => _canModify;
            set => SetProperty(ref _canModify, value);
        }


        public ObservableCollection<CalculationMethodItemViewModel> CalculationMethods { get; } =
            new ObservableCollection<CalculationMethodItemViewModel>();


        #endregion

        #region ctor

        public WeightEditorDialogViewModel(IDialogService dialogService)
        {
            _dialogService = dialogService;
            SaveProjectCommand = new DelegateCommand(SaveProject);
        }

        #endregion

        #region impl

        private void SaveWeights()
        {

            foreach (var weightCollection in Weights)
            {
                var fileName = $"{weightCollection.Index:D3}.weight";
                var filePath = Path.Combine(_weightDir, fileName);
                var lines = weightCollection.WeightItems.Select(w => $"{w.Name} = {w.Weight}").ToArray();
                File.WriteAllLines(filePath, lines);
                weightCollection.NeedToSave = false;
            }
        }

        private void ReadProject(string path)
        {
            _projectFilePath = path;

            var projectDir = Directory.GetParent(path).FullName;

            _weightDir = Path.Combine(projectDir, "Weights");
            _methodDir = Path.Combine(projectDir, "Methods");

            // Read weights
            WeightNamesCsv = string.Join(", ", _constraint.WeightNames);
            var weightsAndNewlyDefinedWeights =
                Helpers.LoadWeights(projectDir, _constraint.WeightNames, _constraint.WeightSetCount);
            Weights = weightsAndNewlyDefinedWeights.weightCollection;

            // Read methods
            InputNamesCsv = string.Join(", ", _constraint.InputNames);
            var (loadedMethods, missingMethods) = Helpers.LoadMethods(projectDir, _constraint.OutputNames);
            foreach (var loadedMethod in loadedMethods)
            {
                CalculationMethods.Add(loadedMethod);
            }

            foreach (var missingMethod in missingMethods)
            {
                CalculationMethods.Add(new CalculationMethodItemViewModel() { OutputName = missingMethod });
            }

            TryGenerateMethodHint();
        }



        private void SaveProject()
        {
            var projectItem = new ProjectItem()
            {
                InputNamesCsv = string.Join(",", _constraint.InputNames),
                WeightNamesCsv = string.Join(",", _constraint.WeightNames),
                WeightSets = _constraint.WeightSetCount
            };

            using (var writer = new StreamWriter(_projectFilePath))
            {
                var serializer = new XmlSerializer(projectItem.GetType());
                serializer.Serialize(writer, projectItem);
            }

            // Save weight files and method files
            SaveWeights();
            if (TrySaveMethods())
            {
                // Ask if the user want to preview
                _dialogService.ShowDialog("VisionConsoleConfirmDialog",
                    new DialogParameters() { { "content", "保存成功! 是否预览?" } },
                    result =>
                    {
                        if (result.Result == ButtonResult.OK)
                        {
                            _dialogService.ShowDialog("FillPreviewInputsDialog",
                                new DialogParameters() { { "WeightSets", _constraint.WeightSetCount }, { "InputNames", _constraint.InputNames } },
                                dialogResult =>
                                {
                                    if (dialogResult.Result == ButtonResult.OK)
                                    {
                                        var selectedSetIndex =
                                            dialogResult.Parameters.GetValue<int>("SelectedSetIndex");
                                        var selectedWeights = Weights[selectedSetIndex].WeightItems
                                            .ToDictionary(item => item.Name, item => item.Weight);
                                        var inputs =
                                            dialogResult.Parameters.GetValue<Dictionary<string, double>>("Inputs");
                                        _dialogService.ShowDialog("PreviewDialog", new DialogParameters()
                                        {
                                            {"Inputs", inputs},
                                            {"Weights", selectedWeights},
                                            {"SetIndex", selectedSetIndex},
                                            {
                                                "Methods",
                                                CalculationMethods.ToDictionary(m => m.OutputName,
                                                    m => m.MethodDefinition)
                                            }
                                        }, result1 => { FinishAndCloseDialog(); });
                                    }
                                });
                        }
                        else
                        {
                            FinishAndCloseDialog();
                        }
                    });
            }
        }

        private void FinishAndCloseDialog()
        {
            RaiseRequestClose(new DialogResult(ButtonResult.OK));
        }

        private bool TrySaveMethods()
        {

            var erroredMethods = CalculationMethods.Where(m => m.Errored).ToArray();
            if (erroredMethods.Length != 0)
            {
                _dialogService.ShowDialog("VisionConsoleNotificationDialog",
                    new DialogParameters() { { "message", "请先解决红色高亮的错误" } },
                    result => { });
                return false;
            }

            return SaveMethodsAfterSuccessfulRun();
        }


        private bool SaveMethodsAfterSuccessfulRun()
        {
            var firstSetOfWeights = _weights[0].WeightItems.ToDictionary(item => item.Name, item => item.Weight);
            var scriptOutputAndExpressions =
                CalculationMethods.ToDictionary(m => m.OutputName, m => m.MethodDefinition);
            var random = new Random(42);
            var testInputs = _constraint.InputNames.ToDictionary(name => name, name => random.NextDouble());

            var (output, exceptions) = WeightWeaver.Weight(testInputs, firstSetOfWeights, scriptOutputAndExpressions);
            if (exceptions.Count > 0)
            {
                // Show exception details
                var exceptionDetails = new List<string>();
                foreach (var outputName in exceptions.Keys)
                {
                    var exception = exceptions[outputName];
                    exceptionDetails.Add($"{outputName}: [{exception.GetType()}] {exception.Message}");
                }

                var exceptionDetailsText = string.Join("\n", exceptionDetails);
                var content = $"试运行时出错: \n{exceptionDetailsText}";
                _dialogService.ShowDialog("VisionConsoleNotificationDialog", new DialogParameters() { { "message", content } },
                    result => { });

                // Mark methods with runtime exceptions
                var methodsWithRuntimeExceptions =
                    CalculationMethods.Where(m => exceptions.Keys.Contains(m.OutputName));
                foreach (var method in methodsWithRuntimeExceptions)
                {
                    method.Errored = true;
                }

                return false;
            }

            // Save methods
            Directory.CreateDirectory(_methodDir);
            foreach (var method in CalculationMethods)
            {
                var path = Path.Combine(_methodDir, $"{method.OutputName}.calc");
                File.WriteAllText(path, method.MethodDefinition);
            }

            return true;
        }

        private void TryGenerateMethodHint()
        {
            var firstOutputName = _constraint.OutputNames[0];
            if (_constraint.InputNames.Length == 1)
            {
                var methodHint = $"{firstOutputName} = input.{_constraint.InputNames[0]} * weight.{_constraint.WeightNames[0]}";
                if (_constraint.WeightNames.Length > 1) methodHint += $" + weight.{_constraint.WeightNames[1]}";
                MethodHint = methodHint;
            }
            else
            {
                var hint = $"{firstOutputName} = pow(input.{_constraint.InputNames[0]}, 2) * weight.{_constraint.WeightNames[0]} + input.{_constraint.InputNames[1]}";
                if (_constraint.WeightNames.Length > 1) hint += $" * weight.{_constraint.WeightNames[1]}";
                MethodHint = hint;
            }
        }




        public override void OnDialogOpened(IDialogParameters parameters)
        {
            _constraint = parameters.GetValue<WeightConfigurationConstraint>("Constraint");
            CanModify = parameters.GetValue<bool>("Login");
            ReadProject(_constraint.ProjectFilePath);
        }


        #endregion
    }
}