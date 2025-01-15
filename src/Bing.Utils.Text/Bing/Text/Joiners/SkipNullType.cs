namespace Bing.Text.Joiners;

/// <summary>
/// 跳过 null 的类型
/// </summary>
public enum SkipNullType
{
    /// <summary>
    /// 无
    /// </summary>
    Nothing,

    /// <summary>
    /// 与。当 key 与 value 都为 null 时
    /// </summary>
    WhenBoth,

    /// <summary>
    /// 或。当 key 或者 value 为 null 时
    /// </summary>
    WhenEither,

    /// <summary>
    /// 当 key 为 null
    /// </summary>
    WhenKeyIsNull,

    /// <summary>
    /// 当 value 为 null
    /// </summary>
    WhenValueIsNull,
}