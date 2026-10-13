namespace Bing.Text.Splitters;

/// <summary>
/// 映射分割器
/// </summary>
public interface IMapSplitter
{
    /// <summary>
    /// 裁剪结果
    /// </summary>
    /// <returns>返回配置后的当前实例。</returns>
    IMapSplitter TrimResults();

    /// <summary>
    /// 裁剪结果
    /// </summary>
    /// <param name="keyTrimFunc">键裁剪函数</param>
    /// <param name="valueTrimFunc">值裁剪函数</param>
    /// <returns>返回配置后的当前实例。</returns>
    IMapSplitter TrimResults(Func<string, string> keyTrimFunc, Func<string, string> valueTrimFunc);

    /// <summary>
    /// 限制
    /// </summary>
    /// <param name="limit">限制长度</param>
    /// <returns>返回配置后的当前实例。</returns>
    IMapSplitter Limit(int limit);

    /// <summary>
    /// 切割
    /// </summary>
    /// <param name="originalString">字符串</param>
    /// <returns>按输入字符串顺序生成的键值对集合。</returns>
    IEnumerable<KeyValuePair<string, string>> Split(string originalString);

    /// <summary>
    /// 切割为字典
    /// </summary>
    /// <param name="originalString">字符串</param>
    /// <returns>由分割结果组成的字典。</returns>
    Dictionary<string, string> SplitToDictionary(string originalString);
}
