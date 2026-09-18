# 密码安全审查 TODO

> 审查日期：2026-09-18  
> 审查范围：当前工作区（包含未提交改动）中的加密解密、Hash、国密、签名验签与密钥处理。  
> 说明：本文是实施清单，不代表列出的改动已经完成。

## 实施状态

- 已完成：SEC-001 至 SEC-010。
- 当前清单中的实现、测试、迁移文档、依赖门禁与性能基线均已落地。

## 审查基线

当前现代安全模块已经具备以下能力，不再重复列为待办：

- AES-GCM 单块载荷与 BSS2 分块流式加密，包含随机 Nonce、AAD、结束记录、乱序和截断检测。
- RSA-OAEP-SHA256 加密、RSA-PSS-SHA256 签名、ECDSA 签名。
- SHA-2、HMAC-SHA2、PBKDF2、HKDF 和固定时间 MAC 校验。
- SM2 密钥、加解密与签名验签，SM3，HMAC-SM3，以及 SM4-GCM。
- 版本化载荷解析、输入长度限制、篡改检测和关键临时字节数组清理。

审查时的测试结果：

| 测试项目 | 目标框架 | 每框架结果 |
| --- | --- | --- |
| `Bing.Utils.Security.Tests` | `net6.0`、`net7.0`、`net8.0` | 90 项通过，0 项失败 |
| `Bing.Utils.Security.Gm.Tests` | `net6.0`、`net7.0`、`net8.0` | 19 项通过，0 项失败 |

优先级：P1 表示公开接口存在高风险误用；P2 表示密钥安全、跨平台、互操作或供应链问题；P3 表示资源、契约、文档和性能维护问题。当前未发现需要按 P0 紧急停用或发布修复的事项。

## P1：遗留接口迁移

### [x] SEC-001 分阶段弃用弱算法和不安全默认值

