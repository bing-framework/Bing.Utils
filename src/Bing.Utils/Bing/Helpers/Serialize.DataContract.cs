using System.Collections;
using System.Runtime.Serialization;
using System.Xml;

namespace Bing.Helpers;

public static partial class Serialize
{
    /// <summary>
    /// DataContract Binary XML 读取时允许的最大对象图项目数。
    /// </summary>
    private const int MaxDataContractObjectGraphItems = 1_000_000;

    /// <summary>
    /// DataContract Binary XML 读取时允许的最大字符串内容长度。
    /// </summary>
    private const int MaxDataContractStringContentLength = 1_048_576;

    /// <summary>
    /// 将对象序列化为 DataContract Binary XML 字节数组。
    /// </summary>
    /// <typeparam name="T">对象的声明类型。</typeparam>
    /// <param name="data">要序列化的非空对象。</param>
    /// <param name="knownTypes">允许出现在对象图中的显式已知派生类型集合。</param>
    /// <returns>使用 DataContract Binary XML 编码的字节数组。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="data"/> 为 null 时抛出。</exception>
    /// <exception cref="ArgumentException">当 <paramref name="knownTypes"/> 包含非 <see cref="Type"/> 或 null 元素时抛出。</exception>
    /// <exception cref="SerializationException">当对象图无法按 DataContract 契约序列化时抛出。</exception>
    /// <remarks>
    /// 此方法保留对象引用，并且只信任调用方通过 <paramref name="knownTypes"/> 显式注册的多态类型。
    /// 对于跨平台、缓存、消息传递和可调试存储，请使用 <see cref="Json.ToBytes{T}(T,System.Text.Json.JsonSerializerOptions)"/>；
    /// DataContract Binary XML 适用于需要二进制对象图和 .NET 类型模型的场景。
    /// </remarks>
    public static byte[] ToDataContractBytes<T>(T data, IEnumerable knownTypes = null)
    {
        if (data == null)
            throw new ArgumentNullException(nameof(data), "要序列化的数据不能为null");

        var serializer = CreateDataContractSerializer<T>(knownTypes);
        using var stream = new MemoryStream();
        using var writer = XmlDictionaryWriter.CreateBinaryWriter(stream);
        serializer.WriteObject(writer, data);
        writer.Flush();
        return stream.ToArray();
    }

    /// <summary>
    /// 将 DataContract Binary XML 字节数组反序列化为对象。
    /// </summary>
    /// <typeparam name="T">期望的目标类型。</typeparam>
    /// <param name="bytes">DataContract Binary XML 字节数组。</param>
    /// <param name="knownTypes">允许出现在对象图中的显式已知派生类型集合。</param>
    /// <returns>反序列化后的目标类型对象。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="bytes"/> 为 null 时抛出。</exception>
    /// <exception cref="ArgumentException">当 <paramref name="bytes"/> 为空，或 <paramref name="knownTypes"/> 包含非法元素时抛出。</exception>
    /// <exception cref="SerializationException">当字节内容无效、超出对象图限制或结果与 <typeparamref name="T"/> 不匹配时抛出。</exception>
    /// <remarks>
    /// 读取过程使用固定的 <see cref="XmlDictionaryReaderQuotas"/>，不会根据输入内容加载类型、调用 <see cref="Type.GetType(string)"/> 或扫描程序集。
    /// </remarks>
    public static T FromDataContractBytes<T>(byte[] bytes, IEnumerable knownTypes = null)
    {
        if (bytes == null)
            throw new ArgumentNullException(nameof(bytes), "字节数组不能为null");
        if (bytes.Length == 0)
            throw new ArgumentException("字节数组不能为空", nameof(bytes));

        var serializer = CreateDataContractSerializer<T>(knownTypes);
        using var reader = XmlDictionaryReader.CreateBinaryReader(bytes, 0, bytes.Length, CreateDataContractReaderQuotas());
        var value = serializer.ReadObject(reader);
        if (value is T result)
            return result;

        throw new SerializationException($"反序列化结果无法转换为类型 {typeof(T).FullName}");
    }

