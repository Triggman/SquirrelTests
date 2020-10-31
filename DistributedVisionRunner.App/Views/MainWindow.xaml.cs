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

            var ni = new System.Windows.Forms.NotifyIcon
            {
                //Icon = new System.Drawing.Icon("Main.ico"),
                Visible = true
            };
            ni.DoubleClick +=
                 (sender, args) =>
                {
                    this.Show();
                    this.WindowState = WindowState.Normal;
                };
        }

        private void MainWindow_OnClosed(object sender, EventArgs e)
        {
            FileSystemHelper.RemoveOutdatedFiles(Constants.AppDataDir, TimeSpan.FromDays(30), "log");
        }

        protected override void OnStateChanged(EventArgs e)
        {
            if (WindowState == System.Windows.WindowState.Minimized)
                this.Hide();

            base.OnStateChanged(e);
        }
    }
}
