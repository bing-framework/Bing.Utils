using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Bing.Reflection;
using Shouldly;
using Xunit;

namespace Bing.Utils.Tests.ConvUT;

/// <summary>
/// 类型转换操作单元测试
/// </summary>
[Trait("TypeUT", "TypeConv")]
public class TypeConvTests
{
    /// <summary>
    /// 测试 - 可空基础类型转换为不可空基础类型
    /// </summary>
    [Fact]
    public void Test_NullableTypeToNonNullableType()
    {
        TypeConv.GetNonNullableType(typeof(object)).ShouldBe(typeof(object));
        TypeConv.GetNonNullableType(null).ShouldBeNull();

        TypeConv.GetNonNullableType(typeof(byte)).ShouldBe(typeof(byte));
        TypeConv.GetNonNullableType(typeof(byte?)).ShouldBe(typeof(byte));
        TypeConv.GetNonNullableType(typeof(sbyte)).ShouldBe(typeof(sbyte));
        TypeConv.GetNonNullableType(typeof(sbyte?)).ShouldBe(typeof(sbyte));
        TypeConv.GetNonNullableType(typeof(short)).ShouldBe(typeof(short));
        TypeConv.GetNonNullableType(typeof(short?)).ShouldBe(typeof(short));
        TypeConv.GetNonNullableType(typeof(ushort)).ShouldBe(typeof(ushort));
        TypeConv.GetNonNullableType(typeof(ushort?)).ShouldBe(typeof(ushort));
        TypeConv.GetNonNullableType(typeof(int)).ShouldBe(typeof(int));
        TypeConv.GetNonNullableType(typeof(int?)).ShouldBe(typeof(int));
        TypeConv.GetNonNullableType(typeof(uint)).ShouldBe(typeof(uint));
        TypeConv.GetNonNullableType(typeof(uint?)).ShouldBe(typeof(uint));
        TypeConv.GetNonNullableType(typeof(long)).ShouldBe(typeof(long));
        TypeConv.GetNonNullableType(typeof(long?)).ShouldBe(typeof(long));
        TypeConv.GetNonNullableType(typeof(ulong)).ShouldBe(typeof(ulong));
        TypeConv.GetNonNullableType(typeof(ulong?)).ShouldBe(typeof(ulong));

        TypeConv.GetNonNullableType(typeof(float)).ShouldBe(typeof(float));
        TypeConv.GetNonNullableType(typeof(float?)).ShouldBe(typeof(float));
        TypeConv.GetNonNullableType(typeof(double)).ShouldBe(typeof(double));
        TypeConv.GetNonNullableType(typeof(double?)).ShouldBe(typeof(double));
        TypeConv.GetNonNullableType(typeof(decimal)).ShouldBe(typeof(decimal));
        TypeConv.GetNonNullableType(typeof(decimal?)).ShouldBe(typeof(decimal));

        TypeConv.GetNonNullableType(typeof(string)).ShouldBe(typeof(string));
        TypeConv.GetNonNullableType(typeof(char)).ShouldBe(typeof(char));
        TypeConv.GetNonNullableType(typeof(char?)).ShouldBe(typeof(char));
        TypeConv.GetNonNullableType(typeof(bool)).ShouldBe(typeof(bool));
        TypeConv.GetNonNullableType(typeof(bool?)).ShouldBe(typeof(bool));

        TypeConv.GetNonNullableType(typeof(DateTime)).ShouldBe(typeof(DateTime));
        TypeConv.GetNonNullableType(typeof(DateTime?)).ShouldBe(typeof(DateTime));
        TypeConv.GetNonNullableType(typeof(DateTimeOffset)).ShouldBe(typeof(DateTimeOffset));
        TypeConv.GetNonNullableType(typeof(DateTimeOffset?)).ShouldBe(typeof(DateTimeOffset));
        TypeConv.GetNonNullableType(typeof(TimeSpan)).ShouldBe(typeof(TimeSpan));
        TypeConv.GetNonNullableType(typeof(TimeSpan?)).ShouldBe(typeof(TimeSpan));

        TypeConv.GetNonNullableType(typeof(Guid)).ShouldBe(typeof(Guid));
        TypeConv.GetNonNullableType(typeof(Guid?)).ShouldBe(typeof(Guid));
        TypeConv.GetNonNullableType(typeof(ValueTuple)).ShouldBe(typeof(ValueTuple));
        TypeConv.GetNonNullableType(typeof(ValueTuple?)).ShouldBe(typeof(ValueTuple));
        TypeConv.GetNonNullableType(typeof(ValueTuple<>)).ShouldBe(typeof(ValueTuple<>));

        TypeConv.GetNonNullableType(typeof(Task)).ShouldBe(typeof(Task));
        TypeConv.GetNonNullableType(typeof(Task<>)).ShouldBe(typeof(Task<>));
        TypeConv.GetNonNullableType(typeof(ValueTask)).ShouldBe(typeof(ValueTask));
        TypeConv.GetNonNullableType(typeof(ValueTask?)).ShouldBe(typeof(ValueTask));
        TypeConv.GetNonNullableType(typeof(ValueTask<>)).ShouldBe(typeof(ValueTask<>));
    }

    /// <summary>
    /// 测试 - 可空数组类型转换为不可空数组类型
    /// </summary>
    [Fact]
    public void Test_NullableTypeArrayToNonNullableTypeArray()
    {
        TypeConv.GetNonNullableType(typeof(byte)).ShouldBe(typeof(byte));
        TypeConv.GetNonNullableType(typeof(byte?[])).ShouldBe(typeof(byte[]));
        TypeConv.GetNonNullableType(typeof(List<byte>)).ShouldBe(typeof(List<byte>));
        TypeConv.GetNonNullableType(typeof(List<byte?>)).ShouldBe(typeof(List<byte>));
        TypeConv.GetNonNullableType(typeof(Dictionary<string, int>)).ShouldBe(typeof(Dictionary<string, int>));
        TypeConv.GetNonNullableType(typeof(Dictionary<string, int?>)).ShouldBe(typeof(Dictionary<string, int>));
    }

    /// <summary>
    /// 测试 - 可空数组接口类型转换为不可空数据接口类型
    /// </summary>
    [Fact]
    public void Test_NullableTypeInterfaceToNonNullableInterface()
    {
        TypeConv.GetNonNullableType(typeof(IList<byte>)).ShouldBe(typeof(IList<byte>));
        TypeConv.GetNonNullableType(typeof(IList<byte?>)).ShouldBe(typeof(IList<byte>));
        TypeConv.GetNonNullableType(typeof(IDictionary<string, int>)).ShouldBe(typeof(IDictionary<string, int>));
        TypeConv.GetNonNullableType(typeof(IDictionary<string, int?>)).ShouldBe(typeof(IDictionary<string, int>));
    }
}