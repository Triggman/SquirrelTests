using Afterbunny.Windows.Helpers;
using DistributedVisionRunner.Module;
using Squirrel;
using System;
using System.IO;
using System.Threading.Tasks;
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

            Loaded += async (sender, args) =>
            {
                await CheckForUpdateAsync();
            };
        }

        private void MainWindow_OnClosed(object sender, EventArgs e)
        {
            FileSystemHelper.RemoveOutdatedFiles(Constants.AppDataDir, TimeSpan.FromDays(30), "log");
        }

        private async Task CheckForUpdateAsync()
        {
            var parentDir = Directory.GetParent(Directory.GetCurrentDirectory()).FullName;
            var updateExe = Path.Combine(parentDir, "Update.exe");
            var released = File.Exists(updateExe);
            if (released)
            {

                using (var manager = new UpdateManager(KnownFolders.GetDefaultPath(KnownFolder.Downloads)))
                {
                    await manager.UpdateApp();
                }
            }
        }

    }
}
