using System;

namespace BuildScript
{
    // 构建脚本 - 工具
    public partial class BuildScript
    {
        /// <summary>
        /// 创建时间戳
        /// </summary>
        /// <returns>自2020年1月1日以来的总秒数的字符串表示</returns>
        public string CreateStamp()
        {
            var seconds=(long)(DateTime.UtcNow - new DateTime(2020, 1, 1)).TotalSeconds;
            return seconds.ToString();
        }
    }
}