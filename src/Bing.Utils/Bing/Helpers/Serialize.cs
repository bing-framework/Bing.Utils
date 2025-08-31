using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml;
using System.Xml.Serialization;

namespace Bing.Helpers;

/// <summary>
/// 序列化操作辅助类，提供多种序列化和反序列化方法。
/// </summary>
/// <remarks>
/// 此类提供了二进制、XML、JSON等多种序列化方式。
/// 注意：二进制序列化方法仅适用于值类型和简单结构体。
/// </remarks>
public static partial class Serialize
{
    #region 结构体序列化（Marshal方式）

    /// <summary>
    /// 将值类型或结构体序列化为字节数组。
    /// </summary>
    /// <typeparam name="T">值类型或结构体类型</typeparam>
    /// <param name="data">要序列化的数据</param>
    /// <returns>序列化后的字节数组</returns>
    /// <exception cref="ArgumentNullException">当数据为null时抛出</exception>
    /// <exception cref="ArgumentException">当类型不是值类型或结构体时抛出</exception>
    /// <remarks>
    /// 此方法使用Marshal进行序列化，仅适用于值类型和非托管结构体。
    /// 不需要类型标记 [Serializable] 特性。
    /// </remarks>
    public static byte[] ToBytes<T>(T data) where T : struct
    {
        if (data.Equals(default(T)))
            throw new ArgumentNullException(nameof(data), "数据不能为默认值");

        var size = Marshal.SizeOf<T>();
        var bytes = new byte[size];
        var ptr = IntPtr.Zero;

        try
        {
            ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(data, ptr, false);
            Marshal.Copy(ptr, bytes, 0, size);
            return bytes;
        }
        finally
        {
            if (ptr != IntPtr.Zero)
                Marshal.FreeHGlobal(ptr);
        }
    }

    /// <summary>
    /// 将字节数组反序列化为值类型或结构体。
    /// </summary>
    /// <typeparam name="T">值类型或结构体类型</typeparam>
    /// <param name="bytes">要反序列化的字节数组</param>
    /// <returns>反序列化后的对象</returns>
    /// <exception cref="ArgumentNullException">当字节数组为null时抛出</exception>
    /// <exception cref="ArgumentException">当字节数组长度不匹配时抛出</exception>
    /// <remarks>
    /// 此方法使用Marshal进行反序列化，仅适用于值类型和非托管结构体。
    /// </remarks>
    public static T FromBytes<T>(byte[] bytes) where T : struct
    {
        if (bytes == null)
            throw new ArgumentNullException(nameof(bytes), "字节数组不能为null");

        var size = Marshal.SizeOf<T>();
        if (bytes.Length != size)
            throw new ArgumentException($"字节数组长度 {bytes.Length} 与类型 {typeof(T).Name} 的大小 {size} 不匹配", nameof(bytes));

        var ptr = IntPtr.Zero;
        try
        {
            ptr = Marshal.AllocHGlobal(size);
            Marshal.Copy(bytes, 0, ptr, size);
            return Marshal.PtrToStructure<T>(ptr);
        }
        finally
        {
            if (ptr != IntPtr.Zero)
                Marshal.FreeHGlobal(ptr);
        }
    }

    #endregion

    #region 二进制序列化（BinaryFormatter方式）

    /// <summary>
    /// 将数据序列化为二进制数组。
    /// </summary>
    /// <param name="data">要序列化的数据</param>
    /// <returns>序列化后的字节数组</returns>
    /// <exception cref="ArgumentNullException">当数据为null时抛出</exception>
    /// <remarks>
    /// 此方法使用BinaryFormatter进行序列化，需要类型标记[Serializable]特性。
    /// 注意：BinaryFormatter在某些平台可能存在安全限制，建议优先使用JSON序列化。
    /// </remarks>
    public static byte[] ToBinary(object data)
    {
        if (data == null)
            throw new ArgumentNullException(nameof(data), "要序列化的数据不能为null");

        using var ms = new MemoryStream();
#pragma warning disable SYSLIB0011 // BinaryFormatter已过时但仍支持
        var formatter = new BinaryFormatter();
        formatter.Serialize(ms, data);
#pragma warning restore SYSLIB0011
        return ms.ToArray();
    }

    /// <summary>
    /// 将二进制数组反序列化为强类型数据。
    /// </summary>
    /// <typeparam name="T">目标类型</typeparam>
    /// <param name="bytes">二进制数组</param>
    /// <returns>反序列化后的对象</returns>
    /// <exception cref="ArgumentNullException">当字节数组为null时抛出</exception>
    /// <exception cref="ArgumentException">当字节数组为空时抛出</exception>
    /// <remarks>
    /// 此方法使用BinaryFormatter进行反序列化。
    /// 注意：仅能反序列化由ToBinary方法序列化的数据。
    /// </remarks>
    public static T FromBinary<T>(byte[] bytes)
    {
        if (bytes == null)
            throw new ArgumentNullException(nameof(bytes), "字节数组不能为null");
        if (bytes.Length == 0)
            throw new ArgumentException("字节数组不能为空", nameof(bytes));

        using var ms = new MemoryStream(bytes);
#pragma warning disable SYSLIB0011 // BinaryFormatter已过时但仍支持
        var formatter = new BinaryFormatter();
        return (T)formatter.Deserialize(ms);
#pragma warning restore SYSLIB0011
    }

    /// <summary>
    /// 将数据序列化为二进制数组并写入文件。
    /// </summary>
    /// <param name="fileName">文件路径</param>
    /// <param name="data">要序列化的数据</param>
    /// <exception cref="ArgumentException">当文件名为null或空时抛出</exception>
    /// <exception cref="ArgumentNullException">当数据为null时抛出</exception>
    /// <remarks>
    /// 此方法将对象序列化为二进制格式并保存到文件。
    /// 自动创建不存在的目录。
    /// </remarks>
    public static void ToBinaryFile(string fileName, object data)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("文件名不能为null或空", nameof(fileName));
        if (data == null)
            throw new ArgumentNullException(nameof(data), "要序列化的数据不能为null");

