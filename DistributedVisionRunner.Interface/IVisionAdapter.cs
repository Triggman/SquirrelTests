using System.Collections.Generic;

namespace DistributedVisionRunner.Interface
{
    /// <summary>
    /// Should be implemented by software engineers
    /// Adapt input and output from and to <see cref="IVisionProcessor{TData}"/>
    /// </summary>
    /// <typeparam name="TOutput">The type of array that is accepted by <see cref="IVisionProcessor{TData}"/></typeparam>
    public interface IVisionAdapter<TOutput>
    {
        /// <summary>
        /// Will be used as the name of the tab
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Any valid ZeroMQ address, for example, "tcp://localhost:6000"
        /// </summary>
        string ZeroMQAddress { get; }

        /// <summary>
        /// Name of the project, for example, 995X.
        /// This will be used as information to create database file
        /// </summary>
        string ProjectName { get; }

        /// <summary>
        /// Whether outputs from <see cref="IVisionProcessor{TData}"/>
        /// will be recalculated and output results with different names
        /// </summary>
        bool EnableWeighting { get; }

        /// <summary>
        /// Equals to count of Cavity
        /// </summary>
        int WeightSetCount { get; }


        /// <summary>
        /// extensions: for example, new []{tif, tiff}
        /// fileTypePrompt: for example, "tif image files"
        /// 
        /// Will be used to filter files when RunSingleFile file dialog is opened
        /// </summary>
        (string[] extensions, string fileTypePrompt)? ImageFileFilter { get; }

        /// <summary>
        /// The final output names that will be send to ALC
        /// </summary>
        (string[] floatNames, string[] integerNames, string[] textNames) OutputNames { get; }

        /// <summary>
        /// Parameters that define how the image for displaying will be shown
        /// </summary>
        GraphicMetaData GraphicMetaData { get; }




        /// <summary>
        /// Convert byte array to any type of array for <see cref="IVisionProcessor{TData}"/>
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        List<TOutput[]> ConvertInput(byte[] input);

        /// <summary>
        /// Deduce result type based on statistics result
        /// </summary>
        /// <param name="statistics"></param>
        /// <returns></returns>
        ResultType GetResultType(Statistics statistics);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="imageData"></param>
        /// <param name="mainFolder"></param>
        /// <param name="subFolder"></param>
        /// <param name="fileName"></param>
        /// <param name="exceptionDetail">null when no error occours during image processing</param>
        void SaveImage(List<TOutput[]> imageData, string mainFolder, string subFolder, string fileName, string exceptionDetail);

        /// <summary>
        /// Read data from file
        /// The output will be directly fed to <see cref="IVisionProcessor{TData}"/>
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        List<TOutput[]> ReadFile(string path);

    }

    public class GraphicMetaData
    {
        /// <summary>
        /// The type of the displaying image
        /// </summary>
        public DataSampleType SampleType { get; set; }

        /// <summary>
        /// The dimensions of each image for displaying
        /// </summary>
        public (int width, int height)[] Dimensions { get; set; }

        /// <summary>
        /// Whether images should be displayed
        /// </summary>
        public bool ShouldDisplay { get; set; }

        /// <summary>
        /// Range to define max and min values for the displaying images
        /// Images within this range will be rescaled to (0, 255) for displaying
        /// if the image data type is not byte. Otherwise, it will be ignored
        /// </summary>
        public (float min, float max) DataRange { get; }
    }

    public enum DataSampleType
    {
        /// <summary>
        /// Curve
        /// </summary>
        OneDimension,
        /// <summary>
        /// Single channel image
        /// </summary>
        TwoDimension,
        /// <summary>
        /// RGB image
        /// </summary>
        TwoDimensionRGB
    }
}
