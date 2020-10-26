using Prism.Mvvm;

namespace RemoteVisionConsole.Module
{
    public class ProcessUnitUserSetting : BindableBase
    {
        private bool _saveRawDataOnline;
        public bool SaveRawDataOnline
        {
            get { return _saveRawDataOnline; }
            set { SetProperty(ref _saveRawDataOnline, value); }
        }

        private bool _saveRawDataOffline;
        public bool SaveRawDataOffline
        {
            get { return _saveRawDataOffline; }
            set { SetProperty(ref _saveRawDataOffline, value); }
        }

        private bool _saveWeightedDataOnline;
        public bool SaveWeightedDataOnline
        {
            get { return _saveWeightedDataOnline; }
            set { SetProperty(ref _saveWeightedDataOnline, value); }
        }

        private bool _saveWeightedDataOffline;



        public bool SaveWeightedDataOffline
        {
            get { return _saveWeightedDataOffline; }
            set { SetProperty(ref _saveWeightedDataOffline, value); }
        }

        private ImageSaveSchema _imageSaveSchema;
        public ImageSaveSchema ImageSaveSchema
        {
            get { return _imageSaveSchema; }
            set { SetProperty(ref _imageSaveSchema, value); }
        }

        private string _imageSaveMainFolder;

        public string ImageSaveMainFolder
        {
            get { return _imageSaveMainFolder; }
            set { SetProperty(ref _imageSaveMainFolder, value); }
        }

        private ImageSaveFilter _imageSaveFilter;
        public ImageSaveFilter ImageSaveFilter
        {
            get { return _imageSaveFilter; }
            set { SetProperty(ref _imageSaveFilter, value); }
        }

    }
}
