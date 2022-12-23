using System.Text;

namespace Bing.Text.Joiners;

/// <summary>
/// 连接器
/// </summary>
public partial class Joiner : IJoiner
{
    /// <summary>
    /// 分隔符
    /// </summary>
    private readonly string _on;

    /// <summary>
    /// 连接器选项配置
    /// </summary>
    private JoinerOptions Options { get; set; } = new();

    /// <summary>
    /// 初始化一个<see cref="Joiner"/>类型的实例
    /// </summary>
    /// <param name="on">分隔符</param>
    private Joiner(string on) => _on = on;

    #region SkipNulls(跳过 null)
    
    /// <summary>
    /// 跳过 null
    /// </summary>
    IJoiner IJoiner.SkipNulls()
    {
        Options.SetSkipNulls();
        return this;
    }

    #endregion

    #region UseForNull(如果为 null，则使用指定的值来替代)

    /// <summary>
    /// 如果为 null，则使用指定的值来替代
    /// </summary>
    /// <param name="value">值</param>
    IJoiner IJoiner.UseForNull(string value)
    {
        Options.SetReplacer<string>(_ => value);
        return this;
    }

    /// <summary>
    /// 如果为 null，则使用指定的值来替代
    /// </summary>
    /// <param name="valueFunc">值函数</param>
    IJoiner IJoiner.UseForNull(Func<string, string> valueFunc)
    {
        Options.SetReplacer(valueFunc);
        return this;
    }

    /// <summary>
    /// 如果为 null，则使用指定的值来替代
    /// </summary>
    /// <param name="valueFunc">值函数</param>
    IJoiner IJoiner.UseForNull(Func<string, int, string> valueFunc)
    {
        Options.SetIndexedReplacer(valueFunc);
        return this;
    }

    #endregion

    #region WithKeyVaslueSeparator(设置键值对分隔符)

    /// <summary>
    /// 设置键值对分隔符
    /// </summary>
    /// <param name="separator">分隔符</param>
    IMapJoiner IJoiner.WithKeyValueSeparator(char separator)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// 设置键值对分隔符
    /// </summary>
    /// <param name="separator">分隔符</param>
    IMapJoiner IJoiner.WithKeyValueSeparator(string separator)
    {
        throw new NotImplementedException();
    }

    #endregion
    /// <summary>
    /// 连接
    /// </summary>
    /// <param name="list">列表</param>
    string IJoiner.Join(IEnumerable<string> list)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// 连接
    /// </summary>
    /// <param name="str1">字符串</param>
    /// <param name="restStrings">其余字符串</param>
    string IJoiner.Join(string str1, params string[] restStrings)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// 附加到...
    /// </summary>
    /// <param name="builder">字符串拼接器</param>
    /// <param name="list">列表</param>
    StringBuilder IJoiner.AppendTo(StringBuilder builder, IEnumerable<string> list)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// 连接
    /// </summary>
    /// <param name="builder">字符串拼接器</param>
    /// <param name="str1">字符串</param>
    /// <param name="restStrings">其余字符串</param>
    StringBuilder IJoiner.Join(StringBuilder builder, string str1, params string[] restStrings)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// 连接器选项配置
    /// </summary>
    private partial class JoinerOptions
    {
        #region Skip Nulls - List

        /// <summary>
        /// 跳过 null 标志
        /// </summary>
        public bool SkipNullsFlag { get; private set; }

        /// <summary>
        /// 设置跳过 null
        /// </summary>
        public void SetSkipNulls() => SkipNullsFlag = true;

        #endregion

        #region UseForNull

        /// <summary>
        /// 连接器对象替代器
        /// </summary>
        private JoinerObjectReplacer ObjectReplacer { get; set; }

        /// <summary>
        /// 对象替代器标志
        /// </summary>
        private bool ObjectReplacerFlag { get; set; }

        /// <summary>
        /// 对象替代器索引标志
        /// </summary>
        private bool ObjectReplacerIndexedFlag { get; set; }

