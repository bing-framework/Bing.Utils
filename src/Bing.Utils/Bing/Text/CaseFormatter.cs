using Bing.Text.Splitters;

namespace Bing.Text;

/// <summary>
/// 大小写格式化器
/// </summary>
public class CaseFormatter
{
    /// <summary>
    /// 是否人性化模式
    /// </summary>
    private readonly bool _humanizerMode;

    /// <summary>
    /// 字符串分割器
    /// </summary>
    private readonly ISplitter _splitter;

    /// <summary>
    /// 初始化一个<see cref="CaseFormatter"/>类型的实例
    /// </summary>
    /// <param name="splitter">字符串分割器</param>
    /// <exception cref="ArgumentNullException"></exception>
    private CaseFormatter(ISplitter splitter)
    {
        _splitter = splitter ?? throw new ArgumentNullException(nameof(splitter));
        _humanizerMode = false;
    }

    /// <summary>
    /// 初始化一个<see cref="CaseFormatter"/>类型的实例
    /// </summary>
    private CaseFormatter()
    {
        _splitter = null;
        _humanizerMode = true;
    }

    ///// <summary>
    ///// 转换
    ///// </summary>
    ///// <param name="style">大小写格式化样式</param>
    ///// <param name="sequence">序列</param>
    //public string To(Style style, string sequence)
    //{

    //}

    /// <summary>
    /// 大小写格式化样式
    /// </summary>
    public enum Style
    {
        /// <summary>
        /// 小驼峰式命名法。第一个单词以小写字母开始，第二个单词的首字母大写。例如：firstName、lastName。
        /// </summary>
        LowerCamel,

        /// <summary>
        /// 大驼峰式命名法。每一个单词的首字母都采用大写字母。例如：FirstName、LastName、CamelCase，也被称为 Pascal 命名法。
        /// </summary>
        UpperCamel,
    }
}