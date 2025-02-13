namespace BuildScript
{
    /// <summary>
    /// 构建版本
    /// </summary>
    public class BuildVersion
    {
        /// <summary>
        /// 主版本号
        /// </summary>
        public int Major { get; init; }

        /// <summary>
        /// 次版本号
        /// </summary>
        public int Minor { get; init; }

        /// <summary>
        /// 补丁版本号
        /// </summary>
        public int Patch { get; init; }

        /// <summary>
        /// 质量标识（例如：alpha、beta、rc、preview等）
        /// </summary>
        public string Quality { get; init; }

        /// <summary>
        /// 版本后缀（例如：-SNAPSHOT、-RELEASE等）
        /// </summary>
        public string Suffix { get; set; }

        /// <summary>
        /// 获取不带质量标识的版本号
        /// </summary>
        /// <returns>不带质量标识的版本号字符串</returns>
        public string VersionWithoutQuality() => $"{Major}.{Minor}.{Patch}";

        /// <summary>
        /// 获取带质量标识的版本号
        /// </summary>
        /// <returns>带质量标识的版本号字符串</returns>
        public string Version() => VersionWithoutQuality() + (string.IsNullOrWhiteSpace(Quality) ? string.Empty : $"-{Quality}");

        /// <summary>
        /// 获取带质量标识和后缀的版本号
        /// </summary>
        /// <returns>带质量标识和后缀的版本号字符串</returns>
        public string VersionWithSuffix() => Version() + (string.IsNullOrWhiteSpace(Suffix) ? string.Empty : $"-{Suffix}");
    }
}