namespace Bing.Collections;

/// <summary>
/// 相等比较 操作辅助类，用于快速创建<see cref="IEqualityComparer{T}"/>的实例
/// </summary>
/// <typeparam name="T">要比较的对象的类型</typeparam>
/// <example>
/// var equalityComparer1 = EqualityHelper[Person].CreateComparer(p => p.ID);
/// var equalityComparer2 = EqualityHelper[Person].CreateComparer(p => p.Name);
/// var equalityComparer3 = EqualityHelper[Person].CreateComparer(p => p.Birthday.Year);
/// </example>
public static class EqualityHelper<T>
{
    /// <summary>
    /// 创建相等比较器
    /// </summary>
    /// <typeparam name="TV">用于比较的键的类型</typeparam>
    /// <param name="keySelector">键选择器，从类型 <typeparamref name="T"/> 中选择用于比较的键。</param>
    /// <returns>一个基于指定键的相等比较器。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IEqualityComparer<T> CreateComparer<TV>(Func<T, TV> keySelector) => new CommonEqualityComparer<TV>(keySelector);

    /// <summary>
    /// 创建相等比较器
    /// </summary>
    /// <typeparam name="TV">用于比较的键的类型</typeparam>
    /// <param name="keySelector">键选择器，从类型 <typeparamref name="T"/> 中选择用于比较的键。</param>
    /// <param name="comparer">用于比较键的相等比较器。</param>
    /// <returns>一个基于指定键和指定比较器的相等比较器。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IEqualityComparer<T> CreateComparer<TV>(Func<T, TV> keySelector, IEqualityComparer<TV> comparer) => new CommonEqualityComparer<TV>(keySelector, comparer);

    /// <summary>
    /// 通用相等比较器
    /// </summary>
    /// <typeparam name="TV">用于比较的键类型</typeparam>
    private class CommonEqualityComparer<TV> : IEqualityComparer<T>
    {
        /// <summary>
        /// 相等比较器
        /// </summary>
        private readonly IEqualityComparer<TV> _comparer;

        /// <summary>
        /// 键选择器
        /// </summary>
        private readonly Func<T, TV> _keySelector;

        /// <summary>
        /// 初始化一个<see cref="CommonEqualityComparer{TV}"/>类型的实例
        /// </summary>
        /// <param name="keySelector">键选择器</param>
        public CommonEqualityComparer(Func<T, TV> keySelector) : this(keySelector, EqualityComparer<TV>.Default) { }

        /// <summary>
        /// 初始化一个<see cref="CommonEqualityComparer{TV}"/>类型的实例
        /// </summary>
        /// <param name="keySelector">键选择器</param>
        /// <param name="comparer">相等比较器</param>
        public CommonEqualityComparer(Func<T, TV> keySelector, IEqualityComparer<TV> comparer)
        {
            _keySelector = keySelector;
            _comparer = comparer;
        }

        /// <inheritdoc />
        public bool Equals(T x, T y) => _comparer.Equals(_keySelector(x), _keySelector(y));

        /// <inheritdoc />
        public int GetHashCode(T obj) => _comparer.GetHashCode(_keySelector(obj));
    }
}