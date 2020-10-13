﻿using BitMiracle.LibTiff.Classic;
using System;

namespace RemoteVisionConsole.Module.Helper
{
    public class ImageHelper
    {
        public static void SaveTiff(float[] data, int samplesPerPixel, int width, string path, float xRes = 100, float yRes = 100, ResUnit resUnit = ResUnit.CENTIMETER, Photometric photometric = Photometric.MINISBLACK)
        {
            var perfectDivided = data.Length % (samplesPerPixel * width) == 0;
            if (!perfectDivided)
                throw new InvalidOperationException($"Data length({data.Length}) is incompatible with current samplesPerPixel({samplesPerPixel}) and width({width})");

            var byteArray = new byte[data.Length * 4];
            Buffer.BlockCopy(data, 0, byteArray, 0, byteArray.Length);

            // Calcualte suitable ROWSPERSTRIP
            var optimalSizePerStrip = 8000;
            var optimalRowsPerStrip = optimalSizePerStrip / sizeof(float) / width / samplesPerPixel;
            var height = data.Length / samplesPerPixel / width;

            using (Tiff image = Tiff.Open(path, "w"))
            {
                if (image == null)
                {
                    throw new InvalidOperationException($"Can not open image: {path}");
                }

                // We need to set some values for basic tags before we can add any data
                image.SetField(TiffTag.SAMPLEFORMAT, SampleFormat.IEEEFP);
                image.SetField(TiffTag.IMAGEWIDTH, width);
                image.SetField(TiffTag.IMAGELENGTH, height);
                image.SetField(TiffTag.BITSPERSAMPLE, sizeof(float) * 8);
                image.SetField(TiffTag.SAMPLESPERPIXEL, samplesPerPixel);
                image.SetField(TiffTag.ROWSPERSTRIP, optimalRowsPerStrip);
                image.SetField(TiffTag.PHOTOMETRIC, photometric);
                image.SetField(TiffTag.FILLORDER, FillOrder.MSB2LSB);
                image.SetField(TiffTag.PLANARCONFIG, PlanarConfig.CONTIG);

                image.SetField(TiffTag.XRESOLUTION, xRes);
                image.SetField(TiffTag.YRESOLUTION, yRes);
                image.SetField(TiffTag.RESOLUTIONUNIT, resUnit);

                var maxStrip = Math.Ceiling((float)height / optimalRowsPerStrip);
                var byteCountEachRow = width * samplesPerPixel * sizeof(float);
                for (int stripIndex = 0; stripIndex < maxStrip; stripIndex++)
                {
                    // Write the information to the file
                    var currentStripLength = (stripIndex == maxStrip - 1) && (height % optimalRowsPerStrip != 0) ?
                        height % optimalRowsPerStrip : optimalRowsPerStrip;
                    var offset = stripIndex * byteCountEachRow * optimalRowsPerStrip;

                    var ret = image.WriteEncodedStrip(stripIndex, byteArray, offset, currentStripLength * byteCountEachRow);
                    if (ret == -1) throw new InvalidOperationException("Error when writing the image");
                }

                // file will be auto-closed during disposal
                // but you can close image yourself
                image.Close();
            }
        }

