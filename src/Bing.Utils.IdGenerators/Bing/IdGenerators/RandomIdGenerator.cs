using System;
using System.Text;

namespace Bing.IdGenerators
{
    /// <summary>
    /// 随机 Id 生成器
    /// </summary>
    public static class RandomIdGenerator
    {
        /// <summary>
        /// 常量，0 - 9 的所有数字
        /// </summary>
        public const string AllNumbers = "0123456789";

        /// <summary>
        /// 常量，包含 0 - 9，a - z 和 A - Z
        /// </summary>
        public const string AllWords = "1234567890qwertyuiopasdfghjklzxcvbnm1234567890QWERTYUIOPASDFGHJKLZXCVBNM";

        /// <summary>
        /// 常量，但不包含容易混淆的字符，比如 1 和 I，0 和 O 等
        /// </summary>
        public const string SimpleWords = "2345678wertyuipasdfghjkzxcvbnm2345678WERTYUPASDFGHJKLZXCVBNM";

        /// <summary>
        /// 常量，Nano Id 所需的字符
        /// </summary>
        public const string NanoWords = "_-0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";

        /// <summary>
        /// 创建一个随机ID
        /// </summary>
        /// <param name="length">长度</param>
        /// <param name="dict">随机字符串字典</param>
        public static string Create(int length, string dict = AllWords) => new RandomId(length, dict).Create();

        /// <summary>
        /// 创建一个随机ID
        /// </summary>
        /// <param name="format">格式化字符串</param>
        /// <param name="dict">随机字符串字典</param>
        public static string Create(string format, string dict = AllWords) => new RandomId(format, dict).Create();
    }

    /// <summary>
    /// 随机ID
    /// </summary>
    internal sealed class RandomId : IFormattable
    {
        /// <summary>
        /// 转换格式
        /// </summary>
        private static readonly Func<int, string> ToFormat = length =>
        {
            var sb = new StringBuilder(length * 3);
            for (var i = 0; i < length; i++)
                sb.Append("{0}");
            return sb.ToString();
        };

        /// <summary>
        /// 随机字符串字典
        /// </summary>
        private readonly string _dict;

        /// <summary>
        /// 最大可随机长度
        /// </summary>
        private readonly int _rMax;

        /// <summary>
        /// 格式化字符串
        /// </summary>
        private readonly string _format;

        /// <summary>
        /// 初始化一个<see cref="RandomId"/>类型的实例
        /// </summary>
        /// <param name="length">生成Id长度</param>
        /// <param name="dict">随机字符串字典</param>
        public RandomId(int length, string dict = RandomIdGenerator.AllWords) : this(ToFormat(length), dict) { }

        /// <summary>
        /// 初始化一个<see cref="RandomId"/>类型的实例
        /// </summary>
        /// <param name="format">格式化字符串</param>
        /// <param name="dict">随机字符串字典</param>
        public RandomId(string format, string dict = RandomIdGenerator.AllWords)
        {
            _dict = dict;
            _format = format;
            _rMax = dict.Length;
        }

        /// <summary>
        /// 生成ID
        /// </summary>
        public string Create() => string.Format(_format, this);

        /// <summary>
        /// 随机
        /// </summary>
        private static readonly Random Rand = new Random();

        /// <summary>
        /// 输出字符串
        /// </summary>
        string IFormattable.ToString(string format, IFormatProvider formatProvider) => _dict[Rand.Next(0, _rMax)].ToString();
    }
}
