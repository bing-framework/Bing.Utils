using System.Diagnostics.Contracts;
using System.Text;

namespace Bing.Text;

/// <summary>
/// 字符串工具
/// </summary>
public static partial class Strings
{
    #region Reverse

    /// <summary>
    /// 反转字符串
    /// </summary>
    /// <param name="builder">StringBuilder</param>
    public static void Reverse(StringBuilder builder)
    {
        if (builder is null || builder.Length == 0)
            return;

        var destination = new char[builder.Length];
        builder.CopyTo(0, destination, 0, builder.Length);
        Array.Reverse(destination);

        builder.Clear();
        builder.Append(destination);
    }

    /// <summary>
    /// 反转字符串，并返回一个新的 <see cref="StringBuilder"/> 实例
    /// </summary>
    /// <param name="builder">字符串生成器</param>
    public static StringBuilder ReverseAndReturnNewInstance(StringBuilder builder)
    {
        if (builder is null || builder.Length == 0)
            return new StringBuilder();

        var destination = new char[builder.Length];
        builder.CopyTo(0, destination, 0, builder.Length);
        Array.Reverse(destination);
        return new StringBuilder().Append(destination);
    }

    /// <summary>
    /// 反转字符串
    /// </summary>
    /// <param name="builder">字符串生成器</param>
    public static string ReverseAndToString(StringBuilder builder)
    {
        if (builder is null || builder.Length == 0)
            return string.Empty;

        var destination = new char[builder.Length];
        builder.CopyTo(0, destination, 0, builder.Length);
        Array.Reverse(destination);
        return string.Join(null, destination);
    }

    #endregion

    #region Remove

    /// <summary>
    /// 移除起始字符串
    /// </summary>
    /// <param name="value">字符串生成器</param>
    /// <param name="start">要移除的值</param>
    public static StringBuilder RemoveStart(StringBuilder value, string start)
    {
        if (value == null)
            return null;
        if (value.Length == 0)
            return value;
        if(string.IsNullOrEmpty(start))
            return value;
        if (start.Length > value.Length)
            return value;
        var chars = start.ToCharArray();
        for (var i = 0; i < chars.Length; i++)
        {
            if (value[i] != chars[i])
                return value;
        }
        return value.Remove(0, start.Length);
    }

    /// <summary>
    /// 移除末尾字符串
    /// </summary>
    /// <param name="value">字符串生成器</param>
    /// <param name="end">要移除的值</param>
    public static StringBuilder RemoveEnd(StringBuilder value, string end)
    {
        if (value == null)
            return null;
        if (value.Length == 0)
            return value;
        if (string.IsNullOrEmpty(end))
            return value;
        if (end.Length > value.Length)
            return value;
        var chars = end.ToCharArray();
        for (var i = chars.Length - 1; i >= 0; i--)
        {
            var j = value.Length - (chars.Length - i);
            if (value[j] != chars[i])
                return value;
        }

        return value.Remove(value.Length - end.Length, end.Length);
    }

    #endregion
}

/// <summary>
/// 字符串扩展
/// </summary>
public static partial class StringsExtensions
{
    #region Append

    /// <summary>
    /// 将一组值添加到 <see cref="StringBuilder"/> 中。
    /// </summary>
    /// <typeparam name="T">类型</typeparam>
    /// <param name="target">StringBuilder</param>
    /// <param name="values">值</param>
    /// <param name="separator">分隔符</param>
    /// <exception cref="NullReferenceException"></exception>
    public static StringBuilder AppendAll<T>(this StringBuilder target, IEnumerable<T> values, string separator = null)
    {
        if (target is null)
            throw new NullReferenceException();
        Contract.EndContractBlock();
        if (values != null)
        {
            if (string.IsNullOrEmpty(separator))
            {
                foreach (var value in values) 
                    target.Append(value);
            }
            else
            {
                foreach (var value in values) 
                    target.AppendWithSeparator(separator, value);
            }
        }
        return target;
    }

