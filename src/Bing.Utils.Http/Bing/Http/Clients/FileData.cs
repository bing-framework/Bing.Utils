namespace Bing.Http.Clients;

/// <summary>
/// 文件数据
/// </summary>
public class FileData
{
    /// <summary>
    /// 初始化一个<see cref="FileData"/>类型的实例。
    /// </summary>
    /// <param name="stream">文件流</param>
    /// <param name="fileName">文件名</param>
    /// <param name="name">参数名</param>
    /// <param name="leaveOpen">发送后是否保持流打开</param>
    public FileData(Stream stream, string fileName, string name, bool leaveOpen = false)
    {
        Stream = stream;
        FileName = fileName;
        Name = name;
        LeaveOpen = leaveOpen;
    }

    /// <summary>
    /// 初始化一个<see cref="FileData"/>类型的实例。
    /// </summary>
    /// <param name="filePath">文件路径</param>
    /// <param name="name">参数名</param>
    public FileData(string filePath, string name)
    {
        FilePath = filePath;
        Name = name;
    }

    /// <summary>
    /// 文件流
    /// </summary>
    public Stream Stream { get; }

    /// <summary>
    /// 发送后是否保留调用方的流。
    /// </summary>
    public bool LeaveOpen { get; }

    /// <summary>
    /// 文件路径
    /// </summary>
    public string FilePath { get; }

    /// <summary>
    /// 文件名
    /// </summary>
    public string FileName { get; }

    /// <summary>
    /// 参数名
    /// </summary>
    public string Name { get; }
}
