namespace Bing.Text.Splitters;

/// <summary>
/// 字符串分割器
/// </summary>
public interface ISplitter
{
    /// <summary>
    /// 忽略空字符串
    /// </summary>
    /// <returns>返回配置后的当前实例。</returns>
    ISplitter OmitEmptyStrings();

    /// <summary>
    /// 裁剪结果
    /// </summary>
    /// <returns>返回配置后的当前实例。</returns>
    ISplitter TrimResults();

    /// <summary>
    /// 裁剪结果
    /// </summary>
    /// <param name="trimFunc">裁剪函数</param>
    /// <returns>返回配置后的当前实例。</returns>
    ISplitter TrimResults(Func<string, string> trimFunc);

    /// <summary>
    /// 限制
    /// </summary>
    /// <param name="limit">限制长度</param>
    /// <returns>返回配置后的当前实例。</returns>
    ISplitter Limit(int limit);

    /// <summary>
    /// 设置键值对分隔符
    /// </summary>
    /// <param name="separator">分隔符</param>
    /// <returns>返回配置后的当前实例。</returns>
    IMapSplitter WithKeyValueSeparator(char separator);

    /// <summary>
    /// 设置键值对分隔符
    /// </summary>
    /// <param name="separator">分隔符</param>
    /// <returns>返回配置后的当前实例。</returns>
    IMapSplitter WithKeyValueSeparator(string separator);

    /// <summary>
    /// 切割
    /// </summary>
    /// <param name="originalString">字符串</param>
    /// <returns>返回分割后的字符串序列。</returns>
    IEnumerable<string> Split(string originalString);

    /// <summary>
    /// 切割为列表
    /// </summary>
    /// <param name="originalString">字符串</param>
    /// <returns>返回分割后的字符串列表。</returns>
    List<string> SplitToList(string originalString);

    /// <summary>
    /// 切割为数组
    /// </summary>
    /// <param name="originalString">字符串</param>
    /// <returns>返回分割后的字符串数组。</returns>
    string[] SplitToArray(string originalString);
}
