# Bing.Utils.Security

## 功能说明

`Bing.Utils.Security` 提供基于 .NET 平台密码学 API 的现代安全基础能力：SHA-2、HMAC、随机数、固定时间比较、HKDF、规范化参数，以及在较新目标框架上的 PBKDF2、AES-GCM、分块认证流、RSA/ECDSA、PEM、X.509 证书和版本化请求签名。

## 核心实现与关键文件

源码根目录为 `src/Bing.Utils.Security/Bing/Security`。

- 摘要与认证：`Hashing/Hashing.cs`、`Authentication/Hmac.cs`、`SecureComparison.cs`。
- 对称与混合加密：`Cryptography/AesGcmEncryption.cs`、`AesGcmStreamEncryption.cs`、`HybridEncryption.cs`、`RsaEncryption.cs`。
- 密码和密钥：`Passwords/Pbkdf2PasswordHasher.cs`、`Keys/Hkdf.cs`、`RsaKeyGenerator.cs`、`EcdsaKeyGenerator.cs`、`PemKeySerializer.cs`。
- 签名与请求规范化：`Signatures/RsaSignature.cs`、`EcdsaSignature.cs`、`Canonicalization/CanonicalParameterSerializer.cs`、`VersionedRequestSigner.cs`。
- 证书：`Certificates/CertificateLoader.cs`、`CertificateKeyReader.cs`、`CertificateFingerprint.cs`。
- 测试：`tests/Bing.Utils.Security.Tests`；详细协议、安全边界和迁移说明见 [安全专题](../security.md)。

## 对外接口与用法

```csharp
using System.Text;
using Bing.Security.Cryptography;
using Bing.Security.Keys;

byte[] key = AesKeyGenerator.Generate(AesKeySize.Size256);
byte[] data = Encoding.UTF8.GetBytes("payload");
var payload = AesGcmEncryption.Encrypt(data, key);
byte[] plaintext = AesGcmEncryption.Decrypt(payload, key);
```

该 AES-GCM 示例适用于本项目支持的 `.NET 6/7/8` 目标框架；`netstandard2.0` 不提供这些 API。

密码存储使用 `Pbkdf2PasswordHasher.Hash/Verify`；大文件使用 `AesGcmStreamEncryption`；请求签名优先使用 `VersionedRequestSigner`。

## 依赖与配置

- 主包无第三方密码学依赖；`netstandard2.0` 额外引用 `System.Memory 4.5.5`。
- 目标框架为 `net8.0;net7.0;net6.0;netstandard2.0`。
- PBKDF2 默认迭代次数为 310000，可通过 `Pbkdf2PasswordHasherOptions` 在允许范围内调整。
- 流式 AES-GCM 默认块大小为 65536 字节，可通过 `AesGcmStreamOptions` 调整。

| 能力 | `netstandard2.0` | `.NET 6/7/8` |
| --- | --- | --- |
| Hex/Base64Url、SHA-2、HMAC、随机数、固定时间比较、HKDF | 支持 | 支持 |
| 参数规范化、HMAC 请求签名 | 支持 | 支持 |
| AES-GCM 单块载荷 | 不支持 | 支持 |
| BSS2 流式 AES-GCM | 不支持 | 支持 |
| PBKDF2 密码散列 | 不支持 | 支持 |
| RSA/ECDSA 密钥、PEM、加密与签名 | 不支持 | 支持 |
| X.509 证书、版本化 RSA-PSS 请求签名 | 不支持 | 支持 |

## 已知限制与注意事项

- 只提供现代方案；ECB、CBC、MD5、SHA-1、DES、3DES 和 RSA PKCS#1 v1.5 不属于本包的新 API。
- AES-GCM Nonce 对同一密钥必须唯一；不要手工复用载荷字段。
- 版本化文本载荷限制单块密文为 16 MiB；更大内容应使用 BSS2 流格式。
- BSS2 是唯一支持的认证流格式，旧 BSS1 会被拒绝。
- 直接使用流解密 API 时，后续终止记录验证失败前，已通过认证的块可能已经写入输出；文件 API 使用临时文件避免替换有效目标。
- 库不能替代密钥安全存储、轮换、访问控制和审计；调用方仍需妥善清理和保护敏感数据。
- ZUC 和格式保留加密（FPE）尚未实现，状态为**待实现/待确认**。
