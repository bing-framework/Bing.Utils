using Bing.Reflection;

// ReSharper disable once CheckNamespace
namespace Bing;

/// <summary>
/// 类型(<see cref="Type"/>) 扩展
/// </summary>
public static class BingTypeExtensions
{
    #region GetFullNameWithAssemblyName(获取包含程序集名称的类型全名)

    /// <summary>
    /// 获取包含程序集名称的类型全名。
    /// </summary>
    /// <param name="type">类型</param>
    /// <returns>包含程序集名称的类型全名。</returns>
    /// <exception cref="ArgumentNullException"></exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string GetFullNameWithAssemblyName(this Type type) => Reflections.GetFullNameWithAssemblyName(type);

    #endregion

    #region IsAssignableTo(检查类型是否可分配给目标类型)

    /// <summary>
    /// 检查类型是否可分配给目标类型 <typeparamref name="TTarget"/>。<br />
    /// 内部使用 <see cref="Type.IsAssignableFrom"/> 方法。
    /// </summary>
    /// <typeparam name="TTarget">目标类型。</typeparam>
    /// <param name="type">要检查的类型。</param>
    /// <returns>如果类型可分配给目标类型，则为 true；否则为 false。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsAssignableTo<TTarget>(this Type type) => Reflections.IsAssignableTo<TTarget>(type);

    /// <summary>
    /// 检查类型是否可分配给目标类型。<br />
    /// 内部使用 <see cref="Type.IsAssignableFrom"/> 方法。
    /// </summary>
    /// <param name="type">要检查的类型。</param>
    /// <param name="targetType">目标类型。</param>
    /// <returns>如果类型可分配给目标类型，则为 true；否则为 false。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsAssignableTo(this Type type, Type targetType) => Reflections.IsAssignableTo(type, targetType);

    #endregion

    #region GetBaseClasses(获取指定类型的所有基类型)

    /// <summary>
    /// 获取指定类型及其基类型的数组。
    /// </summary>
    /// <param name="type">要获取基类型的类型。</param>
    /// <param name="includeObject">是否包含 <see cref="object"/> 类型（默认为 true）。</param>
    /// <returns>包含类型及其基类型的数组。</returns>
    public static Type[] GetBaseClasses(this Type type, bool includeObject = true) => Reflections.GetBaseClasses(type, includeObject);

    /// <summary>
    /// 获取指定类型的所有基类型
    /// </summary>
    /// <param name="type">要获取基类型的类型。</param>
    /// <param name="stoppingType">停止添加的类型。</param>
    /// <param name="includeObject">是否包含 <see cref="object"/> 类型（默认为 true）。</param>
    /// <returns>包含类型及其基类型的数组。</returns>
    public static Type[] GetBaseClasses(this Type type, Type stoppingType, bool includeObject = true)=>Reflections.GetBaseClasses(type, stoppingType, includeObject);

    #endregion
}