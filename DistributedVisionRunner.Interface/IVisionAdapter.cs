using System.Collections.Generic;

namespace DistributedVisionRunner.Interface
{
    /// <summary>
    /// 由上位机工程师实现
    /// 此类负责转换视觉工程师(<see cref="IVisionProcessor{TData}"/>类)的输入输出
    /// </summary>
    /// <typeparam name="TOutput">
    /// 相应<see cref="IVisionProcessor{TData}"/>类的输入数据类型,
    /// 目前支持byte(单通道和3通道一样时byte), float, short, ushort
    /// </typeparam>
    public interface IVisionAdapter<TOutput>
    {
        /// <summary>
        /// 将用来命名响应的Tab
        /// </summary>
        string Name { get; }

        /// <summary>
        /// 任何有效的ZeroMQ地址, 例如, "tcp://localhost:6000"
        /// </summary>
        string ZeroMQAddress { get; }

        /// <summary>
        /// 项目名称, 例如, 995X.
        /// 数据库的相关命名会同此挂钩
        /// </summary>
        string ProjectName { get; }

        /// <summary>
        /// 是否将来自 <see cref="IVisionProcessor{TData}"/>
        /// 的原始数据先经过补偿服务后才输入<see cref="GetResultType"/>
        /// </summary>
        bool EnableWeighting { get; }

        /// <summary>
        /// 等于穴(Cavity)的个数
        /// </summary>
        int WeightSetCount { get; }


        /// <summary>
        /// extensions: 例如, new []{tif, tiff}
        /// fileTypePrompt: 例如, "tif image files"
        /// 运行图片文件时, 用来过滤掉后缀名不符的文件
        /// </summary>
        (string[] extensions, string fileTypePrompt)? ImageFileFilter { get; }

        /// <summary>
        /// 定义最终输出数据的变量名
        /// </summary>
        (string[] floatNames, string[] integerNames, string[] textNames) OutputNames { get; }

        /// <summary>
        /// 定义图片显示的参数
        /// </summary>
        GraphicMetaData GraphicMetaData { get; }




        /// <summary>
        /// 将byte数组转换成<see cref="IVisionProcessor{TData}"/>所需的数据类型的数组
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        List<TOutput[]> ConvertInput(byte[] input);

        /// <summary>
        /// 根据视觉工程师给出的数据返回判断结果, 并且改写输入的内容
        /// <see cref="statistics"/>内的词典变量名称必须要同<see cref="OutputNames"/>一致
        /// 这个方法完成之后, <see cref="statistics"/> 将会返回给ALC
        /// </summary>
        /// <param name="statistics">来自Processor(视觉工程师)的数据, </param>
        /// <returns></returns>
        ResultType GetResultType(Statistics statistics);

        /// <summary>
        /// 注意: 此方法内不需要再调用Directory.CreateDirectory
        /// </summary>
        /// <param name="imageData"></param>
        /// <param name="mainFolder"></param>
        /// <param name="subFolder"></param>
        /// <param name="fileNameWithoutExtension"></param>
        /// <param name="exceptionDetail">null when no error occurs during image processing</param>
        void SaveImage(List<TOutput[]> imageData, string mainFolder, string subFolder, string fileNameWithoutExtension, string exceptionDetail);

        /// <summary>
        /// 定义从文件获取图片数据的方法
        /// 获取的数据之后会输入到<see cref="IVisionProcessor{TData}"/>
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        List<TOutput[]> ReadFile(string path);

    }

    public class GraphicMetaData
    {
        /// <summary>
        /// 显示图片的类型
        /// </summary>
        public DataSampleType SampleType { get; set; } = DataSampleType.TwoDimension;

        /// <summary>
        /// 显示图片的尺寸,
        /// 由于可以一次输入多张图片, 因此各张图片具有不同的尺寸也是允许的
        /// </summary>
        public (int width, int height)[] Dimensions { get; set; }

        /// <summary>
        /// 是否显示图片
        /// </summary>
        public bool ShouldDisplay { get; set; } = true;

        /// <summary>
        /// 显示图片的原始数据的最小最大范围,
        /// 将用来缩放整张图片的灰度值, 使其处于图片显示的0-255之间的范围
        /// </summary>
        public (float min, float max) DataRange { get; set; }
    }

    public enum DataSampleType
    {
        /// <summary>
        /// 曲线(未实现)
        /// </summary>
        OneDimension,
        /// <summary>
        /// 单通道图片
        /// </summary>
        TwoDimension,
        /// <summary>
        /// RGB图片
        /// </summary>
        TwoDimensionRGB
    }
}
