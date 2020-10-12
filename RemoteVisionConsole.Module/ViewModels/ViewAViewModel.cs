using Prism.Mvvm;
using RemoteVisionConsole.Module.Helper;
using System.Linq;

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
            var row = Enumerable.Range(1, width).Select(i => (float)i / width).ToArray();
            var data = new float[width * width];

            for (int rowIndex = 0; rowIndex < width; rowIndex++)
            {
                for (int colIndex = 0; colIndex < width; colIndex++)
                {
                    var index = rowIndex * width + colIndex;
                    data[index] = row[colIndex];
                }
            }

            ImageHelper.SaveTiff(data, 1, width, path);


            var dataRead = ImageHelper.ReadTiffAsFloatArray(path);
        }
    }
}