        public static float[] ReadTiffAsFloatArray(string path)
        {
            float[] output = null;
            // Open the TIFF image
            using (Tiff image = Tiff.Open(path, "r"))
            {
                if (image == null)
                {
                    throw new InvalidOperationException("Could not open incoming image");
                }

                // Check that it is of a type that we support
                FieldValue[] value = image.GetField(TiffTag.BITSPERSAMPLE);
                if (value == null)
                {
                    throw new InvalidOperationException("Undefined number of bits per sample");
                }

                short bitsPerSample = value[0].ToShort();
                if (bitsPerSample != sizeof(float) * 8)
                {
                    throw new InvalidOperationException("Unsupported number of bits per sample");
                }

                // Check that it is of a type that we support
                value = image.GetField(TiffTag.ROWSPERSTRIP);
                if (value == null)
                {
                    throw new InvalidOperationException("Undefined number of ROWSPERSTRIP");
                }

                short rowsPerStrip = value[0].ToShort();

                // Get image width
                value = image.GetField(TiffTag.IMAGEWIDTH);
                if (value == null)
                {
                    throw new InvalidOperationException("Undefined number of IMAGEWIDTH");
                }

                var width = value[0].ToShort();

                // Get samples per pixel
                value = image.GetField(TiffTag.SAMPLESPERPIXEL);
                if (value == null)
                {
                    throw new InvalidOperationException("Undefined number of SAMPLESPERPIXEL");
                }

                var samplesPerPixel = value[0].ToShort();

                // Check that it is of a type that we support
                value = image.GetField(TiffTag.IMAGELENGTH);
                if (value == null)
                {
                    throw new InvalidOperationException("Undefined number of IMAGELENGTH");
                }

                var height = value[0].ToShort();

                // Read in the possibly multiple strips
                int stripSize = image.StripSize();
                int stripCount = image.NumberOfStrips();
                int imageOffset = 0;

                var bytesPerRow = width * sizeof(float) * samplesPerPixel;
                int bufferSize = bytesPerRow * height;
                byte[] buffer = new byte[bufferSize];

                for (int stripIndex = 0; stripIndex < stripCount; stripIndex++)
                {
                    int result = image.ReadEncodedStrip(stripIndex, buffer, imageOffset, stripSize);
                    if (result == -1)
                    {
                        throw new InvalidOperationException($"Read error on input strip number {stripIndex}");
                    }

                    imageOffset += result;
                }

                // Deal with fillorder
                value = image.GetField(TiffTag.FILLORDER);
                if (value == null)
                {
                    throw new InvalidOperationException("Image has an undefined fillorder");
                }

                FillOrder fillorder = (FillOrder)value[0].ToInt();
                if (fillorder != FillOrder.MSB2LSB)
                {
                    // We need to swap bits -- ABCDEFGH becomes HGFEDCBA
                    System.Console.Out.WriteLine("Fixing the fillorder");

                    for (int count = 0; count < bufferSize; count++)
                    {
                        byte tempbyte = 0;
                        if ((buffer[count] & 128) != 0) tempbyte += 1;
                        if ((buffer[count] & 64) != 0) tempbyte += 2;
                        if ((buffer[count] & 32) != 0) tempbyte += 4;
                        if ((buffer[count] & 16) != 0) tempbyte += 8;
                        if ((buffer[count] & 8) != 0) tempbyte += 16;
                        if ((buffer[count] & 4) != 0) tempbyte += 32;
                        if ((buffer[count] & 2) != 0) tempbyte += 64;
                        if ((buffer[count] & 1) != 0) tempbyte += 128;
                        buffer[count] = tempbyte;
                    }
                }

                var outputSize = buffer.Length / sizeof(float);
                output = new float[outputSize];
                Buffer.BlockCopy(buffer, 0, output, 0, bufferSize);

                image.Close();
            }

            return output;
        }

        public static void SaveTiff(byte[] data, int samplesPerPixel, int width, string path, float xRes = 100, float yRes = 100, ResUnit resUnit = ResUnit.CENTIMETER, Photometric photometric = Photometric.MINISBLACK)
        {
            var perfectDivided = data.Length % (samplesPerPixel * width) == 0;
            if (!perfectDivided)
                throw new InvalidOperationException($"Data length({data.Length}) is incompatible with current samplesPerPixel({samplesPerPixel}) and width({width})");

            // Calcualte suitable ROWSPERSTRIP
            var optimalSizePerStrip = 8000;
            var optimalRowsPerStrip = optimalSizePerStrip / width / samplesPerPixel;
            var height = data.Length / samplesPerPixel / width;

            using (Tiff image = Tiff.Open(path, "w"))
            {
                if (image == null)
                {
                    throw new InvalidOperationException($"Can not open image: {path}");
                }

                // We need to set some values for basic tags before we can add any data
                image.SetField(TiffTag.SAMPLEFORMAT, SampleFormat.UINT);
                image.SetField(TiffTag.IMAGEWIDTH, width);
                image.SetField(TiffTag.IMAGELENGTH, height);
                image.SetField(TiffTag.BITSPERSAMPLE, 8);
                image.SetField(TiffTag.SAMPLESPERPIXEL, samplesPerPixel);
                image.SetField(TiffTag.ROWSPERSTRIP, optimalRowsPerStrip);
                image.SetField(TiffTag.PHOTOMETRIC, photometric);
                image.SetField(TiffTag.FILLORDER, FillOrder.MSB2LSB);
                image.SetField(TiffTag.PLANARCONFIG, PlanarConfig.CONTIG);

                image.SetField(TiffTag.XRESOLUTION, xRes);
                image.SetField(TiffTag.YRESOLUTION, yRes);
                image.SetField(TiffTag.RESOLUTIONUNIT, resUnit);

                var maxStrip = Math.Ceiling((float)height / optimalRowsPerStrip);
                var byteCountEachRow = data.Length / height;
                for (int stripIndex = 0; stripIndex < maxStrip; stripIndex++)
                {
                    // Write the information to the file
                    var currentStripLength = (stripIndex == maxStrip - 1) && (height % optimalRowsPerStrip != 0) ?
                        height % optimalRowsPerStrip : optimalRowsPerStrip;
                    var offset = stripIndex * byteCountEachRow * optimalRowsPerStrip;

                    var ret = image.WriteEncodedStrip(stripIndex, data, offset, currentStripLength * byteCountEachRow);
                    if (ret == -1) throw new InvalidOperationException("Error when writing the image");
                }

                // file will be auto-closed during disposal
                // but you can close image yourself
                image.Close();
            }
        }

