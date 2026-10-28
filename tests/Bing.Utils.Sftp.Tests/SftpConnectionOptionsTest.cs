using Bing.Sftp;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Shouldly;
using Xunit;

namespace Bing.Utils.Sftp.Tests;

/// <summary>
/// SFTP 配置单元测试。
/// </summary>
public class SftpConnectionOptionsTest
{
    /// <summary>
    /// 测试目的：缺少主机密钥固定信息时应在连接前被拒绝。
    /// </summary>
    [Fact]
    public async Task ListDirectoryAsync_WhenHostKeyIsMissing_ShouldFailBeforeNetworkAccess()
    {
        // Arrange
        var client = new SftpTransferClient(new SftpConnectionOptions { Host = "localhost", UserName = "user", Password = "password" });

        // Act and Assert
        await Should.ThrowAsync<ArgumentException>(() => client.ListDirectoryAsync("/"));
    }

    /// <summary>
    /// 测试目的：主机指纹比较应兼容 OpenSSH 前缀及可选 Base64 填充。
    /// </summary>
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void MatchesHostKey_WithPrefixAndOptionalPadding_ShouldAccept(bool withPrefix, bool withPadding)
    {
        // Arrange
        var hostKey = new byte[] { 1, 2, 3, 4, 5, 6, 7 };
        using var sha256 = SHA256.Create();
        var fingerprint = Convert.ToBase64String(sha256.ComputeHash(hostKey));
        if (!withPadding) fingerprint = fingerprint.TrimEnd('=');
        if (withPrefix) fingerprint = "SHA256:" + fingerprint;

        // Act and Assert
        SftpTransferClient.MatchesHostKey(hostKey, fingerprint).ShouldBeTrue();
    }

    /// <summary>
    /// 测试目的：不匹配或格式错误的主机指纹必须拒绝信任。
    /// </summary>
    [Theory]
    [InlineData("SHA256:AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA")]
    [InlineData("SHA256:")]
    public void MatchesHostKey_WithWrongFingerprint_ShouldReject(string fingerprint)
    {
        SftpTransferClient.MatchesHostKey(new byte[] { 1, 2, 3 }, fingerprint).ShouldBeFalse();
    }

    /// <summary>
    /// 测试目的：清理异常应附加到首个操作异常，而不能替换其身份。
    /// </summary>
    [Fact]
    public void AttachCleanupError_ShouldPreservePrimaryException()
    {
        var operationError = new InvalidDataException("copy failed");
        var cleanupError = new IOException("temporary delete failed");

        var result = SftpTransferClient.AttachCleanupError(operationError, cleanupError);

        ReferenceEquals(operationError, result).ShouldBeTrue();
        var errors = operationError.Data["Bing.Sftp.CleanupErrors"] as System.Collections.Generic.List<Exception>;
        errors.ShouldNotBeNull();
        errors.Count.ShouldBe(1);
        ReferenceEquals(cleanupError, errors[0]).ShouldBeTrue();
    }
}
