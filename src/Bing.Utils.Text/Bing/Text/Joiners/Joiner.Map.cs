using Bing.Collections;

namespace Bing.Text.Joiners;

/// <summary>
/// 连接器
/// </summary>
public partial class Joiner : IMapJoiner
{
    #region SkipValueNulls

    /// <summary>
    /// 跳过 null
    /// </summary>
    IMapJoiner IMapJoiner.SkipNulls()
    {
        Options.SetSkipValueNulls();
        return this;
    }

    /// <summary>
    /// 跳过 null
    /// </summary>
    /// <param name="type">跳过 null 的类型</param>
    IMapJoiner IMapJoiner.SkipNulls(SkipNullType type)
    {
        Options.SetSkipValueNulls(type);
        return this;
    }

    #endregion

    #region UseForNull

    /// <summary>
    /// 如果为 null，则使用指定的值来替代
    /// </summary>
    /// <param name="key">键</param>
    /// <param name="value">值</param>
    IMapJoiner IMapJoiner.UseForNull(string key, string value)
    {
        Options.SetMapReplace<string, string, string, string>(_ => key, _ => value);
        return this;
    }

    /// <summary>
    /// 如果为 null，则使用指定的值来替代
    /// </summary>
    /// <param name="keyFunc">键函数</param>
    /// <param name="valueFunc">值函数</param>
    IMapJoiner IMapJoiner.UseForNull(Func<string, string> keyFunc, Func<string, string> valueFunc)
    {
        Options.SetMapReplace(keyFunc, valueFunc);
        return this;
    }

    /// <summary>
    /// 如果为 null，则使用指定的值来替代
    /// </summary>
    /// <param name="keyFunc">键函数</param>
    /// <param name="valueFunc">值函数</param>
    IMapJoiner IMapJoiner.UseForNull(Func<string, int, string> keyFunc, Func<string, int, string> valueFunc)
    {
        Options.SetIndexedMapReplace(keyFunc, valueFunc);
        return this;
    }

    /// <summary>
    /// 如果为 null，则使用指定的值来替代
    /// </summary>
    /// <typeparam name="T1">键类型</typeparam>
    /// <typeparam name="T2">值类型</typeparam>
    /// <param name="key">键</param>
    /// <param name="value">值</param>
    IMapJoiner IMapJoiner.UseForNull<T1, T2>(T1 key, T2 value)
    {
        Options.SetMapReplace<T1, T1, T2, T2>(_ => key, _ => value);
        return this;
    }

    /// <summary>
    /// 如果为 null，则使用指定的值来替代
    /// </summary>
    /// <typeparam name="T1">键类型</typeparam>
    /// <typeparam name="T2">值类型</typeparam>
    /// <param name="keyFunc">键函数</param>
    /// <param name="valueFunc">值函数</param>
    IMapJoiner IMapJoiner.UseForNull<T1, T2>(Func<T1, T1> keyFunc, Func<T2, T2> valueFunc)
    {
        Options.SetMapReplace(keyFunc, valueFunc);
        return this;
    }

    /// <summary>
    /// 如果为 null，则使用指定的值来替代
    /// </summary>
    /// <typeparam name="T1">键类型</typeparam>
    /// <typeparam name="T2">值类型</typeparam>
    /// <param name="keyFunc">键函数</param>
    /// <param name="valueFunc">值函数</param>
    IMapJoiner IMapJoiner.UseForNull<T1, T2>(Func<T1, int, T1> keyFunc, Func<T2, int, T2> valueFunc)
    {
        Options.SetIndexedMapReplace(keyFunc, valueFunc);
        return this;
    }

    #endregion

    #region FromTuple

    /// <summary>
    /// 切换为 Tuple 模式
    /// </summary>
    ITupleJoiner IMapJoiner.FromTuple() => this;

    #endregion

    #region Join - KeyValuePair

    /// <summary>
    /// 连接
    /// </summary>
    /// <param name="list">列表</param>
    string IMapJoiner.Join(IEnumerable<string> list)
    {
        var replacer = Options.GetMapReplace<string, string, string, string>();
        var defaultKey = replacer.KeyFunc?.Invoke(string.Empty) ?? string.Empty;
        var defaultValue = replacer.ValueFunc?.Invoke(string.Empty) ?? string.Empty;
        var middle = new List<string>();
        JoinToKeyValuePairString(
            middle, (c, k, v, _) => c.Add($"{k}{Options.MapSeparator}{v}"),
            list, defaultKey, defaultValue, s => s, v => v, replacer);
        return middle.JoinToString(_on, JoinerUtils.GetMapPredicate(Options));
    }