    /// <summary>
    /// 将一组值添加到 <see cref="StringBuilder"/> 中。
    /// </summary>
    /// <typeparam name="T">类型</typeparam>
    /// <param name="target">StringBuilder</param>
    /// <param name="values">值</param>
    /// <param name="separator">分隔符</param>
    /// <exception cref="NullReferenceException"></exception>
    public static StringBuilder AppendAll<T>(this StringBuilder target, IEnumerable<T> values, in char separator)
    {
        if (target is null)
            throw new NullReferenceException();
        Contract.EndContractBlock();
        if (values != null)
        {
            foreach (var value in values) 
                target.AppendWithSeparator(in separator, value);
        }
        return target;
    }

    /// <summary>
    /// 将值附加到 <see cref="StringBuilder"/> 以提供的分隔符为前缀。
    /// </summary>
    /// <typeparam name="T">类型</typeparam>
    /// <param name="target">StringBuilder</param>
    /// <param name="separator">分隔符</param>
    /// <param name="values">值</param>
    /// <exception cref="NullReferenceException"></exception>
    /// <exception cref="ArgumentException"></exception>
    public static StringBuilder AppendWithSeparator<T>(this StringBuilder target, string separator, params T[] values)
    {
        if (target is null)
            throw new NullReferenceException();
        if (values is null || values.Length == 0)
            throw new ArgumentException("Parameters missing.", nameof(values));
        Contract.EndContractBlock();
        if (!string.IsNullOrEmpty(separator) && target.Length != 0)
            target.Append(separator);
        target.AppendAll(values);
        return target;
    }

    /// <summary>
    /// 将值附加到 <see cref="StringBuilder"/> 以提供的分隔符为前缀。
    /// </summary>
    /// <typeparam name="T">类型</typeparam>
    /// <param name="target">StringBuilder</param>
    /// <param name="separator">分隔符</param>
    /// <param name="values">值</param>
    /// <exception cref="NullReferenceException"></exception>
    /// <exception cref="ArgumentException"></exception>
    public static StringBuilder AppendWithSeparator<T>(this StringBuilder target, in char separator, params T[] values)
    {
        if (target is null)
            throw new NullReferenceException();
        if (values is null || values.Length == 0)
            throw new ArgumentException("Parameters missing.", nameof(values));
        Contract.EndContractBlock();
        if (target.Length != 0)
            target.Append(separator);
        target.AppendAll(values);
        return target;
    }

    /// <summary>
    /// 将键值对附加到 <see cref="StringBuilder"/> 以提供的分隔符为前缀。
    /// </summary>
    /// <typeparam name="T">泛型类型</typeparam>
    /// <param name="target">StringBuilder</param>
    /// <param name="source">源</param>
    /// <param name="key">键名</param>
    /// <param name="itemSeparator">项分隔符</param>
    /// <param name="keyValueSeparator">键值对分隔符</param>
    /// <exception cref="NullReferenceException"></exception>
    /// <exception cref="ArgumentNullException"></exception>
    public static void AppendWithSeparator<T>(this StringBuilder target, IDictionary<string, T> source, string key, string itemSeparator, string keyValueSeparator)
    {
        if (target is null)
            throw new NullReferenceException();
        if (source is null)
            throw new ArgumentNullException(nameof(source));
        if (key is null)
            throw new ArgumentNullException(nameof(key));
        if (itemSeparator is null)
            throw new ArgumentNullException(nameof(itemSeparator));
        if (keyValueSeparator is null)
            throw new ArgumentNullException(nameof(keyValueSeparator));
        Contract.EndContractBlock();
        if (source.TryGetValue(key, out var result))
            target.AppendWithSeparator(itemSeparator, key)
                .Append(keyValueSeparator)
                .Append(result);
    }

