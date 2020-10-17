﻿using System;
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
            var path = "image.tiff";
            var width = 70;
            var b = 70;
            var max = (float) Math.Log(width * width, b);
            var data = new float[width * width];

            for (int rowIndex = 0; rowIndex < width; rowIndex++)
            {
                for (int colIndex = 0; colIndex < width; colIndex++)
                {
                    var value = Math.Log(colIndex * rowIndex, b);
                    data[rowIndex * width + colIndex] = (float) value / max;
                }
            }

            ImageHelper.SaveTiff(data, 1, width, path);


            var (dataRead, widthRead) = ImageHelper.ReadFloatTiff(path);

            ImageHelper.SaveTiff(dataRead, 1, width, "image1.tiff");
        }


        [Test]
        public void SaveAndLoadByteImage()
        {
            var path = "imageByte.tiff";
            var width = 100;
            var b = 100;
            var data = new byte[width * width];

            for (int rowIndex = 0; rowIndex < width; rowIndex++)
            {
                for (int colIndex = 0; colIndex < width; colIndex++)
                {
                    data[rowIndex * width + colIndex] = (byte) (rowIndex + colIndex);
                }
            }

            ImageHelper.SaveTiff(data, width, 1, Photometric.MINISWHITE, "grayscale.tiff");

            var (dataRead, samplesPerPixel, widthRead) = ImageHelper.ReadByteTiff("grayscale.tiff");
            ImageHelper.SaveTiff(dataRead, width, samplesPerPixel, Photometric.MINISBLACK, "grayscaleRewrite.tiff");
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
            var path = "imageShort.tiff";
            var width = 100;
            var b = 100;
            var data = new short[width * width];
            var max = (double) width + width;

            for (int rowIndex = 0; rowIndex < width; rowIndex++)
            {
                for (int colIndex = 0; colIndex < width; colIndex++)
                {
                    data[rowIndex * width + colIndex] = (short) ((colIndex + rowIndex) / max * short.MaxValue);
                }
            }

            ImageHelper.SaveTiff(data, 100,  path);

            // Load
            var (dataRead, widthRead) = ImageHelper.ReadShortTiff(path);
            ImageHelper.SaveTiff(dataRead, widthRead, "imageShortReload.tiff");
        }

        [Test]
        public void SaveAndReadUshort()
        {

            var (data, samplesPerPixel, width) = ImageHelper.ReadByteTiff("Sample Data/marbles.tif");
            var path = "ushort.tiff";
            var outputData = new ushort[data.Length / samplesPerPixel];
            // convert bytes to ushorts
            for (int i = 0; i < outputData.Length; i++)
            {
                outputData[i] = (ushort) (data[i*samplesPerPixel] / 255.0 * ushort.MaxValue);
            }
            
            ImageHelper.SaveTiff(outputData, width,  path);
            
            // Read
            var (dataRead, widthRead) = ImageHelper.ReadUshortTiff(path);
            ImageHelper.SaveTiff(dataRead, widthRead, "ushortReload.tiff");
        }
    }
}