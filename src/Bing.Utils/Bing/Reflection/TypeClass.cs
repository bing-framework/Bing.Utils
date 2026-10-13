namespace Bing.Reflection;

/// <summary>
/// 类类型
/// </summary>
public static class TypeClass
{
    /// <summary>
    /// 获取void
    /// </summary>
    public static Type VoidClazz { get; } = typeof(void);

    /// <summary>
    /// 获取object
    /// </summary>
    public static Type ObjectClazz { get; } = typeof(object);

    /// <summary>
    /// 获取object[]
    /// </summary>
    public static Type ObjectArrayClazz { get; } = typeof(object[]);

    /// <summary>
    /// 获取byte
    /// </summary>
    public static Type ByteClazz { get; } = typeof(byte);

    /// <summary>
    /// 获取byte?
    /// </summary>
    public static Type ByteNullableClazz { get; } = typeof(byte?);

    /// <summary>
    /// 获取byte[]
    /// </summary>
    public static Type ByteArrayClazz { get; } = typeof(byte[]);

    /// <summary>
    /// 获取sbyte
    /// </summary>
    public static Type SByteClazz { get; } = typeof(sbyte);

    /// <summary>
    /// 获取sbyte?
    /// </summary>
    public static Type SByteNullableClazz { get; } = typeof(sbyte?);

    /// <summary>
    /// 获取int16
    /// </summary>
    public static Type Int16Clazz { get; } = typeof(short);

    /// <summary>
    /// 获取int16?
    /// </summary>
    public static Type Int16NullableClazz { get; } = typeof(short?);

    /// <summary>
    /// 获取uint16
    /// </summary>
    public static Type UInt16Clazz { get; } = typeof(ushort);

    /// <summary>
    /// 获取uint16?
    /// </summary>
    public static Type UInt16NullableClazz { get; } = typeof(ushort?);

    /// <summary>
    /// 获取int32
    /// </summary>
    public static Type Int32Clazz { get; } = typeof(int);

    /// <summary>
    /// 获取int32?
    /// </summary>
    public static Type Int32NullableClazz { get; } = typeof(int?);

    /// <summary>
    /// 获取uint32
    /// </summary>
    public static Type UInt32Clazz { get; } = typeof(uint);

    /// <summary>
    /// 获取uint32?
    /// </summary>
    public static Type UInt32NullableClazz { get; } = typeof(uint?);

    /// <summary>
    /// 获取int64
    /// </summary>
    public static Type Int64Clazz { get; } = typeof(long);

    /// <summary>
    /// 获取int64?
    /// </summary>
    public static Type Int64NullableClazz { get; } = typeof(long?);

    /// <summary>
    /// 获取uint64
    /// </summary>
    public static Type UInt64Clazz { get; } = typeof(ulong);

    /// <summary>
    /// 获取uint64?
    /// </summary>
    public static Type UInt64NullableClazz { get; } = typeof(ulong?);

    /// <summary>
    /// 获取short
    /// </summary>
    public static Type ShortClazz => Int16Clazz;

    /// <summary>
    /// 获取short?
    /// </summary>
    public static Type ShortNullableClazz => Int16NullableClazz;

    /// <summary>
    /// 获取ushort
    /// </summary>
    public static Type UShortClazz => UInt16Clazz;

    /// <summary>
    /// 获取ushort?
    /// </summary>
    public static Type UShortNullableClazz => UInt16NullableClazz;

    /// <summary>
    /// 获取int
    /// </summary>
    public static Type IntClazz => Int32Clazz;

    /// <summary>
    /// 获取int?
    /// </summary>
    public static Type IntNullableClazz => Int32NullableClazz;

    /// <summary>
    /// 获取uint
    /// </summary>
    public static Type UIntClazz => UInt32Clazz;

    /// <summary>
    /// 获取uint?
    /// </summary>
    public static Type UIntNullableClazz => UInt32NullableClazz;

    /// <summary>
    /// 获取long
    /// </summary>
    public static Type LongClazz => Int64Clazz;

    /// <summary>
    /// 获取long?
    /// </summary>
    public static Type LongNullableClazz => Int64NullableClazz;

    /// <summary>
    /// 获取ulong
    /// </summary>
    public static Type ULongClazz => UInt64Clazz;

    /// <summary>
    /// 获取ulong?
    /// </summary>
    public static Type ULongNullableClazz => UInt64NullableClazz;

