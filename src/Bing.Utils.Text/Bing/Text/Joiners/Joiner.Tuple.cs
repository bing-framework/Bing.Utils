using Bing.Collections;

namespace Bing.Text.Joiners;

/// <summary>
/// 连接器
/// </summary>
public partial class Joiner : ITupleJoiner
{
    #region SkipValueNulls

    /// <summary>
    /// 跳过 null
    /// </summary>
    ITupleJoiner ITupleJoiner.SkipNulls()
    {
        Options.SetSkipTupleValueNulls();
        return this;
    }

    /// <summary>
    /// 跳过 null
    /// </summary>
    /// <param name="type">跳过 null 的类型</param>
    ITupleJoiner ITupleJoiner.SkipNulls(SkipNullType type)
    {
        Options.SetSkipTupleValueNulls(type);
        return this;
    }

    #endregion

    #region UseForNull

    /// <summary>
    /// 如果为 null，则使用指定的值来替代
    /// </summary>
    /// <typeparam name="T1">键类型</typeparam>
    /// <typeparam name="T2">值类型</typeparam>
    /// <param name="tupleKeyFunc">元组键函数</param>
    /// <param name="tupleValueFunc">元祖值函数</param>
    ITupleJoiner ITupleJoiner.UseForNull<T1, T2>(Func<T1, T2, T1> tupleKeyFunc, Func<T1, T2, T2> tupleValueFunc)
    {
        Options.SetTupleReplace(tupleKeyFunc,tupleValueFunc);
        return this;
    }

    #endregion

    #region Join - Tuple

    /// <summary>
    /// 连接
    /// </summary>
    /// <param name="list">列表</param>
    string ITupleJoiner.Join(IEnumerable<(string, string)> list)
    {
        var replacer = Options.GetTupleReplace<string, string, string, string>();
        var defaultKey = replacer.KeyFunc?.Invoke(string.Empty, string.Empty) ?? string.Empty;
        var defaultValue = replacer.ValueFunc?.Invoke(string.Empty, string.Empty) ?? string.Empty;
        var middle = new List<string>();
        JoinToTupleString(
            middle, (c, k, v, _) => c.Add($"{k}{Options.MapSeparator}{v}"),
            list, defaultKey, defaultValue, k => k, v => v, replacer);
        return middle.JoinToString(_on, JoinerUtils.GetTuplePredicate(Options));
    }

    /// <summary>
    /// 连接
    /// </summary>
    /// <param name="list">列表</param>
    /// <param name="defaultKey">默认键</param>
    /// <param name="defaultValue">默认值</param>
    string ITupleJoiner.Join(IEnumerable<(string, string)> list, string defaultKey, string defaultValue)
    {
        var replacer = Options.GetTupleReplace<string, string, string, string>();
        var middle = new List<string>();
        JoinToTupleString(
            middle, (c, k, v, _) => c.Add($"{k}{Options.MapSeparator}{v}"),
            list, defaultKey, defaultValue, k => k, v => v, replacer);
        return middle.JoinToString(_on, JoinerUtils.GetTuplePredicate(Options));
    }

    /// <summary>
    /// 连接
    /// </summary>
    /// <typeparam name="T1">键类型</typeparam>
    /// <typeparam name="T2">值类型</typeparam>
    /// <param name="list">列表</param>
    /// <param name="keyFunc">键函数</param>
    /// <param name="valueFunc">值函数</param>
    string ITupleJoiner.Join<T1, T2>(IEnumerable<(T1, T2)> list, Func<T1, string> keyFunc, Func<T2, string> valueFunc)
    {
        var middle = new List<string>();
        JoinToTupleString(
            middle, (c, k, v, _) => c.Add($"{k}{Options.MapSeparator}{v}"),
            list, default, default, keyFunc, valueFunc, Options.GetTupleReplace<T1, T2>());
        return middle.JoinToString(_on, JoinerUtils.GetTuplePredicate(Options));
    }

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
    string ITupleJoiner.Join<T1, T2>(IEnumerable<(T1, T2)> list, T1 defaultKey, T2 defaultValue, Func<T1, string> keyFunc, Func<T2, string> valueFunc)
    {
        var middle = new List<string>();
        JoinToTupleString(
            middle, (c, k, v, _) => c.Add($"{k}{Options.MapSeparator}{v}"),
            list, defaultKey, defaultValue, keyFunc, valueFunc, Options.GetTupleReplace<T1, T2>());
        return middle.JoinToString(_on, JoinerUtils.GetTuplePredicate(Options));
    }

