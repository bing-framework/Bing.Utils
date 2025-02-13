using Bing.IO;

namespace Bing.Utils.Tests.IO;

public class ByteBufferTest
{
    /// <summary>
    /// 测试 - 申请缓冲区 - 指定长度
    /// </summary>
    [Fact]
    public void Test_Allocate_Capacity()
    {
        var buffer = ByteBuffer.Allocate(256);
        buffer.Capacity.ShouldBe(256);
    }

    /// <summary>
    /// 测试 - 申请缓冲区 - 根据byte[]初始化
    /// </summary>
    [Fact]
    public void Test_Allocate_Bytes()
    {
        var data = new byte[512];
        var buffer = ByteBuffer.Allocate(data);
        buffer.Capacity.ShouldBe(512);
    }

    /// <summary>
    /// 测试 - 申请缓冲区 - 池化申请
    /// </summary>
    [Fact]
    public void Test_Allocate_Pool()
    {
        var buffer = ByteBuffer.Allocate(256, true);
        buffer.Capacity.ShouldBe(256);
    }

    /// <summary>
    /// 测试 - 申请缓冲区 - 池化申请
    /// </summary>
    [Fact]
    public void Test_Allocate_Pool_1()
    {
        var testData = new byte[] { 1, 2, 3, 4, 5 };
        var buffer = ByteBuffer.Allocate(testData, true);
        buffer.Capacity.ShouldBe(testData.Length);
        buffer.ReadableBytes.ShouldBe(testData.Length);
        buffer.ToArray().ShouldBe(testData);
    }

    /// <summary>
    /// 测试 - 写入int
    /// </summary>
    [Fact]
    public void Test_WriteInt()
    {
        var buffer = ByteBuffer.Allocate(256);
        buffer.WriteInt32(1234);
        buffer.ReadInt32().ShouldBe(1234);
    }
}