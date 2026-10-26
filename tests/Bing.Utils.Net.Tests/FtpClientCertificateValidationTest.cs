using System.Net.Security;
using System.Reflection;
using FluentFTP;

namespace Bing.Net.FTP;

/// <summary>
/// 验证 FTP 客户端注册的 FTPS 证书策略。
/// </summary>
public sealed class FtpClientCertificateValidationTest
{
    /// <summary>
    /// 验证默认策略仅接受没有 SSL 策略错误的证书。
    /// </summary>
    /// <param name="policyErrors">模拟的证书策略错误。</param>
    /// <param name="expectedAccept">证书预期是否被接受。</param>
    [Theory]
    [InlineData(SslPolicyErrors.None, true)]
    [InlineData(SslPolicyErrors.RemoteCertificateNameMismatch, false)]
    [InlineData(SslPolicyErrors.RemoteCertificateChainErrors, false)]
    [InlineData(SslPolicyErrors.RemoteCertificateNotAvailable, false)]
    public void EncryptedMode_DefaultPolicyAcceptsOnlyErrorFreeCertificates(
        SslPolicyErrors policyErrors,
        bool expectedAccept)
    {
        var config = new FtpClientConfig
        {
            Host = "127.0.0.1",
            EnableSsl = false,
            EncryptionType = EncryptionType.Explicit
        };

        using var client = new FtpClient(config);
        var eventArgs = CreateEventArgs(policyErrors, initialAccept: !expectedAccept);

        RaiseCertificateValidation(client, eventArgs);

        Assert.Equal(expectedAccept, eventArgs.Accept);
    }

    /// <summary>
    /// 验证关闭 EnableSsl 标记时仍会按显式加密模式调用自定义策略。
    /// </summary>
    [Fact]
    public void EncryptedMode_UsesExplicitCertificatePolicyWhenEnableSslIsFalse()
    {
        var invoked = false;
        var observedErrors = SslPolicyErrors.None;
        var config = new FtpClientConfig
        {
            Host = "127.0.0.1",
            EnableSsl = false,
            EncryptionType = EncryptionType.Implicit,
            SslCertificateValidationPolicy = args =>
            {
                invoked = true;
                observedErrors = args.PolicyErrors;
                return true;
            }
        };

        using var client = new FtpClient(config);
        var eventArgs = CreateEventArgs(SslPolicyErrors.RemoteCertificateNameMismatch, initialAccept: false);

        RaiseCertificateValidation(client, eventArgs);

        Assert.True(invoked);
        Assert.Equal(SslPolicyErrors.RemoteCertificateNameMismatch, observedErrors);
        Assert.True(eventArgs.Accept);
    }

    /// <summary>
    /// 验证自定义策略异常时证书保持拒绝状态且异常继续传播。
    /// </summary>
    [Fact]
    public void EncryptedMode_CertificatePolicyExceptionFailsClosed()
    {
        var config = new FtpClientConfig
        {
            Host = "127.0.0.1",
            EncryptionType = EncryptionType.Explicit,
            SslCertificateValidationPolicy = _ => throw new InvalidOperationException("policy failure")
        };

        using var client = new FtpClient(config);
        var eventArgs = CreateEventArgs(SslPolicyErrors.None, initialAccept: true);

        var exception = Assert.Throws<TargetInvocationException>(() => RaiseCertificateValidation(client, eventArgs));

        Assert.IsType<InvalidOperationException>(exception.InnerException);
        Assert.False(eventArgs.Accept);
    }

    /// <summary>
    /// 验证未启用实际加密模式时不会注册证书验证策略。
    /// </summary>
    [Fact]
    public void NoEncryption_DoesNotRegisterCertificatePolicyEvenWhenEnableSslIsTrue()
    {
        var invoked = false;
        var config = new FtpClientConfig
        {
            Host = "127.0.0.1",
            EnableSsl = true,
            EncryptionType = EncryptionType.None,
            SslCertificateValidationPolicy = _ =>
            {
                invoked = true;
                return false;
            }
        };

        using var client = new FtpClient(config);
        var eventArgs = CreateEventArgs(SslPolicyErrors.RemoteCertificateNameMismatch, initialAccept: true);

        RaiseCertificateValidation(client, eventArgs);

        Assert.False(invoked);
        Assert.True(eventArgs.Accept);
    }

    /// <summary>
    /// 创建带指定策略错误和初始接受状态的 FluentFTP 事件参数。
    /// </summary>
    /// <param name="policyErrors">模拟的证书策略错误。</param>
    /// <param name="initialAccept">触发事件前的接受状态。</param>
    /// <returns>可传递给 FluentFTP 证书验证事件的参数。</returns>
    private static FtpSslValidationEventArgs CreateEventArgs(SslPolicyErrors policyErrors, bool initialAccept)
    {
        return new FtpSslValidationEventArgs
        {
            PolicyErrors = policyErrors,
            Accept = initialAccept
        };
    }

    /// <summary>
    /// 通过 FluentFTP 的事件触发入口执行包装器已注册的证书验证处理器。
    /// </summary>
    /// <param name="client">持有 FluentFTP 客户端的包装器。</param>
    /// <param name="eventArgs">证书验证事件参数。</param>
    private static void RaiseCertificateValidation(FtpClient client, FtpSslValidationEventArgs eventArgs)
    {
        var innerClientField = typeof(FtpClient).GetField("_client", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(innerClientField);
        var innerClient = Assert.IsType<FluentFTP.FtpClient>(innerClientField.GetValue(client));
        var raiseMethod = innerClient.GetType().GetMethod(
            "OnValidateCertficate",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(raiseMethod);

        raiseMethod.Invoke(innerClient, new object[] { eventArgs });
    }
}
