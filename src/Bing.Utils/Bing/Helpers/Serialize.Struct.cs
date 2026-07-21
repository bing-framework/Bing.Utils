using System.Reflection;
using System.Runtime.InteropServices;

namespace Bing.Helpers;

public static partial class Serialize
{
    /// <summary>
    /// 将不包含托管引用的结构体转换为其内存布局字节数组。
    /// </summary>
    /// <typeparam name="T">不包含托管引用的结构体类型。</typeparam>
    /// <param name="data">要转换的结构体值。</param>
    /// <returns>结构体当前内存布局对应的字节数组。</returns>
    /// <exception cref="ArgumentException">当 <typeparamref name="T"/> 包含托管引用字段时抛出。</exception>
    /// <remarks>
    /// 默认值是有效输入。此方法不是普通对象序列化协议，字节布局受运行时、平台和版本影响，不应用于跨进程或长期存储。
    /// </remarks>
    public static byte[] StructToBytes<T>(T data) where T : struct
    {
        EnsureStructWithoutManagedReferences<T>();
#if NET6_0_OR_GREATER
        var size = GetStructSize<T>();
        var bytes = new byte[size];
#if NET8_0_OR_GREATER
        MemoryMarshal.Write(bytes.AsSpan(), in data);
#else
        MemoryMarshal.Write(bytes.AsSpan(), ref data);
#endif
        return bytes;
#else
        if (typeof(T) == typeof(DateTime))
            return BitConverter.GetBytes(((DateTime)(object)data).ToBinary());

        var size = Marshal.SizeOf<T>();
        var bytes = new byte[size];
        var pointer = IntPtr.Zero;
        try
        {
            pointer = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(data, pointer, false);
            Marshal.Copy(pointer, bytes, 0, size);
            return bytes;
        }
        finally
        {
            if (pointer != IntPtr.Zero)
                Marshal.FreeHGlobal(pointer);
        }
#endif
    }

    /// <summary>
    /// 将结构体内存布局字节数组转换为不包含托管引用的结构体。
    /// </summary>
    /// <typeparam name="T">不包含托管引用的结构体类型。</typeparam>
    /// <param name="bytes">与目标结构体内存布局完全一致的字节数组。</param>
    /// <returns>转换后的结构体值。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="bytes"/> 为 null 时抛出。</exception>
    /// <exception cref="ArgumentException">当字节数组为空、长度不匹配或 <typeparamref name="T"/> 包含托管引用字段时抛出。</exception>
    /// <remarks>
    /// 此方法只适合由 <see cref="StructToBytes{T}"/> 产生的同运行时布局字节。对于跨平台、缓存、消息传递和可调试存储，请使用
    /// <see cref="Json.ToBytes{T}(T,System.Text.Json.JsonSerializerOptions)"/> 与 <see cref="Json.ToObject{T}(byte[],System.Text.Json.JsonSerializerOptions)"/>。
    /// </remarks>
    public static T BytesToStruct<T>(byte[] bytes) where T : struct
    {
        if (bytes == null)
            throw new ArgumentNullException(nameof(bytes), "字节数组不能为null");

        EnsureStructWithoutManagedReferences<T>();
#if !NET6_0_OR_GREATER
        if (typeof(T) == typeof(DateTime))
        {
            if (bytes.Length != sizeof(long))
                throw new ArgumentException($"字节数组长度 {bytes.Length} 与类型 {typeof(T).Name} 的大小 {sizeof(long)} 不匹配", nameof(bytes));

            return (T)(object)DateTime.FromBinary(BitConverter.ToInt64(bytes, 0));
        }
#endif
        var size = GetStructSize<T>();
        if (bytes.Length != size)
            throw new ArgumentException($"字节数组长度 {bytes.Length} 与类型 {typeof(T).Name} 的大小 {size} 不匹配", nameof(bytes));

#if NET6_0_OR_GREATER
        return MemoryMarshal.Read<T>(bytes.AsSpan());
#else
        var pointer = IntPtr.Zero;
        try
        {
            pointer = Marshal.AllocHGlobal(size);
            Marshal.Copy(bytes, 0, pointer, size);
            return Marshal.PtrToStructure<T>(pointer);
        }
        finally
        {
            if (pointer != IntPtr.Zero)
                Marshal.FreeHGlobal(pointer);
        }
#endif
    }

