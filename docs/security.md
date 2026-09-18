# 安全与密码学

`Bing.Utils.Security` 提供现代密码学基础能力；`Bing.Utils.Security.Gm` 提供隔离的国密扩展。两个包均支持 `netstandard2.0`、`.NET 6`、`.NET 7` 和 `.NET 8`，测试项目覆盖 `.NET 6`、`.NET 7` 和 `.NET 8`。

## 包与依赖

| 包 | 职责 | 直接第三方密码学依赖 |
| --- | --- | --- |
| `Bing.Utils.Security` | SHA-2、HMAC、AES-GCM、PBKDF2、RSA、ECDSA、PEM、证书、规范化签名、认证流 | 无 |
| `Bing.Utils.Security.Gm` | SM3、HMAC-SM3、SM4-GCM、SM2 C1C3C2、SM2withSM3、SM2 PEM | `BouncyCastle.Cryptography 2.6.2` |

主包仅使用 .NET 平台密码学 API。国密扩展通过 PEM、`byte[]` 和本包 DTO 暴露功能，不在公共签名中泄露 BouncyCastle 类型。

## 支持矩阵

| 能力 | 算法或格式 | 包 |
| --- | --- | --- |
| 摘要与认证码 | SHA-256/384/512、HMAC-SHA-2 | 主包 |
| 对称认证加密 | AES-256-GCM、分块认证流 `BSS2` | 主包 |
| 密码派生与存储 | PBKDF2-HMAC-SHA256 | 主包 |
| 非对称加密与签名 | RSA-OAEP-SHA256、RSA-PSS-SHA256、ECDSA DER | 主包 |
| 密钥与证书 | RSA/ECDSA PEM、X.509/PFX、SHA-256 指纹 | 主包 |
| 请求签名 | Ordinal 参数规范化、HMAC/RSA-PSS/ECDSA | 主包 |
| 国密摘要与认证码 | SM3、HMAC-SM3 | 国密扩展 |
| 国密认证加密 | SM4-GCM | 国密扩展 |
| 国密非对称 | SM2 C1C3C2、SM2withSM3、PKCS#8/SPKI PEM | 国密扩展 |

## 安全边界

- 仅提供现代认证加密和签名方案；不提供 ECB、CBC、MD5、SHA-1、DES、3DES 或 RSA PKCS#1 v1.5 加密签名入口。
- AES-GCM 和 SM4-GCM 的 Nonce 必须对同一密钥唯一。库会为单次加密生成随机 Nonce；调用方不得跨密钥流程复用载荷字段。AES、混合加密和 SM4 负载独占字段字节；公开读取会得到只读视图或副本。
- `BSS2` 是默认的认证流写入和读取格式。它使用随机 32 字节文件盐经 HKDF-SHA256 派生每文件 AES-256 子密钥，并使用随机 8 字节 Nonce 前缀加 32 位块序号。固定头摘要、记录类型、块序号、块长度与终止记录的块数和总长度均作为 GCM AAD 认证。终止记录使用保留序号，尾随字节、截断、乱序、删除和插入记录会被拒绝。
- `BSS2` 是唯一受支持的认证流格式；`BSS1` 密文会被明确拒绝。解密文件先写入同目录临时文件，整个流验证完成后才替换目标文件。直接使用流 API 时，先前已通过认证的块可能在后续终止记录失败前写入输出。
- 参数规范化固定对键和值使用 RFC 3986 UTF-8 百分号编码，以原始键的 Ordinal 顺序排序，并以 `&` 与 `=` 连接。`DateTimeKind.Unspecified`、NaN 和无穷大值会被拒绝；日期时间统一为 UTC `O` 格式，布尔值为小写，浮点数使用 `R` 格式。请求签名 API 统一使用原始签名字节；HMAC 的十六进制和 Base64Url 文本验签分别使用显式入口。
- 密码散列仅用于验证，不可逆恢复。PBKDF2-HMAC-SHA256 的迭代次数限制为 100000 至 1000000，盐限制为 16 至 64 字节，哈希限制为 32 至 64 字节，待验证编码记录最长 512 个字符。超限或格式非法的记录会返回验证失败，不执行派生。
- 私钥、密码、派生密钥和明文均属于调用方敏感数据。库会清理内部临时缓冲区，但不能替代调用方的密钥轮换、访问控制、审计和安全存储。

## 版本化负载

- `BSP1`：AES-GCM 文本负载，包含魔数、版本、Nonce 长度、标签长度、密文长度、Nonce、密文和标签。解析限制密文为 16 MiB。
- `BSH1`：RSA-OAEP-SHA256 包装的 AES-GCM 混合负载。外层仅进行一次 Base64Url 编码，内嵌 AES-GCM 二进制载荷；解析会在解码前限制外部输入长度，并严格验证字段长度和尾随数据。
- `BSM1`：SM4-GCM 文本负载，字段结构与 `BSP1` 一致，包含版本、Nonce、密文和标签，解析限制密文为 16 MiB。

