﻿namespace RemoteVisionConsole.Interface
{
    /// <summary>
    /// Adapte input and output from <see cref="IVisionProcessor{TData}"/>
    /// </summary>
    /// <typeparam name="TOutput">The type of array that is accepted by <see cref="IVisionProcessor{TData}"/></typeparam>
    public interface IVisionAdapter<TOutput>
    {
        object Name { get; set; }

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
        string GetResultType(Statistics statistics);

        /// <summary>
        /// Deduce whether image should be saved based on result from <see cref="GetResultType(Statistics)"/>
        /// </summary>
        /// <param name="resultType"></param>
        /// <returns></returns>
        bool ShouldSaveImage(string resultType);

        /// <summary>
        /// Filter away data that are not interested by source id 
        /// </summary>
        /// <param name="sourceId"></param>
        /// <returns></returns>
        bool IsInterestingData(string sourceId);


        void SaveImage(TOutput[] imageData, int cavity);

        Statistics Weight(Statistics statistics);

        (TOutput[] data, int cavity) ReadFile(string path);

        GraphicMetaData GraphicMetaData { get; set; }
    }

    public class GraphicMetaData
    {
        public DataSampleType SampleType { get; set; }
        public int ScanLineSize { get; set; }
        public bool ShouldDisplay { get; set; }
    }

    public enum DataSampleType
    {
        OneDimension,
        TwoDimension,
        TwoDimensionRGB
    }
}
