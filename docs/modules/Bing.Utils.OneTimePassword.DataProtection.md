# Bing.Utils.OneTimePassword.DataProtection

Bing.OneTimePassword.DataProtection.DataProtectionOtpSecretProtector 引用 DataProtection.Abstractions 8.0.33 和核心包，支持四个类库目标框架。源码位于同名项目。

```csharp
services.AddDataProtection().SetApplicationName("MyApplication")
    .PersistKeysToFileSystem(new DirectoryInfo(keyDirectory))
    .ProtectKeysWithCertificate(certificate);
services.AddOtpSecretProtector(sp =>
    new DataProtectionOtpSecretProtector(sp.GetRequiredService<IDataProtectionProvider>()));
```

宿主持久化并保护密钥环，可用证书或 Windows DPAPI。应用、账号、凭据、类型、算法、位数、周期分别作为用途链；密文替换或配置改写会解密失败。适配器借用提供器，不释放它，不调用网络。

轮换保护 key 不改变 OTP 共享密钥。保留旧 key 直到相关凭据重新保护或撤销；当前不提供批量重新保护管理 API。删除旧 key 会使旧凭据不可解密，不能作为常规轮换。备份恢复同时保留数据库和密钥环。

DataProtection.Tests.Integration 使用临时本地 key ring 测试上下文隔离、篡改/交换/未知 key、轮换和重建。测试中的未加密临时 key ring 不可直接用于生产。