    /// <summary>
    /// 连接
    /// </summary>
    /// <param name="list">列表</param>
    /// <param name="defaultKey">默认键</param>
    /// <param name="defaultValue">默认值</param>
    string IMapJoiner.Join(IEnumerable<string> list, string defaultKey, string defaultValue)
    {
        var replacer = Options.GetMapReplace<string, string, string, string>();
        var middle = new List<string>();
        JoinToKeyValuePairString(
            middle, (c, k, v, _) => c.Add($"{k}{Options.MapSeparator}{v}"),
            list, defaultKey, defaultValue, s => s, v => v, replacer);
        return middle.JoinToString(_on, JoinerUtils.GetMapPredicate(Options));
    }

    /// <summary>
    /// 连接
    /// </summary>
    /// <param name="str1">字符串</param>
    /// <param name="str2">字符串</param>
    /// <param name="restStrings">其余字符串</param>
    string IMapJoiner.Join(string str1, string str2, params string[] restStrings)
    {
        var list = new List<string> { str1, str2 };
        list.AddRange(restStrings);
        return ((IMapJoiner)this).Join(list);
    }

    /// <summary>
    /// 连接键值对字符串
    /// </summary>
    /// <typeparam name="T">泛型类型</typeparam>
    /// <typeparam name="TContainer">容器类型</typeparam>
    /// <param name="container">容器</param>
    /// <param name="containerUpdateFunc">容器更新函数</param>
    /// <param name="list">列表</param>
    /// <param name="defaultKey">默认键</param>
    /// <param name="defaultValue">默认值</param>
    /// <param name="keyFunc">键函数</param>
    /// <param name="valueFunc">值函数</param>
    /// <param name="replacer">替换器</param>
    private void JoinToKeyValuePairString<T, TContainer>(
        TContainer container, Action<TContainer, string, string, int> containerUpdateFunc,
        IEnumerable<T> list, T defaultKey, T defaultValue, Func<T, string> keyFunc, Func<T, string> valueFunc,
        (Func<T, T> KeyFunc, Func<T, T> ValueFunc) replacer)
    {
        if (list == null)
            return;

        var instances = list.ToList();
        if (!instances.Any())
            return;

        if (instances.Count % 2 == 1)
            instances.Add(defaultValue);

        var timesToLoops = instances.Count / 2;
        var index = 0;
        for (var i = 0; i < timesToLoops; i++)
        {
            var k = instances[index++];
            var v = instances[index++];
            var key = keyFunc(k);
            var value = valueFunc(v);

            if (JoinerUtils.SkipMap(Options, key, value))
                continue;
            if (JoinerUtils.NeedFixMapValue(Options, key, value))
            {
                key = JoinerUtils.FixMapKeySafety(k, key, value, defaultKey, keyFunc, replacer.KeyFunc, Options.SkipValueNullType);
                value = JoinerUtils.FixMapValueSafety(v, value, key, defaultValue, valueFunc, replacer.ValueFunc, Options.SkipValueNullType);
            }
            containerUpdateFunc(container, key, value, i);
        }
    }

