using Bing.Validation;

namespace Bing.Text;

/// <summary>
/// 字符守护帮助类
/// </summary>
internal static class CharGuardHelper
{
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