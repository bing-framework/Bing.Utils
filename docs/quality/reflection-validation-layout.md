# 反射验证目录与基线

更新时间：2026-10-13

## 当前入口

`benchmarks` 仅保留性能入口；反射裁剪、NativeAOT、程序集加载和依赖图验证已集中到 `tests/Bing.Utils.Reflection.Tests.Integration/`。该集成目录包含 4 个验证宿主、独立模型/依赖夹具、运行时循环夹具、验证工具和基线，不是生产包依赖，也不是 BenchmarkDotNet 性能基准。

| 入口 | 职责 |
| --- | --- |
| `benchmarks/Bing.Utils.Benchmark` | 性能基准以及反射缓存、容量探针 |
| `benchmarks/ReflectionComparisonProbe` | 基线/候选程序集的性能比较 |
| `tests/Bing.Utils.Reflection.Tests.Integration/hosts/ReflectionTrimProbe` | 主反射契约、linker roots、trimming/NativeAOT smoke |
| `tests/Bing.Utils.Reflection.Tests.Integration/hosts/ReflectionTrimPluginLoadProbe` | path/resolver/bytes 外部加载与部署 |
| `tests/Bing.Utils.Reflection.Tests.Integration/hosts/ReflectionTrimThirdPartyPluginProbe` | 多版本、强名称冲突、依赖图与可回收上下文 |
| `tests/Bing.Utils.Reflection.Tests.Integration/hosts/ReflectionTrimProbe.UnrootedModelsHost` | 类型存在但成员未 root 的裁剪边界 |

## 基线已完成的整理

工作流默认读取以下四份当前配置，均位于 `tests/Bing.Utils.Reflection.Tests.Integration/baselines/`；手动运行仍可通过输入覆盖仓库责任清单与非仓库责任清单路径：

| 文件 | 用途 |
| --- | --- |
| `NativeAotWarningBaseline.txt` | 历史警告 ID 集合；CI 的 ID 严格检查沿用显式开关 |
| `NativeAotRepositoryDiagnosticBaseline.json` | 仓库诊断指纹及次数上限：363 项，合计 429 次 |
| `NativeAotRepositoryDiagnosticAllowlist.json` | 同一批 363 项的责任人与接受理由 |
| `NativeAotNonRepositoryDiagnosticOwnership.json` | 16 项生成物、依赖与工具链诊断归属 |

旧 339 项、405 次的基线和责任清单集中归档到当前目录的 `history/20261013-before-approval/`，保持内容不变。批准日期与原快照关系由当前文件的 `approval` 字段记录，不再用平铺的带日期文件名区分当前输入。目录迁移已完成；当前基线和 allowlist 中 74 条主探针 `Program.cs` 路径按新目录一一迁移，363 个诊断 key、429 次上限、消息指纹和上下文保持不变。提案中的 24 条 `file` 值保留批准时的旧路径，仅用于审批溯源。

更新流程：先修复生产侧真实契约问题，再逐项审阅剩余诊断；新的接受范围仍需明确批准。不得直接用候选报告覆盖冻结历史快照，也不得批量忽略探针的全部诊断。生成报告和符号映射继续存放在忽略的 `bin/` 中。

从仓库根目录执行。示例使用迁移后同一次发布的本机产物（未纳入 Git）；自行发布时，将前两个变量替换为同一次发布的实际日志与源码映射路径：

```powershell
$integrationRoot = 'tests/Bing.Utils.Reflection.Tests.Integration'
$publishLog = "$integrationRoot/hosts/ReflectionTrimProbe/bin/layout-migration-nativeaot-20261013.log"
$symbolMap = "$integrationRoot/hosts/ReflectionTrimProbe/bin/layout-migration-source-map-20261013.json"
$reportPath = "$integrationRoot/hosts/ReflectionTrimProbe/bin/layout-migration-warning-report-20261013.json"
$baselineDir = "$integrationRoot/baselines"
& "$integrationRoot/tools/Check-NativeAotWarnings.ps1" `
  -LogPath $publishLog -SymbolMapPath $symbolMap -OutputPath $reportPath `
  -DiagnosticContext 'sdk=8.0.425;tfm=net8.0;rid=win-x64;trim=partial;aot=true;mode=ci-single-publish' `
  -BaselinePath "$baselineDir/NativeAotWarningBaseline.txt" `
  -DiagnosticBaselinePath "$baselineDir/NativeAotRepositoryDiagnosticBaseline.json" `
  -DiagnosticAllowlistPath "$baselineDir/NativeAotRepositoryDiagnosticAllowlist.json" `
  -NonRepositoryOwnershipPath "$baselineDir/NativeAotNonRepositoryDiagnosticOwnership.json" `
  -FailOnNewWarning -FailOnNewRepositoryDiagnostic `
  -FailOnUnapprovedRepositoryDiagnostic -FailOnUnownedNonRepositoryDiagnostic `
  -FailOnUnparsedDiagnostic
