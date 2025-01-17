namespace Bing.Reflection;

/// <summary>
/// 简单类型名称集合。
/// 基元类型 + TimeSpan + DateTime + Guid + Decimal + String + 任何可从字符串转入的对象
/// </summary>
public class SimpleTypeNames
{
    /// <summary>
    /// 基元类型
    /// </summary>
    public static readonly HashSet<string> PrimitiveTypes = new()
    {
        Byte, SByte, Int16, UInt16, Int32, UInt32, Int64, UInt64, Single, Double, Boolean, Char, IntPtr, UIntPtr
    };

    /// <summary>
    /// 简单类型
    /// </summary>
    public static readonly string[] SimpleTypes = new[]
    {
        Decimal, TimeSpan, DateTime, DateTimeOffset, Guid, String, Bytes
    };

    #region Primitive Types

    /// <summary>
    /// byte
    /// </summary>
    public const string Byte = "System.Byte";

    /// <summary>
    /// sbyte
    /// </summary>
    public const string SByte = "System.SByte";

    /// <summary>
    /// short
    /// </summary>
    public const string Int16 = "System.Int16";

    /// <summary>
    /// ushort
    /// </summary>
    public const string UInt16 = "System.UInt16";

    /// <summary>
    /// int
    /// </summary>
    public const string Int32 = "System.Int32";

    /// <summary>
    /// uint
    /// </summary>
    public const string UInt32 = "System.UInt32";

    /// <summary>
    /// long
    /// </summary>
    public const string Int64 = "System.Int64";

    /// <summary>
    /// ulong
    /// </summary>
    public const string UInt64 = "System.UInt64";

    /// <summary>
    /// float
    /// </summary>
    public const string Single = "System.Single";

    /// <summary>
    /// double
    /// </summary>
    public const string Double = "System.Double";

    /// <summary>
    /// bool
    /// </summary>
    public const string Boolean = "System.Boolean";

    /// <summary>
    /// char
    /// </summary>
    public const string Char = "System.Char";

    /// <summary>
    /// IntPtr
    /// </summary>
    public const string IntPtr = "System.IntPtr";

    /// <summary>
    /// UIntPtr
    /// </summary>
    public const string UIntPtr = "System.UIntPtr";

    #endregion

    #region Simple Type

    /// <summary>
    /// decimal
    /// </summary>
    public const string Decimal = "System.Decimal";

    /// <summary>
    /// TimeSpan
    /// </summary>
    public const string TimeSpan = "System.TimeSpan";

    /// <summary>
    /// DateTime
    /// </summary>
    public const string DateTime = "System.DateTime";

    /// <summary>
    /// DateTimeOffset
    /// </summary>
    public const string DateTimeOffset = "System.DateTimeOffset";

    /// <summary>
    /// Guid
    /// </summary>
    public const string Guid = "System.Guid";

    /// <summary>
    /// String
    /// </summary>
    public const string String = "System.String";

    /// <summary>
    /// byte[]
    /// </summary>
    public const string Bytes = "System.Byte[]";

    #endregion
}