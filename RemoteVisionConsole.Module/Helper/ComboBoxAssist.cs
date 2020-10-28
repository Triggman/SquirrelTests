using System;
using System.Linq;
using System.Windows.Controls;

namespace RemoteVisionConsole.Module.Helper
{

    public static class ComboBoxAssist
    {
        public static void SetEnumBinding<T>(ComboBox comboBox, string path) where T : struct, IConvertible
        {
            var itemsSource = Enum.GetValues(typeof(T)).Cast<T>();
            comboBox.ItemsSource = itemsSource;
            comboBox.SetBinding(ComboBox.SelectedItemProperty, path);
        }
    }
}