    /// <summary>
    /// 连接键值对字符串
    /// </summary>
    /// <typeparam name="T">泛型类型</typeparam>
    /// <typeparam name="TContainer">容器类型</typeparam>
    /// <param name="container">容器</param>
    /// <param name="containerUpdateFunc">容器更新函数</param>
    /// <param name="list">列表</param>
    /// <param name="defaultKeyFunc">默认键函数</param>
    /// <param name="defaultValueFunc">默认值函数</param>
    /// <param name="keyFunc">键函数</param>
    /// <param name="valueFunc">值函数</param>
    /// <param name="replacer">替换器</param>
    private void JoinToKeyValuePairString<T, TContainer>(
        TContainer container, Action<TContainer, string, string, int> containerUpdateFunc,
        IEnumerable<T> list, Func<int, T> defaultKeyFunc, Func<int, T> defaultValueFunc, Func<T, int, string> keyFunc, Func<T, int, string> valueFunc,
        (Func<T, int, T> KeyFunc, Func<T, int, T> ValueFunc) replacer)
    {
        if (list == null)
            return;

        var instances = list.ToList();
        if (!instances.Any())
            return;

        if (instances.Count % 2 == 1)
            instances.Add(defaultValueFunc is null ? default : defaultKeyFunc.Invoke(instances.Count));

        var timesToLoops = instances.Count / 2;
        var index = 0;
        for (var i = 0; i < timesToLoops; i++)
        {
            var indexK = index++;
            var indexV = index++;

            var k = instances[indexK];
            var v = instances[indexV];
            var key = keyFunc(k, indexK);
            var value = valueFunc(v, indexV);

            if (JoinerUtils.SkipMap(Options, key, value))
                continue;
            if (JoinerUtils.NeedFixMapValue(Options, key, value))
            {
                var defaultKey = defaultKeyFunc is null ? default : defaultKeyFunc.Invoke(indexK);
                var defaultValue = defaultValueFunc is null ? default : defaultValueFunc.Invoke(indexV);
                key = JoinerUtils.FixMapKeySafety(k, key, value, indexK, defaultKey, keyFunc, replacer.KeyFunc, Options.SkipValueNullType);
                value = JoinerUtils.FixMapValueSafety(v, value, key, indexV, defaultValue, valueFunc, replacer.ValueFunc, Options.SkipValueNullType);
            }
            containerUpdateFunc(container, key, value, i);
        }
    }

    #endregion

    #region AppendTo

    /// <summary>
    /// 附加到...
    /// </summary>
    /// <param name="builder">字符串拼接器</param>
    /// <param name="list">列表</param>
    StringBuilder IMapJoiner.AppendTo(StringBuilder builder, IEnumerable<string> list)
    {
        var replacer = Options.GetMapReplace<string, string, string, string>();
        var defaultKey = replacer.KeyFunc?.Invoke(string.Empty) ?? string.Empty;
        var defaultValue = replacer.ValueFunc?.Invoke(string.Empty) ?? string.Empty;
        JoinToKeyValuePairString(
            builder, (c, k, v, i) => c.Append($"{(i > 0 ? _on : string.Empty)}{k}{Options.MapSeparator}{v}"),
            list, defaultKey, defaultValue, s => s, v => v, replacer);
        return builder;
    }

    /// <summary>
    /// 附加到...
    /// </summary>
    /// <param name="builder">字符串拼接器</param>
    /// <param name="list">列表</param>
    /// <param name="defaultKey">默认键</param>
    /// <param name="defaultValue">默认值</param>
    StringBuilder IMapJoiner.AppendTo(StringBuilder builder, IEnumerable<string> list, string defaultKey, string defaultValue)
    {
        var replacer = Options.GetMapReplace<string, string, string, string>();
        JoinToKeyValuePairString(
            builder, (c, k, v, i) => c.Append($"{(i > 0 ? _on : string.Empty)}{k}{Options.MapSeparator}{v}"),
            list, defaultKey, defaultValue, s => s, v => v, replacer);
        return builder;
    }

    /// <summary>
    /// 附加到...
    /// </summary>
    /// <param name="builder">字符串拼接器</param>
    /// <param name="str1">字符串</param>
    /// <param name="str2">字符串</param>
    /// <param name="restStrings">其余字符串</param>
    StringBuilder IMapJoiner.AppendTo(StringBuilder builder, string str1, string str2, params string[] restStrings)
    {
        var list = new List<string> { str1, str2 };
        list.AddRange(restStrings);
        return ((IMapJoiner)this).AppendTo(builder, list);
    }

    #endregion

    /// <summary>
    /// 连接器选项配置
    /// </summary>
    private partial class JoinerOptions
    {
        #region Skip Value Nulls - Map

        /// <summary>
        /// 值跳过 null 标志
        /// </summary>
        public bool SkipValueNullsFlag { get; private set; }

        /// <summary>
        /// 值跳过 null 的类型
        /// </summary>
        public SkipNullType SkipValueNullType { get; private set; } = SkipNullType.Nothing;

        /// <summary>
        /// 设置值跳过 null
        /// </summary>
        public void SetSkipValueNulls()
        {
            SkipValueNullsFlag = true;
            SkipValueNullType = SkipNullType.WhenBoth;
        }

        /// <summary>
        /// 设置值跳过 null
        /// </summary>
        /// <param name="type">值跳过 null 的类型</param>
        public void SetSkipValueNulls(SkipNullType type)
        {
            SkipValueNullsFlag = type != SkipNullType.Nothing;
            SkipValueNullType = type;
        }

        #endregion

