namespace Bing.Text;

/// <summary>
/// 字符串操作 - 工具
/// </summary>
public static partial class Str
{
    /// <summary>
    /// 新行
    /// </summary>
    public const string NewLine = "\r\n";

    /// <summary>
    /// 重复指定次数的字符串
    /// </summary>
    /// <param name="source">字符串</param>
    /// <param name="times">次数</param>
    public static string Repeat(string source, int times) => source.Repeat(times);

    /// <summary>
    /// 重复指定次数的字符
    /// </summary>
    /// <param name="source">字符</param>
    /// <param name="times">次数</param>
    public static string Repeat(char source, int times) => source.Repeat(times);

    /// <summary>
    /// 填充。向左填充
    /// </summary>
    /// <param name="source">字符串</param>
    /// <param name="width">宽度</param>
    /// <param name="appendChar">拼接字符</param>
    public static string PadStart(string source, int width, char appendChar) => source.PadLeft(width, appendChar);

    /// <summary>
    /// 填充。向右填充
    /// </summary>
    /// <param name="source">字符串</param>
    /// <param name="width">宽度</param>
    /// <param name="appendChar">拼接字符</param>
    public static string PadEnd(string source, int width, char appendChar) => source.PadRight(width, appendChar);
}