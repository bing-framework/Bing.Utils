namespace Bing.Http.Clients;

public partial class HttpRequest<TResult> where TResult : class
{
    /// <summary>
    /// 恢复下载状态文件当前使用的架构版本。
    /// </summary>
    private const int ResumeStateSchemaVersion = 1;

    /// <summary>

    /// 保存断点续传时用于验证和恢复下载的状态。

    /// </summary>
    private sealed class ResumeDownloadState
    {
        /// <summary>
        /// 获取或设置状态文件的架构版本。
        /// </summary>
        public int SchemaVersion { get; set; }
        /// <summary>
        /// 获取或设置用于校验恢复请求目标的身份信息。
        /// </summary>
        public string Identity { get; set; }
        /// <summary>
        /// 获取或设置用于比较恢复请求配置的指纹。
        /// </summary>
        public string RequestFingerprint { get; set; }
        /// <summary>
        /// 获取或设置已保存部分响应使用的内容编码。
        /// </summary>
        public string ContentEncoding { get; set; }
        /// <summary>
        /// 获取或设置部分文件中已写入的字节数。
        /// </summary>
        public long BytesWritten { get; set; }
        /// <summary>
        /// 获取或设置用于验证响应版本的 ETag 值。
        /// </summary>
        public string ETag { get; set; }
        /// <summary>
        /// 获取或设置用于验证响应版本的最后修改时间。
        /// </summary>
        public string LastModified { get; set; }
        /// <summary>
        /// 获取或设置已知时表示完整响应的总字节数。
        /// </summary>
        public long? TotalLength { get; set; }
    }
}
