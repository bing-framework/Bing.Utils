# 健康 Redis 集成验收

2026-10-29 07:58（Asia/Shanghai），用户要求验证此前跳过的 10 项健康 Redis 集成测试。结果：**10 通过，0 失败，0 跳过**。已核对原始 [TRX 报告](evidence/healthy-redis-20261029.trx) 的 total/executed/passed 均为 10，notExecuted 为 0。

## 环境与影响范围

- 客户端：Windows，.NET SDK 8.0.425，net8.0 Release，StackExchange.Redis 2.13.1。
- 服务端：真实 Redis 7.2.11（Alpine 包 7.2.11-r0），临时独立 WSL 2 环境，standalone 模式，127.0.0.1:16379；关闭 RDB/AOF 持久化，仅用于本次测试。
- Docker Desktop 启动时因既有 dockerInference 文件无法访问而失败；没有重置 Docker 或删除其数据，改用临时 WSL 环境。下载的 [Alpine 官方 rootfs](https://dl-cdn.alpinelinux.org/alpine/v3.21/releases/x86_64/) 已与官方 SHA-256 对照，Redis 通过 Alpine 官方软件仓库安装。
- ChangedSource/Contracts/TFMs：无。只新增验证报告与原始结果，更新验收状态。RiskLevel=LOW；仅执行受影响的 L3 Redis 集成，不重复核心、Web、绘图、pack 或性能测试。
- 运行前 Windows 到该 Redis 的 PING 返回 PONG。测试过滤掉已有的“连接不可用”专项用例，确保本轮准确执行 10 项健康服务用例。

## 测试结果

| 测试 | 主要验收行为 | 结果 |
| --- | --- | --- |
| ReplaceAsync_SetsExpiryAndReplacesCurrentEntry | 替换当前记录、TTL、旧 ID 不消费新记录、消费后失效 | PASS |
| TryTakeAsync_ConcurrentCalls_ReturnsEntryExactlyOnce | 16 次并发取出恰有一次得到记录 | PASS |
| TryTakeAsync_AcrossIndependentMultiplexers_ReturnsEntryExactlyOnce | 两个独立客户端并发消费恰有一次得到记录 | PASS |
| ReplaceAsync_ExpiredEntryIsRemovedByRedis | Redis 真实 TTL 到期删除，约 3.053 秒完成断言 | PASS |
| ReplaceAsync_ConcurrentRefreshAndOldIdTake_PreservesReplacement | 刷新与旧 ID 消费竞争仍保留新挑战 | PASS |
| TryTakeAsync_DoesNotConsumeEntryForDifferentContext | 跨会话不能消费原挑战 | PASS |
| TryTakeAsync_RoundTripsSingleCharacterIdAcrossLuaHeaderOffset | 单字符 ID 的 Lua 偏移与摘要往返 | PASS |
| TryTakeAsync_UnknownProtocolIsPreservedAndReturnsStorageError | 未知协议报存储错误且记录保留 | PASS |
| TryTakeAsync_MalformedIdLengthIsPreservedAndReturnsStorageError | 畸形 ID 长度报存储错误且记录保留 | PASS |
| TryTakeAsync_RoundTripsMaximumBmpUtf8Fields | 64 个中文字符的 ID/密钥版本往返 | PASS |

VSTest 报告测试持续约 3 秒；TRX 运行窗口为 07:58:40.317～07:58:44.896。测试命令如下，连接变量仅在该 PowerShell 进程设置：

```powershell
$env:BING_CAPTCHA_REDIS = '127.0.0.1:16379,abortConnect=true,connectTimeout=5000,syncTimeout=5000'
dotnet test tests/Bing.Utils.Captcha.Redis.Tests.Integration/Bing.Utils.Captcha.Redis.Tests.Integration.csproj -c Release -f net8.0 --no-restore --filter 'FullyQualifiedName!~WhenRedisConnectionIsUnavailable' --logger 'trx;LogFileName=healthy-redis.trx' --results-directory ai_docs/tasks/2026-10-28-captcha-lifecycle/artifacts/redis-20261029 -v minimal
```

## 隔离、清理与候选

独立 luna_worker 只读确认：每例创建独立 GUID 前缀，并在 finally 中删除自身已知键，没有全库清理或通配删除。运行后 DBSIZE=0。随后执行 SHUTDOWN NOSAVE，核对临时 WSL 目录位于仓库 output 后移除本次新建的 BingCaptchaRedisTest20261029 分发；确认 16379 端口关闭、剩余 WSL 分发仅为原有 docker-desktop。日志及下载文件保留于忽略目录 output，不保留后台 Redis 服务。

源码 SHA-256：

- CaptchaRedisStore.cs：`7E91D6F964E7D9E495222867761E32FBAF82DC23544120155540CB3DBCD25BC1`
- CaptchaRedisStoreIntegrationTest.cs：`855C5B50EB0453A5540B15913CD0ED8BBA8D42E79E53121724E66BF7D6E3FF61`

本轮没有修改实现或测试代码。构建仍报告既有 System.IO.Hashing 9.0.12 对 net6/net7 的支持警告；这 10 项实际运行在 net8.0，未调整依赖或压制警告。

## 当前状态

健康 Redis 验收由 BLOCKED_EXTERNAL 更新为 PASS；未知协议保留的真实服务断言已通过。OPEN_ACTIONABLE=0。远端 CI 运行证据仍为 BLOCKED_EXTERNAL，本次本机 WSL 验证不冒充远端 CI。Redis 故障切换、数据回滚和跨区域多主仍遵循既定能力边界。

Completed：10/10 健康 Redis 验收、测试数据及临时服务清理。Open Actionable/Blocked Approval：无。Blocked External：远端 CI。其他原计划边界保持不变。Next Action=STOP，不因剩余 CI 证据缺失启动重复测试或修复。
