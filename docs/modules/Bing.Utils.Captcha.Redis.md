# Bing.Utils.Captcha.Redis

实际实现：[CaptchaRedisStore](../../src/Bing.Utils.Captcha.Redis/CaptchaRedisStore.cs)。固定 StackExchange.Redis 2.13.1，复用宿主 IConnectionMultiplexer，不释放外部连接。

```csharp
using Bing.Captcha.Redis;

services.AddCaptchaStore(sp => new CaptchaRedisStore(
    sp.GetRequiredService<StackExchange.Redis.IConnectionMultiplexer>(),
    keyPrefix: "myapp:production:captcha:v1:"));
```

前缀区分应用和环境；上下文摘要构成单个键。值仅保存 ID、摘要、过期时间和策略/密钥版本，采用有界二进制协议。Redis 6.2+ 支持所需绝对毫秒过期写入；单键 Lua 在主节点比较 ID 并消费。无需应用端扫描、额外状态缓存或分布式锁。

依赖行为依据 [Redis SET](https://redis.io/docs/latest/commands/set/) 与 [StackExchange.Redis 连接复用说明](https://seredis.dev/Basics.html)。2.13.1 的传递依赖 System.IO.Hashing 9.0.12 在 net6/net7 构建时发出支持警告，本仓库保留该警告；本地运行证据与实际 Redis 环境验收分别报告。

派发前取消不调用 Redis；派发后的取消/超时不能说明操作未执行，客户端应重新发放挑战，不自动重试写入。宿主负责共享连接配置与 Redis 可用性，不回退到内存。故障切换和异步复制回滚不在持久一次性承诺内。

真实测试配置 `BING_CAPTCHA_REDIS`。未配置显式跳过，配置错误必须失败。测试只访问随机唯一前缀，结束时删除自身已知键。CI 使用独立 Redis 7 容器；实际运行结果见 [执行记录](../../ai_docs/tasks/2026-10-28-captcha-lifecycle/execution.md)。
