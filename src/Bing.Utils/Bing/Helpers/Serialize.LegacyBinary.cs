using System.Runtime.Serialization.Formatters.Binary;

namespace Bing.Helpers;

public static partial class Serialize
{
    /// <summary>
    /// 将对象序列化为历史 BinaryFormatter 二进制字节。
    /// </summary>
    /// <param name="data">仅用于受控迁移的可信历史对象。</param>
    /// <returns>历史 BinaryFormatter 字节数组。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="data"/> 为 null 时抛出。</exception>
    /// <remarks>
    /// 此 API 仅用于读取历史格式所需的受控迁移，禁止作为新数据的默认写入方案。
    /// BinaryFormatter 不安全，禁止将其用于网络输入、用户上传文件或不可信消息队列；Binder 或类型白名单不能彻底修复其安全问题。
    /// .NET 6、7、8 的最终应用可能需要自行显式启用 EnableUnsafeBinaryFormatterSerialization；.NET 9 及以后不得依赖内置实现。
    /// </remarks>
    [Obsolete("BinaryFormatter 不安全，仅用于受控历史数据迁移。新数据请使用 ToDataContractBytes 或 Json.ToBytes。")]
    public static byte[] ToLegacyBinary(object data)
    {
        if (data == null)
            throw new ArgumentNullException(nameof(data), "要序列化的数据不能为null");

        return WriteLegacyBinary(data);
    }

    /// <summary>
    /// 将历史 BinaryFormatter 字节反序列化为目标类型。
    /// </summary>
    /// <typeparam name="T">期望的目标类型。</typeparam>
    /// <param name="bytes">仅来自受控历史存储的可信 BinaryFormatter 字节。</param>
    /// <returns>反序列化后的目标类型对象。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="bytes"/> 为 null 时抛出。</exception>
    /// <exception cref="ArgumentException">当 <paramref name="bytes"/> 为空时抛出。</exception>
    /// <exception cref="InvalidCastException">当反序列化结果与 <typeparamref name="T"/> 不匹配时抛出。</exception>
    /// <remarks>
    /// 此 API 不安全，仅用于受控历史数据迁移。禁止反序列化网络输入、用户上传文件或不可信消息队列；Binder 或类型白名单不能彻底修复安全问题。
    /// 读取失败时不得自动尝试其他格式，也不得用此方法作为未知输入的格式探测器。
    /// </remarks>
    [Obsolete("BinaryFormatter 不安全，仅用于受控历史数据迁移。新数据请使用 FromDataContractBytes 或 Json.ToObject。")]
    public static T FromLegacyBinary<T>(byte[] bytes)
    {
        if (bytes == null)
            throw new ArgumentNullException(nameof(bytes), "字节数组不能为null");
        if (bytes.Length == 0)
            throw new ArgumentException("字节数组不能为空", nameof(bytes));

        var value = ReadLegacyBinary(bytes);
        if (value is T result)
            return result;

        throw new InvalidCastException($"反序列化结果无法转换为类型 {typeof(T).FullName}");
    }

    /// <summary>
    /// 将对象写入历史 BinaryFormatter 二进制文件。
    /// </summary>
    /// <param name="fileName">历史数据文件路径。</param>
    /// <param name="data">仅用于受控迁移的可信历史对象。</param>
    /// <exception cref="ArgumentException">当 <paramref name="fileName"/> 为空白时抛出。</exception>
    /// <exception cref="ArgumentNullException">当 <paramref name="data"/> 为 null 时抛出。</exception>
    /// <remarks>
    /// 此 API 仅为历史兼容保留。禁止将 BinaryFormatter 用于网络输入、用户上传文件或不可信消息队列，且 Binder 或类型白名单不能彻底修复安全问题。
    /// </remarks>
    [Obsolete("BinaryFormatter 不安全，仅用于受控历史数据迁移。新数据请使用 ToDataContractBytes 或 Json.ToBytes。")]
    public static void ToLegacyBinaryFile(string fileName, object data)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("文件名不能为null或空", nameof(fileName));
        if (data == null)
            throw new ArgumentNullException(nameof(data), "要序列化的数据不能为null");

