using Afterbunny.Windows.Helpers;
using System.Windows.Controls;

namespace DistributedVisionRunner.Module.Views
{
    /// <summary>
    /// Interaction logic for UserSettingDialog.xaml
    /// </summary>
    public partial class UserSettingDialog : UserControl
    {
        public UserSettingDialog()
        {
            InitializeComponent();
            comboBoxImageSaveFilter.SetEnumBinding<ImageSaveFilter>("ViewModel.ImageSaveFilter");
            comboBoxImageSaveSchema.SetEnumBinding<ImageSaveSchema>("ViewModel.ImageSaveSchema");
        }
    }
}
