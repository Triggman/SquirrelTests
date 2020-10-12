using BitMiracle.LibTiff.Classic;
using System;

namespace RemoteVisionConsole.Module.Helper
{
    internal class ImageHelper
    {
        internal static void SaveTiff(float[] data, int samplesPerPixel, int width, string path, float xRes = 100, float yRes = 100, ResUnit resUnit = ResUnit.CENTIMETER, Photometric photometric = Photometric.MINISBLACK)
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
                image.SetField(TiffTag.ROWSPERSTRIP, height);
                image.SetField(TiffTag.PHOTOMETRIC, photometric);
                image.SetField(TiffTag.FILLORDER, FillOrder.MSB2LSB);
                image.SetField(TiffTag.PLANARCONFIG, PlanarConfig.CONTIG);


                image.SetField(TiffTag.XRESOLUTION, xRes);
                image.SetField(TiffTag.YRESOLUTION, yRes);
                image.SetField(TiffTag.RESOLUTIONUNIT, resUnit);

                // Write the information to the file
                var ret = image.WriteEncodedStrip(0, byteArray, byteArray.Length);
                if (ret == -1) throw new InvalidOperationException("Error when writing the image");

                // file will be auto-closed during disposal
                // but you can close image yourself
                image.Close();
            }

        }

        internal static float[] ReadTiffAsFloatArray(string path)
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

                short bps = value[0].ToShort();
                if (bps != sizeof(float) * 8)
                {
                    throw new InvalidOperationException("Unsupported number of bits per sample");
                }





                // Read in the possibly multiple strips
                int stripSize = image.StripSize();
                int stripMax = image.NumberOfStrips();
                int imageOffset = 0;

                int bufferSize = image.NumberOfStrips() * stripSize;
                byte[] buffer = new byte[bufferSize];

                for (int stripCount = 0; stripCount < stripMax; stripCount++)
                {
                    int result = image.ReadEncodedStrip(stripCount, buffer, imageOffset, stripSize);
                    if (result == -1)
                    {
                        throw new InvalidOperationException($"Read error on input strip number {stripCount}");
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
                Buffer.BlockCopy(buffer, 0, output, 0, outputSize);

                image.Close();
            }

            return output;
        }
    }
}
