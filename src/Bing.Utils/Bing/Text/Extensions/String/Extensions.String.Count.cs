

// ReSharper disable once CheckNamespace
namespace Bing.Text;

/// <summary>
/// 字符串(<see cref="string"/>) 扩展
/// </summary>
public static partial class StringExtensions
{
    /// <summary>
    /// 计算行数
    /// </summary>
    /// <param name="str">字符串</param>
    public static int CountLines(this string str)
    {
        int index = 0, lines = 0;
        while (true)
        {
            var newIndex = str.IndexOf(Environment.NewLine, index, StringComparison.Ordinal);
            if (newIndex < 0)
            {
                if (str.Length > index)
                    lines++;
                return lines;
            }

            index = newIndex + 2;
            lines++;
        }
    }
}