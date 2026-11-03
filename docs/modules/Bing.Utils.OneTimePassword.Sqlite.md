# Bing.Utils.OneTimePassword.Sqlite

Bing.OneTimePassword.Sqlite.SqliteOtpStore 使用 Microsoft.Data.Sqlite 8.0.33，支持四个类库目标框架。源码 src/Bing.Utils.OneTimePassword.Sqlite/SqliteOtpStore.cs。

```csharp
var store = new SqliteOtpStore("Data Source=/local-private-data/otp.db");
await store.InitializeAsync(); // 显式启动初始化，未知版本失败。
```

schema v1 保存账号、凭据和事务。HOTP 用 8 字节大端 BLOB 保存完整 ulong；连接启用外键、WAL、synchronous=FULL，非延迟写事务将检查、解密匹配、因子更新、失败累计、事务消费一起提交。多连接/进程由数据库写锁协调，等待上限 5 秒，超时抛 OtpStoreException。

SQLite I/O 实际同步，Task 用于组合，不使用 Task.Run。回调不得返回 Task 或安排未完成异步工作；提交前观察到取消即回滚，未知提交结果不能自动重试。

宿主按需调用 CleanupAsync(DateTimeOffset.UtcNow, 256)，每次清理有界过期状态，保留有效凭据重放状态。空账号安全记录至少保留一天，覆盖配置允许的最大失败窗口。

每应用独占本地文件，限制权限并备份数据库和保护密钥环。禁止多主机共享网络文件系统。其他数据库实现必须保持 IOtpStore 原子性，不拆成独立 CAS。真实测试在 Sqlite.Tests.Integration，覆盖并发、重启、回滚、锁等待和损坏记录。
