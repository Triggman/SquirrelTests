using Prism.Commands;
using Prism.Services.Dialogs;
using RemoteVisionConsole.Module.ChangeTracking;
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
        private string _changeHistoryFilePath;
        private List<Commit> _changeHistory;
        private string _weightProjectDir;
        private readonly List<Change> _changes = new List<Change>();

        #endregion

        #region props

        public List<WeightCollectionViewModel> Weights
        {
            get => _weights;
            set => SetProperty(ref _weights, value);
        }

        public List<Commit> ChangeHistory
        {
            get => _changeHistory;
            set => SetProperty(ref _changeHistory, value);
        }


        public ICommand SaveProjectCommand { get; }
        public ICommand ShowChangeHistoryCommand { get; }


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
            ShowChangeHistoryCommand = new DelegateCommand(ShowChangeHistory);

        }



        #endregion

        #region impl

        private void ShowChangeHistory()
        {
            if (ChangeHistory.Any())
                _dialogService.ShowDialog("CommitViewerDialog", new DialogParameters() { { "Commits", ChangeHistory } }, r => { });
        }

        private void SaveWeights()
        {
            // Load old values and compare for changes
            var (oldValues, newlyDefinedWeights) = Helpers.LoadWeights(_weightProjectDir, _constraint.WeightNames, _constraint.WeightSetCount);
            for (var index = 0; index < oldValues.Count; index++)
            {
                var weightCollectionOld = oldValues[index];
                var weightCollectionNew = Weights[index];
                var weightSetIndex = weightCollectionOld.Index;

                for (var weightIndex = 0; weightIndex < weightCollectionOld.WeightItems.Count; weightIndex++)
                {
                    var oldWeightItem = weightCollectionOld.WeightItems[weightIndex];
                    var newWeightItem = weightCollectionNew.WeightItems[weightIndex];
                    var weightName = oldWeightItem.Name;
                    if (Math.Abs(oldWeightItem.Weight - newWeightItem.Weight) > 0.00000000000001)
                    {
                        _changes.Add(new Change() { Name = $"{weightName} in weight set {weightSetIndex}", OldValue = oldWeightItem.Weight, NewValue = newWeightItem.Weight });
                    }
                }
            }


            // Save
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

            _weightProjectDir = Directory.GetParent(path).FullName;

            _weightDir = Path.Combine(_weightProjectDir, "Weights");
            _methodDir = Path.Combine(_weightProjectDir, "Methods");

            // Read weights
            WeightNamesCsv = string.Join(", ", _constraint.WeightNames);
            var weightsAndNewlyDefinedWeights =
                Helpers.LoadWeights(_weightProjectDir, _constraint.WeightNames, _constraint.WeightSetCount);
            Weights = weightsAndNewlyDefinedWeights.weightCollection;

            // Read methods
            InputNamesCsv = string.Join(", ", _constraint.InputNames);
            var (loadedMethods, missingMethods) = Helpers.LoadMethods(_weightProjectDir, _constraint.OutputNames);
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
                                        }, result1 => { SaveChangeHistoryAndCloseDialog(); });
                                    }
                                });
                        }
                        else
                        {
                            SaveChangeHistoryAndCloseDialog();
                        }
                    });
            }
        }

        private void SaveChangeHistoryAndCloseDialog()
        {
            // Record changes
            if (_changes.Any())
            {
                var commit = new Commit() { Time = DateTime.Now, Changes = _changes };
                ChangeHistory.Add(commit);
                var historyToSave = ChangeHistory.Count > 50
                    ? ChangeHistory.Skip(ChangeHistory.Count - 50).ToList()
                    : ChangeHistory;

                using (var writer = new StreamWriter(_changeHistoryFilePath))
                {
                    var serializer = new XmlSerializer(typeof(List<Commit>));
                    serializer.Serialize(writer, historyToSave);
                }
            }

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

            // Load old values and compare for changes
            var (oldValues, missingMethods) = Helpers.LoadMethods(_weightProjectDir, _constraint.OutputNames);
            for (int i = 0; i < oldValues.Count; i++)
            {
                var oldMethod = oldValues[i];
                var newMethod = CalculationMethods[i];
                if (oldMethod.MethodDefinition != newMethod.MethodDefinition)
                {
                    _changes.Add(new Change()
                    {
                        Name = $"Method: {oldMethod.OutputName}",
                        OldValue = oldMethod.MethodDefinition,
                        NewValue = newMethod.MethodDefinition
                    });
                }
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

            ChangeHistory = LoadChangeHistory();
        }


        private List<Commit> LoadChangeHistory()
        {
            var dir = Path.Combine(Constants.AppDataDir, "Changes");
            Directory.CreateDirectory(dir);
            _changeHistoryFilePath = Path.Combine(dir, "Weights.xml");
            if (!File.Exists(_changeHistoryFilePath)) return new List<Commit>();

            using (var reader = new StreamReader(_changeHistoryFilePath))
            {
                var serializer = new XmlSerializer(typeof(List<Commit>));
                return serializer.Deserialize(reader) as List<Commit>;
                
            }
        }

        #endregion
    }
}