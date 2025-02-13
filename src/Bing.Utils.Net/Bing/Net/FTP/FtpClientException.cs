namespace Bing.Net.FTP;

/// <summary>
/// FTP客户端异常
/// </summary>
public class FtpClientException : Exception
{
    /// <summary>
    /// 初始化一个<see cref="FtpClientException"/>类型的实例
    /// </summary>
    /// <param name="message">错误消息</param>
    /// <param name="exception">内部异常</param>
    public FtpClientException(string message, Exception exception = null) : base(message ?? "", exception)
    {
    }
}