    /// <summary>
    /// 创建 DataContract 序列化器。
    /// </summary>
    /// <typeparam name="T">对象的声明类型。</typeparam>
    /// <param name="knownTypes">调用方显式提供的已知类型集合。</param>
    /// <returns>配置完成的 DataContract 序列化器。</returns>
    private static DataContractSerializer CreateDataContractSerializer<T>(IEnumerable knownTypes)
    {
        var registeredKnownTypes = GetKnownTypes(knownTypes);
        return new DataContractSerializer(typeof(T), new DataContractSerializerSettings
        {
            KnownTypes = registeredKnownTypes,
            DataContractResolver = new ExplicitKnownTypeResolver(registeredKnownTypes),
            PreserveObjectReferences = true,
            MaxItemsInObjectGraph = MaxDataContractObjectGraphItems
        });
    }

    /// <summary>
    /// 验证并快照调用方提供的已知类型。
    /// </summary>
    /// <param name="knownTypes">调用方显式提供的已知类型集合。</param>
    /// <returns>经过验证的已知类型集合。</returns>
    /// <exception cref="ArgumentException">当集合包含 null 或非 <see cref="Type"/> 元素时抛出。</exception>
    private static IEnumerable<Type> GetKnownTypes(IEnumerable knownTypes)
    {
        if (knownTypes == null)
            return Array.Empty<Type>();

        var result = new HashSet<Type>();
        foreach (var knownType in knownTypes)
        {
            if (!(knownType is Type type))
                throw new ArgumentException("已知类型集合只能包含非null的 Type 实例", nameof(knownTypes));
            result.Add(type);
        }

        return result;
    }

    /// <summary>
    /// 创建受限的 Binary XML 读取配额。
    /// </summary>
    /// <returns>限制读取资源消耗的配额实例。</returns>
    private static XmlDictionaryReaderQuotas CreateDataContractReaderQuotas()
    {
        return new XmlDictionaryReaderQuotas
        {
            MaxDepth = 64,
            MaxStringContentLength = MaxDataContractStringContentLength,
            MaxArrayLength = MaxDataContractStringContentLength,
            MaxBytesPerRead = 4_096,
            MaxNameTableCharCount = 16_384
        };
    }

    /// <summary>
    /// 仅解析调用方显式注册的多态类型，避免序列化器隐式发现类型。
    /// </summary>
    private sealed class ExplicitKnownTypeResolver : DataContractResolver
    {
        private readonly ISet<Type> _knownTypes;

        /// <summary>
        /// 初始化显式已知类型解析器。
        /// </summary>
        /// <param name="knownTypes">调用方提供且已验证的已知类型集合。</param>
        public ExplicitKnownTypeResolver(IEnumerable<Type> knownTypes)
        {
            _knownTypes = new HashSet<Type>(knownTypes);
        }

        /// <summary>
        /// 将显式已知运行时类型写为默认稳定类型名。
        /// </summary>
        /// <param name="type">运行时类型。</param>
        /// <param name="declaredType">声明类型。</param>
        /// <param name="knownTypeResolver">默认类型解析器。</param>
        /// <param name="typeName">解析后的类型名称。</param>
        /// <param name="typeNamespace">解析后的类型命名空间。</param>
        /// <returns>类型已明确允许时返回 true，否则返回 false。</returns>
        public override bool TryResolveType(Type type, Type declaredType, DataContractResolver knownTypeResolver,
            out XmlDictionaryString typeName, out XmlDictionaryString typeNamespace)
        {
            if (type != declaredType && !_knownTypes.Contains(type))
            {
                typeName = null;
                typeNamespace = null;
                return false;
            }

            return knownTypeResolver.TryResolveType(type, declaredType, null, out typeName, out typeNamespace);
        }

        /// <summary>
        /// 将二进制 XML 中的类型名称解析为显式已知类型。
        /// </summary>
        /// <param name="typeName">输入类型名称。</param>
        /// <param name="typeNamespace">输入类型命名空间。</param>
        /// <param name="declaredType">声明类型。</param>
        /// <param name="knownTypeResolver">默认类型解析器。</param>
        /// <returns>已显式注册的类型；其他输入返回 null。</returns>
        public override Type ResolveName(string typeName, string typeNamespace, Type declaredType,
            DataContractResolver knownTypeResolver)
        {
            var type = knownTypeResolver.ResolveName(typeName, typeNamespace, declaredType, null);
            return type != null && _knownTypes.Contains(type) ? type : null;
        }
    }
}