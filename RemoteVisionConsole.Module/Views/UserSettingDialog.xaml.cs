using System.Windows.Controls;

namespace RemoteVisionConsole.Module.Views
{
    /// <summary>
    /// Interaction logic for UserSettingDialog.xaml
    /// </summary>
    public partial class UserSettingDialog : UserControl
    {
        public UserSettingDialog()
        {
            InitializeComponent();
            ComboBoxAssist.SetEnumBinding<ImageSaveFilter>(comboBoxImageSaveFilter, "ViewModel.ImageSaveFilter");
            ComboBoxAssist.SetEnumBinding<ImageSaveSchema>(comboBoxImageSaveSchema, "ViewModel.ImageSaveSchema");
        }
    }
}
