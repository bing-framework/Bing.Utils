using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Bing.Security.Hashing;
using Shouldly;
using Xunit;
using HashingOperations = global::Bing.Security.Hashing.Hashing;
using HexEncodingOperations = global::Bing.Security.Encoding.HexEncoding;

namespace Bing.Utils.Security.Tests.Bing.Security.Hashing;

/// <summary>
/// 验证 SHA-2 摘要的标准向量和流操作。
/// </summary>
public class HashingTests
{
    /// <summary>
    /// 测试目的：SHA-2 实现应匹配 abc 的公开标准测试向量。
    /// </summary>
    [Theory]
    [InlineData(HashAlgorithmType.Sha256, "ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad")]
    [InlineData(HashAlgorithmType.Sha384, "cb00753f45a35e8bb5a03d699ac65007272c32ab0eded1631a8b605a43ff5bed8086072ba1e7cc2358baeca134c825a7")]
    [InlineData(HashAlgorithmType.Sha512, "ddaf35a193617abacc417349ae20413112e6fa4e89a97ea20a9eeee64b55d39a2192992a274fc1a836ba3c23a3feebbd454d4423643ce80e2a9ac94fa54ca49f")]
    public void ComputeHex_WhenValueIsAbc_ShouldMatchStandardVector(HashAlgorithmType algorithm, string expected)
    {
        // Arrange

        // Act
        var result = HashingOperations.ComputeHex("abc", algorithm);

        // Assert
        result.ShouldBe(expected);
    }

    /// <summary>
    /// 测试目的：摘要验证应使用匹配内容成功并拒绝被修改的内容。
    /// </summary>
    [Fact]
    public void VerifyHex_WhenValueChanges_ShouldReturnFalse()
    {
        // Arrange
        var hash = HashingOperations.ComputeHex("安全数据");

        // Act
        var success = HashingOperations.VerifyHex("安全数据", hash);
        var failure = HashingOperations.VerifyHex("安全数据!", hash);

        // Assert
        success.ShouldBeTrue();
        failure.ShouldBeFalse();
    }

    /// <summary>
    /// 测试目的：异步流摘要应支持不可 Seek 流且不关闭调用方流。
    /// </summary>
    [Fact]
    public async Task ComputeAsync_WhenStreamIsNonSeekable_ShouldHashWithoutClosingStream()
    {
        // Arrange
        var content = System.Text.Encoding.UTF8.GetBytes("流摘要");
        using var stream = new NonSeekableReadStream(content);

        // Act
        var hash = await HashingOperations.ComputeAsync(stream);

        // Assert
        HashingOperations.ComputeHex("流摘要").ShouldBe(HexEncodingOperations.Encode(hash));
        stream.CanRead.ShouldBeTrue();
    }

    /// <summary>
    /// 表示仅支持顺序读取的测试流。
    /// </summary>
    private sealed class NonSeekableReadStream : MemoryStream
    {
        /// <summary>
        /// 使用测试数据初始化流。
        /// </summary>
        /// <param name="buffer">测试数据。</param>
        public NonSeekableReadStream(byte[] buffer) : base(buffer)
        {
        }

        /// <inheritdoc />
        public override bool CanSeek => false;

        /// <inheritdoc />
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        /// <inheritdoc />
        public override long Position
        {
            get => base.Position;
            set => throw new NotSupportedException();
        }
    }
}