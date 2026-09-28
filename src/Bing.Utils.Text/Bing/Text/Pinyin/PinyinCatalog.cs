using System.IO;

namespace Bing.Text.Pinyin;

/// <summary>
/// 使用外部单字与词组词库的拼音转换目录。
/// </summary>
/// <remarks>构造时读取全部数据，之后可由多个线程并发复用。</remarks>
public sealed class PinyinCatalog
{
    /// <summary>
    /// 当前目录使用的词库快照。
    /// </summary>
    private readonly PinyinUtil.ContextualData _data;

    /// <summary>
    /// 初始化一个 <see cref="PinyinCatalog"/> 类型的实例。
    /// </summary>
    /// <param name="characters">当前位置起读取的 UTF-8 gzip 单字词库流。</param>
    /// <param name="phrases">当前位置起读取的 UTF-8 gzip 词组词库流。</param>
    /// <remarks>数据格式与仓库 `asset/pinyin/data` 中的生成文件一致；调用结束后不关闭输入流。</remarks>
    public PinyinCatalog(Stream characters, Stream phrases)
    {
        _data = PinyinUtil.LoadContextualData(characters, phrases);
    }

    /// <summary>
    /// 获取带声调的上下文拼音。
    /// </summary>
    /// <param name="text">待转换文本。</param>
    /// <param name="separator">相邻拼音音节之间的分隔符。</param>
    /// <returns>带调拼音；text 为 null 时返回 null。</returns>
    public string GetPinyinWithTone(string text, string separator = "") =>
        PinyinUtil.ConvertContextual(text, separator, true, _data);

    /// <summary>
    /// 获取无声调的上下文拼音。
    /// </summary>
    /// <param name="text">待转换文本。</param>
    /// <param name="separator">相邻拼音音节之间的分隔符。</param>
    /// <returns>无调拼音；text 为 null 时返回 null。</returns>
    public string GetContextualPinyin(string text, string separator = "") =>
        PinyinUtil.ConvertContextual(text, separator, false, _data);
}