    /// <summary>
    /// 连接
    /// </summary>
    /// <param name="tuple1">元组</param>
    /// <param name="restTuples">其余元组</param>
    string ITupleJoiner.Join((string, string) tuple1, params (string, string)[] restTuples)
    {
        var list = new List<(string, string)> { tuple1 };
        list.AddRange(restTuples);
        return ((ITupleJoiner)this).Join(list);
    }

    /// <summary>
    /// 连接
    /// </summary>
    /// <typeparam name="T1">键类型</typeparam>
    /// <typeparam name="T2">值类型</typeparam>
    /// <param name="keyFunc">键函数</param>
    /// <param name="valueFunc">值函数</param>
    /// <param name="tuple1">元组</param>
    /// <param name="restTuples">其余元组</param>
    string ITupleJoiner.Join<T1, T2>(Func<T1, string> keyFunc, Func<T2, string> valueFunc, (T1, T2) tuple1, params (T1, T2)[] restTuples)
    {
        var list = new List<(T1, T2)> { tuple1 };
        list.AddRange(restTuples);
        return ((ITupleJoiner)this).Join(list, keyFunc, valueFunc);
    }

    /// <summary>
    /// 连接元组字符串
    /// </summary>
    /// <typeparam name="T1">泛型类型</typeparam>
    /// <typeparam name="T2">泛型类型</typeparam>
    /// <typeparam name="TContainer">容器类型</typeparam>
    /// <param name="container">容器</param>
    /// <param name="containerUpdateFunc">容器更新函数</param>
    /// <param name="list">列表</param>
    /// <param name="defaultKey">默认键</param>
    /// <param name="defaultValue">默认值</param>
    /// <param name="keyFunc">键函数</param>
    /// <param name="valueFunc">值函数</param>
    /// <param name="replacer">替换器</param>
    private void JoinToTupleString<T1, T2, TContainer>(
        TContainer container, Action<TContainer, string, string, int> containerUpdateFunc,
        IEnumerable<(T1, T2)> list, T1 defaultKey, T2 defaultValue, Func<T1, string> keyFunc, Func<T2, string> valueFunc,
        (Func<T1, T2, T1> KeyFunc, Func<T1, T2, T2> ValueFunc) replacer)
    {
        if (list == null)
            return;

        var instances = list.ToList();
        if (!instances.Any())
            return;

        var index = 0;
        foreach (var instance in instances)
        {
            var k = instance.Item1;
            var v = instance.Item2;
            var key = keyFunc(k);
            var value = valueFunc(v);

            if (JoinerUtils.SkipTuple(Options, key, value))
                continue;
            else if (JoinerUtils.NeedFixTupleValue(Options, key, value))
            {
                key = JoinerUtils.FixTupleKeySafety(k, key, v, value, defaultKey, keyFunc, replacer.KeyFunc, Options.SkipTupleValueNullType);
                value = JoinerUtils.FixTupleValueSafety(v, value, k, key, defaultValue, valueFunc, replacer.ValueFunc, Options.SkipTupleValueNullType);
            }

            containerUpdateFunc(container, key, value, index++);
        }
    }

    #endregion

    #region AppendTo

