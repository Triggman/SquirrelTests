using RemoteVisionConsole.Interface;
using System;
using System.Linq;
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


        public static string GetFileFromDialog(string initialDir = null,
            (string[] extensions, string fileTypePrompt)? pattern = null)
        {
            if (string.IsNullOrEmpty(initialDir)) initialDir = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

            string filter;
            if (pattern != null)
            {
                var exp = string.Join(";", pattern.Value.extensions.Select(ex => $"*.{ex}"));
                filter = $"{pattern.Value.fileTypePrompt} ({exp})|{exp}";
            }
            else
            {
                filter = "All files (*.*)|*.*";
            }
            using (var openFileDialog = new OpenFileDialog())
            {
                openFileDialog.InitialDirectory = initialDir;
                openFileDialog.Filter = filter;

                if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    //Get the path of specified file
                    return openFileDialog.FileName;
                }
            }

            return string.Empty;
        }


        public static string GetDirFromDialog()
        {
            using (var fbd = new FolderBrowserDialog())
            {
                var result = fbd.ShowDialog();

                if (result == System.Windows.Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                {
                    return fbd.SelectedPath;
                }
            }

            return string.Empty;
        }

        internal static T CopyObject<T>(T o) where T : class, new()
        {
            var copy = new T();

            var type = copy.GetType();
            var props = type.GetProperties();

            foreach (var prop in props)
            {
                if (prop.CanWrite)
                    prop.SetValue(copy, prop.GetValue(o));
            }

            return copy;

        }
    }


}
