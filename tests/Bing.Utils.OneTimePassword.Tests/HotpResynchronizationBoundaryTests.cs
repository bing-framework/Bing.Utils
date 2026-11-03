using System.Text;
using Bing.OneTimePassword;
using Xunit;

namespace Bing.Utils.OneTimePassword.Tests;

/// <summary>
/// 验证 HOTP 双码重同步的窗口、歧义和计数器耗尽边界。
/// </summary>
public sealed class HotpResynchronizationBoundaryTests
{
    /// <summary>
    /// 起点为 1 时偏移 100 包含计数器 101，偏移 101 超出窗口；失败不能消费状态。
    /// </summary>
    /// <param name="firstCode">固定 RFC 测试密钥对应的第一个验证码。</param>
    /// <param name="secondCode">紧随其后的第二个验证码。</param>
    /// <param name="succeeds">该连续双码是否位于重同步窗口内。</param>
    [Theory]
    [InlineData("329376", "629694", true)] // 计数器 101、102，最大允许偏移 100。
    [InlineData("629694", "378717", false)] // 计数器 102、103，偏移 101 必须拒绝。
    public async Task Resynchronize_WhenPairReachesWindowBoundary_ShouldRespectMaximumOffset(
        string firstCode, string secondCode, bool succeeds)
    {
        var (fixture, credentialId) = await CreateFixedCredentialAsync();
        var transaction = await fixture.Service.BeginVerificationAsync(
            fixture.Manage, OtpTransactionKind.Resynchronization, credentialId);

        var result = await fixture.Service.ResynchronizeHotpAsync(
            fixture.Manage, credentialId, transaction.TransactionId, firstCode, secondCode);

        Assert.Equal(succeeds ? OtpVerifyStatus.Success : OtpVerifyStatus.Invalid, result.Status);
        await fixture.Store.ExecuteAsync("alice", state =>
        {
            var credential = Assert.Single(state.Credentials);
            var savedTransaction = Assert.Single(state.Transactions);
            Assert.Equal(succeeds ? 103UL : 1UL, credential.NextCounter);
            Assert.False(credential.CounterExhausted);
            Assert.Equal(succeeds, savedTransaction.Consumed);
            Assert.Equal(succeeds ? 0 : 1, savedTransaction.FailureCount);
            Assert.Equal(succeeds ? 0 : 1, state.Failure.Count);
            Assert.Equal(succeeds ? 0 : 1, credential.Failure.Count);
            return true;
        });

        if (succeeds)
        {
            var next = await fixture.Service.BeginVerificationAsync(fixture.Login);
            Assert.True((await fixture.Service.VerifyAsync(
                fixture.Login, credentialId, next.TransactionId, "378717")).Succeeded);
        }
        else
        {
            // 同一事务仍能用计数器 1、2 的正确双码完成重同步。
            Assert.True((await fixture.Service.ResynchronizeHotpAsync(
                fixture.Manage, credentialId, transaction.TransactionId, "287082", "359152")).Succeeded);
            Assert.Equal(3UL, await fixture.Store.ExecuteAsync("alice", state => state.Credentials.Single().NextCounter));
        }
    }

    /// <summary>
    /// 受控双码匹配覆盖歧义分支，验证任意两组匹配均拒绝且仍扫描完整窗口。
    /// </summary>
    /// <param name="firstMatch">第一组匹配对的起始计数器。</param>
    /// <param name="secondMatch">第二组匹配对的起始计数器。</param>
    [Theory]
    [InlineData(1UL, 2UL)]
    [InlineData(1UL, 101UL)]
    [InlineData(100UL, 101UL)]
    public void Resynchronize_WhenMultiplePairsMatch_ShouldRejectAmbiguity(ulong firstMatch, ulong secondMatch)
    {
        var visited = new List<ulong>();
        var counter = OtpService.FindHotpResynchronizationCounter(1, 100, candidate =>
        {
            visited.Add(candidate);
            return candidate == firstMatch || candidate == secondMatch;
        });

        Assert.Null(counter);
        Assert.Equal(Enumerable.Range(1, 101).Select(value => (ulong)value), visited);
    }

    /// <summary>
    /// 双码可以结束于 ulong 最大值，但不能跨上限回绕到计数器零。
    /// </summary>
    /// <param name="start">持久化的下一个计数器。</param>
    /// <param name="firstCode">起点的固定验证码。</param>
    /// <param name="secondCode">最大值或回绕到零的固定验证码。</param>
    /// <param name="succeeds">双码是否能在无符号计数器范围内完成。</param>
    [Theory]
    [InlineData(ulong.MaxValue - 1, "488204", "094451", true)]
    [InlineData(ulong.MaxValue, "094451", "755224", false)]
    public async Task Resynchronize_WhenPairReachesCounterMaximum_ShouldNeverWrap(
        ulong start, string firstCode, string secondCode, bool succeeds)
    {
        var (fixture, credentialId) = await CreateFixedCredentialAsync();
        await fixture.Store.ExecuteAsync("alice", state => state.Credentials.Single().NextCounter = start);
        var transaction = await fixture.Service.BeginVerificationAsync(
            fixture.Manage, OtpTransactionKind.Resynchronization, credentialId);

        var result = await fixture.Service.ResynchronizeHotpAsync(
            fixture.Manage, credentialId, transaction.TransactionId, firstCode, secondCode);

        Assert.Equal(succeeds ? OtpVerifyStatus.Success : OtpVerifyStatus.Invalid, result.Status);
        await fixture.Store.ExecuteAsync("alice", state =>
        {
            var credential = Assert.Single(state.Credentials);
            Assert.Equal(start, credential.NextCounter);
            Assert.Equal(succeeds, credential.CounterExhausted);
            Assert.Equal(succeeds, Assert.Single(state.Transactions).Consumed);
            return true;
        });

        var next = await fixture.Service.BeginVerificationAsync(fixture.Login);
        var nextResult = await fixture.Service.VerifyAsync(fixture.Login, credentialId, next.TransactionId, "094451");
        Assert.Equal(succeeds ? OtpVerifyStatus.CounterExhausted : OtpVerifyStatus.Success, nextResult.Status);
    }

    /// <summary>
    /// 使用复制保护器保存固定 RFC 密钥；所有预期码由独立 Python hmac/大端计数器预先计算。
    /// </summary>
    private static async Task<(LifecycleFixture Fixture, string CredentialId)> CreateFixedCredentialAsync()
    {
        var fixture = new LifecycleFixture();
        var enrollment = await fixture.Service.BeginEnrollmentAsync(
            fixture.Manage, new OtpParameters(OtpType.Hotp), "alice@example.test");
        await fixture.Store.ExecuteAsync("alice", state =>
        {
            // TestProtector 仅复制字节，因此这里保存的是测试用的固定明文，不用于生产保护器。
            Assert.Single(state.Credentials).ProtectedSecret = Encoding.ASCII.GetBytes("12345678901234567890");
            return true;
        });
        Assert.True((await fixture.Service.ConfirmEnrollmentAsync(
            fixture.Manage, enrollment.CredentialId, enrollment.TransactionId, "755224")).Succeeded);
        Assert.Equal(1UL, await fixture.Store.ExecuteAsync("alice", state => state.Credentials.Single().NextCounter));
        return (fixture, enrollment.CredentialId);
    }
}
