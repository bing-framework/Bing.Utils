namespace Bing.Utils.Parameters.Formats;

/// <summary>
/// Url参数格式化器
/// </summary>
public class UrlParameterFormat : ParameterFormatBase
{
    /// <summary>
    /// 获取Url参数格式化器实例
    /// </summary>
    public static IParameterFormat Instance { get; } = new UrlParameterFormat();

    /// <inheritdoc />
    protected override string FormatSeparator => "=";

    /// <inheritdoc />
    protected override string JoinSeparator => "&";
}
