using Bing.Net.FTP;

/// <summary>
/// 验证同步 FTP 旧构造及显式完整模式的跨框架调用兼容性。
/// </summary>
internal static class FtpContracts
{
    /// <summary>
    /// 仅用于编译契约，不在消费者构建中连接网络。
    /// </summary>
    internal static void Compile(FtpClientConfig config)
    {
        using var legacy = new FtpClient(config);
        using var explicitLegacy = new FtpClient(config: config, mode: FtpConfigurationMode.Legacy);
        using var complete = new FtpClient(config: config, mode: FtpConfigurationMode.Complete);
        using var nullConfig = new FtpClient(config: null, mode: default);
        IFtpClient oldInterface = legacy;
        _ = oldInterface.IsConnected;
    }
}
