# 转换模块修复验证记录

## 工作区与影响范围

- 日期：2026-09-26；Windows；分支 `codex/image-processing-refactor`。
- 基准 HEAD：`b87b706126eaa1f63001e2e89ca1a814b52b4b48`；本次针对其上已有未提交工作区进行增量修复，不代表该提交本身的测试结果。
- 生产变更：主包的进制、Base32、字节十六进制、二进制反转；Drawing 的 ICO；Drawing.Shared 的 RGB 边界。
- 行为契约：溢出及非法输入明确拒绝，32 进制按已批准的 Crockford 映射迁移。公开签名、依赖、目标框架、打包配置不变。风险等级 MEDIUM，主要风险为历史编码迁移。
- 测试归属按 `test-project-boundary` 及其 refs 文档执行：通用转换在主包、颜色和 ICO 参数在 Drawing、真实 ICO 编解码在 Drawing.Integration。未批量迁移旧测试；仅将旧测试中的非法 26 进制输入改为有效字母输入，以保持“同进制保留文本”的原断言意图。
- 保留已有 Http、图像处理等修改和历史测试记录；本轮不重跑无关 Http 用例。不做性能提升声明，不新增 Benchmark。

## 缺陷与回归对应

| 缺陷 | 主要测试 |
|---|---|
| 数值溢出 | ConversionRegressionTest.Radix_Overflow_IsRejected |
| Base32 非法输入 | ConversionRegressionTest.Base32_InvalidInput_IsFormatException；KnownVectors；AllBytesAndTailLengths |
| 十六进制字节边界丢失 | ConversionRegressionTest.Hex_ByteBoundaries_ArePreserved |
| 二进制反转补位 | ConversionRegressionTest.BinaryReverse_PreservesPartialByte |
| 字符集和零值 | ConversionRegressionTest.Radix_AllBasesAndStrategies_RoundTrip；CrockfordAndIdentity |
| ICO 字段截断 | ImageHelperIcoConversionIntegrationTest.ToIcoStream_256By256LargePng_WritesFullLengthOffsetAndDecodableFrame；ImageHelperConversionBoundaryTest |
| ICO 共享头并发竞争 | ImageHelperIcoConversionIntegrationTest.ToIcoStream_ConcurrentDifferentSizes_KeepIndependentHeaders |
| RGB 整数回绕 | ColorConvBoundaryTest |

ICO 并发用例提交 64 次不同尺寸转换，核对每次目录尺寸、长度、偏移及实际解码尺寸；这是一致性测试，不作为 64 路同时执行的吞吐或容量证明。旧实现并发用例可能通过，不能据此否定静态共享可变数组的数据竞争。

## 执行证据

独立结果目录：`TestResults/conversion-fixes/`。保留修复前 TRX，不用最终结果覆盖。通用转换修复前 31 项中 20 失败、11 通过；图像参数与 RGB 修复前 21 项中 18 失败、3 通过；ICO 修复前 2 项中 1 失败、1 通过。

最终矩阵（全部 Release；测试未跳过用例）：

| 范围 | 框架 | 通过 / 总数 | TRX |
|---|---|---|---|
| 主包新增精准回归 | net8.0 | 31 / 31 | core-after-net8.trx |
| 主包完整测试 | net8.0 | 6914 / 6914 | core-full-net8.trx |
| Drawing 完整测试（含复核后新增三组 HSL 断言） | net8.0 | 106 / 106 | Bing.Utils.Drawing.Tests-final-net8.trx |
| ImageSharp 完整测试 | net8.0 | 255 / 255 | Bing.Utils.Drawing.ImageSharp.Tests-final-net8.trx |
| SkiaSharp 完整测试 | net8.0 | 156 / 156 | Bing.Utils.Drawing.SkiaSharp.Tests-final-net8.trx |
| 图像集成完整测试 | net8.0 | 109 / 109 | integration-full-net8.trx |
| 旧 BingUtilsUT.ConvUT 转换用例 | net8.0 | 57 / 57 | legacy-conversion-net8.trx |
| 主包新旧转换用例 | net6.0 / net7.0 | 各 75 / 75 | core-net6.trx、core-net7.trx |
| Drawing 颜色与 ICO 参数用例 | net6.0 / net7.0 | 各 52 / 52 | drawing-net6.trx、drawing-net7.trx |
| ICO 编解码与并发 | net6.0 / net7.0 | 各 2 / 2 | ico-net6.trx、ico-net7.trx |

`Bing.Utils`、`Bing.Utils.Drawing.Shared`、`Bing.Utils.Drawing` 的 netstandard2.0 Release 编译均成功，日志为对应 `*-netstandard2-build.log`。本轮没有新增依赖，因此使用现有恢复结果 `--no-restore`。历史编译警告仍可能出现，不将零错误写成零警告。

可复用命令模板：

```powershell
dotnet test tests/<项目>/<项目>.csproj -c Release -f <框架> --no-restore --logger "trx;LogFileName=<唯一结果名>.trx" --results-directory TestResults/conversion-fixes
dotnet build src/<项目>/<项目>.csproj -c Release -f netstandard2.0 --no-restore
git diff --check
```

精准过滤：主包使用 `FullyQualifiedName~ConversionRegressionTest|FullyQualifiedName~ConversionsTests`；Drawing 使用 `FullyQualifiedName~ColorConv|FullyQualifiedName~ImageHelperConversionBoundaryTest`；ICO 使用 `FullyQualifiedName~ImageHelperIcoConversionIntegrationTest`；旧项目使用 `FullyQualifiedName~BingUtilsUT.ConvUT`。主包完整运行使用此前已构建的测试二进制（`--no-build`）；之后主包仅补充 XML 注释，其行为证据复用。图像最终完整测试重新构建并包含最终源码。

`drawing-full-net8.trx`（103 项）和 `image-after-net8.trx`（21 项）保留为复核前的阶段证据；最终 Drawing 结论以 106 项的新文件为准，不覆盖旧产物。

## 审查与终态

- 第一轮：通用转换独立审查 PASS；图像审查发现共享 `RgbToHsb` 有效输入缺少直接断言。
- 第二轮：补黑、白、绿色的 `[H,S,L]` 常量断言；独立复核 PASS，新增测试在 net8/net6/net7 均已运行通过。
- Implementation：8 / 8 CLOSED；OPEN_ACTIONABLE = 0；无待批准事项。
- 本地验证：L0 编译和差异检查、L1 精准回归、L2 受影响项目完整测试、L3 图像集成均通过。未运行无关全解决方案测试、Http 测试或性能实验。
- No-Progress Check：CHANGED（实现和回归证据已补齐）。下一动作 STOP；仅剩下述已明确的范围边界和外部环境验证，不启动额外修复循环。

## 兼容与外部边界

- [迁移说明](../modules/conversion-migration.md) 包含 32 进制历史映射、异常变化、零值及字节格式变化。
- 旧原生图像加载入口无统一限额是本轮已明确接受的范围边界；网络接收和 Base64 解码前仍需调用方限额。不宣称该资源风险已修复。
- .NET 5 / .NET Core 3.1 运行时及 Linux 验证环境缺失，记为外部验证阻塞；不升级目标框架，不以本地 Windows 测试替代 Linux 证据。
