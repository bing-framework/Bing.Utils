using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Bing.Reflection
{
    /// <summary>
    /// 类类型
    /// </summary>
    public static class TypeClass
    {
        /// <summary>
        /// void
        /// </summary>
        public static Type VoidClazz { get; } = typeof(void);

        /// <summary>
        /// object
        /// </summary>
        public static Type ObjectClazz { get; } = typeof(object);

        /// <summary>
        /// object[]
        /// </summary>
        public static Type ObjectArrayClazz { get; } = typeof(object[]);

        /// <summary>
        /// byte
        /// </summary>
        public static Type ByteClazz { get; } = typeof(byte);

        /// <summary>
        /// byte?
        /// </summary>
        public static Type ByteNullableClazz { get; } = typeof(byte?);

        /// <summary>
        /// byte[]
        /// </summary>
        public static Type ByteArrayClazz { get; } = typeof(byte[]);

        /// <summary>
        /// sbyte
        /// </summary>
        public static Type SByteClazz { get; } = typeof(sbyte);

        /// <summary>
        /// sbyte?
        /// </summary>
        public static Type SByteNullableClazz { get; } = typeof(sbyte?);

        /// <summary>
        /// int16
        /// </summary>
        public static Type Int16Clazz { get; } = typeof(short);

        /// <summary>
        /// int16?
        /// </summary>
        public static Type Int16NullableClazz { get; } = typeof(short?);

        /// <summary>
        /// uint16
        /// </summary>
        public static Type UInt16Clazz { get; } = typeof(ushort);

        /// <summary>
        /// uint16?
        /// </summary>
        public static Type UInt16NullableClazz { get; } = typeof(ushort?);

        /// <summary>
        /// int32
        /// </summary>
        public static Type Int32Clazz { get; } = typeof(int);

        /// <summary>
        /// int32?
        /// </summary>
        public static Type Int32NullableClazz { get; } = typeof(int?);

        /// <summary>
        /// uint32
        /// </summary>
        public static Type UInt32Clazz { get; } = typeof(uint);

        /// <summary>
        /// uint32?
        /// </summary>
        public static Type UInt32NullableClazz { get; } = typeof(uint?);

        /// <summary>
        /// int64
        /// </summary>
        public static Type Int64Clazz { get; } = typeof(long);

        /// <summary>
        /// int64?
        /// </summary>
        public static Type Int64NullableClazz { get; } = typeof(long?);

        /// <summary>
        /// uint64
        /// </summary>
        public static Type UInt64Clazz { get; } = typeof(ulong);

        /// <summary>
        /// uint64?
        /// </summary>
        public static Type UInt64NullableClazz { get; } = typeof(ulong?);

        /// <summary>
        /// short
        /// </summary>
        public static Type ShortClazz => Int16Clazz;

        /// <summary>
        /// short?
        /// </summary>
        public static Type ShortNullableClazz => Int16NullableClazz;

        /// <summary>
        /// ushort
        /// </summary>
        public static Type UShortClazz => UInt16Clazz;

        /// <summary>
        /// ushort?
        /// </summary>
        public static Type UShortNullableClazz => UInt16NullableClazz;

        /// <summary>
        /// int
        /// </summary>
        public static Type IntClazz => Int32Clazz;

        /// <summary>
        /// int?
        /// </summary>
        public static Type IntNullableClazz => Int32NullableClazz;

        /// <summary>
        /// uint
        /// </summary>
        public static Type UIntClazz => UInt32Clazz;

        /// <summary>
        /// uint?
        /// </summary>
        public static Type UIntNullableClazz => UInt32NullableClazz;

        /// <summary>
        /// long
        /// </summary>
        public static Type LongClazz => Int64Clazz;

        /// <summary>
        /// long?
        /// </summary>
        public static Type LongNullableClazz => Int64NullableClazz;

        /// <summary>
        /// ulong
        /// </summary>
        public static Type ULongClazz => UInt64Clazz;

        /// <summary>
        /// ulong?
        /// </summary>
        public static Type ULongNullableClazz => UInt64NullableClazz;

        /// <summary>
        /// float
        /// </summary>
        public static Type FloatClazz { get; } = typeof(float);

        /// <summary>
        /// float?
        /// </summary>
        public static Type FloatNullableClazz { get; } = typeof(float?);

        /// <summary>
        /// float
        /// </summary>
        public static Type SingleClazz { get; } = typeof(float);

        /// <summary>
        /// float?
        /// </summary>
        public static Type SingleNullableClazz { get; } = typeof(float?);

        /// <summary>
        /// double
        /// </summary>
        public static Type DoubleClazz { get; } = typeof(double);

        /// <summary>
        /// double?
        /// </summary>
        public static Type DoubleNullableClazz { get; } = typeof(double?);

        /// <summary>
        /// decimal
        /// </summary>
        public static Type DecimalClazz { get; } = typeof(decimal);

        /// <summary>
        /// decimal?
        /// </summary>
        public static Type DecimalNullableClazz { get; } = typeof(decimal?);

        /// <summary>
        /// string
        /// </summary>
        public static Type StringClazz { get; } = typeof(string);

        /// <summary>
        /// DateTime
        /// </summary>
        public static Type DateTimeClazz { get; } = typeof(DateTime);

        /// <summary>
        /// DateTime?
        /// </summary>
        public static Type DateTimeNullableClazz { get; } = typeof(DateTime?);

        /// <summary>
        /// DateTimeOffset
        /// </summary>
        public static Type DateTimeOffsetClazz { get; } = typeof(DateTimeOffset);

        /// <summary>
        /// DateTimeOffset?
        /// </summary>
        public static Type DateTimeOffsetNullableClazz { get; } = typeof(DateTimeOffset?);

        /// <summary>
        /// TimeSpan
        /// </summary>
        public static Type TimeSpanClazz { get; } = typeof(TimeSpan);

        /// <summary>
        /// TimeSpan?
        /// </summary>
        public static Type TimeSpanNullableClazz { get; } = typeof(TimeSpan?);

        /// <summary>
        /// Guid
        /// </summary>
        public static Type GuidClazz { get; } = typeof(Guid);

        /// <summary>
        /// Guid?
        /// </summary>
        public static Type GuidNullableClazz { get; } = typeof(Guid?);

        /// <summary>
        /// bool
        /// </summary>
        public static Type BooleanClazz { get; } = typeof(bool);

        /// <summary>
        /// bool?
        /// </summary>
        public static Type BooleanNullableClazz { get; } = typeof(bool?);

        /// <summary>
        /// char
        /// </summary>
        public static Type CharClazz { get; } = typeof(char);

        /// <summary>
        /// char?
        /// </summary>
        public static Type CharNullableClazz { get; } = typeof(char?);

        /// <summary>
        /// Enum
        /// </summary>
        public static Type EnumClazz { get; } = typeof(Enum);

        /// <summary>
        /// ValueTuple
        /// </summary>
        public static Type ValueTupleClazz { get; } = typeof(ValueTuple);

        /// <summary>
        /// Task
        /// </summary>
        public static Type TaskClazz { get; } = typeof(Task);

        /// <summary>
        /// Generic Task
        /// </summary>
        public static Type GenericTaskClazz { get; } = typeof(Task<>);

        /// <summary>
        /// ValueTask
        /// </summary>
        public static Type ValueTaskClazz { get; } = typeof(ValueTask);

        /// <summary>
        /// Generic ValueTask
        /// </summary>
        public static Type GenericValueTaskClazz { get; } = typeof(ValueTask<>);

        /// <summary>
        /// Generic List
        /// </summary>
        public static Type GenericListClazz { get; } = typeof(List<>);

        /// <summary>
        /// Nullable
        /// </summary>
        public static Type NullableClazz { get; } = typeof(Nullable);

        /// <summary>
        /// Generic Nullable
        /// </summary>
        public static Type GenericNullableClazz { get; } = typeof(Nullable<>);

        /// <summary>
        /// IFormattable
        /// </summary>
        public static Type FormattableClazz { get; } = typeof(IFormattable);

        /// <summary>
        /// IFormatProvider
        /// </summary>
        public static Type FormatProviderClazz { get; } = typeof(IFormatProvider);
    }
}
