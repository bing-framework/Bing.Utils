namespace Bing.Text.Joiners;

/// <summary>
/// 连接器
/// </summary>
public interface IJoiner
{
    /// <summary>
    /// 跳过 null
    /// </summary>
    IJoiner SkipNulls();

    /// <summary>
    /// 如果为 null，则使用指定的值来替代
    /// </summary>
    /// <param name="value">值</param>
    IJoiner UseForNull(string value);

    /// <summary>
    /// 如果为 null，则使用指定的值来替代
    /// </summary>
    /// <param name="valueFunc">值函数</param>
    IJoiner UseForNull(Func<string, string> valueFunc);

    /// <summary>
    /// 如果为 null，则使用指定的值来替代
    /// </summary>
    /// <param name="valueFunc">值函数</param>
    IJoiner UseForNull(Func<string, int, string> valueFunc);

    /// <summary>
    /// 设置键值对分隔符
    /// </summary>
    /// <param name="separator">分隔符</param>
    IMapJoiner WithKeyValueSeparator(char separator);

    /// <summary>
    /// 设置键值对分隔符
    /// </summary>
    /// <param name="separator">分隔符</param>
    IMapJoiner WithKeyValueSeparator(string separator);

    /// <summary>
    /// 连接
    /// </summary>
    /// <param name="list">列表</param>
    string Join(IEnumerable<string> list);

    /// <summary>
    /// 连接
    /// </summary>
    /// <param name="str1">字符串</param>
    /// <param name="restStrings">其余字符串</param>
    string Join(string str1, params string[] restStrings);

    /// <summary>
    /// 连接
    /// </summary>
    /// <typeparam name="T">泛型类型</typeparam>
    /// <param name="list">列表</param>
    /// <param name="to">转换函数</param>
    string Join<T>(IEnumerable<T> list, Func<T, string> to);

    /// <summary>
    /// 连接
    /// </summary>
    /// <typeparam name="T">泛型类型</typeparam>
    /// <param name="to">转换函数</param>
    /// <param name="item1">项</param>
    /// <param name="restItems">其余项</param>
    string Join<T>(Func<T, string> to, T item1, params T[] restItems);

    /// <summary>
    /// 附加到...
    /// </summary>
    /// <param name="builder">字符串拼接器</param>
    /// <param name="list">列表</param>
    StringBuilder AppendTo(StringBuilder builder, IEnumerable<string> list);

    /// <summary>
    /// 附加到...
    /// </summary>
    /// <param name="builder">字符串拼接器</param>
    /// <param name="str1">字符串</param>
    /// <param name="restStrings">其余字符串</param>
    StringBuilder AppendTo(StringBuilder builder, string str1, params string[] restStrings);

    /// <summary>
    /// 附加到...
    /// </summary>
    /// <typeparam name="T">泛型类型</typeparam>
    /// <param name="builder">字符串拼接器</param>
    /// <param name="list">列表</param>
    /// <param name="to">转换函数</param>
    StringBuilder AppendTo<T>(StringBuilder builder, IEnumerable<T> list, Func<T, string> to);

    /// <summary>
    /// 附加到...
    /// </summary>
    /// <typeparam name="T">泛型类型</typeparam>
    /// <param name="builder">字符串拼接器</param>
    /// <param name="to">转换函数</param>
    /// <param name="item1">项</param>
    /// <param name="restItems">其余项</param>
    StringBuilder AppendTo<T>(StringBuilder builder, Func<T, string> to, T item1, params T[] restItems);
}