        // 确保目录存在
        var directory = Path.GetDirectoryName(fileName);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        using var fs = new FileStream(fileName, FileMode.Create, FileAccess.Write);
#pragma warning disable SYSLIB0011 // BinaryFormatter已过时但仍支持
        var formatter = new BinaryFormatter();
        formatter.Serialize(fs, data);
#pragma warning restore SYSLIB0011
    }

    /// <summary>
    /// 从二进制文件中反序列化对象。
    /// </summary>
    /// <typeparam name="T">目标类型</typeparam>
    /// <param name="fileName">文件路径</param>
    /// <returns>反序列化后的对象</returns>
    /// <exception cref="ArgumentException">当文件名为null或空时抛出</exception>
    /// <exception cref="FileNotFoundException">当文件不存在时抛出</exception>
    /// <remarks>
    /// 此方法从二进制文件中反序列化对象。
    /// 仅能反序列化由ToBinaryFile方法序列化的文件。
    /// </remarks>
    public static T FromBinaryFile<T>(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("文件名不能为null或空", nameof(fileName));
        if (!File.Exists(fileName))
            throw new FileNotFoundException($"文件不存在: {fileName}");

        using var fs = new FileStream(fileName, FileMode.Open, FileAccess.Read);
#pragma warning disable SYSLIB0011 // BinaryFormatter已过时但仍支持
        var formatter = new BinaryFormatter();
        return (T)formatter.Deserialize(fs);
#pragma warning restore SYSLIB0011
    }

    #endregion

    #region Xml序列化

    /// <summary>
    /// 将对象序列化为XML字符串。
    /// </summary>
    /// <param name="data">要序列化的对象</param>
    /// <param name="encoding">编码格式，默认为UTF-8</param>
    /// <returns>XML字符串</returns>
    /// <exception cref="ArgumentNullException">当数据为null时抛出</exception>
    public static string ToXml(object data, Encoding encoding = null)
    {
        if (data == null)
            throw new ArgumentNullException(nameof(data), "要序列化的数据不能为null");
        encoding ??= Encoding.UTF8;

        using var ms = new MemoryStream();
        using var writer = new XmlTextWriter(ms, encoding)
        {
            Formatting = Formatting.Indented
        };

        var serializer = new XmlSerializer(data.GetType());
        var ns = new XmlSerializerNamespaces();
        ns.Add("", ""); // 移除默认命名空间

        serializer.Serialize(writer, data, ns);
        ms.Position = 0;

        using var reader = new StreamReader(ms, encoding);
        return reader.ReadToEnd();
    }

    /// <summary>
    /// 将XML字符串反序列化为指定类型的对象。
    /// </summary>
    /// <typeparam name="T">目标类型</typeparam>
    /// <param name="xml">XML字符串</param>
    /// <param name="encoding">编码格式，默认为UTF-8</param>
    /// <returns>反序列化后的对象</returns>
    /// <exception cref="ArgumentException">当XML字符串为null或空时抛出</exception>
    public static T FromXml<T>(string xml, Encoding encoding = null)
    {
        if (string.IsNullOrWhiteSpace(xml))
            throw new ArgumentException("XML字符串不能为null或空", nameof(xml));

        encoding ??= Encoding.UTF8;
        var bytes = encoding.GetBytes(xml);

        using var ms = new MemoryStream(bytes);
        var serializer = new XmlSerializer(typeof(T));
        return (T)serializer.Deserialize(ms);
    }

    /// <summary>
    /// 将对象序列化为XML并写入文件。
    /// </summary>
    /// <param name="fileName">文件路径</param>
    /// <param name="data">要序列化的数据</param>
    /// <param name="encoding">编码格式，默认为UTF-8</param>
    /// <exception cref="ArgumentException">当文件名为null或空时抛出</exception>
    /// <exception cref="ArgumentNullException">当数据为null时抛出</exception>
    public static void ToXmlFile(string fileName, object data, Encoding encoding = null)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("文件名不能为null或空", nameof(fileName));
        if (data == null)
            throw new ArgumentNullException(nameof(data), "要序列化的数据不能为null");

        encoding ??= Encoding.UTF8;

        // 确保目录存在
        var directory = Path.GetDirectoryName(fileName);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        using var fs = new FileStream(fileName, FileMode.Create, FileAccess.Write);
        using var writer = new XmlTextWriter(fs, encoding)
        {
            Formatting = Formatting.Indented
        };

        var serializer = new XmlSerializer(data.GetType());
        var ns = new XmlSerializerNamespaces();
        ns.Add("", ""); // 移除默认命名空间

        serializer.Serialize(writer, data, ns);
    }

    /// <summary>
    /// 从XML文件中反序列化对象。
    /// </summary>
    /// <typeparam name="T">目标类型</typeparam>
    /// <param name="fileName">文件路径</param>
    /// <returns>反序列化后的对象</returns>
    /// <exception cref="ArgumentException">当文件名为null或空时抛出</exception>
    /// <exception cref="FileNotFoundException">当文件不存在时抛出</exception>
    public static T FromXmlFile<T>(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("文件名不能为null或空", nameof(fileName));
        if (!File.Exists(fileName))
            throw new FileNotFoundException($"文件不存在: {fileName}");

        using var fs = new FileStream(fileName, FileMode.Open, FileAccess.Read);
        var serializer = new XmlSerializer(typeof(T));
        return (T)serializer.Deserialize(fs);
    }

    #endregion
}