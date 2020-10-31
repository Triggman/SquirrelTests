using Afterbunny.Windows.Helpers;
using DistributedVisionRunner.Module;
using System;
using System.Windows;

namespace DistributedVisionRunner.App.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            this.SetMinimizeToTray("icon.ico");
        }

        private void MainWindow_OnClosed(object sender, EventArgs e)
        {
            FileSystemHelper.RemoveOutdatedFiles(Constants.AppDataDir, TimeSpan.FromDays(30), "log");
        }

    }
}
