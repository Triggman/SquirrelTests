using RemoteVisionConsole.Data;
using System;

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
        (string[] extensions, string fileTypePrompt)? ImageFileFilter { get; }
        (string[] floatNames, string[] integerNames, string[] textNames) OutputNames { get; }



        GraphicMetaData GraphicMetaData { get; }


        /// <summary>
        /// Convert byte array to any type of array for <see cref="IVisionProcessor{TData}"/>
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        TOutput[] ConvertInput(byte[] input);

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
        void SaveImage(TOutput[] imageData, string mainFolder, string subFolder, string fileName, string exceptionDetail);

        Statistics Weight(Statistics statistics);

        (TOutput[] data, int cavity, string sn) ReadFile(string path);

    }

    public class GraphicMetaData
    {
        public DataSampleType SampleType { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public bool ShouldDisplay { get; set; }
    }

    public enum DataSampleType
    {
        OneDimension,
        TwoDimension,
        TwoDimensionRGB
    }
}
