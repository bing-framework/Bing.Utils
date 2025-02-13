using System.Runtime.CompilerServices;

namespace Bing.Text;

/// <summary>
/// 字符(<see cref="char"/>) 检查器
/// </summary>
public static class CharJudge
{
    /// <summary>
    /// 判断给定的字符是否包含在左值和右值之间。
    /// </summary>
    /// <param name="value">数值</param>
    /// <param name="left">左值</param>
    /// <param name="right">右值</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsBetween(char value, char left, char right) => value >= left && value <= right;
}