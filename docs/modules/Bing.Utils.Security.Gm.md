# Bing.Utils.Security.Gm

## 功能说明

`Bing.Utils.Security.Gm` 是独立的国密扩展包，提供 SM3、HMAC-SM3、SM4-GCM、SM2 密钥生成、C1C3C2 加解密、SM2withSM3 签名验签及 PEM 序列化。

## 核心实现与关键文件

源码根目录为 `src/Bing.Utils.Security.Gm/Bing/Security/Gm`。

- `Sm3.cs`、`HmacSm3.cs`：一次性、流式、异步摘要/认证码及编码辅助。
- `Sm4GcmEncryption.cs`、`Sm4GcmPayload.cs`：认证加密和 `BSM1` 版本化载荷。
- `Sm2KeyGenerator.cs`、内部 `Sm2KeySerializer.cs`、`Sm2KeyPair.cs`：`sm2p256v1` 密钥生成和 PKCS#8/SPKI PEM。
- `Sm2Encryption.cs`：固定 C1C3C2 排列的 SM2 加解密。
- `Sm2Signature.cs`：SM2withSM3 DER 签名；未指定时使用 GM/T 默认用户标识。
- 测试：`tests/Bing.Utils.Security.Gm.Tests`，包含固定向量、篡改和互操作场景。

## 对外接口与用法

```csharp
using System.Text;
using Bing.Security.Gm;

var keys = Sm2KeyGenerator.Generate();
byte[] data = Encoding.UTF8.GetBytes("payload");
byte[] encrypted = Sm2Encryption.Encrypt(data, keys.PublicKeyPem);
byte[] plaintext = Sm2Encryption.Decrypt(encrypted, keys.PrivateKeyPem);

string digest = Sm3.ComputeHex(data);
```

## 依赖与配置

- 依赖 `Bing.Utils.Security` 和 `BouncyCastle.Cryptography 2.6.2`。
- 公共签名只暴露 PEM、`byte[]` 和本包 DTO，不泄露 BouncyCastle 类型。
- `Sm2KeySerializer` 是内部实现；对外通过密钥生成、加解密和签名 API 消费或生成标准 PEM，不提供通用公共序列化器。
- 目标框架继承 `common.props`。

## 已知限制与注意事项

- SM2 只接受 `sm2p256v1`；公钥必须是单个 SPKI `PUBLIC KEY`，私钥必须是单个 PKCS#8 `PRIVATE KEY`。
- SM2 加密格式固定为 C1C3C2，不提供其他排列自动探测。
- SM2 签名使用 DER 编码；自定义用户标识长度限制为 1 至 8191 字节。
- SM4-GCM Nonce 不得在同一密钥下复用；认证失败会抛出密码学异常。
- `BSM1` 版本化载荷限制密文为 16 MiB；本包没有 SM4 分块流式容器。
- SM2 加解密和 SM4-GCM 目前仅提供内存载荷 API，没有流式或异步加解密接口。
- 国密能力依赖 BouncyCastle 的实现与平台兼容性，升级依赖时必须重跑标准向量和互操作测试。
