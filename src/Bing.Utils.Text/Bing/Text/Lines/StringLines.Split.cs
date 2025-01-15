// ReSharper disable once CheckNamespace
namespace Bing.Text;

/// <summary>
/// 字符串行工具
/// </summary>
public static partial class StringLines
{
    /// <summary>
    /// 将多行文本分割为单行文本集合。
    /// </summary>
    /// <param name="text">文本</param>
    /// <returns>单行文本集合</returns>
    public static IEnumerable<string> SplitByLines(string text)
    {
        var index = 0;
        while (true)
        {
            var newIndex = text.IndexOf(Environment.NewLine, index, StringComparison.Ordinal);
            if (newIndex < 0)
            {
                if (text.Length > index)
                    yield return text.Substring(index);
                yield break;
            }

            var currentString = text.Substring(index, newIndex - index);
            index = newIndex + 2;
            yield return currentString;
        }
    }

    /// <summary>
    /// 逐行分割文本，并将分割所得的字符数组转换为指定类型的实例数据。
    /// </summary>
    /// <typeparam name="T">目标类型</typeparam>
    /// <param name="text">文本</param>
    /// <returns>转换后的指定类型的实例数组</returns>
    public static T[] SplitInLinesTyped<T>(string text) where T : IComparable
        => text.SplitTyped<T>(Environment.NewLine);

    /// <summary>
    /// 逐行分割文本，并移除空行。
    /// </summary>
    /// <param name="text">文本</param>
    /// <returns>单行文本数组</returns>
    public static string[] SplitInLinesWithoutEmpty(string text)
        => text.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
}

/// <summary>
/// 字符串(<see cref="string"/>) 扩展
/// </summary>
public static partial class StringLinesExtensions
{
    /// <summary>
    /// 将多行文本分割为单行文本集合。
    /// </summary>
    /// <param name="text">文本</param>
    /// <returns>单行文本集合</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IEnumerable<string> SplitByLines(this string text) => StringLines.SplitByLines(text);

    /// <summary>
    /// 逐行分割文本，并将分割所得的字符数组转换为指定类型的实例数据。
    /// </summary>
    /// <typeparam name="T">目标类型</typeparam>
    /// <param name="text">文本</param>
    /// <returns>转换后的指定类型的实例数组</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T[] SplitInLinesTyped<T>(this string text) where T : IComparable
        => StringLines.SplitInLinesTyped<T>(text);

    /// <summary>
    /// 逐行分割文本，并移除空行。
    /// </summary>
    /// <param name="text">文本</param>
    /// <returns>单行文本数组</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string[] SplitInLinesWithoutEmpty(this string text) => StringLines.SplitInLinesWithoutEmpty(text);
}