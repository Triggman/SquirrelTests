using System;
using System.IO;
using BitMiracle.LibTiff.Classic;

namespace DistributedVisionRunner.Interface
{
    public class ImageHelper
    {
        public static void SaveTiff(float[] data, int width, string path, float xRes = 100,
            float yRes = 100, ResUnit resUnit = ResUnit.CENTIMETER, Photometric photometric = Photometric.MINISBLACK)
        {
            var samplesPerPixel = 1;
            var perfectDivided = data.Length % (samplesPerPixel * width) == 0;
            if (!perfectDivided)
                throw new InvalidOperationException(
                    $"Data length({data.Length}) is incompatible with current samplesPerPixel({samplesPerPixel}) and width({width})");

            var byteArray = new byte[data.Length * 4];
            Buffer.BlockCopy(data, 0, byteArray, 0, byteArray.Length);

            // Calcualte suitable ROWSPERSTRIP
            var optimalSizePerStrip = 8000.0;
            var optimalRowsPerStripDouble = optimalSizePerStrip / sizeof(float) / width / samplesPerPixel;
            var optimalRowsPerStrip = optimalRowsPerStripDouble < 1 ? 1 : (int)optimalRowsPerStripDouble;
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
                    var currentStripLength = (stripIndex == maxStrip - 1) && (height % optimalRowsPerStrip != 0)
                        ? height % optimalRowsPerStrip
                        : optimalRowsPerStrip;
                    var offset = stripIndex * byteCountEachRow * optimalRowsPerStrip;

                    var ret = image.WriteEncodedStrip(stripIndex, byteArray, offset,
                        currentStripLength * byteCountEachRow);
                    if (ret == -1) throw new InvalidOperationException("Error when writing the image");
                }

