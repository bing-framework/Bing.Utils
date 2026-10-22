using System.Runtime.CompilerServices;

namespace Bing.Helpers.Internals;

/// <summary>
/// 缓存转换目标的类型元数据。
/// </summary>
/// <typeparam name="T">声明的类型。</typeparam>
internal static class ConvTypeInfo<T>
{
    /// <summary>
    /// 保存去除可空包装后的基础类型或声明类型。
    /// </summary>
    internal static readonly Type UnderlyingType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);

    /// <summary>
    /// 指示声明类型是否为可空值类型。
    /// </summary>
    internal static readonly bool IsNullableValueType = UnderlyingType != typeof(T);

    /// <summary>
    /// 构造与目标类型相同的可空值而不创建中间装箱对象。
    /// </summary>
    /// <typeparam name="TValue">可空值的基础类型。</typeparam>
    /// <param name="value">转换成功的基础值。</param>
    /// <returns>目标类型的可空值。</returns>
    /// <exception cref="InvalidOperationException">目标类型不是传入基础类型对应的可空类型。</exception>
    internal static T FromNullable<TValue>(TValue value) where TValue : struct
    {
        if (typeof(T) != typeof(TValue?))
            throw new InvalidOperationException("目标类型与可空值类型不一致。");
        TValue? nullable = value;
        return Unsafe.As<TValue?, T>(ref nullable);
    }
}
