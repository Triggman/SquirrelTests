using System.Collections.Generic;

namespace DistributedVisionRunner.Interface
{
    /// <summary>
    /// 由视觉工程师负责实现
    /// </summary>
    /// <typeparam name="TData">
    /// 输入数据的类型
    /// 目前支持byte(单通道和3通道一样时byte), float, short, ushort
    /// </typeparam>
    public interface IVisionProcessor<TData>
    {
        /// <summary>
        /// 处理来自上位机的数据, 并返回结果
        /// </summary>
        /// <param name="data">
        /// 每个元素都是一张图片的数据
        /// </param>
        /// <returns></returns>
        ProcessResult<TData> Process(List<TData[]> data);

        /// <summary>
        /// <see cref="Process"/>返回的数值型变量的名称,
        /// </summary>
        string[] OutputNames { get; }

        /// <summary>
        /// 补偿值的名称
        /// </summary>
        string[] WeightNames { get; }
    }

    public class ProcessResult<TData>
    {
        /// <summary>
        /// 用于显示的图片数据,
        /// 一个元素代表一张图片, 无论是1或3通道
        /// </summary>
        public List<TData[]> DisplayData { get; set; }

        /// <summary>
        /// 输出的数据
        /// </summary>
        public Statistics Statistics { get; set; }
    }

    public class Statistics
    {
        public Dictionary<string, float> FloatResults { get; set; } = new Dictionary<string, float>();
        public Dictionary<string, int> IntegerResults { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, string> TextResults { get; set; } = new Dictionary<string, string>();
    }







}