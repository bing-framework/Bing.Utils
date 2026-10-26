using System;
using System.Threading;
using System.Threading.Tasks;
using Bing.Net.FTP;
using FluentFTP;
using Moq;

namespace Bing.Utils.Net.Tests;

/// <summary>
/// 验证旧异步 FTP 客户端不会将预算目录操作降级为无限额操作。
/// </summary>
public sealed class FtpDirectoryLimitsCompatibilityTest
{
    [Fact]
    public async Task ClientWithoutDirectoryCapabilitiesRejectsLimitedTransfers()
    {
        var client = new Mock<IFtpAsyncClient>(MockBehavior.Strict);
        var limits = new FtpDirectoryTransferLimits();

        await Assert.ThrowsAsync<NotSupportedException>(() =>
            client.Object.UploadDirectoryWithLimitsAsync("local", "/remote", limits));
        await Assert.ThrowsAsync<NotSupportedException>(() =>
            client.Object.DownloadDirectoryWithLimitsAsync("local", "/remote", limits));

        Assert.Empty(client.Invocations);
    }

    [Fact]
    public async Task LegacyDirectoryClientDoesNotFallBackToUnboundedTransfers()
    {
        var client = new Mock<IFtpAsyncClient>(MockBehavior.Strict);
        var directory = client.As<IFtpAsyncDirectoryClient>();
        directory
            .Setup(x => x.UploadDirectoryAsync(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<FtpConflictPolicy>(), It.IsAny<IProgress<FtpProgress>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<FtpDirectoryTransferResult>(null));
        directory
            .Setup(x => x.DownloadDirectoryAsync(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<FtpConflictPolicy>(), It.IsAny<IProgress<FtpProgress>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<FtpDirectoryTransferResult>(null));
        var limits = new FtpDirectoryTransferLimits();

        await Assert.ThrowsAsync<NotSupportedException>(() =>
            client.Object.UploadDirectoryWithLimitsAsync("local", "/remote", limits));
        await Assert.ThrowsAsync<NotSupportedException>(() =>
            client.Object.DownloadDirectoryWithLimitsAsync("local", "/remote", limits));

        directory.Verify(x => x.UploadDirectoryAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<FtpConflictPolicy>(), It.IsAny<IProgress<FtpProgress>>(), It.IsAny<CancellationToken>()),
            Times.Never);
        directory.Verify(x => x.DownloadDirectoryAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<FtpConflictPolicy>(), It.IsAny<IProgress<FtpProgress>>(), It.IsAny<CancellationToken>()),
            Times.Never);
        Assert.Empty(client.Invocations);
    }
}
