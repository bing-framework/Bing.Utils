namespace Bing.Net.FTP;

/// <summary>
/// 指定同步 FTP 客户端采用的配置兼容模式。
/// </summary>
public enum FtpConfigurationMode
{
    /// <summary>
    /// 保留历史配置映射和行为。
    /// </summary>
    Legacy = 0,

    /// <summary>
    /// 使用完整配置映射、配置快照和确定的路径基准。
    /// </summary>
    Complete = 1
}
