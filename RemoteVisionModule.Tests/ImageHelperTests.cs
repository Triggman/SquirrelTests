﻿using System;
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
            var width = 100;
            var b = 100;
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


        [Test]
        public void SaveAndLoadByteImage()
        {
            var path = "image2.tiff";
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

            ImageHelper.SaveTiff(data, 1, width, path);

            
        }
    }
    
    
}