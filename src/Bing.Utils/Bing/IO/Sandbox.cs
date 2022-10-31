using System.IO;

namespace Bing.IO;

/// <summary>
/// 沙箱。当沙箱被释放时，沙箱中创建的文件和目录将被删除
/// </summary>
public class Sandbox : TempDirectory
{
    /// <summary>
    /// 默认前缀("Sandbox-")
    /// </summary>
    public const string DefaultPrefix = "Sandbox-";

    /// <summary>
    /// 初始化一个<see cref="Sandbox"/>类型的实例
    /// </summary>
    /// <param name="name">沙箱前缀。默认值：<see cref="DefaultPrefix"/></param>
    public Sandbox(string name = DefaultPrefix) : base(name)
    {
    }

    /// <summary>
    /// 解析路径
    /// </summary>
    /// <param name="path">路径</param>
    public string ResolvePath(string path) => Path.Combine(FullPath, path);

    /// <summary>
    /// 创建目录
    /// </summary>
    /// <param name="path">路径</param>
    public string CreateDirectory(string path)
    {
        var dirInfo = Directory.CreateDirectory(ResolvePath(path));
        return dirInfo.FullName;
    }

    /// <summary>
    /// 创建文件
    /// </summary>
    /// <param name="path">路径</param>
    /// <param name="text">文本</param>
    public string CreateFile(string path, string text = "")
    {
        var fullPath = ResolvePath(path);
        var parentDir = Directory.GetParent(fullPath)?.FullName;
        Directory.CreateDirectory(parentDir);
        File.WriteAllText(fullPath, text);
        return fullPath;
    }
}