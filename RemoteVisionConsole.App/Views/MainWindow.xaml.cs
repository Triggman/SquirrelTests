using Afterbunny.Windows.Helpers;
using RemoteVisionConsole.Module;
using System;
using System.Windows;

namespace RemoteVisionConsole.App.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void MainWindow_OnClosed(object sender, EventArgs e)
        {
            FileSystemHelper.RemoveOutdatedFiles(Constants.AppDataDir, TimeSpan.FromDays(30), "log");
        }
    }
}
