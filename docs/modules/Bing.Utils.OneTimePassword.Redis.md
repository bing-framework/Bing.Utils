# Bing.Utils.OneTimePassword.Redis

Bing.OneTimePassword.Redis.OtpRedisRateLimiter 使用固定 StackExchange.Redis 2.13.1，仅负责分布式限流。OTP 权威状态由 SQLite 事务保存。

```csharp
services.AddOtpRateLimiter(sp => new OtpRedisRateLimiter(
    sp.GetRequiredService<IConnectionMultiplexer>(), "my-app:otp:rate:v1:"));
```

宿主复用并释放连接，适配器不拥有连接。分区 SHA256 派生键，单键 Lua INCR，首次 PEXPIRE；后续请求不延长窗口。各维度独立计费，后续维度拒绝不退款，不与 SQLite 组成分布式事务。

超额 false，连接/脚本/协议故障 OtpStoreException，禁止自动降级。派发前取消不请求 Redis；派发后取消停止等待，已计费不退款，也不重试。

支持四个类库目标框架。固定依赖的 System.IO.Hashing 9.0.12 对 net6/net7 输出支持提示；保留用户指定 Redis 版本，实际多框架运行证据见执行记录。

源码 src/Bing.Utils.OneTimePassword.Redis/OtpRedisRateLimiter.cs；Redis.Tests 覆盖参数、故障与取消；Redis.Tests.Integration 覆盖真实并发、隔离、固定过期。BING_OTP_REDIS 缺失明确跳过，配置不可达必须失败。

OneTimePassword.Tests.Integration 另覆盖真实 Redis TCP 断连后的 Web 行为：TOTP/HOTP 均返回 503，不消费验证码或验证事务、不增加失败计数、不签发 MFA 会话。使用同一环境变量连接单节点 Redis，独立键前缀隔离数据，故障仅作用于测试自己的 TCP 通道。

```powershell
$env:BING_OTP_REDIS = '127.0.0.1:6379,abortConnect=true'
dotnet test tests/Bing.Utils.OneTimePassword.Redis.Tests.Integration -c Release
dotnet test tests/Bing.Utils.OneTimePassword.Tests.Integration -c Release
```
