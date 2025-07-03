// ReSharper disable once CheckNamespace
namespace Bing.Extensions;

/// <summary>
/// 布尔值(<see cref="Boolean"/>) 扩展
/// </summary>
public static class BooleanExtensions
{
    #region ToLower(将布尔值转换为小写字符串)

    /// <summary>
    /// 将布尔值转换为小写字符串
    /// </summary>
    /// <param name="value">值</param>
    public static string ToLower(this bool value) => value.ToString().ToLower();

    /// <summary>
    /// 将布尔值转换为小写字符串
    /// </summary>
    /// <param name="value">值</param>
    public static string ToLower(this bool? value) => ToLower(value ?? false);

    #endregion

    #region ToBinaryTypeNumber(将布尔值转换为二进制数字类型)

    /// <summary>
    /// 将布尔值转换为二进制数字类型（true:1、false:0）
    /// </summary>
    /// <param name="value">值</param>
    public static int ToBinaryTypeNumber(this bool value) => value ? 1 : 0;

    /// <summary>
    /// 将布尔值转换为二进制数字类型（true:1、false:0）
    /// </summary>
    /// <param name="value">值</param>
    public static int ToBinaryTypeNumber(this bool? value) => ToBinaryTypeNumber(value ?? false);

    #endregion

    /// <summary>
    /// 验证布尔值必须为真
    /// </summary>
    /// <param name="value">要验证的布尔值</param>
    /// <exception cref="ArgumentException">当值为 false 时抛出</exception>
    /// <remarks>
    /// 此方法用于参数校验，确保布尔条件必须为 true，否则立即中断执行流程。
    /// </remarks>
    public static void MustTrue(this bool value)
    {
        if (!value)
            throw new ArgumentException("值必须为真");
    }

    /// <summary>
    /// 验证布尔值必须为假
    /// </summary>
    /// <param name="value">要验证的布尔值</param>
    /// <exception cref="ArgumentException">当值为 true 时抛出</exception>
    /// <remarks>
    /// 此方法用于参数校验，确保布尔条件必须为 false，否则立即中断执行流程。
    /// </remarks>
    public static void MustFalse(this bool value)
    {
        if (value)
            throw new ArgumentException("值必须为假");
    }

    /// <summary>
    /// 逻辑与操作
    /// </summary>
    /// <param name="value">当前布尔值</param>
    /// <param name="condition">要进行逻辑与运算的条件</param>
    /// <returns>两个布尔值的逻辑与结果。只有当两个值都为 true 时，才返回 true</returns>
    /// <remarks>
    /// 此扩展方法提供了一种更流畅的方式来组合多个布尔条件，可以链式调用。
    /// </remarks>
    public static bool And(this bool value, bool condition) => value && condition;

    /// <summary>
    /// 逻辑或操作
    /// </summary>
    /// <param name="value">当前布尔值</param>
    /// <param name="condition">要进行逻辑或运算的条件</param>
    /// <returns>两个布尔值的逻辑或结果。当任一值为 true 时，返回 true</returns>
    /// <remarks>
    /// 此扩展方法提供了一种更流畅的方式来组合多个布尔条件，可以链式调用。
    /// </remarks>
    public static bool Or(this bool value, bool condition) => value || condition;
}