namespace Bing.Text.Joiners;

/// <summary>
/// 元组连接器
/// </summary>
public interface ITupleJoiner
{
    /// <summary>
    /// 跳过 null
    /// </summary>
    ITupleJoiner SkipNulls();

    /// <summary>
    /// 跳过 null
    /// </summary>
    /// <param name="type">跳过 null 的类型</param>
    ITupleJoiner SkipNulls(SkipNullType type);

    /// <summary>
    /// 如果为 null，则使用指定的值来替代
    /// </summary>
    /// <typeparam name="T1">键类型</typeparam>
    /// <typeparam name="T2">值类型</typeparam>
    /// <param name="tupleKeyFunc">元组键函数</param>
    /// <param name="tupleValueFunc">元祖值函数</param>
    ITupleJoiner UseForNull<T1, T2>(Func<T1, T2, T1> tupleKeyFunc, Func<T1, T2, T2> tupleValueFunc);

    /// <summary>
    /// 连接
    /// </summary>
    /// <param name="list">列表</param>
    string Join(IEnumerable<(string, string)> list);

    /// <summary>
    /// 连接
    /// </summary>
    /// <param name="list">列表</param>
    /// <param name="defaultKey">默认键</param>
    /// <param name="defaultValue">默认值</param>
    string Join(IEnumerable<(string, string)> list, string defaultKey, string defaultValue);

    /// <summary>
    /// 连接
    /// </summary>
    /// <param name="tuple1">元组</param>
    /// <param name="restTuples">其余元组</param>
    string Join((string, string) tuple1, params (string, string)[] restTuples);

    /// <summary>
    /// 连接
    /// </summary>
    /// <typeparam name="T1">键类型</typeparam>
    /// <typeparam name="T2">值类型</typeparam>
    /// <param name="list">列表</param>
    /// <param name="keyFunc">键函数</param>
    /// <param name="valueFunc">值函数</param>
    string Join<T1, T2>(IEnumerable<(T1, T2)> list, Func<T1, string> keyFunc, Func<T2, string> valueFunc);

    /// <summary>
    /// 连接
    /// </summary>
    /// <typeparam name="T1">键类型</typeparam>
    /// <typeparam name="T2">值类型</typeparam>
    /// <param name="list">列表</param>
    /// <param name="defaultKey">默认键</param>
    /// <param name="defaultValue">默认值</param>
    /// <param name="keyFunc">键函数</param>
    /// <param name="valueFunc">值函数</param>
    string Join<T1, T2>(IEnumerable<(T1, T2)> list, T1 defaultKey, T2 defaultValue, Func<T1, string> keyFunc, Func<T2, string> valueFunc);

    /// <summary>
    /// 连接
    /// </summary>
    /// <typeparam name="T1">键类型</typeparam>
    /// <typeparam name="T2">值类型</typeparam>
    /// <param name="keyFunc">键函数</param>
    /// <param name="valueFunc">值函数</param>
    /// <param name="tuple1">元组</param>
    /// <param name="restTuples">其余元组</param>
    string Join<T1, T2>(Func<T1, string> keyFunc, Func<T2, string> valueFunc, (T1, T2) tuple1, params (T1, T2)[] restTuples);

    /// <summary>
    /// 附加到...
    /// </summary>
    /// <param name="builder">字符串拼接器</param>
    /// <param name="list">列表</param>
    StringBuilder AppendTo(StringBuilder builder, IEnumerable<(string, string)> list);

    /// <summary>
    /// 附加到...
    /// </summary>
    /// <param name="builder">字符串拼接器</param>
    /// <param name="list">列表</param>
    /// <param name="defaultKey">默认键</param>
    /// <param name="defaultValue">默认值</param>
    StringBuilder AppendTo(StringBuilder builder, IEnumerable<(string, string)> list, string defaultKey, string defaultValue);

    /// <summary>
    /// 附加到...
    /// </summary>
    /// <param name="builder">字符串拼接器</param>
    /// <param name="tuple1">元组</param>
    /// <param name="restTuples">其余元组</param>
    StringBuilder AppendTo(StringBuilder builder, (string, string) tuple1, params (string, string)[] restTuples);

    /// <summary>
    /// 附加到...
    /// </summary>
    /// <typeparam name="T1">键类型</typeparam>
    /// <typeparam name="T2">值类型</typeparam>
    /// <param name="builder">字符串拼接器</param>
    /// <param name="list">列表</param>
    /// <param name="keyFunc">键函数</param>
    /// <param name="valueFunc">值函数</param>
    StringBuilder AppendTo<T1, T2>(StringBuilder builder, IEnumerable<(T1, T2)> list, Func<T1, string> keyFunc, Func<T2, string> valueFunc);

    /// <summary>
    /// 附加到...
    /// </summary>
    /// <typeparam name="T1">键类型</typeparam>
    /// <typeparam name="T2">值类型</typeparam>
    /// <param name="builder">字符串拼接器</param>
    /// <param name="list">列表</param>
    /// <param name="defaultKey">默认键</param>
    /// <param name="defaultValue">默认值</param>
    /// <param name="keyFunc">键函数</param>
    /// <param name="valueFunc">值函数</param>
    StringBuilder AppendTo<T1, T2>(StringBuilder builder, IEnumerable<(T1, T2)> list, T1 defaultKey, T2 defaultValue, Func<T1, string> keyFunc, Func<T2, string> valueFunc);

    /// <summary>
    /// 附加到...
    /// </summary>
    /// <typeparam name="T1">键类型</typeparam>
    /// <typeparam name="T2">值类型</typeparam>
    /// <param name="builder">字符串拼接器</param>
    /// <param name="keyFunc">键函数</param>
    /// <param name="valueFunc">值函数</param>
    /// <param name="tuple1">元组</param>
    /// <param name="restTuples">其余元组</param>
    StringBuilder AppendTo<T1, T2>(StringBuilder builder, Func<T1, string> keyFunc, Func<T2, string> valueFunc, (T1, T2) tuple1, params (T1, T2)[] restTuples);
}