        #region UseForNull - Map

        /// <summary>
        /// 映射替代器
        /// </summary>
        private JoinerObjectReplacer MapReplacer { get; set; }

        /// <summary>
        /// 映射值替代器标志
        /// </summary>
        private bool MapValueReplacerFlag { get; set; }

        /// <summary>
        /// 映射值替代器索引标志
        /// </summary>
        private bool MapValueReplacerIndexedFlag { get; set; }

        /// <summary>
        /// 获取映射替代器
        /// </summary>
        /// <typeparam name="T1">泛型类型</typeparam>
        /// <typeparam name="T2">泛型类型</typeparam>
        /// <typeparam name="T3">泛型类型</typeparam>
        /// <typeparam name="T4">泛型类型</typeparam>
        public (Func<T1, T2> KeyFunc, Func<T3, T4> ValueFunc) GetMapReplace<T1, T2, T3, T4>()
        {
            var keyFunc = MapValueReplacerFlag ? MapReplacer?.GetMapKey<T1, T2>() : null;
            var valueFunc = MapValueReplacerFlag ? MapReplacer?.GetMapValue<T3, T4>() : null;
            return (keyFunc, valueFunc);
        }

        /// <summary>
        /// 获取索引映射替代器
        /// </summary>
        /// <typeparam name="T1">泛型类型</typeparam>
        /// <typeparam name="T2">泛型类型</typeparam>
        /// <typeparam name="T3">泛型类型</typeparam>
        /// <typeparam name="T4">泛型类型</typeparam>
        public (Func<T1, int, T2> KeyFunc, Func<T3, int, T4> ValueFunc) GetIndexedMapReplace<T1, T2, T3, T4>()
        {
            var keyFunc = MapValueReplacerFlag && MapValueReplacerIndexedFlag ? MapReplacer?.GetIndexedMapKey<T1, T2>() : null;
            var valueFunc = MapValueReplacerFlag && MapValueReplacerIndexedFlag ? MapReplacer?.GetIndexedMapKey<T3, T4>() : null;
            return (keyFunc, valueFunc);
        }

        /// <summary>
        /// 设置映射替代器
        /// </summary>
        /// <typeparam name="T1">泛型类型</typeparam>
        /// <typeparam name="T2">泛型类型</typeparam>
        /// <typeparam name="T3">泛型类型</typeparam>
        /// <typeparam name="T4">泛型类型</typeparam>
        /// <param name="mapKeyFunc">映射键函数</param>
        /// <param name="mapValueFunc">映射值函数</param>
        public void SetMapReplace<T1, T2, T3, T4>(Func<T1, T2> mapKeyFunc, Func<T3, T4> mapValueFunc)
        {
            MapValueReplacerFlag = true;
            MapReplacer = JoinerObjectReplacer.CreateForMap(mapKeyFunc, mapValueFunc);
            SetSkipValueNulls(SkipNullType.Nothing);
        }

        /// <summary>
        /// 设置索引映射替代器
        /// </summary>
        /// <typeparam name="T1">泛型类型</typeparam>
        /// <typeparam name="T2">泛型类型</typeparam>
        /// <typeparam name="T3">泛型类型</typeparam>
        /// <typeparam name="T4">泛型类型</typeparam>
        /// <param name="mapKeyFunc">映射键函数</param>
        /// <param name="mapValueFunc">映射值函数</param>
        public void SetIndexedMapReplace<T1, T2, T3, T4>(Func<T1, int, T2> mapKeyFunc, Func<T3, int, T4> mapValueFunc)
        {
            MapValueReplacerFlag = true;
            MapValueReplacerIndexedFlag = true;
            MapReplacer = JoinerObjectReplacer.CreateForMap(mapKeyFunc, mapValueFunc);
            SetSkipValueNulls(SkipNullType.Nothing);
        }

        #endregion

        #region WithKeyValueSeparator(设置键值对分隔符)

        /// <summary>
        /// 映射分隔符
        /// </summary>
        public string MapSeparator { get; private set; }

        /// <summary>
        /// 设置键值对分隔符
        /// </summary>
        /// <param name="separator">分隔符</param>
        public void SetMapSeparator(string separator) => MapSeparator = separator;

        /// <summary>
        /// 设置键值对分隔符
        /// </summary>
        /// <param name="separator">分隔符</param>
        public void SetMapSeparator(char separator) => MapSeparator = $"{separator}";

        #endregion
    }