                // file will be auto-closed during disposal
                // but you can close image yourself
                image.Close();
            }
        }

        public static (float[] data, int width) ReadFloatTiff(string path)
        {
            if (!File.Exists(path)) throw new FileNotFoundException($"File not exists: {path}");

            float[] output = null;
            int width;
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

                width = value[0].ToShort();

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
                    Console.Out.WriteLine("Fixing the fillorder");

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

            return (output, width);
        }


        /// <summary>
        /// Write byte images, support single channel 8 bit grayscale image and 24 bit RGB image
        /// </summary>
        /// <param name="data"></param>
        /// <param name="width">Number of pixels in one row</param>
        /// <param name="samplesPerPixel">1 for grayscale image, 3 for RGB image</param>
        /// <param name="photo">
        ///     <see cref="Photometric.MINISBLACK"/> or <see cref="Photometric.MINISWHITE"/> for grayscale image
        ///     <see cref="Photometric.RGB"/> for RGB image
        /// </param>
        /// <param name="path"></param>
        /// <param name="xRes"></param>
        /// <param name="yRes"></param>
        /// <param name="resUnit"></param>
        public static void SaveTiff(byte[] data, int width, int samplesPerPixel, Photometric photo, string path,
            float xRes = 100, float yRes = 100, ResUnit resUnit = ResUnit.CENTIMETER)
        {
            var optimalSizePerStrip = 8000.0;
            var optimalRowsPerStripDouble = optimalSizePerStrip / sizeof(byte) / width / samplesPerPixel;
            var optimalRowsPerStrip = optimalRowsPerStripDouble < 1 ? 1 : (int)optimalRowsPerStripDouble;
            var height = data.Length / width / samplesPerPixel;
            using (var output = Tiff.Open(path, "w"))
            {
                output.SetField(TiffTag.IMAGEWIDTH, width);
                output.SetField(TiffTag.IMAGELENGTH, height);
                output.SetField(TiffTag.SAMPLESPERPIXEL, samplesPerPixel);
                output.SetField(TiffTag.BITSPERSAMPLE, 8);
                output.SetField(TiffTag.ROWSPERSTRIP, optimalRowsPerStrip);
                output.SetField(TiffTag.PHOTOMETRIC, (int)photo);
                output.SetField(TiffTag.PLANARCONFIG, PlanarConfig.CONTIG);
                output.SetField(TiffTag.RESOLUTIONUNIT, resUnit);
                output.SetField(TiffTag.XRESOLUTION, xRes);
                output.SetField(TiffTag.YRESOLUTION, yRes);

                var stripCount = Math.Ceiling((float)height / optimalRowsPerStrip);

                var stripSize = optimalRowsPerStrip * width * samplesPerPixel;
                for (int stripIndex = 0; stripIndex < stripCount; stripIndex++)
                {
                    var start = stripIndex * stripSize;
                    var bytesToWrite = (stripIndex == stripCount - 1) && (height % optimalRowsPerStrip != 0)
                        ? (height * width * samplesPerPixel - start)
                        : stripSize;
                    output.WriteEncodedStrip(stripIndex, data, start, bytesToWrite);
                }

                output.Close();
            }
        }

        public static (byte[] data, int samplesPerPixel, int width) ReadByteTiff(string path)
        {
            if (!File.Exists(path)) throw new FileNotFoundException($"File not exists: {path}");

            byte[] buffer = null;
            int samplesPerPixel;
            int width;
            using (var input = Tiff.Open(path, "r"))
            {
                if (input == null)
                {
                    throw new InvalidOperationException("Could not open incoming image");
                }

                width = input.GetField(TiffTag.IMAGEWIDTH)[0].ToInt();
                int height = input.GetField(TiffTag.IMAGELENGTH)[0].ToInt();
                samplesPerPixel = input.GetField(TiffTag.SAMPLESPERPIXEL)[0].ToInt();
                int rowsPerStrip = input.GetField(TiffTag.ROWSPERSTRIP)[0].ToInt();
                var stripSize = input.StripSize();


                buffer = new byte[height * width * samplesPerPixel];

                var stripCount = Math.Ceiling((float)height / rowsPerStrip);

                for (int stripIndex = 0; stripIndex < stripCount; stripIndex++)
                {
                    var start = stripIndex * stripSize;
                    var bytesToRead = (stripIndex == stripCount - 1) && (height % rowsPerStrip != 0)
                        ? (height * width * samplesPerPixel - start)
                        : stripSize;
                    input.ReadEncodedStrip(stripIndex, buffer, start, bytesToRead);
                }

                input.Close();
            }

            return (buffer, samplesPerPixel, width);
        }


        public static void SaveTiff(short[] data, int width, string path, float xRes = 100,
            float yRes = 100, ResUnit resUnit = ResUnit.CENTIMETER)
        {
            var samplesPerPixel = 1;
            var photometric = Photometric.MINISBLACK;
            var perfectDivided = data.Length % (samplesPerPixel * width) == 0;
            if (!perfectDivided)
                throw new InvalidOperationException(
                    $"Data length({data.Length}) is incompatible with current samplesPerPixel({samplesPerPixel}) and width({width})");

            var byteArray = new byte[data.Length * sizeof(short)];
            Buffer.BlockCopy(data, 0, byteArray, 0, byteArray.Length);

            // Calcualte suitable ROWSPERSTRIP
            var optimalSizePerStrip = 8000.0;
            var optimalRowsPerStripDouble = optimalSizePerStrip / sizeof(short) / width / samplesPerPixel;
            var optimalRowsPerStrip = optimalRowsPerStripDouble < 1 ? 1 : (int)optimalRowsPerStripDouble;
            var height = data.Length / samplesPerPixel / width;

            using (Tiff image = Tiff.Open(path, "w"))
            {
                if (image == null)
                {
                    throw new InvalidOperationException($"Can not open image: {path}");
                }

                // We need to set some values for basic tags before we can add any data
                image.SetField(TiffTag.SAMPLEFORMAT, SampleFormat.INT);
                image.SetField(TiffTag.IMAGEWIDTH, width);
                image.SetField(TiffTag.IMAGELENGTH, height);
                image.SetField(TiffTag.BITSPERSAMPLE, sizeof(short) * 8);
                image.SetField(TiffTag.SAMPLESPERPIXEL, samplesPerPixel);
                image.SetField(TiffTag.ROWSPERSTRIP, optimalRowsPerStrip);
                image.SetField(TiffTag.PHOTOMETRIC, photometric);
                image.SetField(TiffTag.FILLORDER, FillOrder.MSB2LSB);
                image.SetField(TiffTag.COMPRESSION, Compression.NONE);
                image.SetField(TiffTag.PLANARCONFIG, PlanarConfig.CONTIG);

                image.SetField(TiffTag.XRESOLUTION, xRes);
                image.SetField(TiffTag.YRESOLUTION, yRes);
                image.SetField(TiffTag.RESOLUTIONUNIT, resUnit);

                var maxStrip = Math.Ceiling((float)height / optimalRowsPerStrip);
                var byteCountEachRow = width * samplesPerPixel * sizeof(short);
                for (int stripIndex = 0; stripIndex < maxStrip; stripIndex++)
                {
                    // Write the information to the file
                    var currentStripLength = (stripIndex == maxStrip - 1) && (height % optimalRowsPerStrip != 0)
                        ? height % optimalRowsPerStrip
                        : optimalRowsPerStrip;
                    var offset = stripIndex * byteCountEachRow * optimalRowsPerStrip;

                    var ret = image.WriteEncodedStrip(stripIndex, byteArray, offset,
                        currentStripLength * byteCountEachRow);
                    if (ret == -1) throw new InvalidOperationException("Error when writing the image");
                }

                // file will be auto-closed during disposal
                // but you can close image yourself
                image.Close();
            }
        }

        public static (short[] data, int width) ReadShortTiff(string path)
        {
            if (!File.Exists(path)) throw new FileNotFoundException($"File not exists: {path}");

            short[] output = null;
            int width;
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
                if (bitsPerSample != sizeof(short) * 8)
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

                width = value[0].ToShort();

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

                var bytesPerRow = width * sizeof(short) * samplesPerPixel;
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
                    throw new InvalidOperationException("Image has an undefined FILLORDER");
                }

                FillOrder fillorder = (FillOrder)value[0].ToInt();
                if (fillorder != FillOrder.MSB2LSB)
                {
                    // We need to swap bits -- ABCDEFGH becomes HGFEDCBA
                    Console.Out.WriteLine("Fixing the fillorder");

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

                var outputSize = buffer.Length / sizeof(short);
                output = new short[outputSize];
                Buffer.BlockCopy(buffer, 0, output, 0, bufferSize);

                image.Close();
            }

            return (output, width);
        }

        public static void SaveTiff(ushort[] data, int width, string path, float xRes = 100,
            float yRes = 100, ResUnit resUnit = ResUnit.CENTIMETER)
        {
            var samplesPerPixel = 1;
            var photometric = Photometric.MINISBLACK;
            var perfectDivided = data.Length % (samplesPerPixel * width) == 0;
            if (!perfectDivided)
                throw new InvalidOperationException(
                    $"Data length({data.Length}) is incompatible with current samplesPerPixel({samplesPerPixel}) and width({width})");

            var byteArray = new byte[data.Length * sizeof(ushort)];
            Buffer.BlockCopy(data, 0, byteArray, 0, byteArray.Length);

            // Calcualte suitable ROWSPERSTRIP
            var optimalSizePerStrip = 8000.0;
            var optimalRowsPerStripDouble = optimalSizePerStrip / sizeof(ushort) / width / samplesPerPixel;
            var optimalRowsPerStrip = optimalRowsPerStripDouble < 1 ? 1 : (int)optimalRowsPerStripDouble;
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
                image.SetField(TiffTag.BITSPERSAMPLE, sizeof(ushort) * 8);
                image.SetField(TiffTag.SAMPLESPERPIXEL, samplesPerPixel);
                image.SetField(TiffTag.ROWSPERSTRIP, optimalRowsPerStrip);
                image.SetField(TiffTag.PHOTOMETRIC, photometric);
                image.SetField(TiffTag.FILLORDER, FillOrder.MSB2LSB);
                image.SetField(TiffTag.COMPRESSION, Compression.NONE);
                image.SetField(TiffTag.PLANARCONFIG, PlanarConfig.CONTIG);

                image.SetField(TiffTag.XRESOLUTION, xRes);
                image.SetField(TiffTag.YRESOLUTION, yRes);
                image.SetField(TiffTag.RESOLUTIONUNIT, resUnit);

                var maxStrip = Math.Ceiling((float)height / optimalRowsPerStrip);
                var byteCountEachRow = width * samplesPerPixel * sizeof(ushort);
                for (int stripIndex = 0; stripIndex < maxStrip; stripIndex++)
                {
                    // Write the information to the file
                    var currentStripLength = (stripIndex == maxStrip - 1) && (height % optimalRowsPerStrip != 0)
                        ? height % optimalRowsPerStrip
                        : optimalRowsPerStrip;
                    var offset = stripIndex * byteCountEachRow * optimalRowsPerStrip;

                    var ret = image.WriteEncodedStrip(stripIndex, byteArray, offset,
                        currentStripLength * byteCountEachRow);
                    if (ret == -1) throw new InvalidOperationException("Error when writing the image");
                }

                // file will be auto-closed during disposal
                // but you can close image yourself
                image.Close();
            }
        }

        public static (ushort[] data, int width) ReadUshortTiff(string path)
        {
            if (!File.Exists(path)) throw new FileNotFoundException($"File not exists: {path}");

            ushort[] output = null;
            int width;
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
                if (bitsPerSample != sizeof(ushort) * 8)
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

                width = value[0].ToShort();

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

                var bytesPerRow = width * sizeof(ushort) * samplesPerPixel;
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
                    throw new InvalidOperationException("Image has an undefined FILLORDER");
                }

                FillOrder fillorder = (FillOrder)value[0].ToInt();
                if (fillorder != FillOrder.MSB2LSB)
                {
                    // We need to swap bits -- ABCDEFGH becomes HGFEDCBA
                    Console.Out.WriteLine("Fixing the fillorder");

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

                var outputSize = buffer.Length / sizeof(ushort);
                output = new ushort[outputSize];
                Buffer.BlockCopy(buffer, 0, output, 0, bufferSize);

                image.Close();
            }

            return (output, width);
        }
    }
}