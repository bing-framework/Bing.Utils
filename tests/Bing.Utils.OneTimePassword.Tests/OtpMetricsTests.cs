using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using Xunit;

using Bing.OneTimePassword;

namespace Bing.Utils.OneTimePassword.Tests;

/// <summary>
/// 检查诊断字段和回调不会暴露敏感输入或改变已提交的认证结果。
/// </summary>
public sealed class OtpMetricsTests
{
    [Fact]
    public async Task Metrics_HaveOnlyBoundedLabelsAndReportFault()
    {
        var events = new ConcurrentBag<KeyValuePair<string, object?>[]>();
        using var listener = new MeterListener
        {
            InstrumentPublished = (instrument, l) =>
            {
                if (instrument.Meter.Name == "Bing.Utils.OneTimePassword") l.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((_, _, tags, _) => events.Add(tags.ToArray()));
        listener.Start();
        var f = new LifecycleFixture();
        var e = await f.EnrollAsync(OtpType.Hotp);
        var tx = await f.Service.BeginVerificationAsync(f.Login);
        var code = Hotp.Generate(OtpSecretEncoding.Decode(e.SecretBase32), 1);
        f.Protector.Fail = true;
        await Assert.ThrowsAsync<OtpProtectionException>(() => f.Service.VerifyAsync(f.Login, e.CredentialId, tx.TransactionId, code));
        f.Protector.Fail = false;
        Assert.True((await f.Service.VerifyAsync(f.Login, e.CredentialId, tx.TransactionId, code)).Succeeded);
        Assert.Contains(events, tags => tags.Any(x => x.Key == "result" && Equals(x.Value, "protection_fault")));
        Assert.All(events.SelectMany(x => x), tag =>
        {
            Assert.Contains(tag.Key, new[] { "purpose", "type", "algorithm", "result" });
            Assert.DoesNotContain(e.SecretBase32, tag.Value?.ToString() ?? "");
            Assert.DoesNotContain(e.ProvisioningUri, tag.Value?.ToString() ?? "");
            Assert.DoesNotContain(code, tag.Value?.ToString() ?? "");
            Assert.DoesNotContain(e.CredentialId, tag.Value?.ToString() ?? "");
        });
    }

    [Fact]
    public async Task ThrowingObserver_CannotUndoSuccessfulConsumption()
    {
        using var listener = new MeterListener
        {
            InstrumentPublished = (instrument, l) =>
            {
                if (instrument.Meter.Name == "Bing.Utils.OneTimePassword") l.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((_, _, _, _) => throw new InvalidOperationException("observer"));
        listener.Start();
        var f = new LifecycleFixture();
        var e = await f.EnrollAsync(OtpType.Hotp);
        var tx = await f.Service.BeginVerificationAsync(f.Login);
        var code = Hotp.Generate(OtpSecretEncoding.Decode(e.SecretBase32), 1);
        Assert.True((await f.Service.VerifyAsync(f.Login, e.CredentialId, tx.TransactionId, code)).Succeeded);
        Assert.False((await f.Service.VerifyAsync(f.Login, e.CredentialId, tx.TransactionId, code)).Succeeded);
    }
}