    /// <summary>
    /// 附加到...
    /// </summary>
    /// <param name="builder">字符串拼接器</param>
    /// <param name="list">列表</param>
    StringBuilder ITupleJoiner.AppendTo(StringBuilder builder, IEnumerable<(string, string)> list)
    {
        var replacer = Options.GetTupleReplace<string, string, string, string>();
        var defaultKey = replacer.KeyFunc?.Invoke(string.Empty, string.Empty) ?? string.Empty;
        var defaultValue = replacer.ValueFunc?.Invoke(string.Empty, string.Empty) ?? string.Empty;
        JoinToTupleString(
            builder, (c, k, v, i) => c.Append($"{(i > 0 ? _on : string.Empty)}{k}{Options.MapSeparator}{v}"),
            list, defaultKey, defaultValue, k => k, v => v, replacer);
        return builder;
    }

    /// <summary>
    /// 附加到...
    /// </summary>
    /// <param name="builder">字符串拼接器</param>
    /// <param name="list">列表</param>
    /// <param name="defaultKey">默认键</param>
    /// <param name="defaultValue">默认值</param>
    StringBuilder ITupleJoiner.AppendTo(StringBuilder builder, IEnumerable<(string, string)> list, string defaultKey, string defaultValue)
    {
        var replacer = Options.GetTupleReplace<string, string, string, string>();
        JoinToTupleString(
            builder, (c, k, v, i) => c.Append($"{(i > 0 ? _on : string.Empty)}{k}{Options.MapSeparator}{v}"),
            list, defaultKey, defaultValue, k => k, v => v, replacer);
        return builder;
    }

    /// <summary>
    /// 附加到...
    /// </summary>
    /// <param name="builder">字符串拼接器</param>
    /// <param name="tuple1">元组</param>
    /// <param name="restTuples">其余元组</param>
    StringBuilder ITupleJoiner.AppendTo(StringBuilder builder, (string, string) tuple1, params (string, string)[] restTuples)
    {
        var list = new List<(string, string)> { tuple1 };
        list.AddRange(restTuples);
        return ((ITupleJoiner)this).AppendTo(builder, list);
    }

    /// <summary>
    /// 附加到...
    /// </summary>
    /// <typeparam name="T1">键类型</typeparam>
    /// <typeparam name="T2">值类型</typeparam>
    /// <param name="builder">字符串拼接器</param>
    /// <param name="list">列表</param>
    /// <param name="keyFunc">键函数</param>
    /// <param name="valueFunc">值函数</param>
    StringBuilder ITupleJoiner.AppendTo<T1, T2>(StringBuilder builder, IEnumerable<(T1, T2)> list, Func<T1, string> keyFunc, Func<T2, string> valueFunc)
    {
        JoinToTupleString(
            builder, (c, k, v, i) => c.Append($"{(i > 0 ? _on : string.Empty)}{k}{Options.MapSeparator}{v}"),
            list, default, default, keyFunc, valueFunc, Options.GetTupleReplace<T1, T2>());
        return builder;
    }

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
    StringBuilder ITupleJoiner.AppendTo<T1, T2>(StringBuilder builder, IEnumerable<(T1, T2)> list, T1 defaultKey, T2 defaultValue, Func<T1, string> keyFunc, Func<T2, string> valueFunc)
    {
        JoinToTupleString(
            builder, (c, k, v, i) => c.Append($"{(i > 0 ? _on : string.Empty)}{k}{Options.MapSeparator}{v}"),
            list, defaultKey, defaultValue, keyFunc, valueFunc, Options.GetTupleReplace<T1, T2>());
        return builder;
    }

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
    StringBuilder ITupleJoiner.AppendTo<T1, T2>(StringBuilder builder, Func<T1, string> keyFunc, Func<T2, string> valueFunc, (T1, T2) tuple1, params (T1, T2)[] restTuples)
    {
        var list = new List<(T1, T2)> { tuple1 };
        list.AddRange(restTuples);
        return ((ITupleJoiner)this).AppendTo(builder, list, keyFunc, valueFunc);
    }

