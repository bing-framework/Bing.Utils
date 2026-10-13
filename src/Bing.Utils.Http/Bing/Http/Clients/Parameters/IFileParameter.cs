namespace Bing.Http.Clients.Parameters;

/// <summary>
/// 文件参数
/// </summary>
public interface IFileParameter : IDisposable
{
    /// <summary>
    /// 获取文件流
    /// </summary>
    /// <returns>文件内容流。</returns>
    Stream GetFileStream();

    /// <summary>
    /// 获取文件名称
    /// </summary>
    /// <returns>文件名称。</returns>
    string GetFileName();

    /// <summary>
    /// 获取参数名
    /// </summary>
    /// <returns>表单参数名称。</returns>
    string GetName();
}