SM2 密钥仅接受 `sm2p256v1`。公钥必须是单个 SubjectPublicKeyInfo `PUBLIC KEY` PEM，私钥必须是单个 PKCS#8 `PRIVATE KEY` PEM；曲线点与私钥标量会在使用前验证。SM2 加密固定为 C1C3C2，签名固定为 SM2withSM3 DER，未传入用户标识时使用 GM/T 默认 `1234567812345678`，用户标识长度限制为 1 至 8191 字节。

`TryParse` 对格式、版本、长度和尾随数据返回 `false`；`Parse` 将同类输入报告为 `FormatException`。认证失败保持为 `CryptographicException`，输入流截断和非法结构保持为 `InvalidDataException`。

## 与 Hutool Crypto 的定位差异

Hutool Crypto 面向 Java 生态并覆盖较宽的算法与编码兼容面。本模块面向 .NET，默认缩小算法面以降低误用风险：只提供明确的现代默认值和版本化认证载荷。需要与既有 Hutool 协议互操作时，应先固定算法、填充、字节顺序、字符编码、签名编码和载荷格式，再为该协议建立单独的互操作测试；不要通过放宽主包默认值来兼容历史弱方案。

## ZUC 与 FPE

当前未实现 ZUC 或格式保留加密（FPE）。ZUC 的使用场景、密钥管理与协议组合需要单独确认；FPE 需要明确 FF1/FF3-1、域大小、tweak 管理和合规要求。两者在完成标准向量、互操作和威胁建模前仅作为评估项，不应作为通用 API 加入。
## 遗留密码 API 迁移

| 遗留入口 | 推荐入口 | 旧数据兼容性 |
| --- | --- | --- |
| `Encrypt.DesEncrypt/DesDecrypt`、`StringExtensions.EncryptToBytes/DecryptFromBytes` | `AesGcmEncryption`；大文件使用 `AesGcmStreamEncryption` | 不兼容；迁移期应先解密旧数据，再用版本化 AES-GCM 载荷重新加密 |
| `Encrypt.AesEncrypt/AesDecrypt` | `AesGcmEncryption` | 不兼容；旧 CBC 密文没有认证标签，必须显式迁移 |
| `Encrypt.Md5By16/Md5By32`、`FileHelper.GetMd5/GetSha1` | `Hashing` 的 SHA-256/384/512 | 摘要值不兼容；MD5/SHA-1 只保留既有非安全文件校验用途 |
| `Encrypt.HmacMd5/HmacSha1` | `Hmac` 的 HMAC-SHA256/384/512 | MAC 不兼容；协议双方应通过版本字段协商切换 |
| `Encrypt.RsaEncrypt/RsaDecrypt` 的 PKCS#1 v1.5 | `RsaEncryption` 的 OAEP-SHA256 | 密文不兼容；只对新数据使用 OAEP |
| `SignManager`、`Encrypt.RsaSign/Rsa2Sign` | `VersionedRequestSigner`（`BRS1.RSA-PSS-SHA256`） | 签名不兼容；迁移期使用旧、新协议双验签，签发端切换后再停止接受旧签名 |

遗留入口已标记为 `Obsolete`，当前主版本继续保留原计算结果。新签名协议必须显式携带协议版本和算法标识，不得在旧入口中静默更换算法。

下一主版本的删除范围和发布要求见 [密码 API 移除清单](security-breaking-changes-next-major.md)。

## 失败语义

- 参数为空、密钥格式错误、密钥用途不匹配或密钥强度不足：抛出 `ArgumentException` 或其派生类型。
- 签名格式合法但内容不匹配，以及可识别的畸形签名字节：`Verify` 返回 `false`。
- AEAD 密钥、AAD、Nonce 或认证标签不匹配：抛出 `CryptographicException`，错误消息不区分内部认证阶段。
- 版本化载荷的文本或结构解析：`Parse` 抛出格式异常，`TryParse` 返回 `false`。
- 不支持的协议版本、算法或曲线：抛出 `NotSupportedException` 或明确的 `ArgumentException`。

## 安全性能基线

安全基准位于 `Bing.Utils.Benchmark`，覆盖 SHA-256、HMAC-SHA256、AES-GCM、SM3、SM4-GCM、RSA、SM2 和 BSS2 流式加密。使用同一台机器和同一运行时执行：

```powershell
dotnet run -c Release --project benchmarks/Bing.Utils.Benchmark -- --filter *SecurityPrimitiveBenchmarks* *AesGcmStreamBenchmarks* *RsaSecurityBenchmarks* *Sm2Benchmarks* --exporters json
```

当前可比较结果见 [密码模块性能基线](security-benchmark-baseline.md)。全解决方案 NuGet 审计使用 [历史漏洞基线](../build/nuget-vulnerability-baseline.json) 记录既有债务，并阻止任何新增高危或严重漏洞；基线项修复后必须删除。评审时保存 BenchmarkDotNet 生成的环境信息、均值、误差、吞吐量和分配量。相同环境下任一关键场景吞吐下降超过 10% 或分配增加超过 15% 时，必须说明原因或回退改动；跨机器结果只用于趋势参考。
