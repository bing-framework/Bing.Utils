using Bing.Text.Joiners;
using Bing.Text.Splitters;
using System.Text.RegularExpressions;

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

    /// <summary>
    /// 转换
    /// </summary>
    /// <param name="style">大小写格式化样式</param>
    /// <param name="sequence">序列</param>
    public string To(Style style, string sequence)
    {
        var splitter = _splitter ?? Splitter.On(new Regex("[-_ ]+"));
        var list = splitter.SplitToList(sequence);
        IJoiner joiner;
        switch (style)
        {
            case Style.LowerCamel:
            {
                CaseFormatterUtils.LowerCase(list);
                CaseFormatterUtils.UpperCaseEachFirstChar(list);
                CaseFormatterUtils.LowerCaseFirstItemFirstChar(list);
                joiner = Joiner.On("");
                break;
            }
            case Style.UpperCamel:
            {
                CaseFormatterUtils.LowerCase(list);
                CaseFormatterUtils.UpperCaseEachFirstChar(list);
                joiner = Joiner.On("");
                break;
            }
            default:
                throw new InvalidOperationException("Invalid operation.");
        }
        return joiner.Join(list);
    }

    /// <summary>
    /// 创建一个 <see cref="CaseFormatter"/> 类型的实例（连接符分割）
    /// </summary>
    public static CaseFormatter LowerHyphen => new(Splitter.On("-"));

    /// <summary>
    /// 创建一个 <see cref="CaseFormatter"/> 类型的实例（下划线分割）
    /// </summary>
    public static CaseFormatter LowerUnderscore => new(Splitter.On("_"));

    /// <summary>
    /// 创建一个 <see cref="CaseFormatter"/> 类型的实例（普通分割器）
    /// </summary>
    public static CaseFormatter Instance => new(Splitter.On(new Regex("[-_ ]+")));

    /// <summary>
    /// 创建一个 <see cref="CaseFormatter"/> 类型的实例（人性化）
    /// </summary>
    public static CaseFormatter Humanizer => new();

    /// <summary>
    /// 大小写格式化器工具
    /// </summary>
    private static class CaseFormatterUtils
    {
        /// <summary>
        /// 全小写
        /// </summary>
        /// <param name="list">列表</param>
        public static void LowerCase(List<string> list)
        {
            for (var i = 0; i < list.Count; i++) 
                list[i] = list[i].ToLower();
        }

        /// <summary>
        /// 全大写
        /// </summary>
        /// <param name="list">列表</param>
        public static void UpperCase(List<string> list)
        {
            for (var i = 0; i < list.Count; i++) 
                list[i] = list[i].ToUpper();
        }

        /// <summary>
        /// 首字母小写
        /// </summary>
        /// <param name="list">列表</param>
        public static void LowerCaseEachFirstChar(List<string> list)
        {
            for (var i = 0; i < list.Count; i++)
            {
                if(string.IsNullOrWhiteSpace(list[i]))
                    continue;
                var array = list[i].ToCharArray();
                array[0] = char.ToLowerInvariant(array[0]);
                list[i] = new string(array);
            }
        }

        /// <summary>
        /// 首字母大写
        /// </summary>
        /// <param name="list">列表</param>
        public static void UpperCaseEachFirstChar(List<string> list)
        {
            for (var i = 0; i < list.Count; i++)
            {
                if(string.IsNullOrWhiteSpace(list[i]))
                    continue;
                var array = list[i].ToCharArray();
                array[0] = char.ToUpperInvariant(array[0]);
                list[i] = new string(array);
            }
        }

        /// <summary>
        /// 首行首字母小写
        /// </summary>
        /// <param name="list">列表</param>
        public static void LowerCaseFirstItemFirstChar(List<string> list)
        {
            if (list != null && list.Any() && !string.IsNullOrWhiteSpace(list[0]))
            {
                var array = list[0].ToCharArray();
                array[0] = char.ToLowerInvariant(array[0]);
                list[0] = new string(array);
            }
        }

        /// <summary>
        /// 首行首字母大写
        /// </summary>
        /// <param name="list">列表</param>
        public static void UpperCaseFirstItemFirstChar(List<string> list)
        {
            if (list != null && list.Any() && !string.IsNullOrWhiteSpace(list[0]))
            {
                var array = list[0].ToCharArray();
                array[0] = char.ToUpperInvariant(array[0]);
                list[0] = new string(array);
            }
        }
    }

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