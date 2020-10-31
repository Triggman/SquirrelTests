using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;

namespace DistributedVisionRunner.Module.ViewModels
{
    public class FillPreviewInputsDialogViewModel : BindableBase, IDialogAware
    {
        #region private fields

        private List<string> _weightSetSelectionList;
        private List<InputItemViewModel> _inputItems;
        private int _selectedIndex;

        #endregion

        #region props

        public List<string> WeightSetSelectionList
        {
            get => _weightSetSelectionList;
            set => SetProperty(ref _weightSetSelectionList, value);
        }

        public List<InputItemViewModel> InputItems
        {
            get => _inputItems;
            set => SetProperty(ref _inputItems, value);
        }

        public int SelectedIndex
        {
            get => _selectedIndex;
            set => SetProperty(ref _selectedIndex, value);
        }
        
        public ICommand OkCommand { get; }

        #endregion

        #region ctor

        public FillPreviewInputsDialogViewModel()
        {
            OkCommand = new DelegateCommand(FinishFilling);
        }


        public bool CanCloseDialog()
        {
            return true;
        }

        public void OnDialogClosed()
        {
            
        }

        public  void OnDialogOpened(IDialogParameters parameters)
        {
            var weightSets = parameters.GetValue<int>("WeightSets");
            var inputNames = parameters.GetValue<string[]>("InputNames");
            GenWeightSetSelectionList(weightSets);
            GenInputItems(inputNames);
        }

        public string Title { get; }
        public event Action<IDialogResult> RequestClose;

        #endregion

        #region impl

        private void FinishFilling()
        {
            var dialogResult = new DialogResult(ButtonResult.OK);
            dialogResult.Parameters.Add("SelectedSetIndex", SelectedIndex);
            var inputs = InputItems.ToDictionary(item => item.Name, item => item.Value);
            dialogResult.Parameters.Add("Inputs", inputs);
            RequestClose?.Invoke(dialogResult);
        }

        private void GenInputItems(string[] inputNames)
        {
            InputItems = inputNames.Select(name => new InputItemViewModel() {Name = name}).ToList();
        }

        private void GenWeightSetSelectionList(int weightSets)
        {
            WeightSetSelectionList = Enumerable.Range(0, weightSets).Select(num => $"Set {num}").ToList();
            SelectedIndex = 0;
        }

        #endregion
    }

    public class InputItemViewModel : BindableBase
    {
        private double _value = 1;
        public string Name { get; set; }

        public double Value
        {
            get => _value;
            set => SetProperty(ref _value, value);
        }
    }
}