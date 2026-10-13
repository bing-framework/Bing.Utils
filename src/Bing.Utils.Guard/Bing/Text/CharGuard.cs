using Bing.Validation;

namespace Bing.Text;

/// <summary>
/// 字符守护帮助类
/// </summary>
internal static class CharGuardHelper
{
    /// <summary>
    /// 按字符校验选项检查可空字符值，并在失败时使用指定参数信息报告错误。</summary>
    /// <typeparam name="T">要检查的可空值类型，必须为值类型。</typeparam>
    /// <param name="x">待检查的可空值。</param>
    /// <param name="options">用于判断字符是否允许的选项。</param>
    /// <param name="argumentName">发生校验失败时报告的参数名称。</param>
    /// <param name="message">校验失败时使用的可选错误消息。</param>
    /// <returns>校验通过时返回原值。</returns>
    public static T? C<T>(T? x, CharMayOptions options, string argumentName, string message = null)
        where T : struct
    {
        if (options is CharMayOptions.Default)
            ValidationExceptionHelper.WrapAndAndRaise<ArgumentNullException>(x is not null, argumentName,
                message ?? "The given nullable char should not be null.");
        return x;
    }
}

/// <summary>
/// 字符守护
/// </summary>
public static class CharGuard
{

}
