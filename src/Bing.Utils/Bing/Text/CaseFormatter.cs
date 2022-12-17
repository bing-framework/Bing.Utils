namespace Bing.Text;

/// <summary>
/// 大小写格式化器
/// </summary>
public class CaseFormatter
{
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