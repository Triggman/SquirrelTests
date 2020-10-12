using Prism.Mvvm;
using RemoteVisionConsole.Module.Helper;
using System;

namespace RemoteVisionConsole.Module.ViewModels
{
    public class ViewAViewModel : BindableBase
    {
        private string _message;
        public string Message
        {
            get { return _message; }
            set { SetProperty(ref _message, value); }
        }

        public ViewAViewModel()
        {
            Message = "View A from your Prism Module";
        }

        public static void Test()
        {
            var path = "image.tiff";
            var width = 1000;
            var b = 1000;
            var max = (float)Math.Log(width * width, b);
            var data = new float[width * width];

            for (int rowIndex = 0; rowIndex < width; rowIndex++)
            {
                for (int colIndex = 0; colIndex < width; colIndex++)
                {
                    var value = Math.Log(colIndex * rowIndex, b);
                    data[rowIndex * width + colIndex] = (float)value / max;
                }
            }

            ImageHelper.SaveTiff(data, 1, width, path);


            var dataRead = ImageHelper.ReadTiffAsFloatArray(path);

            ImageHelper.SaveTiff(dataRead, 1, width, "image1.tiff");
        }
    }
}
