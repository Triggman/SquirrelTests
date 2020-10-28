using Prism.Mvvm;

namespace RemoteVisionConsole.Module.Models
{
    public class ProcessUnitUserSetting : BindableBase
    {
        private bool _saveRawDataOnline;
        public bool SaveRawDataOnline
        {
            get => _saveRawDataOnline;
            set => SetProperty(ref _saveRawDataOnline, value);
        }

        private bool _saveRawDataOffline;
        public bool SaveRawDataOffline
        {
            get => _saveRawDataOffline;
            set => SetProperty(ref _saveRawDataOffline, value);
        }

        private bool _saveWeightedDataOnline;
        public bool SaveWeightedDataOnline
        {
            get => _saveWeightedDataOnline;
            set => SetProperty(ref _saveWeightedDataOnline, value);
        }

        private bool _saveWeightedDataOffline;



        public bool SaveWeightedDataOffline
        {
            get => _saveWeightedDataOffline;
            set => SetProperty(ref _saveWeightedDataOffline, value);
        }

        private ImageSaveSchema _imageSaveSchema;
        public ImageSaveSchema ImageSaveSchema
        {
            get => _imageSaveSchema;
            set => SetProperty(ref _imageSaveSchema, value);
        }

        private string _imageSaveMainFolder;

        public string ImageSaveMainFolder
        {
            get => _imageSaveMainFolder;
            set => SetProperty(ref _imageSaveMainFolder, value);
        }

        private ImageSaveFilter _imageSaveFilter;
        public ImageSaveFilter ImageSaveFilter
        {
            get => _imageSaveFilter;
            set => SetProperty(ref _imageSaveFilter, value);
        }

    }
}
