using System.Collections.Generic;
using Prism.Mvvm;

namespace RemoteVisionConsole.Module.ViewModels
{
    public class WeightCollectionViewModel : BindableBase
    {

        #region private fields

        private bool _needToSave;

        #endregion

        #region props

        public List<WeightItemViewModel> WeightItems { get; }



        public int Index { get; set; }

        public bool NeedToSave
        {
            get => _needToSave;
            set => SetProperty(ref _needToSave, value);
        }

        #endregion


        #region ctor

        public WeightCollectionViewModel(List<WeightItemViewModel> weightItems)
        {
            WeightItems = weightItems;
        }


        #endregion


        
        
    }
}