namespace Bing.Text.Splitters;

/// <summary>
/// 修正长度的分割器
/// </summary>
public interface IFixedLengthSplitter
{
    /// <summary>
    /// 裁剪结果
    /// </summary>
    IFixedLengthSplitter TrimResults();

    /// <summary>
    /// 裁剪结果
    /// </summary>
    /// <param name="trimFunc">裁剪函数</param>
    IFixedLengthSplitter TrimResults(Func<string, string> trimFunc);

    /// <summary>
    /// 限制
    /// </summary>
    /// <param name="limit">限制长度</param>
    IFixedLengthSplitter Limit(int limit);

    /// <summary>
    /// 设置键值对分隔符
    /// </summary>
    /// <param name="separator">分隔符</param>
    IMapSplitter WithKeyValueSeparator(char separator);

    /// <summary>
    /// 设置键值对分隔符
    /// </summary>
    /// <param name="separator">分隔符</param>
    IMapSplitter WithKeyValueSeparator(string separator);

    /// <summary>
    /// 切割
    /// </summary>
    /// <param name="originalString">字符串</param>
    IEnumerable<string> Split(string originalString);

    /// <summary>
    /// 切割为列表
    /// </summary>
    /// <param name="originalString">字符串</param>
    List<string> SplitToList(string originalString);

    /// <summary>
    /// 切割为数组
    /// </summary>
    /// <param name="originalString">字符串</param>
    string[] SplitToArray(string originalString);
}