    /// <summary>
    /// 连接器工具
    /// </summary>
    private static partial class JoinerUtils
    {
        /// <summary>
        /// 映射跳过 null
        /// </summary>
        /// <param name="options">连接器选项配置</param>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        public static bool SkipMap(JoinerOptions options, string key, string value)
        {
            if (options.SkipValueNullsFlag)
            {
                switch (options.SkipValueNullType)
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
        /// 是否需要修正映射值
        /// </summary>
        /// <param name="options">连接器选项配置</param>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        public static bool NeedFixMapValue(JoinerOptions options, string key, string value)
        {
            switch (options.SkipValueNullType)
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
        /// 修正映射键
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="k">键</param>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        /// <param name="defaultKey">默认键</param>
        /// <param name="to">转换函数</param>
        /// <param name="keyFunc">键函数</param>
        /// <param name="type">跳过 null 的类型</param>
        public static string FixMapKeySafety<T>(T k, string key, string value, T defaultKey, Func<T, string> to, Func<T, T> keyFunc, SkipNullType type)
        {
            if (!string.IsNullOrWhiteSpace(key))
                return key;
            if (type == SkipNullType.WhenEither)
                return key;
            if (type == SkipNullType.Nothing || (type == SkipNullType.WhenValueIsNull && !string.IsNullOrWhiteSpace(value)))
                return to(keyFunc == null ? defaultKey : keyFunc(k));
            return key;
        }

        /// <summary>
        /// 修正映射键
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="k">键</param>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        /// <param name="index">索引</param>
        /// <param name="defaultKey">默认键</param>
        /// <param name="to">转换函数</param>
        /// <param name="keyFunc">键函数</param>
        /// <param name="type">跳过 null 的类型</param>
        public static string FixMapKeySafety<T>(T k, string key, string value, int index, T defaultKey, Func<T, int, string> to, Func<T, int, T> keyFunc, SkipNullType type)
        {
            if (!string.IsNullOrWhiteSpace(key))
                return key;
            if (type == SkipNullType.WhenEither)
                return key;
            if (type == SkipNullType.Nothing || (type == SkipNullType.WhenValueIsNull && !string.IsNullOrWhiteSpace(value)))
                return to(keyFunc == null ? defaultKey : keyFunc(k, index), index);
            return key;
        }

        /// <summary>
        /// 修正映射值
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="v">值</param>
        /// <param name="value">值</param>
        /// <param name="key">键</param>
        /// <param name="defaultValue">默认值</param>
        /// <param name="to">转换函数</param>
        /// <param name="valueFunc">值函数</param>
        /// <param name="type">跳过 null 的类型</param>
        public static string FixMapValueSafety<T>(T v, string value, string key, T defaultValue, Func<T, string> to, Func<T, T> valueFunc, SkipNullType type)
        {
            if (!string.IsNullOrWhiteSpace(value))
                return value;
            if (type == SkipNullType.WhenEither)
                return value;
            if (type == SkipNullType.Nothing || (type == SkipNullType.WhenKeyIsNull && !string.IsNullOrWhiteSpace(key)))
                return to(valueFunc == null ? defaultValue : valueFunc(v));
            return value;
        }

        /// <summary>
        /// 修正映射值
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="v">值</param>
        /// <param name="value">值</param>
        /// <param name="key">键</param>
        /// <param name="index">索引</param>
        /// <param name="defaultValue">默认值</param>
        /// <param name="to">转换函数</param>
        /// <param name="valueFunc">值函数</param>
        /// <param name="type">跳过 null 的类型</param>
        public static string FixMapValueSafety<T>(T v, string value, string key, int index, T defaultValue, Func<T, int, string> to, Func<T, int, T> valueFunc, SkipNullType type)
        {
            if (!string.IsNullOrWhiteSpace(value))
                return value;
            if (type == SkipNullType.WhenEither)
                return value;
            if (type == SkipNullType.Nothing || (type == SkipNullType.WhenKeyIsNull && !string.IsNullOrWhiteSpace(key)))
                return to(valueFunc == null ? defaultValue : valueFunc(v, index), index);
            return value;
        }

        /// <summary>
        /// 获取映射条件
        /// </summary>
        /// <param name="options">连接器选项配置</param>
        public static Func<string, bool> GetMapPredicate(JoinerOptions options)
        {
            if (options.SkipValueNullsFlag)
                return s => s != options.MapSeparator;
            return null;
        }
    }
}