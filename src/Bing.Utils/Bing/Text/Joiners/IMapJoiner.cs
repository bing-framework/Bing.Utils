using System.Text;

namespace Bing.Text.Joiners;

/// <summary>
/// 映射连接器
/// </summary>
public interface IMapJoiner
{
    /// <summary>
    /// 跳过 null
    /// </summary>
    IMapJoiner SkipNulls();

    /// <summary>
    /// 跳过 null
    /// </summary>
    /// <param name="type">跳过 null 的类型</param>
    IMapJoiner SkipNulls(SkipNullType type);

    /// <summary>
    /// 如果为 null，则使用指定的值来替代
    /// </summary>
    /// <param name="key">键</param>
    /// <param name="value">值</param>
    IMapJoiner UseForNull(string key, string value);

    /// <summary>
    /// 如果为 null，则使用指定的值来替代
    /// </summary>
    /// <param name="keyFunc">键函数</param>
    /// <param name="valueFunc">值函数</param>
    IMapJoiner UseForNull(Func<string, string> keyFunc, Func<string, string> valueFunc);

    /// <summary>
    /// 如果为 null，则使用指定的值来替代
    /// </summary>
    /// <param name="keyFunc">键函数</param>
    /// <param name="valueFunc">值函数</param>
    IMapJoiner UseForNull(Func<string, int, string> keyFunc, Func<string, int, string> valueFunc);

    /// <summary>
    /// 如果为 null，则使用指定的值来替代
    /// </summary>
    /// <typeparam name="T1">键类型</typeparam>
    /// <typeparam name="T2">值类型</typeparam>
    /// <param name="key">键</param>
    /// <param name="value">值</param>
    IMapJoiner UseForNull<T1, T2>(T1 key, T2 value);

    /// <summary>
    /// 如果为 null，则使用指定的值来替代
    /// </summary>
    /// <typeparam name="T1">键类型</typeparam>
    /// <typeparam name="T2">值类型</typeparam>
    /// <param name="keyFunc">键函数</param>
    /// <param name="valueFunc">值函数</param>
    IMapJoiner UseForNull<T1, T2>(Func<T1, T1> keyFunc, Func<T2, T2> valueFunc);

    /// <summary>
    /// 如果为 null，则使用指定的值来替代
    /// </summary>
    /// <typeparam name="T1">键类型</typeparam>
    /// <typeparam name="T2">值类型</typeparam>
    /// <param name="keyFunc">键函数</param>
    /// <param name="valueFunc">值函数</param>
    IMapJoiner UseForNull<T1, T2>(Func<T1, int, T1> keyFunc, Func<T2, int, T2> valueFunc);

    /// <summary>
    /// 切换为 Tuple 模式
    /// </summary>
    ITupleJoiner FromTuple();

    /// <summary>
    /// 连接
    /// </summary>
    /// <param name="list">列表</param>
    string Join(IEnumerable<string> list);

    /// <summary>
    /// 连接
    /// </summary>
    /// <param name="list">列表</param>
    /// <param name="defaultKey">默认键</param>
    /// <param name="defaultValue">默认值</param>
    string Join(IEnumerable<string> list, string defaultKey, string defaultValue);

    /// <summary>
    /// 连接
    /// </summary>
    /// <param name="str1">字符串</param>
    /// <param name="str2">字符串</param>
    /// <param name="restStrings">其余字符串</param>
    string Join(string str1, string str2, params string[] restStrings);

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
    /// <param name="list">列表</param>
    /// <param name="defaultKey">默认键</param>
    /// <param name="defaultValue">默认值</param>
    StringBuilder AppendTo(StringBuilder builder, IEnumerable<string> list, string defaultKey, string defaultValue);

    /// <summary>
    /// 连接
    /// </summary>
    /// <param name="builder">字符串拼接器</param>
    /// <param name="str1">字符串</param>
    /// <param name="str2">字符串</param>
    /// <param name="restStrings">其余字符串</param>
    StringBuilder Join(StringBuilder builder, string str1, string str2, params string[] restStrings);
}