
namespace Bing.Text;

/// <summary>
/// 字符串(<see cref="string"/>) 扩展
/// </summary>
public static partial class StringExtensions
{
    /// <summary>
    /// 将字符串转换成全角字符串(SBC Case)
    /// </summary>
    /// <param name="text">字符串</param>
    /// <returns>全角字符串</returns>
    /// <remarks>
    /// 全角空格为12288，半角空格为32；<br />
    /// 其它字符半角（33-126）与全角（65281-65374）的对应关系：均相差65248。
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ToSbcCase(this string text) => Strings.ToSbcCase(text);

    /// <summary>
    /// 将字符串转换成半角字符串(DBC Case)
    /// </summary>
    /// <param name="text">字符串</param>
    /// <returns>半角字符串</returns>
    /// <remarks>
    /// 全角空格为12288，半角空格为32；<br />
    /// 其它字符半角（33-126）与全角（65281-65374）的对应关系：均相差65248。
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ToDbcCase(this string text) => Strings.ToDbcCase(text);
}