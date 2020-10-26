using BitMiracle.LibTiff.Classic;
using NUnit.Framework;
using RemoteVisionConsole.Module.Helper;

namespace RemoteVisionModule.Tests
{
    [TestFixture]
    public class ImageHelperTests
    {
        [Test]
        public void SaveAndLoadFloatImage()
        {
            var (data, samplesPerPixel, width) = LoadRbgData();
            var fileName = "float";
            var path = $"{fileName}.tif";
            var outputData = new float[data.Length / samplesPerPixel];
            // convert bytes to short
            for (int i = 0; i < outputData.Length; i++)
            {
                outputData[i] = (float)(data[i * samplesPerPixel] / 255.0);
            }

            ImageHelper.SaveTiff(outputData, width, path);

            // Read
            var (dataRead, widthRead) = ImageHelper.ReadFloatTiff(path);
            ImageHelper.SaveTiff(dataRead, widthRead, $"{fileName}Reload.tif");
        }


        [Test]
        public void SaveAndLoadByteImage()
        {
            var (data, samplesPerPixel, width) = LoadRbgData();
            var fileName = "byte";
            var path = $"{fileName}.tif";
            var outputData = new byte[data.Length / samplesPerPixel];
            for (int i = 0; i < outputData.Length; i++)
            {
                outputData[i] = data[i * samplesPerPixel];
            }

            ImageHelper.SaveTiff(outputData, width, 1, Photometric.MINISBLACK, path);

            // Read
            var (dataRead, samplesPerPixelRead, widthRead) = ImageHelper.ReadByteTiff(path);
            ImageHelper.SaveTiff(dataRead, widthRead, samplesPerPixelRead, Photometric.MINISBLACK, $"{fileName}Reload.tif");
        }

        [Test]
        public void LoadAndSaveRGBImage()
        {
            var (data, samplesPerPixel, width) =
                ImageHelper.ReadByteTiff(
                    "Sample Data/marbles.tif");

            ImageHelper.SaveTiff(data, width, 3, Photometric.RGB, "rgb.tiff");
        }

        [Test]
        public void SaveAndLoadShortImage()
        {
            var (data, samplesPerPixel, width) = LoadRbgData();
            var fileName = "short";
            var path = $"{fileName}.tif";

            var outputData = new short[data.Length / samplesPerPixel];
            // convert bytes to short
            for (int i = 0; i < outputData.Length; i++)
            {
                outputData[i] = (short)(data[i * samplesPerPixel] / 255.0 * ushort.MaxValue + short.MinValue);
            }

            ImageHelper.SaveTiff(outputData, width, path);

            // Read
            var (dataRead, widthRead) = ImageHelper.ReadUshortTiff(path);
            ImageHelper.SaveTiff(dataRead, widthRead, $"{fileName}Reload.tif");
        }

        [Test]
        public void SaveAndReadUshort()
        {

            var (data, samplesPerPixel, width) = LoadRbgData();
            var fileName = "ushort";
            var path = $"{fileName}.tif";
            var outputData = new ushort[data.Length / samplesPerPixel];
            // convert bytes to ushorts
            for (int i = 0; i < outputData.Length; i++)
            {
                outputData[i] = (ushort)(data[i * samplesPerPixel] / 255.0 * ushort.MaxValue);
            }

            ImageHelper.SaveTiff(outputData, width, path);

            // Read
            var (dataRead, widthRead) = ImageHelper.ReadUshortTiff(path);
            ImageHelper.SaveTiff(dataRead, widthRead, $"{fileName}Reload.tif");
        }

        private static (byte[] data, int samplesPerPixel, int width) LoadRbgData()
        {
            return ImageHelper.ReadByteTiff(@"C:\Users\afterbunny\source\repos\libtiff.net-master\Samples\Sample Data\pc260001.tif");
        }
    }
}