    /// <summary>
    /// 添加内容并换行
    /// </summary>
    /// <param name="sb">StringBuilder</param>
    /// <param name="value">内容</param>
    /// <param name="parameters">参数</param>
    public static StringBuilder AppendLine(this StringBuilder sb, string value, params object[] parameters)
    {
        return sb.AppendLine(string.Format(value, parameters));
    }

    /// <summary>
    /// 添加数组内容
    /// </summary>
    /// <typeparam name="T">数组内容</typeparam>
    /// <param name="sb">StringBuilder</param>
    /// <param name="separator">分隔符</param>
    /// <param name="values">数组内容</param>
    public static StringBuilder AppendJoin<T>(this StringBuilder sb, string separator, params T[] values)
    {
        return sb.Append(string.Join(separator, values));
    }

    /// <summary>
    /// 根据条件添加内容
    /// </summary>
    /// <param name="sb">StringBuilder</param>
    /// <param name="condition">拼接条件</param>
    /// <param name="value">内容</param>
    public static StringBuilder AppendIf(this StringBuilder sb, bool condition, object value)
    {
        if (condition)
            sb.Append(value.ToString());
        return sb;
    }

    /// <summary>
    /// 根据条件添加内容
    /// </summary>
    /// <param name="sb">StringBuilder</param>
    /// <param name="condition">拼接条件</param>
    /// <param name="value">内容</param>
    /// <param name="parameters">参数</param>
    public static StringBuilder AppendFormatIf(this StringBuilder sb, bool condition, string value, params object[] parameters)
    {
        if (condition)
            sb.AppendFormat(value, parameters);
        return sb;
    }

    /// <summary>
    /// 根据条件添加内容并换行
    /// </summary>
    /// <param name="sb">StringBuilder</param>
    /// <param name="condition">拼接条件</param>
    /// <param name="value">内容</param>
    public static StringBuilder AppendLineIf(this StringBuilder sb, bool condition, object value)
    {
        if (condition)
            sb.AppendLine(value.ToString());
        return sb;
    }

    /// <summary>
    /// 根据条件添加内容并换行
    /// </summary>
    /// <param name="sb">StringBuilder</param>
    /// <param name="condition">拼接条件</param>
    /// <param name="value">内容</param>
    /// <param name="parameters">参数</param>
    public static StringBuilder AppendLine(this StringBuilder sb, bool condition, string value, params object[] parameters)
    {
        if (condition)
            sb.AppendFormat(value, parameters).AppendLine();
        return sb;
    }

    #endregion

    #region Reverse

    /// <summary>
    /// 反转字符串
    /// </summary>
    /// <param name="builder">StringBuilder</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Reverse(this StringBuilder builder)
    {
        Strings.Reverse(builder);
    }

    /// <summary>
    /// 反转字符串，并返回一个新的 <see cref="StringBuilder"/> 实例
    /// </summary>
    /// <param name="builder">字符串生成器</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static StringBuilder ReverseAndReturnNewInstance(this StringBuilder builder)
    {
        return Strings.ReverseAndReturnNewInstance(builder);
    }

    /// <summary>
    /// 反转字符串
    /// </summary>
    /// <param name="builder">字符串生成器</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ReverseAndToString(this StringBuilder builder)
    {
        return Strings.ReverseAndToString(builder);
    }

    #endregion

    #region Remove

    /// <summary>
    /// 移除起始字符串
    /// </summary>
    /// <param name="value">字符串生成器</param>
    /// <param name="start">要移除的值</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static StringBuilder RemoveStart(this StringBuilder value, string start)
    {
        return Strings.RemoveStart(value, start);
    }

    /// <summary>
    /// 移除末尾字符串
    /// </summary>
    /// <param name="value">字符串生成器</param>
    /// <param name="end">要移除的值</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static StringBuilder RemoveEnd(this StringBuilder value, string end)
    {
        return Strings.RemoveEnd(value, end);
    }

    #endregion

    #region ToCharArray

