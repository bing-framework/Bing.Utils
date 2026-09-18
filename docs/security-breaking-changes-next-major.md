# 下一主版本密码 API 移除清单

本文记录下一主版本计划删除或移入兼容包的遗留密码 API。当前主版本继续保持二进制行为，并通过 `Obsolete` 引导迁移。

| 移除范围 | 当前用途限制 | 推荐替代 | 数据兼容策略 |
| --- | --- | --- | --- |
| `Encrypt.Md5By16/Md5By32`、`FileHelper.GetMd5/GetSha1`、`StreamExtensions.GetMd5` | 仅允许既有非安全文件校验 | SHA-256/384/512 `Hashing` API | 摘要不可转换；迁移期按新旧摘要分别校验 |
| `Encrypt.HmacMd5/HmacSha1` | 不得用于新协议、签名或完整性保护 | HMAC-SHA256/384/512 | 协议字段显式升级，不允许静默替换 |
| `Encrypt.DesEncrypt/DesDecrypt`、`StringExtensions` 3DES 入口及 `DesKey` | 仅用于读取受控历史数据 | `AesGcmEncryption` 或 `AesGcmStreamEncryption` | 解密历史密文后重新加密，禁止继续签发旧密文 |
| `Encrypt.AesEncrypt/AesDecrypt` 及 `AesKey` | 固定 IV CBC 仅用于读取受控历史数据 | `AesGcmEncryption` | 旧密文没有认证标签，必须通过受控迁移任务转换 |
| `Encrypt.RsaEncrypt/RsaDecrypt` PKCS#1 v1.5 | 仅用于读取受控历史数据 | `RsaEncryption` OAEP-SHA256 | 新旧密文格式分流，不尝试自动降级 |
| `Encrypt.RsaSign/RsaVerify/Rsa2Sign/Rsa2Verify`、`SignManager`、`SignKey` | 仅用于验证和兼容旧协议 | `VersionedRequestSigner` 的 `BRS1.RSA-PSS-SHA256` | 先双验签，再切换签发端，最后停止接受旧签名 |
| 内部 `RsaHelper` 的 PKCS#1 v1.5 与 SHA-1 路径 | 仅由上述遗留入口调用 | RSA-OAEP-SHA256、RSA-PSS-SHA256 或 ECDSA | 随公开遗留入口一并删除或迁入兼容包 |

## 发布要求

1. 在主版本预览版发布说明中列出全部删除成员及替代 API。
2. 删除前至少保留一个完整弃用周期，并保持当前主版本的兼容测试。
3. 历史数据迁移工具必须显式选择旧格式，不允许现代 API 自动回退弱算法。
4. 若业务仍需读取旧数据，将实现移入名称明确、默认不引用的兼容包。
5. 移除后运行主库、安全模块和国密模块的完整测试，并重新生成 API 基线。