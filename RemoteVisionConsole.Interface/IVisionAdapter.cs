using RemoteVisionConsole.Data;
using System.Collections.Generic;

namespace RemoteVisionConsole.Interface
{
    /// <summary>
    /// Adapte input and output from <see cref="IVisionProcessor{TData}"/>
    /// </summary>
    /// <typeparam name="TOutput">The type of array that is accepted by <see cref="IVisionProcessor{TData}"/></typeparam>
    public interface IVisionAdapter<TOutput>
    {
        string Name { get; }

        string ZeroMQAddress { get; }
        string ProjectName { get; }
        bool EnableWeighting { get; }
        (string[] extensions, string fileTypePrompt)? ImageFileFilter { get; }
        (string[] floatNames, string[] integerNames, string[] textNames) OutputNames { get; }
        (float min, float max) DataRange { get; }


        GraphicMetaData GraphicMetaData { get; }
        int WeightSetCount { get; }


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

        List<TOutput[]> ReadFile(string path);

    }

    public class GraphicMetaData
    {
        public DataSampleType SampleType { get; set; }
        public (int width, int height)[] Dimensions { get; set; }

        public bool ShouldDisplay { get; set; }
    }

    public enum DataSampleType
    {
        OneDimension,
        TwoDimension,
        TwoDimensionRGB
    }
}