    /// <summary>
    /// 获取float
    /// </summary>
    public static Type FloatClazz { get; } = typeof(float);

    /// <summary>
    /// 获取float?
    /// </summary>
    public static Type FloatNullableClazz { get; } = typeof(float?);

    /// <summary>
    /// 获取float
    /// </summary>
    public static Type SingleClazz { get; } = typeof(float);

    /// <summary>
    /// 获取float?
    /// </summary>
    public static Type SingleNullableClazz { get; } = typeof(float?);

    /// <summary>
    /// 获取double
    /// </summary>
    public static Type DoubleClazz { get; } = typeof(double);

    /// <summary>
    /// 获取double?
    /// </summary>
    public static Type DoubleNullableClazz { get; } = typeof(double?);

    /// <summary>
    /// 获取decimal
    /// </summary>
    public static Type DecimalClazz { get; } = typeof(decimal);

    /// <summary>
    /// 获取decimal?
    /// </summary>
    public static Type DecimalNullableClazz { get; } = typeof(decimal?);

    /// <summary>
    /// 获取string
    /// </summary>
    public static Type StringClazz { get; } = typeof(string);

    /// <summary>
    /// 获取DateTime
    /// </summary>
    public static Type DateTimeClazz { get; } = typeof(DateTime);

    /// <summary>
    /// 获取DateTime?
    /// </summary>
    public static Type DateTimeNullableClazz { get; } = typeof(DateTime?);

    /// <summary>
    /// 获取DateTimeOffset
    /// </summary>
    public static Type DateTimeOffsetClazz { get; } = typeof(DateTimeOffset);

    /// <summary>
    /// 获取DateTimeOffset?
    /// </summary>
    public static Type DateTimeOffsetNullableClazz { get; } = typeof(DateTimeOffset?);

    /// <summary>
    /// 获取TimeSpan
    /// </summary>
    public static Type TimeSpanClazz { get; } = typeof(TimeSpan);

    /// <summary>
    /// 获取TimeSpan?
    /// </summary>
    public static Type TimeSpanNullableClazz { get; } = typeof(TimeSpan?);

    /// <summary>
    /// 获取Guid
    /// </summary>
    public static Type GuidClazz { get; } = typeof(Guid);

    /// <summary>
    /// 获取Guid?
    /// </summary>
    public static Type GuidNullableClazz { get; } = typeof(Guid?);

    /// <summary>
    /// 获取bool
    /// </summary>
    public static Type BooleanClazz { get; } = typeof(bool);

    /// <summary>
    /// 获取bool?
    /// </summary>
    public static Type BooleanNullableClazz { get; } = typeof(bool?);

    /// <summary>
    /// 获取char
    /// </summary>
    public static Type CharClazz { get; } = typeof(char);

    /// <summary>
    /// 获取char?
    /// </summary>
    public static Type CharNullableClazz { get; } = typeof(char?);

    /// <summary>
    /// 获取Enum
    /// </summary>
    public static Type EnumClazz { get; } = typeof(Enum);

    /// <summary>
    /// 获取ValueTuple
    /// </summary>
    public static Type ValueTupleClazz { get; } = typeof(ValueTuple);

    /// <summary>
    /// 获取Task
    /// </summary>
    public static Type TaskClazz { get; } = typeof(Task);

    /// <summary>
    /// 获取Generic Task
    /// </summary>
    public static Type GenericTaskClazz { get; } = typeof(Task<>);

    /// <summary>
    /// 获取ValueTask
    /// </summary>
    public static Type ValueTaskClazz { get; } = typeof(ValueTask);

    /// <summary>
    /// 获取Generic ValueTask
    /// </summary>
    public static Type GenericValueTaskClazz { get; } = typeof(ValueTask<>);

    /// <summary>
    /// 获取Generic List
    /// </summary>
    public static Type GenericListClazz { get; } = typeof(List<>);

    /// <summary>
    /// 获取Nullable
    /// </summary>
    public static Type NullableClazz { get; } = typeof(Nullable);

    /// <summary>
    /// 获取Generic Nullable
    /// </summary>
    public static Type GenericNullableClazz { get; } = typeof(Nullable<>);

    /// <summary>
    /// 获取IFormattable
    /// </summary>
    public static Type FormattableClazz { get; } = typeof(IFormattable);

    /// <summary>
    /// 获取IFormatProvider
    /// </summary>
    public static Type FormatProviderClazz { get; } = typeof(IFormatProvider);
}
