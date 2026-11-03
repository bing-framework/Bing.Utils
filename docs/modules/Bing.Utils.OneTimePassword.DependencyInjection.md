# Bing.Utils.OneTimePassword.DependencyInjection

扩展方法在 Microsoft.Extensions.DependencyInjection，依赖 DI Abstractions 8.0.2，支持四个类库框架。源码 src/Bing.Utils.OneTimePassword.DependencyInjection/OtpServiceCollectionExtensions.cs。

```csharp
services.AddBingOneTimePassword(new OtpServiceOptions
{ ApplicationId = "MyApplication", Issuer = "My Service" });
services.AddOtpStore(_ => new SqliteOtpStore(connectionString));
services.AddOtpSecretProtector(sp =>
    new DataProtectionOtpSecretProtector(sp.GetRequiredService<IDataProtectionProvider>()));
services.AddMemoryOtpRateLimiter(); // 显式单实例选择，多实例换 Redis。
// 生成宿主后、接收请求前：
await ((SqliteOtpStore)provider.GetRequiredService<IOtpStore>()).InitializeAsync();
_ = provider.GetRequiredService<IOtpService>();
```

配置快照在注册时冻结，默认使用 SystemOtpClock。启动解析要求存储/保护器/限流器/时钟每角色恰好一个，重复和缺失都拒绝。修改原 options 不改变运行配置。

AddOtpStore/AddOtpSecretProtector/AddOtpRateLimiter 使用单例工厂，其 IDisposable 资源由容器释放。宿主现有实例用 AddSingleton(instance) 注册并由宿主负责释放；OtpService 和适配器不主动释放借用对象。

本包不依赖 Hosting，宿主必须显式初始化数据库并解析服务。Web 示例在接收请求前完成。DependencyInjection.Tests 覆盖配置快照、唯一组件、资源所有权。