```

日志与映射必须是同一次发布的实际产物。仓库指纹、仓库责任、非仓库责任与未解析门禁在 CI 默认开启；警告 ID 门禁保持原来的可选设置。记录的本机五项严格检查使用全部开关，不等同于远程工作流已执行。

## 为什么保留多个夹具

- `Models/PluginModels/TertiaryModels/QuaternaryModels/QuinaryModels` 验证跨程序集 linker root 组合；合并后会失去跨程序集边界。
- `UnrootedModels` 与独立宿主验证未保留成员的行为，不能把模型合入主探针而改变 root 集合。
- `NonTrimmablePlugin*` 的依赖变体、V2、diamond 与深层图故意复用程序集名或改变依赖边，分别部署到隔离目录验证解析结果。
- `StrongNameConflict*` 故意输出无对应强名称身份的 `System.Text.Json`，验证同名冲突。
- `RuntimeCycleFixtures` 通过分阶段 Roslyn 编译构造真实 AssemblyRef 循环；`CycleAStub.cs` 是必需的构建输入。

目前没有已跟踪的明确无引用源码。项目引用、CI 构建/staging 和自动发现的 benchmark 都有对应入口；多个相同 SDK 项目模板不代表重复能力。复杂依赖图与长轮次适合专项验证，是否降低执行频率取决于后续 CI 分层要求，本次没有删减已有覆盖。

## 已完成：验证源码目录迁移

验证源码迁移已完成。`benchmarks` 保留两个性能项目，验证宿主、夹具、工具和基线集中到独立的 `tests/Bing.Utils.Reflection.Tests.Integration/`。该目录承载控制台集成验证，不合入现有 xUnit 项目，也不新增空壳测试项目。

| 历史目录（仅溯源） | 当前目录 |
| --- | --- |
| `benchmarks/ReflectionTrimPluginLoadProbe` | `tests/Bing.Utils.Reflection.Tests.Integration/hosts/ReflectionTrimPluginLoadProbe` |
| `benchmarks/ReflectionTrimProbe` | `tests/Bing.Utils.Reflection.Tests.Integration/hosts/ReflectionTrimProbe` |
| `benchmarks/ReflectionTrimProbe.Models` | `tests/Bing.Utils.Reflection.Tests.Integration/fixtures/ReflectionTrimProbe.Models` |
| `benchmarks/ReflectionTrimProbe.NonTrimmablePluginDeepDependency` | `tests/Bing.Utils.Reflection.Tests.Integration/fixtures/ReflectionTrimProbe.NonTrimmablePluginDeepDependency` |
| `benchmarks/ReflectionTrimProbe.NonTrimmablePluginDeepTransitiveDependencyVariant` | `tests/Bing.Utils.Reflection.Tests.Integration/fixtures/ReflectionTrimProbe.NonTrimmablePluginDeepTransitiveDependencyVariant` |
| `benchmarks/ReflectionTrimProbe.NonTrimmablePluginDependency` | `tests/Bing.Utils.Reflection.Tests.Integration/fixtures/ReflectionTrimProbe.NonTrimmablePluginDependency` |
| `benchmarks/ReflectionTrimProbe.NonTrimmablePluginDependencyV2` | `tests/Bing.Utils.Reflection.Tests.Integration/fixtures/ReflectionTrimProbe.NonTrimmablePluginDependencyV2` |
| `benchmarks/ReflectionTrimProbe.NonTrimmablePluginDependencyVariant` | `tests/Bing.Utils.Reflection.Tests.Integration/fixtures/ReflectionTrimProbe.NonTrimmablePluginDependencyVariant` |
| `benchmarks/ReflectionTrimProbe.NonTrimmablePluginDiamondModels` | `tests/Bing.Utils.Reflection.Tests.Integration/fixtures/ReflectionTrimProbe.NonTrimmablePluginDiamondModels` |
| `benchmarks/ReflectionTrimProbe.NonTrimmablePluginModels` | `tests/Bing.Utils.Reflection.Tests.Integration/fixtures/ReflectionTrimProbe.NonTrimmablePluginModels` |
| `benchmarks/ReflectionTrimProbe.NonTrimmablePluginSecondaryDependency` | `tests/Bing.Utils.Reflection.Tests.Integration/fixtures/ReflectionTrimProbe.NonTrimmablePluginSecondaryDependency` |
| `benchmarks/ReflectionTrimProbe.NonTrimmablePluginTertiaryDependency` | `tests/Bing.Utils.Reflection.Tests.Integration/fixtures/ReflectionTrimProbe.NonTrimmablePluginTertiaryDependency` |
| `benchmarks/ReflectionTrimProbe.NonTrimmablePluginTransitiveDependency` | `tests/Bing.Utils.Reflection.Tests.Integration/fixtures/ReflectionTrimProbe.NonTrimmablePluginTransitiveDependency` |
| `benchmarks/ReflectionTrimProbe.NonTrimmablePluginTransitiveDependencyVariant` | `tests/Bing.Utils.Reflection.Tests.Integration/fixtures/ReflectionTrimProbe.NonTrimmablePluginTransitiveDependencyVariant` |
| `benchmarks/ReflectionTrimProbe.NonTrimmablePluginTripleDiamondModels` | `tests/Bing.Utils.Reflection.Tests.Integration/fixtures/ReflectionTrimProbe.NonTrimmablePluginTripleDiamondModels` |
| `benchmarks/ReflectionTrimProbe.PluginModels` | `tests/Bing.Utils.Reflection.Tests.Integration/fixtures/ReflectionTrimProbe.PluginModels` |
| `benchmarks/ReflectionTrimProbe.QuaternaryModels` | `tests/Bing.Utils.Reflection.Tests.Integration/fixtures/ReflectionTrimProbe.QuaternaryModels` |
| `benchmarks/ReflectionTrimProbe.QuinaryModels` | `tests/Bing.Utils.Reflection.Tests.Integration/fixtures/ReflectionTrimProbe.QuinaryModels` |
| `benchmarks/ReflectionTrimProbe.RuntimeCycleFixtures` | `tests/Bing.Utils.Reflection.Tests.Integration/fixtures/ReflectionTrimProbe.RuntimeCycleFixtures` |
| `benchmarks/ReflectionTrimProbe.StrongNameConflictDependency` | `tests/Bing.Utils.Reflection.Tests.Integration/fixtures/ReflectionTrimProbe.StrongNameConflictDependency` |
| `benchmarks/ReflectionTrimProbe.StrongNameConflictModels` | `tests/Bing.Utils.Reflection.Tests.Integration/fixtures/ReflectionTrimProbe.StrongNameConflictModels` |
| `benchmarks/ReflectionTrimProbe.TertiaryModels` | `tests/Bing.Utils.Reflection.Tests.Integration/fixtures/ReflectionTrimProbe.TertiaryModels` |
| `benchmarks/ReflectionTrimProbe.UnrootedModels` | `tests/Bing.Utils.Reflection.Tests.Integration/fixtures/ReflectionTrimProbe.UnrootedModels` |
| `benchmarks/ReflectionTrimProbe.UnrootedModelsHost` | `tests/Bing.Utils.Reflection.Tests.Integration/hosts/ReflectionTrimProbe.UnrootedModelsHost` |
| `benchmarks/ReflectionTrimThirdPartyPluginProbe` | `tests/Bing.Utils.Reflection.Tests.Integration/hosts/ReflectionTrimThirdPartyPluginProbe` |

工具已集中到 `tests/Bing.Utils.Reflection.Tests.Integration/tools/`，包括 `Build-NativeAotDiagnosticBaseline.ps1`、`Build-NativeAotSourceMap.ps1`、`Check-NativeAotWarnings.ps1`、`Test-Check-NativeAotWarnings.ps1`、`Stage-ExternalPlugin.ps1`、`Test-MultiPluginCapacity.ps1` 和 `Test-MultiPluginLongRounds.ps1`。循环夹具构建脚本与其 C# 输入一起保留在 `tests/Bing.Utils.Reflection.Tests.Integration/fixtures/ReflectionTrimProbe.RuntimeCycleFixtures/`。现有 `baselines/` 已整体迁移到 `tests/Bing.Utils.Reflection.Tests.Integration/baselines/`，历史快照内容不改。

迁移记录（已完成）：

1. 保持所有项目文件名、程序集名、命名空间、版本、root 类型和运行断言；20 个模型项目仍是独立程序集。
2. hosts 的生产源码引用已改为 `../../../../src/`，跨 hosts/fixtures 引用按新位置重算；CI、构建、staging、报告与上传路径已指向集成目录。
3. tools 中依赖仓库根的脚本使用 `../../..`；checker、自测与 builder 已集中到同一目录。循环夹具脚本继续从自身目录读取源码。
4. 当前基线有 74 项指向主探针 `Program.cs`，包含已批准的 24 项；当前基线和 allowlist 已按一一对应迁移路径更新，363 个 key 和 429 次上限未变。旧位置的批准诊断记录由提案和历史快照保留并标明映射。
5. 24 个项目构建、checker 自测、主探针、循环夹具、framework 插件部署 smoke、NativeAOT 发布和五项严格门禁已通过；85 个 workflow PowerShell 块静态解析通过，详见 [迁移后证据](reflection-validation-evidence.md)。
6. 旧目录中的 bin/obj 等本机构建产物未复制到新源码目录；历史日志和快照路径仅用于溯源，不作为当前入口。

当前状态：目录、工具和基线路径已完成迁移；当前 baseline/allowlist 保持 363 个 key 和 429 次上限，历史快照内容未改。本机迁移验证通过，未重跑完整裁剪插件矩阵及压力验证，远程 CI 待确认。接受范围见 [批准记录](reflection-nativeaot-baseline-review.md)。
