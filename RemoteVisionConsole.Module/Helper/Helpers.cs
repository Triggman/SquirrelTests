using RemoteVisionConsole.Interface;
using System;
using System.Windows.Forms;

namespace RemoteVisionConsole.Module.Helper
{
    public static class Helpers
    {

        public static bool IsVisionProcessor<TData>(Type type)
        {
            return type.IsSubclassOf(typeof(IVisionProcessor<TData>));
        }


        public static bool IsVisionAdapter<TData>(Type type)
        {
            return type.IsSubclassOf(typeof(IVisionAdapter<TData>));
        }


        public static string GetFileFromDialog(string initialDir,
            string pattern = "txt files (*.txt)|*.txt|All files (*.*)|*.*")
        {
            using (var openFileDialog = new OpenFileDialog())
            {
                openFileDialog.InitialDirectory = initialDir;
                openFileDialog.Filter = pattern;

                if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    //Get the path of specified file
                    return openFileDialog.FileName;
                }
            }

            return string.Empty;
        }
    }
}
