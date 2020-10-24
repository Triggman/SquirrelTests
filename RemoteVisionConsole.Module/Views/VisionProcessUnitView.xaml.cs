using System;
using System.Collections.ObjectModel;
using System.Dynamic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace RemoteVisionConsole.Module.Views
{
    /// <summary>
    /// Interaction logic for VisionProcessUnitView.xaml
    /// </summary>
    public partial class VisionProcessUnitView : UserControl
    {

        public VisionProcessUnitView()
        {
            InitializeComponent();

            RenderOptions.SetBitmapScalingMode(LoadedImageImage, BitmapScalingMode.HighQuality);

        }

   
    }
}