    #endregion

    /// <summary>
    /// 连接器选项配置
    /// </summary>
    private partial class JoinerOptions
    {
        #region Skip Value Nulls - Map

        /// <summary>
        /// 元组跳过 null 标志
        /// </summary>
        public bool SkipTupleValueNullsFlag { get; private set; }

        /// <summary>
        /// 元组跳过 null 的类型
        /// </summary>
        public SkipNullType SkipTupleValueNullType { get; private set; }

        /// <summary>
        /// 设置元组跳过 null
        /// </summary>
        public void SetSkipTupleValueNulls()
        {
            SkipTupleValueNullsFlag = true;
            SkipTupleValueNullType = SkipNullType.WhenBoth;
        }

        /// <summary>
        /// 设置元组跳过 null
        /// </summary>
        /// <param name="type">元组跳过 null 的类型</param>
        public void SetSkipTupleValueNulls(SkipNullType type)
        {
            SkipTupleValueNullsFlag = type != SkipNullType.Nothing;
            SkipTupleValueNullType = type;
        }

        #endregion

        #region UseForNull - Tuple

        /// <summary>
        /// 元组替代器
        /// </summary>
        private JoinerObjectReplacer TupleReplacer { get; set; }

        /// <summary>
        /// 元组替代器标志
        /// </summary>
        private bool TupleValueReplacerFlag { get; set; }

        /// <summary>
        /// 获取元组替代器
        /// </summary>
        /// <typeparam name="T1">泛型类型</typeparam>
        /// <typeparam name="T2">泛型类型</typeparam>
        public (Func<T1, T2, T1> KeyFunc, Func<T1, T2, T2> ValueFunc) GetTupleReplace<T1, T2>()
        {
            return GetTupleReplace<T1, T2, T1, T2>();
        }

        /// <summary>
        /// 获取元组替代器
        /// </summary>
        /// <typeparam name="T1">泛型类型</typeparam>
        /// <typeparam name="T2">泛型类型</typeparam>
        /// <typeparam name="T3">泛型类型</typeparam>
        /// <typeparam name="T4">泛型类型</typeparam>
        public (Func<T1, T2, T1> KeyFunc, Func<T3, T4, T4> ValueFunc) GetTupleReplace<T1, T2, T3, T4>()
        {
            var keyFunc = TupleValueReplacerFlag ? TupleReplacer?.GetTupleKey<T1, T2>() : null;
            var valueFunc = TupleValueReplacerFlag ? TupleReplacer?.GetTupleValue<T3, T4>() : null;
            return (keyFunc, valueFunc);
        }

        /// <summary>
        /// 设置元组替代器
        /// </summary>
        /// <typeparam name="T1">泛型类型</typeparam>
        /// <typeparam name="T2">泛型类型</typeparam>
        /// <typeparam name="T3">泛型类型</typeparam>
        /// <typeparam name="T4">泛型类型</typeparam>
        /// <param name="tupleKeyFunc">元组键函数</param>
        /// <param name="tupleValueFunc">元组值函数</param>
        public void SetTupleReplace<T1, T2, T3, T4>(Func<T1, T2, T1> tupleKeyFunc, Func<T3, T4, T4> tupleValueFunc)
        {
            TupleValueReplacerFlag = true;
            TupleReplacer = JoinerObjectReplacer.CreateForTuple(tupleKeyFunc, tupleValueFunc);
            SetSkipTupleValueNulls(SkipNullType.Nothing);
        }

        #endregion
    }