- **涉及组件**：`Bing.Helpers.Encrypt`、`Bing.Helpers.Internal.RsaHelper`、`Bing.IO.FileHelper.Hash`。
- **源码证据**：[`Encrypt.cs`](../src/Bing.Utils/Bing/Helpers/Encrypt.cs#L103) 提供全局可变的默认 DES/3DES 密钥，使用 ECB 且没有认证标签；[`Encrypt.cs`](../src/Bing.Utils/Bing/Helpers/Encrypt.cs#L238) 的旧 AES 使用固定 IV 的 CBC/PKCS7；[`Encrypt.cs`](../src/Bing.Utils/Bing/Helpers/Encrypt.cs#L413) 继续公开 HMAC-MD5、HMAC-SHA1 和 SHA-1；[`RsaHelper.cs`](../src/Bing.Utils/Bing/Helpers/Internal/RsaHelper.cs#L50) 支持 SHA-1 签名和 RSA PKCS#1 v1.5 加解密；[`FileHelper.Hash.cs`](../src/Bing.Utils/Bing/IO/FileHelper.Hash.cs#L16) 公开 MD5、SHA-1 文件 Hash，未知算法回退到 MD5。
- **风险**：调用方可能把兼容算法用于保密、身份认证、口令处理或对抗性完整性保护；固定 IV 和 ECB 还会泄露重复明文模式。
- **实施建议**：当前主版本为弱算法入口增加带替代 API 的 `Obsolete`，保持二进制兼容；AES/DES 迁移到 AES-GCM，RSA 加密迁移到 OAEP-SHA256，签名迁移到 PSS-SHA256 或 ECDSA。MD5/SHA-1 仅保留既有非安全校验用途。下一主版本删除弱算法，或移入名称明确的兼容包。
- **验收条件**：弱算法公开成员均有编译期提示；旧密文、摘要和签名测试保持兼容；新文档和示例不再推荐弱算法；下一主版本移除范围形成 breaking-change 清单。
- **建议测试及归属**：弃用期兼容测试放在 `Bing.Utils.Tests`；现代算法迁移示例放在 `Bing.Utils.Security.Tests`。

### [x] SEC-002 为旧签名入口提供版本化迁移路径

- **涉及组件**：`Bing.Utils.Signatures.SignManager`、`SignKey`、规范化请求签名模块。
- **源码证据**：[`SignManager.cs`](../src/Bing.Utils/Bing/Utils/Signatures/SignManager.cs#L48) 直接调用 `Encrypt.Rsa2Sign/Rsa2Verify`，协议固定为 RSA PKCS#1 v1.5 + SHA-256；`SignKey` 以不可清零的 `string` 长期保存密钥材料。
- **风险**：直接替换算法会破坏跨系统验签，继续复用旧入口又会让新调用方固化旧协议；私钥字符串可能在托管堆中长期存活。
- **实施建议**：保留旧行为并标记弃用；新增明确版本的入口，复用 `CanonicalParameterSerializer` 与 `CanonicalRequestSigner`，默认 RSA-PSS-SHA256；协议显式记录版本和算法；新入口接收调用期密钥或可释放的密钥提供器。
- **验收条件**：旧签名向量仍可验证；新旧协议名称不可混淆；未知版本或降级算法被拒绝；新入口不长期缓存私钥字符串；迁移文档包含双验签、切换和停止接受旧签名的步骤。
- **建议测试及归属**：旧协议放在 `Bing.Utils.Tests`；新协议、篡改、降级和跨版本测试放在 `Bing.Utils.Security.Tests`；跨包端到端流程放入对应 `*.Integration` 项目。

## P2：密钥安全、互操作与平台正确性

### [x] SEC-003 消除 PEM 私钥校验产生的未清零副本

- **涉及组件**：`Bing.Security.Keys.PemKeySerializer`。
- **源码证据**：[`PemKeySerializer.cs`](../src/Bing.Utils.Security/Bing/Security/Keys/PemKeySerializer.cs#L46) 导入 RSA/EC 私钥后调用 `ExportParameters(true)` 校验，生成包含 `D`、`P`、`Q` 等字段的托管数组，但未清零这些副本。
- **风险**：扩大私钥材料在托管堆中的数量和存活时间，异常路径同样可能留下副本。
- **实施建议**：优先使用不导出私钥参数的方式验证导入结果；若兼容性要求必须导出，在 `finally` 中使用 `CryptographicOperations.ZeroMemory` 清理全部 RSA/EC 私钥数组。
- **验收条件**：正常和异常路径均清理私钥副本；公私钥、错误 PEM 和密钥类型不匹配的行为明确一致；不降低 `netstandard2.0` 兼容性。
- **建议测试及归属**：在 `Bing.Utils.Security.Tests` 覆盖 RSA/EC 公私钥、错误类型、损坏 PEM 和异常清理；通过内部可测试辅助逻辑验证清理，不依赖堆扫描。

### [x] SEC-004 补齐 HMAC-SM3 与 SM4-GCM 互操作向量

- **涉及组件**：`HmacSm3`、`Sm4GcmEncryption`、国密测试项目。
- **源码证据**：[`GmCryptographyTests.cs`](../tests/Bing.Utils.Security.Gm.Tests/Bing/Security/Gm/GmCryptographyTests.cs#L33) 对 HMAC-SM3 仅验证重复计算一致；同文件 [SM4-GCM 测试](../tests/Bing.Utils.Security.Gm.Tests/Bing/Security/Gm/GmCryptographyTests.cs#L47) 主要覆盖本实现往返、AAD 和篡改。SM2 已有 GM/T 0003 附录向量。
- **风险**：自洽测试无法发现参数顺序、Tag 拼接、Nonce 长度或编码约定与外部实现不一致。
- **实施建议**：选取公开标准或独立实现生成的固定向量，记录 key、message/plaintext、nonce、AAD、ciphertext、tag、来源版本和字节序。
- **验收条件**：同时验证正向结果、单字节篡改和错误 AAD/Tag；向量不得由被测实现生成；维护者可独立复现。
- **建议测试及归属**：全部放在 `Bing.Utils.Security.Gm.Tests`，并在三个目标框架上运行。

### [x] SEC-005 完善 SM3 与 HMAC-SM3 实用 API

- **涉及组件**：`Sm3`、`HmacSm3`。
- **源码证据**：[`Sm3.cs`](../src/Bing.Utils.Security.Gm/Bing/Security/Gm/Sm3.cs#L22) 和 [`HmacSm3.cs`](../src/Bing.Utils.Security.Gm/Bing/Security/Gm/HmacSm3.cs#L21) 仅提供 `byte[]` 一次性计算；主安全模块已有流式 Hash/HMAC 和固定时间验证模式。
- **风险**：大文件必须完整载入内存，调用方还可能使用普通序列比较 MAC。
- **实施建议**：按主安全模块约定增加 `Stream`/异步流计算、固定时间 `Verify`、Hex/Base64 辅助方法，并清理临时密钥和缓冲区。
- **验收条件**：一次性、流式和异步结果一致；`Verify` 固定时间比较；长度不匹配返回失败；取消不返回部分结果；参数、异常和空输入语义与主安全模块一致。
- **建议测试及归属**：在 `Bing.Utils.Security.Gm.Tests` 覆盖空流、大流、不可 Seek 流、取消、错误 MAC、编码往返和标准向量。

### [x] SEC-006 修正 AES-GCM 文件接口的跨平台同路径判断

- **涉及组件**：`AesGcmStreamEncryption` 文件加解密入口。
- **源码证据**：[`AesGcmStreamEncryption.cs`](../src/Bing.Utils.Security/Bing/Security/Cryptography/AesGcmStreamEncryption.cs#L177) 无条件使用 `StringComparison.OrdinalIgnoreCase` 比较完整路径。
- **风险**：在区分大小写的 Unix 文件系统中，仅大小写不同的独立源文件和目标文件会被误判为同一文件。
- **实施建议**：根据目标操作系统选择路径比较规则，继续阻止真实同路径覆盖输入，并保持临时文件和原子替换行为。
- **验收条件**：Windows 上大小写不同的同一路径仍被拒绝；区分大小写的 Linux/macOS 文件系统允许两个仅大小写不同的独立路径；覆盖真实同路径、归一化同路径和失败清理。
- **建议测试及归属**：在 `Bing.Utils.Security.Tests` 添加按操作系统断言的文件测试，由 Windows、Ubuntu、macOS CI 执行。

### [x] SEC-007 将 NuGet 漏洞检查纳入 CI 和发布门禁

- **涉及组件**：`.github/workflows/dotnet.yml`、安全模块依赖，重点包括 BouncyCastle。
- **源码证据**：[`.github/workflows/dotnet.yml`](../.github/workflows/dotnet.yml) 当前执行 restore、build、test 和 publish，但没有直接及传递依赖漏洞扫描；国密模块依赖 `BouncyCastle.Cryptography`。
- **风险**：密码库或传递依赖出现已披露漏洞时，发布流程不会自动提示或阻止发布。
- **实施建议**：verify 阶段执行 `dotnet list Bing.Utils.sln package --vulnerable --include-transitive` 并保存日志；高危或严重漏洞阻断发布，低危结果进入维护清单。
- **验收条件**：推送和手工工作流均扫描；高危或严重漏洞使 verify 失败且 publish 不运行；扫描失败和无漏洞在日志中明确区分；升级依赖后执行完整密码测试与互操作向量。
- **建议测试及归属**：使用 CI 语法检查和手工运行验证门禁；密码回归由两个安全测试项目承担。

## P3：一致性、资源和维护能力

### [x] SEC-008 释放遗留密码对象和流包装器

- **涉及组件**：`Encrypt` 中的 HMAC、`FileHelper.Hash` 中的 `BufferedStream`、`RsaHelper` 的 provider 生命周期。
- **源码证据**：[`Encrypt.cs`](../src/Bing.Utils/Bing/Helpers/Encrypt.cs#L429) 创建 HMAC 后未释放；[`FileHelper.Hash.cs`](../src/Bing.Utils/Bing/IO/FileHelper.Hash.cs#L54) 创建 `BufferedStream` 后未释放；`RsaHelper` 持有 RSA provider 但无明确释放契约。
- **风险**：高频调用会延迟释放密码对象和缓冲资源；包装流还可能意外改变调用方流的生命周期。
- **实施建议**：短生命周期对象使用 `using`；明确包装流的 `leaveOpen`；持有 RSA 实例的类型实现 `IDisposable` 并记录所有权。
- **验收条件**：正常和异常路径均释放内部对象；调用方流生命周期有明确契约和测试；旧 API 的计算结果不变。
- **建议测试及归属**：遗留行为和流生命周期放在 `Bing.Utils.Tests`；现代模块资源回归放在对应专用项目。

### [x] SEC-009 统一输入错误、认证失败和验签失败语义

- **涉及组件**：旧 `Encrypt/RsaHelper`、现代 RSA/ECDSA、SM2/SM4 和载荷解析接口。
- **源码证据**：旧接口混用空字符串、`false`、通用异常和底层密码异常；现代接口通常区分格式错误和认证失败，但尚无统一公开约定。
- **风险**：调用方容易把配置错误当作普通验签失败，或把攻击输入记录为系统故障；不一致语义增加迁移成本。
- **实施建议**：统一为参数/格式错误抛明确异常，合法格式但签名不匹配返回 `false`，认证解密失败抛 `CryptographicException`，解析接口成对提供 `Parse/TryParse`。
- **验收条件**：文档列出各失败类型；验签不吞掉编程或密钥配置错误；认证失败不暴露 padding、tag 或内部解析阶段细节。
- **建议测试及归属**：按模块放入三个对应测试项目，使用同一组失败场景表驱动验证契约。

### [x] SEC-010 完成迁移文档和可比较的性能基线

- **涉及组件**：`docs/security.md`、核心模块文档和安全 Benchmark。
- **源码证据**：已有 [`SecurityPrimitiveBenchmarks.cs`](../benchmarks/Bing.Utils.Benchmark/Benchmarks/SecurityPrimitiveBenchmarks.cs)、[`AesGcmStreamBenchmarks.cs`](../benchmarks/Bing.Utils.Benchmark/Benchmarks/AesGcmStreamBenchmarks.cs) 和 [`Sm2Benchmarks.cs`](../benchmarks/Bing.Utils.Benchmark/Benchmarks/Sm2Benchmarks.cs)，但还需固定环境、数据规模和回归判定；核心文档仍展示遗留入口。
- **风险**：调用方缺少可执行迁移路径；没有稳定基线时，现有 Benchmark 无法判断吞吐量或分配退化。
- **实施建议**：建立“旧 API → 推荐 API → 是否兼容旧数据”矩阵；固定 AES-GCM/BSS2、RSA、SM2、SM4 的数据规模、运行时和 BenchmarkDotNet 配置；记录吞吐量、分配量与大文件峰值内存，先人工评审再考虑 CI 门禁。
- **验收条件**：每个弃用入口都有替代方案和数据迁移说明；基线包含环境、均值、误差、分配和输入规模；后续结果可直接比较并有退化复核流程。
- **建议测试及归属**：文档链接由文档检查验证；性能数据由 `Bing.Utils.Benchmark` 维护，不在单元测试中断言易波动的绝对耗时。

## 公共接口与兼容策略

1. 当前主版本保留遗留 API 的二进制行为，通过 `Obsolete` 和迁移文档推动替换。
2. 新签名协议使用新名称和显式协议版本，不复用旧 `SignManager` 的协议标识。
3. 国密扩展 API 沿用 `Bing.Utils.Security` 的参数校验、固定时间比较、异步取消和内存清理约定。
4. 删除弱算法或改变签名线格式只在下一主版本执行，并提前发布 breaking-change 说明。

## 建议实施顺序

1. `SEC-001`、`SEC-002`：建立弃用边界和版本化迁移入口。
2. `SEC-003`、`SEC-004`、`SEC-006`、`SEC-007`：修复密钥副本、互操作、跨平台和供应链问题。
3. `SEC-005`：在行为契约稳定后扩展国密 Hash/HMAC API。
4. `SEC-008`、`SEC-009`、`SEC-010`：统一资源、失败语义、文档和性能基线。

## 完成定义

- 所有 P1、P2 项均有实现、文档和归属正确的测试；P3 项进入明确版本计划。
- 两个安全测试项目在 `net6.0`、`net7.0`、`net8.0` 全部通过。
- Windows、Ubuntu、macOS CI 全部通过，并执行直接与传递依赖漏洞检查。
- 新增和修改的源码、测试、文档均使用 UTF-8，中文注释与日志无乱码。