    /// <summary>
    /// 将结构体转换为字节数组。
    /// </summary>
    /// <typeparam name="T">结构体类型。</typeparam>
    /// <param name="data">要转换的结构体值。</param>
    /// <returns>结构体内存布局字节数组。</returns>
    /// <exception cref="ArgumentException">当 <typeparamref name="T"/> 包含托管引用字段时抛出。</exception>
    /// <remarks>
    /// 此成员仅为兼容保留，请改用 <see cref="StructToBytes{T}"/>。
    /// </remarks>
    [Obsolete("仅支持不包含托管引用的结构体，请改用 StructToBytes。")]
    public static byte[] ToBytes<T>(T data) where T : struct => StructToBytes(data);

    /// <summary>
    /// 将字节数组转换为结构体。
    /// </summary>
    /// <typeparam name="T">结构体类型。</typeparam>
    /// <param name="bytes">结构体内存布局字节数组。</param>
    /// <returns>转换后的结构体值。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="bytes"/> 为 null 时抛出。</exception>
    /// <exception cref="ArgumentException">当字节长度无效或 <typeparamref name="T"/> 包含托管引用字段时抛出。</exception>
    /// <remarks>
    /// 此成员仅为兼容保留，请改用 <see cref="BytesToStruct{T}"/>。
    /// </remarks>
    [Obsolete("仅支持不包含托管引用的结构体，请改用 BytesToStruct。")]
    public static T FromBytes<T>(byte[] bytes) where T : struct => BytesToStruct<T>(bytes);

    /// <summary>
    /// 获取结构体在当前运行时中的内存布局大小。
    /// </summary>
    /// <typeparam name="T">结构体类型。</typeparam>
    /// <returns>结构体内存布局的字节数。</returns>
    private static int GetStructSize<T>() where T : struct
    {
#if NET6_0_OR_GREATER
        var value = default(T);
        return MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref value, 1)).Length;
#else
    return Marshal.SizeOf<T>();
#endif
    }

    /// <summary>
    /// 验证结构体及其嵌套值类型字段不包含托管引用。
    /// </summary>
    /// <typeparam name="T">待验证的结构体类型。</typeparam>
    /// <exception cref="ArgumentException">当结构体包含托管引用字段时抛出。</exception>
    private static void EnsureStructWithoutManagedReferences<T>() where T : struct
    {
        if (StructTypeInfo<T>.ContainsManagedReferences)
            throw new ArgumentException($"类型 {typeof(T).FullName} 包含托管引用，不能进行结构体内存布局转换", nameof(T));
    }

    /// <summary>
    /// 递归判断值类型是否包含托管引用。
    /// </summary>
    /// <param name="type">要检查的值类型。</param>
    /// <param name="visited">已访问类型集合，用于避免重复检查。</param>
    /// <returns>包含托管引用时返回 true，否则返回 false。</returns>
    private static bool ContainsManagedReferences(Type type, ISet<Type> visited)
    {
        if (type.IsPointer || type.IsPrimitive || type.IsEnum)
            return false;
        if (!type.IsValueType)
            return true;
        if (!visited.Add(type))
            return false;

        foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            if (ContainsManagedReferences(field.FieldType, visited))
                return true;
        }

        return false;
    }

    /// <summary>
    /// 缓存特定结构体类型的托管引用检查结果。
    /// </summary>
    /// <typeparam name="T">结构体类型。</typeparam>
    private static class StructTypeInfo<T> where T : struct
    {
        /// <summary>
        /// 指示结构体是否包含托管引用。
        /// </summary>
        internal static readonly bool ContainsManagedReferences = Serialize.ContainsManagedReferences(typeof(T), new HashSet<Type>());
    }
}