    /// <summary>
    /// 将 <see cref="StringBuilder"/> 转换为字符数组。
    /// </summary>
    /// <param name="builder">StringBuilder</param>
    public static char[] ToCharArray(this StringBuilder builder)
    {
        var res = new char[builder.Length];
        builder.CopyTo(0, res, 0, builder.Length);
        return res;
    }

    #endregion

    #region ToStringBuilder

    /// <summary>
    /// 将一组只读的连续内存的值转换为 <see cref="StringBuilder"/>。
    /// </summary>
    /// <typeparam name="T">类型</typeparam>
    /// <param name="source">数据源</param>
    public static StringBuilder ToStringBuilder<T>(this in ReadOnlySpan<T> source)
    {
        var len = source.Length;
        var sb = new StringBuilder(len);
        for (var i = 0; i < len; i++)
            sb.Append(source[i]);
        return sb;
    }

    /// <summary>
    /// 将一组只读的连续内存的值转换为 <see cref="StringBuilder"/>。
    /// </summary>
    /// <typeparam name="T">类型</typeparam>
    /// <param name="source">数据源</param>
    /// <param name="separator">分隔符</param>
    public static StringBuilder ToStringBuilder<T>(this in ReadOnlySpan<T> source, in string separator)
    {
        var len = source.Length;
        if (len < 2 || string.IsNullOrEmpty(separator))
            return ToStringBuilder(source);
        var sb = new StringBuilder(2 * len - 1);
        sb.Append(source[0]);
        for (var i = 1; i < len; i++)
        {
            sb.Append(separator);
            sb.Append(source[i]);
        }
        return sb;
    }

    /// <summary>
    /// 将一组只读的连续内存的值转换为 <see cref="StringBuilder"/>。
    /// </summary>
    /// <typeparam name="T">类型</typeparam>
    /// <param name="source">数据源</param>
    /// <param name="separator">分隔符</param>
    public static StringBuilder ToStringBuilder<T>(this in ReadOnlySpan<T> source, in char separator)
    {
        var len = source.Length;
        if (len < 2)
            return ToStringBuilder(source);
        var sb = new StringBuilder(2 * len - 1);
        sb.Append(source[0]);
        for (var i = 1; i < len; i++)
        {
            sb.Append(separator);
            sb.Append(source[i]);
        }
        return sb;
    }

    /// <summary>
    /// 将一组对象转换为 <see cref="StringBuilder"/>。
    /// </summary>
    /// <typeparam name="T">类型</typeparam>
    /// <param name="source">数据源</param>
    public static StringBuilder ToStringBuilder<T>(this IEnumerable<T> source)
    {
        var sb = new StringBuilder();
        foreach (var s in source) 
            sb.Append(s);
        return sb;
    }

    /// <summary>
    /// 将一组对象转换为 <see cref="StringBuilder"/>。
    /// </summary>
    /// <typeparam name="T">类型</typeparam>
    /// <param name="source">数据源</param>
    /// <param name="separator">分隔符</param>
    public static StringBuilder ToStringBuilder<T>(this IEnumerable<T> source, in string separator)
    {
        var sb = new StringBuilder();
        var first = true;
        foreach (var s in source)
        {
            if (first)
                first = false;
            else
                sb.Append(separator);
            sb.Append(s);
        }
        return sb;
    }

    /// <summary>
    /// 将一组对象转换为 <see cref="StringBuilder"/>。
    /// </summary>
    /// <typeparam name="T">类型</typeparam>
    /// <param name="source">数据源</param>
    /// <param name="separator">分隔符</param>
    public static StringBuilder ToStringBuilder<T>(this IEnumerable<T> source, in char separator)
    {
        var sb = new StringBuilder();
        var first = true;
        foreach (var s in source)
        {
            if (first)
                first = false;
            else
                sb.Append(separator);
            sb.Append(s);
        }
        return sb;
    }

    #endregion
}