        /// <summary>
        /// 获取对象替代器
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        public Func<T, T> GetReplacer<T>() => ObjectReplacerFlag ? ObjectReplacer?.GetValue<T>() : null;

        /// <summary>
        /// 获取索引对象替代器
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        public Func<T, int, T> GetIndexedReplacer<T>() => ObjectReplacerFlag && ObjectReplacerIndexedFlag ? ObjectReplacer?.GetIndexedValue<T>() : null;

        /// <summary>
        /// 设置对象替代器
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="valueFunc">值函数</param>
        public void SetReplacer<T>(Func<T, T> valueFunc)
        {
            ObjectReplacerFlag = true;
            ObjectReplacer = JoinerObjectReplacer.Create(valueFunc);
            SetSkipNulls();
        }

        /// <summary>
        /// 设置索引对象替代器
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="valueFunc">值函数</param>
        public void SetIndexedReplacer<T>(Func<T, int, T> valueFunc)
        {
            ObjectReplacerFlag = true;
            ObjectReplacerIndexedFlag = true;
            ObjectReplacer = JoinerObjectReplacer.Create(valueFunc);
            SetSkipNulls();
        }

        #endregion

        /// <summary>
        /// 连接器对象替代器
        /// </summary>
        private class JoinerObjectReplacer
        {
            /// <summary>
            /// 键函数
            /// </summary>
            private dynamic KeyFunc { get; }

            /// <summary>
            /// 值函数
            /// </summary>
            private dynamic ValueFunc { get; }

            /// <summary>
            /// 初始化一个<see cref="JoinerObjectReplacer"/>类型的实例
            /// </summary>
            /// <param name="keyFunc">键函数</param>
            /// <param name="valueFunc">值函数</param>
            private JoinerObjectReplacer(dynamic keyFunc, dynamic valueFunc)
            {
                KeyFunc = keyFunc;
                ValueFunc = valueFunc;
            }

            /// <summary>
            /// 获取值函数
            /// </summary>
            /// <typeparam name="T">泛型类型</typeparam>
            public Func<T, T> GetValue<T>() => ValueFunc as Func<T, T>;

            /// <summary>
            /// 获取映射键函数
            /// </summary>
            /// <typeparam name="T1">键类型</typeparam>
            /// <typeparam name="T2">值类型</typeparam>
            public Func<T1, T2> GetMapKey<T1, T2>() => KeyFunc as Func<T1, T2>;

            /// <summary>
            /// 获取映射值函数
            /// </summary>
            /// <typeparam name="T1">键类型</typeparam>
            /// <typeparam name="T2">值类型</typeparam>
            public Func<T1, T2> GetMapValue<T1, T2>() => ValueFunc as Func<T1, T2>;

            /// <summary>
            /// 获取元组键函数
            /// </summary>
            /// <typeparam name="T1">键类型</typeparam>
            /// <typeparam name="T2">值类型</typeparam>
            public Func<T1, T2, T1> GetTupleKey<T1, T2>() => KeyFunc as Func<T1, T2, T1>;

            /// <summary>
            /// 获取元组值函数
            /// </summary>
            /// <typeparam name="T1">键类型</typeparam>
            /// <typeparam name="T2">值类型</typeparam>
            public Func<T1, T2, T2> GetTupleValue<T1, T2>() => ValueFunc as Func<T1, T2, T2>;

            /// <summary>
            /// 获取索引值函数
            /// </summary>
            /// <typeparam name="T">泛型类型</typeparam>
            public Func<T, int, T> GetIndexedValue<T>() => ValueFunc as Func<T, int, T>;

            /// <summary>
            /// 获取索引映射键函数
            /// </summary>
            /// <typeparam name="T1">键类型</typeparam>
            /// <typeparam name="T2">值类型</typeparam>
            public Func<T1, int, T2> GetIndexedMapKey<T1, T2>() => KeyFunc as Func<T1, int, T2>;

