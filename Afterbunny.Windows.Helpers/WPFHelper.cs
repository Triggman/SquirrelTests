﻿using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Forms;
using ComboBox = System.Windows.Controls.ComboBox;

namespace Afterbunny.Windows.Helpers
{
    public static class WPFHelper
    {
        public static (byte r, byte g, byte b) HexadecimalToRGB(string hex)
        {
            if (hex.StartsWith("#"))
                hex = hex.Remove(0, 1);

            byte r = (byte)HexadecimalToDecimal(hex.Substring(0, 2));
            byte g = (byte)HexadecimalToDecimal(hex.Substring(2, 2));
            byte b = (byte)HexadecimalToDecimal(hex.Substring(4, 2));

            return (r, g, b);
        }

        public static NotifyIcon SetMinimizeToTray(this Window window, string iconFilePath)
        {
            var ni = new NotifyIcon
            {
                Icon = new System.Drawing.Icon(iconFilePath),
                Visible = true
            };
            ni.DoubleClick +=
                (sender, args) =>
                {
                    window.Show();
                    window.WindowState = WindowState.Normal;
                };

            window.StateChanged += (sender, args) =>
            {
                if (window.WindowState == WindowState.Minimized)
                    window.Hide();
            };
            return ni;
        }

        public static void SetEnumBinding<T>(this ComboBox comboBox, string path) where T : struct, IConvertible
        {
            var itemsSource = Enum.GetValues(typeof(T)).Cast<T>();
            comboBox.ItemsSource = itemsSource;
            comboBox.SetBinding(Selector.SelectedItemProperty, path);
        }

        private static int HexadecimalToDecimal(string hex)
        {
            hex = hex.ToUpper();

            int hexLength = hex.Length;
            double dec = 0;

            for (int i = 0; i < hexLength; ++i)
            {
                byte b = (byte)hex[i];

                if (b >= 48 && b <= 57)
                    b -= 48;
                else if (b >= 65 && b <= 70)
                    b -= 55;

                dec += b * Math.Pow(16, ((hexLength - i) - 1));
            }

            return (int)dec;
        }
    }
}