        var directory = Path.GetDirectoryName(fileName);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        var bytes = WriteLegacyBinary(data);
        File.WriteAllBytes(fileName, bytes);
    }

    /// <summary>
    /// 从历史 BinaryFormatter 二进制文件读取目标类型对象。
    /// </summary>
    /// <typeparam name="T">期望的目标类型。</typeparam>
    /// <param name="fileName">仅来自受控历史存储的可信数据文件路径。</param>
    /// <returns>反序列化后的目标类型对象。</returns>
    /// <exception cref="ArgumentException">当 <paramref name="fileName"/> 为空白时抛出。</exception>
    /// <exception cref="FileNotFoundException">当文件不存在时抛出。</exception>
    /// <exception cref="InvalidCastException">当反序列化结果与 <typeparamref name="T"/> 不匹配时抛出。</exception>
    /// <remarks>
    /// 此 API 不安全，仅用于受控历史数据迁移。禁止读取网络下载、用户上传或不可信消息队列中的 BinaryFormatter 数据；Binder 或类型白名单不能彻底修复安全问题。
    /// </remarks>
    [Obsolete("BinaryFormatter 不安全，仅用于受控历史数据迁移。新数据请使用 FromDataContractBytes 或 Json.ToObject。")]
    public static T FromLegacyBinaryFile<T>(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("文件名不能为null或空", nameof(fileName));
        if (!File.Exists(fileName))
            throw new FileNotFoundException($"文件不存在: {fileName}");

        var value = ReadLegacyBinary(File.ReadAllBytes(fileName));
        if (value is T result)
            return result;

        throw new InvalidCastException($"反序列化结果无法转换为类型 {typeof(T).FullName}");
    }

    /// <summary>
    /// 将对象序列化为历史 BinaryFormatter 二进制字节。
    /// </summary>
    /// <param name="data">仅用于受控迁移的可信历史对象。</param>
    /// <returns>历史 BinaryFormatter 字节数组。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="data"/> 为 null 时抛出。</exception>
    /// <remarks>
    /// 此成员仅为兼容保留，请改用 <see cref="ToLegacyBinary"/>。
    /// </remarks>
    [Obsolete("BinaryFormatter 不安全，仅用于受控历史数据迁移。请改用 ToLegacyBinary。")]
    public static byte[] ToBinary(object data)
    {
        if (data == null)
            throw new ArgumentNullException(nameof(data), "要序列化的数据不能为null");
        return WriteLegacyBinary(data);
    }

    /// <summary>
    /// 将历史 BinaryFormatter 二进制字节反序列化为目标类型。
    /// </summary>
    /// <typeparam name="T">期望的目标类型。</typeparam>
    /// <param name="bytes">仅来自受控历史存储的可信 BinaryFormatter 字节。</param>
    /// <returns>反序列化后的目标类型对象。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="bytes"/> 为 null 时抛出。</exception>
    /// <exception cref="ArgumentException">当 <paramref name="bytes"/> 为空时抛出。</exception>
    /// <exception cref="InvalidCastException">当反序列化结果与 <typeparamref name="T"/> 不匹配时抛出。</exception>
    /// <remarks>
    /// 此成员仅为兼容保留，请改用 <see cref="FromLegacyBinary{T}"/>。
    /// </remarks>
    [Obsolete("BinaryFormatter 不安全，仅用于受控历史数据迁移。请改用 FromLegacyBinary。")]
    public static T FromBinary<T>(byte[] bytes)
    {
        if (bytes == null)
            throw new ArgumentNullException(nameof(bytes), "字节数组不能为null");
        if (bytes.Length == 0)
            throw new ArgumentException("字节数组不能为空", nameof(bytes));

        var value = ReadLegacyBinary(bytes);
        if (value is T result)
            return result;

        throw new InvalidCastException($"反序列化结果无法转换为类型 {typeof(T).FullName}");
    }

    /// <summary>
    /// 将对象写入历史 BinaryFormatter 二进制文件。
    /// </summary>
    /// <param name="fileName">历史数据文件路径。</param>
    /// <param name="data">仅用于受控迁移的可信历史对象。</param>
    /// <exception cref="ArgumentException">当 <paramref name="fileName"/> 为空白时抛出。</exception>
    /// <exception cref="ArgumentNullException">当 <paramref name="data"/> 为 null 时抛出。</exception>
    /// <remarks>
    /// 此成员仅为兼容保留，请改用 <see cref="ToLegacyBinaryFile"/>。
    /// </remarks>
    [Obsolete("BinaryFormatter 不安全，仅用于受控历史数据迁移。请改用 ToLegacyBinaryFile。")]
    public static void ToBinaryFile(string fileName, object data)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("文件名不能为null或空", nameof(fileName));
        if (data == null)
            throw new ArgumentNullException(nameof(data), "要序列化的数据不能为null");

        var directory = Path.GetDirectoryName(fileName);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        File.WriteAllBytes(fileName, WriteLegacyBinary(data));
    }

    /// <summary>
    /// 从历史 BinaryFormatter 二进制文件读取目标类型对象。
    /// </summary>
    /// <typeparam name="T">期望的目标类型。</typeparam>
    /// <param name="fileName">仅来自受控历史存储的可信数据文件路径。</param>
    /// <returns>反序列化后的目标类型对象。</returns>
    /// <exception cref="ArgumentException">当 <paramref name="fileName"/> 为空白时抛出。</exception>
    /// <exception cref="FileNotFoundException">当文件不存在时抛出。</exception>
    /// <exception cref="InvalidCastException">当反序列化结果与 <typeparamref name="T"/> 不匹配时抛出。</exception>
    /// <remarks>
    /// 此成员仅为兼容保留，请改用 <see cref="FromLegacyBinaryFile{T}"/>。
    /// </remarks>
    [Obsolete("BinaryFormatter 不安全，仅用于受控历史数据迁移。请改用 FromLegacyBinaryFile。")]
    public static T FromBinaryFile<T>(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("文件名不能为null或空", nameof(fileName));
        if (!File.Exists(fileName))
            throw new FileNotFoundException($"文件不存在: {fileName}");

        var value = ReadLegacyBinary(File.ReadAllBytes(fileName));
        if (value is T result)
            return result;

        throw new InvalidCastException($"反序列化结果无法转换为类型 {typeof(T).FullName}");
    }

    /// <summary>
    /// 将对象写入历史 BinaryFormatter 字节数组。
    /// </summary>
    /// <param name="data">可信的历史对象。</param>
    /// <returns>历史 BinaryFormatter 字节数组。</returns>
    private static byte[] WriteLegacyBinary(object data)
    {
        using var stream = new MemoryStream();
#pragma warning disable SYSLIB0011
        new BinaryFormatter().Serialize(stream, data);
#pragma warning restore SYSLIB0011
        return stream.ToArray();
    }

    /// <summary>
    /// 从历史 BinaryFormatter 字节数组读取对象。
    /// </summary>
    /// <param name="bytes">可信的历史 BinaryFormatter 字节数组。</param>
    /// <returns>反序列化后的对象。</returns>
    private static object ReadLegacyBinary(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes, writable: false);
#pragma warning disable SYSLIB0011
        return new BinaryFormatter().Deserialize(stream);
#pragma warning restore SYSLIB0011
    }
}