            /// <summary>
            /// 获取索引映射值函数
            /// </summary>
            /// <typeparam name="T1">键类型</typeparam>
            /// <typeparam name="T2">值类型</typeparam>
            public Func<T1, int, T2> GetIndexedMapValue<T1, T2>() => ValueFunc as Func<T1, int, T2>;

            /// <summary>
            /// 创建一个<see cref="JoinerObjectReplacer"/>类型的实例
            /// </summary>
            /// <typeparam name="T">泛型类型</typeparam>
            /// <param name="valueFunc">值函数</param>
            public static JoinerObjectReplacer Create<T>(Func<T, T> valueFunc) => new(null, valueFunc);

            /// <summary>
            /// 创建一个<see cref="JoinerObjectReplacer"/>类型的实例
            /// </summary>
            /// <typeparam name="T">泛型类型</typeparam>
            /// <param name="valueFunc">值函数</param>
            public static JoinerObjectReplacer Create<T>(Func<T, int, T> valueFunc) => new(null, valueFunc);

            /// <summary>
            /// 创建一个<see cref="JoinerObjectReplacer"/>类型的实例
            /// </summary>
            /// <typeparam name="T1">泛型类型</typeparam>
            /// <typeparam name="T2">泛型类型</typeparam>
            /// <typeparam name="T3">泛型类型</typeparam>
            /// <typeparam name="T4">泛型类型</typeparam>
            /// <param name="keyFunc">键函数</param>
            /// <param name="valueFunc">值函数</param>
            public static JoinerObjectReplacer CreateForMap<T1, T2, T3, T4>(Func<T1, T2> keyFunc, Func<T3, T4> valueFunc) => new(keyFunc, valueFunc);

            /// <summary>
            /// 创建一个<see cref="JoinerObjectReplacer"/>类型的实例
            /// </summary>
            /// <typeparam name="T1">泛型类型</typeparam>
            /// <typeparam name="T2">泛型类型</typeparam>
            /// <typeparam name="T3">泛型类型</typeparam>
            /// <typeparam name="T4">泛型类型</typeparam>
            /// <param name="keyFunc">键函数</param>
            /// <param name="valueFunc">值函数</param>
            public static JoinerObjectReplacer CreateForMap<T1, T2, T3, T4>(Func<T1, int, T2> keyFunc, Func<T3, int, T4> valueFunc) => new(keyFunc, valueFunc);

            /// <summary>
            /// 创建一个<see cref="JoinerObjectReplacer"/>类型的实例
            /// </summary>
            /// <typeparam name="T1">泛型类型</typeparam>
            /// <typeparam name="T2">泛型类型</typeparam>
            /// <typeparam name="T3">泛型类型</typeparam>
            /// <typeparam name="T4">泛型类型</typeparam>
            /// <param name="keyFunc">键函数</param>
            /// <param name="valueFunc">值函数</param>
            public static JoinerObjectReplacer CreateForTuple<T1, T2, T3, T4>(Func<T1, T2, T1> keyFunc, Func<T3, T4, T4> valueFunc) => new(keyFunc, valueFunc);
        }
    }

    /// <summary>
    /// 连接器工具
    /// </summary>
    private static partial class JoinerUtils
    {
        /// <summary>
        /// 获取字符串断言
        /// </summary>
        /// <param name="options">连接器选项配置</param>
        public static Func<string, bool> GetStringPredicate(JoinerOptions options)
        {
            if (options.SkipNullsFlag)
                return s => !string.IsNullOrWhiteSpace(s);
            return null;
        }

        /// <summary>
        /// 获取对象断言
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="options">连接器选项配置</param>
        public static Func<T, bool> GetObjectPredicate<T>(JoinerOptions options)
        {
            if (options.SkipNullsFlag)
                return t => t is not null;
            return null;
        }

        /// <summary>
        /// 获取索引对象断言
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="options">连接器选项配置</param>
        public static Func<T, int, bool> GetIndexedObjectPredicate<T>(JoinerOptions options)
        {
            if (options.SkipNullsFlag)
                return (t, _) => t is not null;
            return null;
        }
    }
}