# Bing.Utils.OneTimePassword

TOTP/HOTP 完整生命周期，命名空间 Bing.OneTimePassword，支持 net8.0/net7.0/net6.0/netstandard2.0。只依赖 Security 和 DiagnosticSource，不引入 Drawing 或 Bing.Utils 主包。

## API 和默认行为

| API | 行为 |
| --- | --- |
| Hotp.Generate / FindMatch | 生成或返回 [C,C+LookAhead] 的最高匹配 ulong |
| Totp.Generate / FindMatch | Unix 时间步及有界前后匹配 |
| OtpSecretEncoding | 严格 Base32，大写无填充输出，小写兼容输入 |
| OtpProvisioningUri.Create / Parse | 注册 URI、转义、issuer 一致性和参数校验 |
| BeginEnrollmentAsync / ConfirmEnrollmentAsync | 新密钥、待确认绑定、确认并消费首码 |
| BeginVerificationAsync / VerifyAsync | 创建绑定事务、原子验证消费 |
| GetCredentialsAsync / RevokeAsync | 脱敏设备列表、撤销清除密文 |
| ResynchronizeHotpAsync | 专用事务、连续两码、有界重同步 |

算法 FindMatch 只表示数学匹配，业务认证必须经 IOtpService。默认 SHA1/6 位；TOTP 30 秒前后一步，支持 SHA256/SHA512、8 位和 60 秒。HOTP 从 0 开始检查 [C,C+5]，SHA256/SHA512 属于需确认认证器兼容性的扩展。

每设备独立密钥 32 字节（SHA512 为 64 字节），默认 5 个有效、1 个待确认设备；绑定 10 分钟，事务 5 分钟。重新绑定替换旧待确认；密钥和 URI 只返回一次。确认消费首码，登录必须用新码。

TOTP LastAcceptedStep 全凭据生效，不按用途复用；HOTP 成功推进匹配值加一，ulong 最大值用后耗尽。重同步向前最多 100 偏移，两码必须唯一匹配。

事务最多 5 次失败；账号/凭据在 15 分钟内累计 5 次失败锁定 15 分钟；成功或新事务不清空当前失败计数。默认每分钟 IP 60、账号/凭据/事务各 10、管理操作 5，可配置。

## 接入示例

```csharp
using Bing.OneTimePassword;

// 由可信认证状态构建，不能从请求体直接绑定。
var management = new OtpContext(subjectId, "manage", serverSessionId,
    primaryAuthenticatedAt, trustedClientAddress);
var enrollment = await otp.BeginEnrollmentAsync(management,
    new OtpParameters(OtpType.Totp), accountDisplayName, "手机");
// 当前响应展示 SecretBase32/ProvisioningUri，禁止写日志。
var confirmed = await otp.ConfirmEnrollmentAsync(management,
    enrollment.CredentialId, enrollment.TransactionId, confirmationCode);

var login = new OtpContext(subjectId, "login", serverSessionId,
    clientAddress: trustedClientAddress);
var transaction = await otp.BeginVerificationAsync(login);
var verified = await otp.VerifyAsync(login, credentialId,
    transaction.TransactionId, nextCode);
if (verified.Succeeded)
{
    // 状态已持久提交，由宿主签发会话或批准敏感操作。
}
```

HOTP 重同步先使用 manage 上下文调用 BeginVerificationAsync(context, OtpTransactionKind.Resynchronization, credentialId)，再将句柄和连续两码传给 ResynchronizeHotpAsync。管理认证默认不得早于 5 分钟，OTP 成功不应续期主认证时间。

## 存储与资源

IOtpStore 在隔离写事务内读取、判定、算法匹配、推进因子、累计失败及消费事务。回调只能执行同步本地运算，不能调用网络；异常/提交前取消回滚，未知提交结果不自动重放。每应用独占存储命名空间；SQLite 每应用一个本地文件。

- [SQLite](Bing.Utils.OneTimePassword.Sqlite.md)
- [Data Protection](Bing.Utils.OneTimePassword.DataProtection.md)
- [Redis](Bing.Utils.OneTimePassword.Redis.md)
- [DI](Bing.Utils.OneTimePassword.DependencyInjection.md)
- [Web 示例](../../samples/OtpWeb/README.md)

OtpService 不拥有注入资源。MemoryOtpRateLimiter 显式用于单实例，固定窗口、默认容量 10000；满容量拒绝新分区，不淘汰有效额度。重启会重置内存限流，SQLite 失败锁定仍保留。

## 错误和指标

配置错误抛 ArgumentException，用户错误码返回失败。OtpVerifyResult 默认为 Invalid，仅 Success 的 Succeeded 为 true。外部隐藏 Replay/Locked/CounterExhausted，统一 otp_invalid；OtpRateLimitException 映射 429；OtpStoreException/OtpProtectionException 映射 503；OtpAuthorizationException 由宿主映射 403。

Meter 为 Bing.Utils.OneTimePassword，计数器 otp.events，固定标签 purpose、result、type、algorithm。用途来自配置白名单；不能把账号/凭据 ID、密钥、URI、验证码作为标签。诊断订阅者异常不改变已提交结果。临时密钥字节清理，敏感字符串生命周期由调用方管理，禁止结构化日志展开请求或注册响应。

核心源码在 src/Bing.Utils.OneTimePassword：Hotp.cs、Totp.cs、OtpService.cs、Contracts.cs、StoreModels.cs、OtpServiceOptions.cs。对应 Tests 项目包含算法/生命周期测试；[验收记录](../../ai_docs/tasks/2026-10-29-otp-lifecycle/execution.md)。

恢复码、Passkey、云 KMS、实际账号系统和真实认证器设备兼容性矩阵不属于本轮实现。
