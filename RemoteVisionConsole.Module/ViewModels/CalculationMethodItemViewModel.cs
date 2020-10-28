using Prism.Mvvm;

namespace RemoteVisionConsole.Module.ViewModels
{
    public class CalculationMethodItemViewModel : BindableBase
    {
        #region private fields

        private string _outputName;
        private string _methodDefinition;
        private bool _errored = true;

        #endregion

        #region props

        public string OutputName
        {
            get => _outputName;
            set => SetProperty(ref _outputName, value);
        }

        public string MethodDefinition
        {
            get => _methodDefinition;
            set => SetProperty(ref _methodDefinition, value);
        }

        public bool Errored
        {
            get => _errored;
            set => SetProperty(ref _errored, value);
        }

        #endregion

        #region ctor

        public CalculationMethodItemViewModel()
        {
            PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == nameof(OutputName) || args.PropertyName == nameof(MethodDefinition))
                {
                    Errored = string.IsNullOrWhiteSpace(OutputName) || string.IsNullOrWhiteSpace(MethodDefinition);
                }
            };
        }

        #endregion
    }
}