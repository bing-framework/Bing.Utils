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
| 对称认证加密 | AES-256-GCM、分块认证流 `BSS1` | 主包 |
| 密码派生与存储 | PBKDF2-HMAC-SHA256 | 主包 |
| 非对称加密与签名 | RSA-OAEP-SHA256、RSA-PSS-SHA256、ECDSA DER | 主包 |
| 密钥与证书 | RSA/ECDSA PEM、X.509/PFX、SHA-256 指纹 | 主包 |
| 请求签名 | Ordinal 参数规范化、HMAC/RSA-PSS/ECDSA | 主包 |
| 国密摘要与认证码 | SM3、HMAC-SM3 | 国密扩展 |
| 国密认证加密 | SM4-GCM | 国密扩展 |
| 国密非对称 | SM2 C1C3C2、SM2withSM3、PKCS#8/SPKI PEM | 国密扩展 |

## 安全边界

- 仅提供现代认证加密和签名方案；不提供 ECB、CBC、MD5、SHA-1、DES、3DES 或 RSA PKCS#1 v1.5 加密签名入口。
- AES-GCM 和 SM4-GCM 的 Nonce 必须对同一密钥唯一。库会为单次加密生成随机 Nonce；调用方不得跨密钥流程复用载荷字段。
- `BSS1` 认证流按块验证，密文头、块序号、块长度和终止记录均参与认证。解密文件先写入同目录临时文件，整个流验证完成后才替换目标文件。直接使用流 API 时，先前已通过认证的块可能在后续终止记录失败前写入输出。
- 密码散列仅用于验证，不可逆恢复。应按环境配置 PBKDF2 迭代次数，并在验证结果提示需要重新散列时升级存储值。
- 私钥、密码、派生密钥和明文均属于调用方敏感数据。库会清理内部临时缓冲区，但不能替代调用方的密钥轮换、访问控制、审计和安全存储。

## 与 Hutool Crypto 的定位差异

Hutool Crypto 面向 Java 生态并覆盖较宽的算法与编码兼容面。本模块面向 .NET，默认缩小算法面以降低误用风险：只提供明确的现代默认值和版本化认证载荷。需要与既有 Hutool 协议互操作时，应先固定算法、填充、字节顺序、字符编码、签名编码和载荷格式，再为该协议建立单独的互操作测试；不要通过放宽主包默认值来兼容历史弱方案。

## ZUC 与 FPE

当前未实现 ZUC 或格式保留加密（FPE）。ZUC 的使用场景、密钥管理与协议组合需要单独确认；FPE 需要明确 FF1/FF3-1、域大小、tweak 管理和合规要求。两者在完成标准向量、互操作和威胁建模前仅作为评估项，不应作为通用 API 加入。