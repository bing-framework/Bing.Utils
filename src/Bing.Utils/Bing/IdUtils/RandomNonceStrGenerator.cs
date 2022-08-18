using System;
using System.Text;

namespace Bing.IdUtils
{
    /// <summary>
    /// 随机字符串生成器
    /// </summary>
    public static class RandomNonceStrGenerator
    {
        /// <summary>
        /// 随机字符串
        /// </summary>
        private const string NonceStr = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

        /// <summary>
        /// 对象锁
        /// </summary>
        // ReSharper disable once InconsistentNaming
        private static readonly object _lock = new();

        /// <summary>
        /// 生成随机字符串
        /// </summary>
        /// <param name="forceToAvoidRepetition">强制避免重复</param>
        public static string Create(bool forceToAvoidRepetition = false) => Create(16, forceToAvoidRepetition);

        /// <summary>
        /// 生成随机字符串
        /// </summary>
        /// <param name="length">长度</param>
        /// <param name="forceToAvoidRepetition">强制避免重复</param>
        public static string Create(int length, bool forceToAvoidRepetition = false)
        {
            if (length <= 16)
                length = 16;
            var sb = new StringBuilder();
            Random rd;
            if (forceToAvoidRepetition)
            {
                lock (_lock)
                    rd = new Random(Convert.ToInt32(RandomIdGenerator.Create(8, RandomIdGenerator.AllNumbers)));
            }
            else
            {
                rd = new Random();
            }

            for (var i = 0; i < length; i++)
                sb.Append(NonceStr[rd.Next(NonceStr.Length - 1)]);
            return sb.ToString();
        }
    }
}
