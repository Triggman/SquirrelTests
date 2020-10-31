using Prism.Mvvm;

namespace DistributedVisionRunner.Module.ViewModels
{
    public class WeightItemViewModel : BindableBase
    {
        #region private fields

        private double _weight = 1;

        #endregion

        #region props

        public double Weight
        {
            get => _weight;
            set => SetProperty(ref _weight, value);
        }

        public string Name { get; set; }

        #endregion
    }
}