    /// <summary>
    /// 连接器工具
    /// </summary>
    private static partial class JoinerUtils
    {
        /// <summary>
        /// 元组跳过 null
        /// </summary>
        /// <param name="options">连接器选项配置</param>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        public static bool SkipTuple(JoinerOptions options, string key, string value)
        {
            if (options.SkipTupleValueNullsFlag)
            {
                switch (options.SkipTupleValueNullType)
                {
                    case SkipNullType.Nothing:
                        return false;

                    case SkipNullType.WhenBoth:
                        return string.IsNullOrWhiteSpace(key) && string.IsNullOrWhiteSpace(value);

                    case SkipNullType.WhenEither:
                        return string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(value);

                    case SkipNullType.WhenKeyIsNull:
                        return string.IsNullOrWhiteSpace(key);

                    case SkipNullType.WhenValueIsNull:
                        return string.IsNullOrWhiteSpace(value);
                }
            }

            return false;
        }

        /// <summary>
        /// 是否需要修正元组值
        /// </summary>
        /// <param name="options">连接器选项配置</param>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        public static bool NeedFixTupleValue(JoinerOptions options, string key, string value)
        {
            switch (options.SkipTupleValueNullType)
            {
                case SkipNullType.Nothing:
                    return string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(value);

                case SkipNullType.WhenBoth:
                    return false;

                case SkipNullType.WhenEither:
                    return false;

                case SkipNullType.WhenKeyIsNull:
                    return !string.IsNullOrWhiteSpace(key) && string.IsNullOrWhiteSpace(value);

                case SkipNullType.WhenValueIsNull:
                    return !string.IsNullOrWhiteSpace(key) && string.IsNullOrWhiteSpace(value);
            }

            return false;
        }

        /// <summary>
        /// 修正元组键
        /// </summary>
        /// <typeparam name="T1">泛型类型</typeparam>
        /// <typeparam name="T2">泛型类型</typeparam>
        /// <param name="k">键</param>
        /// <param name="key">键</param>
        /// <param name="v">值</param>
        /// <param name="value">值</param>
        /// <param name="defaultKey">默认键</param>
        /// <param name="to">转换函数</param>
        /// <param name="keyFunc">键函数</param>
        /// <param name="type">跳过 null 的类型</param>
        public static string FixTupleKeySafety<T1, T2>(T1 k, string key, T2 v, string value, T1 defaultKey, Func<T1, string> to, Func<T1, T2, T1> keyFunc, SkipNullType type)
        {
            if (!string.IsNullOrWhiteSpace(key))
                return key;
            if (type == SkipNullType.WhenEither)
                return key;
            if (type == SkipNullType.Nothing || (type == SkipNullType.WhenValueIsNull && !string.IsNullOrWhiteSpace(value)))
                return to(keyFunc == null ? defaultKey : keyFunc(k, v));
            return key;
        }

        /// <summary>
        /// 修正元组值
        /// </summary>
        /// <typeparam name="T1">泛型类型</typeparam>
        /// <typeparam name="T2">泛型类型</typeparam>
        /// <param name="v">值</param>
        /// <param name="value">值</param>
        /// <param name="k">键</param>
        /// <param name="key">键</param>
        /// <param name="defaultValue">默认值</param>
        /// <param name="to">转换函数</param>
        /// <param name="valueFunc">值函数</param>
        /// <param name="type">跳过 null 的类型</param>
        public static string FixTupleValueSafety<T1, T2>(T2 v, string value, T1 k, string key, T2 defaultValue, Func<T2, string> to, Func<T1, T2, T2> valueFunc, SkipNullType type)
        {
            if (!string.IsNullOrWhiteSpace(value))
                return value;
            if (type == SkipNullType.WhenEither)
                return value;
            if (type == SkipNullType.Nothing || (type == SkipNullType.WhenKeyIsNull && !string.IsNullOrWhiteSpace(key)))
                return to(valueFunc == null ? defaultValue : valueFunc(k, v));
            return value;
        }

        /// <summary>
        /// 获取元组条件
        /// </summary>
        /// <param name="options">连接器选项配置</param>
        public static Func<string, bool> GetTuplePredicate(JoinerOptions options)
        {
            if (options.SkipTupleValueNullsFlag)
                return s => s != options.MapSeparator;
            return null;
        }
    }
}