        public static (byte[] data, int channels) ReadTiffAsByteArray(string path)
        {
            var buffer = new byte[0];
            var samplesPerPixel = 0;
            using (Tiff image = Tiff.Open(path, "r"))
            {
                if (image == null)
                {
                    throw new InvalidOperationException("Could not open incoming image");
                }

                // Check that it is of a type that we support
                FieldValue[] value = image.GetField(TiffTag.BITSPERSAMPLE);
                if (value == null)
                {
                    throw new InvalidOperationException("Undefined number of bits per sample");
                }

                short bitsPerSample = value[0].ToShort();
                if (bitsPerSample != 8)
                {
                    throw new InvalidOperationException("Unsupported number of bits per sample");
                }

                // Get channel count
                value = image.GetField(TiffTag.SAMPLESPERPIXEL);
                if (value == null)
                {
                    throw new InvalidOperationException("Undefined number of SAMPLESPERPIXEL");
                }

                samplesPerPixel = value[0].ToShort();

                // Check that it is of a type that we support
                value = image.GetField(TiffTag.ROWSPERSTRIP);
                if (value == null)
                {
                    throw new InvalidOperationException("Undefined number of ROWSPERSTRIP");
                }

                var rowsPerStrip = value[0].ToShort();

                // Check that it is of a type that we support
                value = image.GetField(TiffTag.IMAGELENGTH);
                if (value == null)
                {
                    throw new InvalidOperationException("Undefined number of IMAGELENGTH");
                }

                var height = value[0].ToShort();

                // Read in the possibly multiple strips
                var stripSize = image.StripSize();
                var stripCount = image.NumberOfStrips();
                var imageOffset = 0;

                var bytesPerRow = stripSize / rowsPerStrip;
                var bufferSize = bytesPerRow * height;
                buffer = new byte[bufferSize];

                for (int stripIndex = 0; stripIndex < stripCount; stripIndex++)
                {
                    int result = image.ReadEncodedStrip(stripIndex, buffer, imageOffset, stripSize);
                    if (result == -1)
                    {
                        throw new InvalidOperationException($"Read error on input strip number {stripIndex}");
                    }

                    imageOffset += result;
                }

                // Deal with fillorder
                value = image.GetField(TiffTag.FILLORDER);
                if (value == null)
                {
                    throw new InvalidOperationException("Image has an undefined fillorder");
                }

                FillOrder fillorder = (FillOrder)value[0].ToInt();
                if (fillorder != FillOrder.MSB2LSB)
                {
                    // We need to swap bits -- ABCDEFGH becomes HGFEDCBA
                    System.Console.Out.WriteLine("Fixing the fillorder");

                    for (int count = 0; count < bufferSize; count++)
                    {
                        byte tempbyte = 0;
                        if ((buffer[count] & 128) != 0) tempbyte += 1;
                        if ((buffer[count] & 64) != 0) tempbyte += 2;
                        if ((buffer[count] & 32) != 0) tempbyte += 4;
                        if ((buffer[count] & 16) != 0) tempbyte += 8;
                        if ((buffer[count] & 8) != 0) tempbyte += 16;
                        if ((buffer[count] & 4) != 0) tempbyte += 32;
                        if ((buffer[count] & 2) != 0) tempbyte += 64;
                        if ((buffer[count] & 1) != 0) tempbyte += 128;
                        buffer[count] = tempbyte;
                    }
                }

                var outputSize = buffer.Length / sizeof(float);

                image.Close();
            }

            return (buffer, samplesPerPixel);
        }
    }
}