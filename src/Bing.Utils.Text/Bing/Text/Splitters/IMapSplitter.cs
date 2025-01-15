namespace Bing.Text.Splitters;

/// <summary>
/// 映射分割器
/// </summary>
public interface IMapSplitter
{
    /// <summary>
    /// 裁剪结果
    /// </summary>
    IMapSplitter TrimResults();

    /// <summary>
    /// 裁剪结果
    /// </summary>
    /// <param name="keyTrimFunc">键裁剪函数</param>
    /// <param name="valueTrimFunc">值裁剪函数</param>
    IMapSplitter TrimResults(Func<string, string> keyTrimFunc, Func<string, string> valueTrimFunc);

    /// <summary>
    /// 限制
    /// </summary>
    /// <param name="limit">限制长度</param>
    IMapSplitter Limit(int limit);

    /// <summary>
    /// 切割
    /// </summary>
    /// <param name="originalString">字符串</param>
    IEnumerable<KeyValuePair<string, string>> Split(string originalString);

    /// <summary>
    /// 切割为字典
    /// </summary>
    /// <param name="originalString">字符串</param>
    Dictionary<string, string> SplitToDictionary(string originalString);
}