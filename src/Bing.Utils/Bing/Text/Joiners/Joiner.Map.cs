using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bing.Text.Joiners;

public partial class Joiner : IMapJoiner
{
    /// <summary>
    /// 跳过 null
    /// </summary>
    IMapJoiner IMapJoiner.SkipNulls()
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// 跳过 null
    /// </summary>
    /// <param name="type">跳过 null 的类型</param>
    IMapJoiner IMapJoiner.SkipNulls(SkipNullType type)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// 如果为 null，则使用指定的值来替代
    /// </summary>
    /// <param name="key">键</param>
    /// <param name="value">值</param>
    IMapJoiner IMapJoiner.UseForNull(string key, string value)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// 如果为 null，则使用指定的值来替代
    /// </summary>
    /// <param name="keyFunc">键函数</param>
    /// <param name="valueFunc">值函数</param>
    IMapJoiner IMapJoiner.UseForNull(Func<string, string> keyFunc, Func<string, string> valueFunc)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// 如果为 null，则使用指定的值来替代
    /// </summary>
    /// <param name="keyFunc">键函数</param>
    /// <param name="valueFunc">值函数</param>
    IMapJoiner IMapJoiner.UseForNull(Func<string, int, string> keyFunc, Func<string, int, string> valueFunc)
    {
        throw new NotImplementedException();
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
        throw new NotImplementedException();
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
        throw new NotImplementedException();
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
        throw new NotImplementedException();
    }

    /// <summary>
    /// 切换为 Tuple 模式
    /// </summary>
    ITupleJoiner IMapJoiner.FromTuple()
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// 连接
    /// </summary>
    /// <param name="list">列表</param>
    string IMapJoiner.Join(IEnumerable<string> list)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// 连接
    /// </summary>
    /// <param name="list">列表</param>
    /// <param name="defaultKey">默认键</param>
    /// <param name="defaultValue">默认值</param>
    string IMapJoiner.Join(IEnumerable<string> list, string defaultKey, string defaultValue)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// 连接
    /// </summary>
    /// <param name="str1">字符串</param>
    /// <param name="str2">字符串</param>
    /// <param name="restStrings">其余字符串</param>
    string IMapJoiner.Join(string str1, string str2, params string[] restStrings)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// 附加到...
    /// </summary>
    /// <param name="builder">字符串拼接器</param>
    /// <param name="list">列表</param>
    StringBuilder IMapJoiner.AppendTo(StringBuilder builder, IEnumerable<string> list)
    {
        throw new NotImplementedException();
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
        throw new NotImplementedException();
    }

    /// <summary>
    /// 连接
    /// </summary>
    /// <param name="builder">字符串拼接器</param>
    /// <param name="str1">字符串</param>
    /// <param name="str2">字符串</param>
    /// <param name="restStrings">其余字符串</param>
    StringBuilder IMapJoiner.Join(StringBuilder builder, string str1, string str2, params string[] restStrings)
    {
        throw new NotImplementedException();
    }

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
    }
}