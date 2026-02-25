# 测试重新归类规划（Test Reclassification Plan）

## 1. 背景与目标

### 1.1 背景
当前仓库已存在多模块测试项目（如 `Bing.Utils.Collections.Tests`、`Bing.Utils.DateTime.Tests`、`Bing.Utils.IdUtils.Tests`、`Bing.Utils.Http.Tests`、`Bing.Utils.Http.Tests.Integration`），但 `Bing.Utils.Tests` 与 `BingUtilsUT` 仍为多模块混合容器，导致边界不清晰、迁移难度高。  
[证据] `Bing.Utils.sln:30` `Bing.Utils.sln:73` `Bing.Utils.sln:83` `Bing.Utils.sln:85` `Bing.Utils.sln:99` `Bing.Utils.sln:101` `Bing.Utils.sln:103`

模板中的 `02-tests/*` 在当前仓库实际路径为 `tests/*`（解决方案项目引用均指向 `tests\...`）。  
[证据] `Bing.Utils.sln:30` `Bing.Utils.sln:73`

### 1.2 目标
1. 按模块边界重新归类测试类/测试方法
2. 将模块专属测试迁移到对应 `Bing.Utils.<Module>.Tests`
3. 保留主包/通用/跨模块测试在 `Bing.Utils.Tests`
4. 识别混合测试类并制定拆分方案
5. 给出 P0/P1/P2 可执行批次规划

### 1.3 范围
- **包含**：`tests/*`（对应模板 `02-tests/*`）
- **重点审计**：`Bing.Utils.Tests`
- **同时观察**：`BingUtilsUT`（历史综合测试容器）
- **默认不修改**：`src/*`
- **本次产物**：规划文档（不执行迁移）

---

## 2. 规则来源与执行原则

### 2.1 规则来源
- `AGENTS.md`
- `.agents/skills/test-project-boundary/SKILL.md`
- `.agents/skills/test-project-boundary/refs/test-project-boundary.md`

### 2.2 关键归属规则摘要（本次执行使用）
1. `Bing.Utils.Tests` 仅承载主包测试、通用/共享测试、真实跨模块测试
2. 模块专属测试优先放 `Bing.Utils.<Module>.Tests`
3. 按“主要断言目标”归属，而非按引用数量归属
4. 混合测试优先按测试方法粒度拆分
5. 迁移时保持断言语义与测试意图不变

[证据] `.agents/skills/test-project-boundary/refs/test-project-boundary.md:18` `.agents/skills/test-project-boundary/refs/test-project-boundary.md:37` `.agents/skills/test-project-boundary/refs/test-project-boundary.md:71` `.agents/skills/test-project-boundary/refs/test-project-boundary.md:123`

### 2.3 执行原则
- 先审计/规划，后迁移
- 优先已有专属测试项目的模块
- 混合项单独列出，不强行迁移
- 信息不足写 `TODO`

---

## 3. 当前测试项目分布概览（审计结果）

### 3.1 测试项目清单
| 测试项目 | 对应模块/用途 | 当前状态 | 备注 |
|---|---|---|---|
| `Bing.Utils.Tests` | 主包/通用（理想） | **重点审计** | 实际引用多个模块 `src` 项目 [证据] `tests/Bing.Utils.Tests/Bing.Utils.Tests.csproj:11` `tests/Bing.Utils.Tests/Bing.Utils.Tests.csproj:20` |
| `BingUtilsUT` | 历史综合测试容器 | **重点关注** | 同样引用多个模块 `src` 项目 [证据] `tests/BingUtilsUT/BingUtilsUT.csproj:10` `tests/BingUtilsUT/BingUtilsUT.csproj:19` |
| `Bing.Utils.Collections.Tests` | Collections 模块 | 已存在 | 单模块引用 [证据] `tests/Bing.Utils.Collections.Tests/Bing.Utils.Collections.Tests.csproj:11` |
| `Bing.Utils.DateTime.Tests` | DateTime 模块 | 已存在 | 单模块引用 [证据] `tests/Bing.Utils.DateTime.Tests/Bing.Utils.DateTime.Tests.csproj:11` |
| `Bing.Utils.IdUtils.Tests` | IdUtils 模块 | 已存在 | 单模块引用 [证据] `tests/Bing.Utils.IdUtils.Tests/Bing.Utils.IdUtils.Tests.csproj:11` |
| `Bing.Utils.Http.Tests` | Http 单元测试 | 已存在 | 单模块引用 [证据] `tests/Bing.Utils.Http.Tests/Bing.Utils.Http.Tests.csproj:11` |
| `Bing.Utils.Http.Tests.Integration` | Http 集成测试 | 已存在 | `.Integration` 分层明确 [证据] `tests/Bing.Utils.Http.Tests.Integration/Bing.Utils.Http.Tests.Integration.csproj:40` |
| `Bing.Utils.Drawing.ImageSharp.Tests` | Drawing.ImageSharp | 已存在 | 单模块引用 [证据] `tests/Bing.Utils.Drawing.ImageSharp.Tests/Bing.Utils.Drawing.ImageSharp.Tests.csproj:11` |
| `Bing.Utils.Drawing.SkiaSharp.Tests` | Drawing.SkiaSharp | 已存在 | 单模块引用 [证据] `tests/Bing.Utils.Drawing.SkiaSharp.Tests/Bing.Utils.Drawing.SkiaSharp.Tests.csproj:11` |
| `Bing.Utils.Text.Tests` | Text 模块 | **待确认** | 当前 `sln` 未见该项目 |
| `Bing.Utils.Reflection.Tests` | Reflection 模块 | **待确认** | 当前 `sln` 未见该项目 |

### 3.2 当前问题概览（初步）
- [x] `Bing.Utils.Tests` 中混入模块专属测试（`Id` / `TypeVisit` / `Colls` 等）  
  [证据] `tests/Bing.Utils.Tests/Bing/Helpers/IdTest.cs:7` `tests/Bing.Utils.Tests/Bing/Reflection/TypeVisitCreateInstancesContractTest.cs:6` `tests/Bing.Utils.Tests/Collections/CollsTests.cs:9`
- [x] `Bing/Text`、`Bing/Reflection` 目录内存在跨程序集同命名空间测试混装  
  [证据] `tests/Bing.Utils.Tests/Bing/Text/CaseFormatterTest.cs:6` `tests/Bing.Utils.Tests/Bing/Text/RegexJudgeTest.cs:6` `tests/Bing.Utils.Tests/Bing/Reflection/ReflectionsTest.cs:6`
- [x] 存在共享基类/Fixture 耦合（`TestBase`、`EnvSerial`）  
  [证据] `tests/Bing.Utils.Tests/Bing/Tests/TestBase.cs:6` `tests/Bing.Utils.Tests/Bing/Helpers/EnvTestCollection.cs:6`
- [x] 存在同名测试重复（不同目录/命名空间）  
  [证据] `tests/Bing.Utils.Tests/Bing/Helpers/ValidTest.cs:6` `tests/Bing.Utils.Tests/Helpers/ValidTest.cs:6`

---

## 4. 模块目标映射（Target Mapping）

| 模块（被测对象） | 目标测试项目 | 归属依据 | 备注 |
|---|---|---|---|
| `Bing.Utils`（主包） | `Bing.Utils.Tests` | 主包/通用/跨模块 | 保留 |
| `Bing.Utils.IdUtils` | `Bing.Utils.IdUtils.Tests` | 模块专属测试 | 已有项目 |
| `Bing.Utils.Collections` | `Bing.Utils.Collections.Tests` | 模块专属测试 | 已有项目 |
| `Bing.Utils.DateTime` | `Bing.Utils.DateTime.Tests` | 模块专属测试 | 已有项目 |
| `Bing.Utils.Http` | `Bing.Utils.Http.Tests` / `*.Integration` | 单元/集成分层 | 已有项目 |
| `Bing.Utils.Text` | `Bing.Utils.Text.Tests`（TODO） | 模块专属测试 | 当前未发现项目 |
| `Bing.Utils.Reflection` | `Bing.Utils.Reflection.Tests`（TODO） | 模块专属测试 | 当前未发现项目 |
| `Bing.Utils.Drawing` | `Bing.Utils.Drawing.Tests`（TODO） | 模块专属测试 | 当前仅有 ImageSharp/SkiaSharp 测试项目 |
| `Bing.Utils.Net` | `Bing.Utils.Net.Tests`（TODO）或暂留主项目 | 模块专属测试 | 当前未发现项目 |

---

## 5. 归类审计结果（按目标测试项目分组）

### 5.1 目标：`Bing.Utils.IdUtils.Tests`

#### 5.1.1 可直接迁移（文件级）
| 源文件路径 | 测试类 | 迁移依据（主要断言目标） | 备注 |
|---|---|---|---|
| `tests/Bing.Utils.Tests/Bing/Helpers/IdTest.cs` | `IdTest` | `Bing.Helpers.Id` 实现位于 `Bing.Utils.IdUtils` [证据] `tests/Bing.Utils.Tests/Bing/Helpers/IdTest.cs:7` `src/Bing.Utils.IdUtils/Bing/Helpers/Id.cs:9` | 目标项目已有同名 `IdTest`，需去重 [证据] `tests/Bing.Utils.IdUtils.Tests/Bing/Helpers/IdTest.cs:7` |
| `tests/Bing.Utils.Tests/IdGenerators/SnowflakeIdGeneratorTest.cs` | `SnowflakeIdGeneratorTest` | 使用 `Bing.IdUtils`/`SnowflakeGenerator` [证据] `tests/Bing.Utils.Tests/IdGenerators/SnowflakeIdGeneratorTest.cs:4` | 高置信度 |
| `tests/Bing.Utils.Tests/IdGenerators/ObjectIdGeneratorTest.cs` | `ObjectIdGeneratorTest` | 调用 `Id.CreateObjectId()`，定义在 `IdUtils` [证据] `src/Bing.Utils.IdUtils/Bing/Helpers/Id.cs:131` | 高置信度 |
| `tests/Bing.Utils.Tests/IdGenerators/TimestampIdGeneratorTest.cs` | `TimestampIdGeneratorTest` | 调用 `Id.CreateTimestampId()`，定义在 `IdUtils` [证据] `src/Bing.Utils.IdUtils/Bing/Helpers/Id.cs:141` | 高置信度 |
| `tests/BingUtilsUT/IdUtilsUT/*.cs` | `GuidTest`/`RandomIdTest`/`TraceIdTest` | 明确使用 `Bing.IdUtils` [证据] `tests/BingUtilsUT/IdUtilsUT/GuidTest.cs:7` `tests/BingUtilsUT/IdUtilsUT/RandomIdTest.cs:7` `tests/BingUtilsUT/IdUtilsUT/TraceIdTest.cs:7` | 历史风格，迁移后需规范命名 |

#### 5.1.2 混合测试（需拆分）
| 源文件路径 | 测试类 | 涉及模块 | 建议拆分方式 | 风险 |
|---|---|---|---|---|
| `tests/Bing.Utils.Tests/Bing/Helpers/CheckAndConvEdgeContractTest.cs` | `CheckAndConvEdgeContractTest` | `Bing.Utils`（`Check/Conv`）+ 可能涉及 `Id` 转换路径 | 方法级拆分（先审查） | 文件名已提示混合语义 [证据] `tests/Bing.Utils.Tests/Bing/Helpers/CheckAndConvEdgeContractTest.cs:6` |
| `tests/BingUtilsUT/ConvUT/*.cs` | 多类 | 主包 `Conv` 为主，可能夹带 `Id` 场景 | 逐文件判定 | 老测试风格、耦合高 |

#### 5.1.3 待确认 / 应保留
- `tests/Bing.Utils.Tests/Bing/Date/NoRepeatTimeStampFactoryTest.cs`：名称含时间戳，但实现位于主包 `Bing.Utils` 日期域，不应误迁 `IdUtils`。  
  [证据] `tests/Bing.Utils.Tests/Bing/Date/NoRepeatTimeStampFactoryTest.cs:8` `src/Bing.Utils/Bing/Date/TimeStamp.NoRepeatFactory.cs:14`

#### 5.1.4 状态更新（2026-02-25，P0 批次）
- `Bing.Utils.Tests -> Bing.Utils.IdUtils.Tests` 的文件级直迁项已完成上一轮处理（`IdGenerators/*` 已迁移；`Bing.Helpers.IdTest` 已去重删除）。
- `UnitTest1.cs` 中的 `IdUtils` 混合方法 `Test_Id` 已在 Mixed Split 子批次拆分至 `Bing.Utils.IdUtils.Tests`（保留原方法名与行为意图）。  
  [证据] `tests/Bing.Utils.Tests/UnitTest1.cs:125` `tests/Bing.Utils.IdUtils.Tests/Bing/IdUtils/Legacy/UnitTest1IdUtilsMixedSplitTest.cs:16`
- `tests/Bing.Utils.Tests/Bing/Helpers/CheckAndConvEdgeContractTest.cs` 经方法级复审后，当前未发现明确 `IdUtils` API 调用；不再作为 `IdUtils` 混合拆分目标，转入主包/Conv 边界专项。  
  [证据] `tests/Bing.Utils.Tests/Bing/Helpers/CheckAndConvEdgeContractTest.cs:6` `tests/Bing.Utils.Tests/Bing/Helpers/CheckAndConvEdgeContractTest.cs:8`
- `IdUtils` Legacy 去重状态更新（2026-02-25）：
  - 已删除 `tests/Bing.Utils.IdUtils.Tests/Bing/Helpers/Legacy/ObjectIdGeneratorTest.cs`
  - 已删除 `tests/Bing.Utils.IdUtils.Tests/Bing/Helpers/Legacy/TimestampIdGeneratorTest.cs`
  - 已删除 `tests/Bing.Utils.IdUtils.Tests/Bing/Helpers/Legacy/SnowflakeIdGeneratorTest.cs`（采用“删除 + 替代覆盖验证”策略）
- 结论：`IdUtils` 在 `Bing.Utils.Tests` 范围内的文件级直迁已收敛；后续重点转向 `Legacy` 重复覆盖去重与跨模块混合专项。

---

### 5.2 目标：`Bing.Utils.Text.Tests`（当前未发现项目，待确认/建议新增）

#### 5.2.1 可直接迁移（文件级）
| 源文件路径 | 测试类（示例） | 迁移依据 | 备注 |
|---|---|---|---|
| `tests/Bing.Utils.Tests/Bing/Text/CaseFormatter*.cs` | `CaseFormatter*` | `CaseFormatter` 位于 `Bing.Utils.Text` [证据] `tests/Bing.Utils.Tests/Bing/Text/CaseFormatterTest.cs:6` `src/Bing.Utils.Text/Bing/Text/CaseFormatter.cs:10` | 建议整组迁移 |
| `tests/Bing.Utils.Tests/Bing/Text/JoinerTest.cs` | `JoinerTest` | `Joiner` 位于 `Bing.Utils.Text` [证据] `src/Bing.Utils.Text/Bing/Text/Joiners/Joiner.cs:8` | 可直接迁移 |
| `tests/Bing.Utils.Tests/Bing/Text/Splitter*.cs` | `Splitter*` | `Splitter` 位于 `Bing.Utils.Text` [证据] `tests/Bing.Utils.Tests/Bing/Text/SplitterTest.cs:8` `src/Bing.Utils.Text/Bing/Text/Splitters/Splitter.cs:8` | 含边界/回归测试 |
| `tests/Bing.Utils.Tests/Bing/Text/StringTruncationTest.cs` | `StringTruncationTest` | `StringTruncateExtensions` 位于 `Bing.Utils.Text` [证据] `tests/Bing.Utils.Tests/Bing/Text/StringTruncationTest.cs:7` `src/Bing.Utils.Text/Bing/Text/Truncation/StringTruncators.cs:35` | 可直接迁移 |
| `tests/BingUtilsUT/SplitterUT/*.cs` | `SplitterUT` 多类 | 明确调用 `Splitter` [证据] `tests/BingUtilsUT/SplitterUT/MapSplitterTest.cs:17` | 历史综合项目分流候选 |

#### 5.2.2 混合测试（需拆分）
| 源文件路径 | 测试类/目录 | 涉及模块 | 建议 | 风险 |
|---|---|---|---|---|
| `tests/Bing.Utils.Tests/Bing/Text/*` | `Bing.Text` 多类 | `Bing.Utils.Text` + 主包 `Bing.Utils` 文本类型 | 先按文件分组迁移，再方法级拆分 | 同命名空间跨程序集并存 [证据] `src/Bing.Utils.Text/Bing/Text/CaseFormatter.cs:10` `src/Bing.Utils/Bing/Text/RegexJudge.cs:9` |
| `tests/BingUtilsUT/StringUT/*.cs` | `StringUT` 多类 | `Bing.Utils.Text` + 主包字符串扩展 | 目录级盘点后逐文件归属 | 老风格命名/注释不统一 |

#### 5.2.3 待确认 / 应保留
- `tests/Bing.Utils.Tests/Bing/Text/StrTest.cs`：`Bing.Text.Str` 当前实现位于主包 `Bing.Utils`，应暂保留主包测试。  
  [证据] `tests/Bing.Utils.Tests/Bing/Text/StrTest.cs:5` `src/Bing.Utils/Bing/Text/Str.cs:9`
- `tests/Bing.Utils.Tests/Bing/Text/RegexJudgeTest.cs` / `RegexPoolTest.cs` / `RegexConstTest.cs` / `DesensitizedHelperTest.cs`：对应实现位于主包 `Bing.Utils`，不应迁入 Text 子包测试。  
  [证据] `src/Bing.Utils/Bing/Text/RegexJudge.cs:9` `src/Bing.Utils/Bing/Text/RegularExpressions/RegexPool.cs:9` `src/Bing.Utils/Bing/Text/RegularExpressions/RegexConst.cs:10` `src/Bing.Utils/Bing/Text/DesensitizedHelper.cs:6`
- `tests/Bing.Utils.Tests/Text/Formatting/FormattedStringValueExtractor*.cs`：类型位于主包 `Bing.Utils` 文本命名空间，暂保留。  
  [证据] `tests/Bing.Utils.Tests/Text/Formatting/FormattedStringValueExtractorTest.cs:6` `src/Bing.Utils/Bing/Text/Formatting/FormattedStringValueExtractor.cs:15`

---

### 5.3 目标：`Bing.Utils.Collections.Tests`

#### 5.3.1 可直接迁移（文件级）
| 源文件路径 | 测试类 | 迁移依据 | 备注 |
|---|---|---|---|
| `tests/Bing.Utils.Tests/Collections/CollsTests.cs` | `CollsTests` | `Colls` 位于 `Bing.Utils.Collections` [证据] `tests/Bing.Utils.Tests/Collections/CollsTests.cs:9` `src/Bing.Utils.Collections/Bing/Collections/Colls.cs:64` | 高置信度 |
| `tests/BingUtilsUT/CollUT/ArrayShortcutTests.cs` | `ArrayShortcutTests` | 调用 `ArraysShortcutExtensions`（Collections 模块） [证据] `tests/BingUtilsUT/CollUT/ArrayShortcutTests.cs:19` `src/Bing.Utils.Collections/Bing/Collections/ArraysShortcutExtensions.cs:17` | 可作为 P0/P1 试点 |

#### 5.3.2 混合 / 待确认
| 源文件路径 | 测试类 | 说明 | 建议 |
|---|---|---|---|
| `tests/Bing.Utils.Tests/Collections/EnumerableExtensionsTest.cs` | `EnumerableExtensionsTest` | 当前测试 `ChunkBy`，实现位于主包 `Bing.Utils` 而非 Collections 子包 [证据] `src/Bing.Utils/Bing/Collections/Extensions/Enumerable/Extensions.Enumerable.cs:19` | 保留 `Bing.Utils.Tests` |
| `tests/Bing.Utils.Tests/Bing/Collections/*.cs` | 多类 | `namespace Bing.Collections` 跨主包/子包实现并存 | 逐文件按被测 API 所在程序集判定 |
| `tests/BingUtilsUT/CollUT/ArrayCopyTests.cs` 等 | 多类 | 需逐文件确认具体被测类型 | TODO |

#### 5.3.3 应保留（示例）
- `tests/Bing.Utils.Tests/Bing/Collections/EqualityHelperTest.cs`：`EqualityHelper` 位于主包 `Bing.Utils`。  
  [证据] `tests/Bing.Utils.Tests/Bing/Collections/EqualityHelperTest.cs:6` `src/Bing.Utils/Bing/Collections/EqualityHelper.cs:12`

---

### 5.4 目标：`Bing.Utils.Reflection.Tests`（当前未发现项目，待确认/建议新增）

#### 5.4.1 可直接迁移（文件级）
| 源文件路径 | 测试类 | 迁移依据 | 备注 |
|---|---|---|---|
| `tests/Bing.Utils.Tests/Bing/Reflection/AssemblyVisitAndTypeVisitGuardTest.cs` | `AssemblyVisitAndTypeVisitGuardTest` | `AssemblyVisit`/`TypeVisit` 实现位于 `Bing.Utils.Reflection` [证据] `tests/Bing.Utils.Tests/Bing/Reflection/AssemblyVisitAndTypeVisitGuardTest.cs:6` `src/Bing.Utils.Reflection/Bing/Reflection/AssemblyVisit.cs:10` `src/Bing.Utils.Reflection/Bing/Reflection/TypeVisit/TypeVisit.CreateInstances.cs:10` | 高置信度 |
| `tests/Bing.Utils.Tests/Bing/Reflection/TypeVisit*.cs` | `TypeVisit*` 系列 | `TypeVisit` 位于 `Bing.Utils.Reflection` [证据] `tests/Bing.Utils.Tests/Bing/Reflection/TypeVisitCreateInstancesContractTest.cs:6` | 建议整组迁移 |
| `tests/Bing.Utils.Tests/Bing/Reflection/TypeMetaVisitExtensionsAdditionalContractTest.cs` | `TypeMetaVisitExtensionsAdditionalContractTest` | `TypeMetaVisitExtensions` 位于 `Bing.Utils.Reflection` [证据] `src/Bing.Utils.Reflection/Bing/Reflection/TypeVisit/TypeVisit.cs:38` | 高置信度 |
| `tests/BingUtilsUT/TypeUT/CreateInterfaceTest.cs` | `CreateInterfaceTest` | 明确调用 `TypeVisit.CreateInstance` [证据] `tests/BingUtilsUT/TypeUT/CreateInterfaceTest.cs:12` | 从 `TypeUT` 中拆出 |

#### 5.4.2 混合 / 待确认
| 源文件路径 | 测试类 | 说明 | 建议 |
|---|---|---|---|
| `tests/Bing.Utils.Tests/Bing/Reflection/ReflectionsTest.cs` | `ReflectionsTest` | `Reflections` 实现位于主包 `Bing.Utils` [证据] `src/Bing.Utils/Bing/Reflection/Reflections/Reflections.cs:10` | 保留主包测试 |
| `tests/Bing.Utils.Tests/Bing/Reflection/TypesAndTypeJudgeTest.cs` | `TypesAndTypeJudgeTest` | `Types`/`TypeJudge` 位于主包 `Bing.Utils` [证据] `src/Bing.Utils/Bing/Reflection/Types/Types.cs:8` `src/Bing.Utils/Bing/Reflection/TypeJudge.cs:6` | 保留主包测试 |
| `tests/BingUtilsUT/TypeUT/*.cs` | 多类 | `Types`（主包）与 `TypeVisit`（Reflection 子包）混装 | 先文件级拆分 |
| `tests/Bing.Utils.Tests/Extensions/Reflections/AssemblyExtensionsTest.cs` | `AssemblyExtensionsTest` | 需核对被测扩展实际所属程序集 | TODO |

---

### 5.5 目标：`Bing.Utils.DateTime.Tests`

#### 5.5.1 可直接迁移（文件级）
- `TODO`：当前重点样本显示 `Bing.Utils.Tests/Bing/Date/*` 多数对应主包 `Bing.Utils` 日期类型，暂未识别高置信度可直接迁移项。

#### 5.5.2 混合 / 待确认
- `tests/Bing.Utils.Tests/Extensions/Common/ExtensionsTest.DateTime.cs` 为 `partial` 类片段，需方法级拆分判断归属。  
  [证据] `tests/Bing.Utils.Tests/Extensions/Common/ExtensionsTest.DateTime.cs:11`

#### 5.5.3 应保留（示例）
- `DateTimeHelperTest` / `DateTimeRangeTest` / `TimeStampTest` / `NoRepeatTimeStampFactoryTest`：对应实现均位于主包 `Bing.Utils`。  
  [证据] `src/Bing.Utils/Bing/Date/DateTimeHelper.cs:6` `src/Bing.Utils/Bing/Date/DateTimeRange.cs:26` `src/Bing.Utils/Bing/Date/TimeStamp.cs:16` `src/Bing.Utils/Bing/Date/TimeStamp.NoRepeatFactory.cs:14`

---

### 5.6 目标：`Bing.Utils.Http.Tests` / `Bing.Utils.Http.Tests.Integration`

#### 5.6.1 可直接迁移（文件级）
| 源文件路径 | 测试类 | 迁移依据 | 备注 |
|---|---|---|---|
| `tests/Bing.Utils.Tests/Helpers/WebTest.cs` | `WebTest`（已注释） | 指向 `Web` helper；实现位于 `Bing.Utils.Http` [证据] `tests/Bing.Utils.Tests/Helpers/WebTest.cs:12` `src/Bing.Utils.Http/Bing/Helpers/Web.cs:17` | 先做历史禁用文件处理，不直接迁移；目标项目已有 `WebTest` [证据] `tests/Bing.Utils.Http.Tests/Bing/Helpers/WebTest.cs:11` |

#### 5.6.2 混合 / 待确认
- `tests/Bing.Utils.Tests/Bing/Helpers/UrlTest.cs` 与 `tests/Bing.Utils.Tests/Parameters/UrlParameterBuilderTest.cs` 虽然语义涉及 URL/HTTP，但实现位于主包 `Bing.Utils`，应保留主包测试。  
  [证据] `src/Bing.Utils/Bing/Helpers/Url.cs:9` `src/Bing.Utils/Bing/Utils/Parameters/UrlParameterBuilder.cs:10`
- `tests/Bing.Utils.Http.Tests.Integration/Http/*.cs` 已位于 `.Integration`，建议后续仅复审是否混入纯单元测试。  
  [证据] `tests/Bing.Utils.Http.Tests.Integration/Http/HttpClientServiceTest.Post.cs:9`

#### 5.6.3 应保留 / 非本模块项
- `tests/Bing.Utils.Tests/Net/FTP/FtpClientTest.cs` 实际是 `Bing.Utils.Net`，且依赖真实内网地址/凭据，不归 `Http`。  
  [证据] `tests/Bing.Utils.Tests/Net/FTP/FtpClientTest.cs:4` `tests/Bing.Utils.Tests/Net/FTP/FtpClientTest.cs:25` `src/Bing.Utils.Net/Bing/Net/FTP/FtpClient.cs:10`

---

### 5.7 目标：`Bing.Utils.Drawing*`

#### 5.7.1 可直接迁移 / 混合 / 待确认
| 源文件路径 | 测试类 | 判定 | 说明 |
|---|---|---|---|
| `tests/Bing.Utils.Tests/Drawing/CaptchaBuilderTest.cs` | `CaptchaBuilderTest` | 待确认 | 被测对象为基础 `Bing.Utils.Drawing`（非 ImageSharp/SkiaSharp），当前无 `Bing.Utils.Drawing.Tests` 项目 [证据] `tests/Bing.Utils.Tests/Drawing/CaptchaBuilderTest.cs:7` `src/Bing.Utils.Drawing/Bing/Drawing/CaptchaBuilder.cs:11` |
| `tests/Bing.Utils.Drawing.ImageSharp.Tests/*` | 多类 | 应保留 | 已在专属项目 |
| `tests/Bing.Utils.Drawing.SkiaSharp.Tests/*` | 多类 | 应保留 | 已在专属项目 |

---

## 6. `Bing.Utils.Tests` 保留项规划（通用 / 跨模块）

### 6.1 保留类别定义
- 主包（`Bing.Utils`）专属测试
- 通用工具/共享行为测试（不属于任何单一模块）
- 真实跨模块协同测试（且不适合拆分）
- 暂无法拆分的历史混合测试（需标注 TODO）

### 6.2 保留清单（示例）
| 文件路径 | 测试类 | 保留原因 | 后续动作 |
|---|---|---|---|
| `tests/Bing.Utils.Tests/Bing/Date/DateTimeHelperTest.cs` | `DateTimeHelperTest` | 主包日期工具 [证据] `src/Bing.Utils/Bing/Date/DateTimeHelper.cs:6` | 保留 |
| `tests/Bing.Utils.Tests/Bing/Text/StrTest.cs` | `StrTest` | `Bing.Text.Str` 实现在主包 [证据] `src/Bing.Utils/Bing/Text/Str.cs:9` | 保留 |
| `tests/Bing.Utils.Tests/Bing/Reflection/ReflectionsTest.cs` | `ReflectionsTest` | `Reflections` 在主包 [证据] `src/Bing.Utils/Bing/Reflection/Reflections/Reflections.cs:10` | 保留 |
| `tests/Bing.Utils.Tests/Collections/EnumerableExtensionsTest.cs` | `EnumerableExtensionsTest` | `ChunkBy` 在主包 [证据] `src/Bing.Utils/Bing/Collections/Extensions/Enumerable/Extensions.Enumerable.cs:19` | 保留 |
| `tests/Bing.Utils.Tests/Bing/Helpers/UrlTest.cs` | `UrlTest` | `Url` helper 在主包 [证据] `src/Bing.Utils/Bing/Helpers/Url.cs:9` | 保留 |
| `tests/Bing.Utils.Tests/Extensions/Common/ExtensionsTest.*.cs` | `ExtensionsTest`（partial） | 混合扩展测试，拆分成本高 | TODO：后续方法级拆分 |

---

## 7. 迁移批次规划（P0 / P1 / P2）

### 7.1 P0（优先执行）
**目标**：边界清晰、已有目标项目、容易判定、易于验证

**建议模块/范围**：
- `Bing.Utils.IdUtils`（已有专属测试项目）
- `Bing.Utils.Collections`（仅高置信度项）
- `BingUtilsUT/IdUtilsUT/*`（历史综合项目中的高置信度项）

#### P0 执行策略
1. 仅处理“可直接迁移”项
2. 不处理混合目录（`Bing/Text`、`Bing/Reflection`、`BingUtilsUT/TypeUT`、`StringUT`）
3. 先做重复测试比对（如 `IdTest`）
4. 迁移后做最小编译/测试验证

#### P0 子批次（状态）
- `IdUtils / Batch-1（试点+去重）`：已完成（见 `11.1`）
- `IdUtils / Batch-2（残余审计收尾）`：已完成（见 `11.2`；无新增直迁项）
- `IdUtils / Batch-3（混合测试拆分）`：已完成（`Mixed-Batch-1` 完成 `UnitTest1.Test_Id` 拆分；`Mixed-Batch-2` 完成收尾复审确认无剩余 `IdUtils` 方法）
- `IdUtils / Batch-4（Legacy 去重）`：已完成（`Batch-1/2` 已完成，`Bing/Helpers/Legacy` 生成器 Legacy 测试已清空）

#### P0 预期产物
- `Bing.Utils.IdUtils.Tests` 收敛 `IdUtils` 专属测试
- `Bing.Utils.Tests` 明显减少 `IdUtils` 混入项
- 形成重复测试清单与去重决策

### 7.2 P1（中期）
**目标**：处理中等复杂度模块与目录内混合场景

**建议模块**：
- `Bing.Utils.Text`（前提：确认/创建 `Bing.Utils.Text.Tests`）
- `Bing.Utils.Reflection`（前提：确认/创建 `Bing.Utils.Reflection.Tests`）
- `BingUtilsUT/SplitterUT/*`
- `Bing.Utils.Tests/Bing/Reflection/TypeVisit*` + `AssemblyVisit*`

#### P1 风险点
- 同命名空间跨程序集并存（`Bing.Text`、`Bing.Reflection`）
- 历史测试目录混装（`StringUT` / `TypeUT`）
- 新测试项目是否创建未决

### 7.3 P2（后期）
**目标**：协作复杂/环境依赖强/集成特征明显测试整理

**建议模块**：
- `Bing.Utils.Http`（复审 `*.Tests` 与 `*.Integration` 分层）
- `Bing.Utils.Drawing`（基础 Drawing 是否建专属测试项目）
- `Bing.Utils.Net`（如 `FtpClientTest`）
- `BingUtilsUT` 剩余混合目录（`TypeUT` / `StringUT` / `ConvUT`）

#### P2 特别注意
- 单元与集成分层保持稳定
- 避免依赖不稳定外部环境（FTP 内网/凭据）
- 先目录级分流，再文件级/方法级拆分

---

## 8. 执行步骤模板（供 Codex 每批次复用）

### 8.1 批次执行标准流程
1. 读取规则（`AGENTS.md` + Skill 规则文档）
2. 输出本批迁移清单（可直接迁移 / 混合 / 待确认）
3. 执行可直接迁移项
4. 做最小修复（命名空间 / using / csproj 引用 / 必要辅助类）
5. 运行最小验证（编译 / 测试）
6. 输出批次验证报告与 TODO

### 8.2 验证清单（每批必须检查）
- [ ] 目标测试项目编译通过
- [ ] 源测试项目编译通过
- [ ] 无重复测试残留（迁移后旧文件已清理）
- [ ] 命名空间与目录结构一致
- [ ] 无失效 using / 引用断裂
- [ ] 测试语义未改变（断言意图保持一致）
- [ ] 已检查目标项目中是否存在同名测试类冲突

---

## 9. 风险与依赖（共享辅助类 / Fixture / 工具方法）

### 9.1 共享测试辅助类风险
| 辅助类/Fixture | 当前所在项目 | 被哪些测试依赖 | 风险 | 建议 |
|---|---|---|---|---|
| `Bing.Tests.TestBase` | 多测试项目各自定义 | `Bing.Utils.Tests` / `Bing.Utils.Http.Tests` / `Bing.Utils.DateTime.Tests` 等 | 迁移时可能绑定到不同实现（同命名空间同类名） [证据] `tests/Bing.Utils.Tests/Bing/Tests/TestBase.cs:6` `tests/Bing.Utils.Http.Tests/Bing/Tests/TestBase.cs:6` | 迁移时使用目标项目原有 `TestBase`，不跨项目引用 |
| `EnvTestCollection`（`EnvSerial`） | `Bing.Utils.Tests` | 环境变量相关测试 | 后续若拆分 `Env*` 测试需同步迁移/复制 [证据] `tests/Bing.Utils.Tests/Bing/Helpers/EnvTestCollection.cs:6` | 先复制最小 Fixture，再考虑抽公共测试设施 |
| `global-usings.cs` | 各测试项目 | 全项目 | 迁移到新项目后 `using` 假设可能失效 [证据] `tests/Bing.Utils.Tests/global-usings.cs:1` `tests/BingUtilsUT/global-usings.cs:1` | 每批迁移后先编译检查 |

### 9.2 命名与目录风险
- `Bing.Utils.Tests` 内存在同名测试类重复（如 `ValidTest`、`CompressionTest`、`RegexsTest`）  
  [证据] `tests/Bing.Utils.Tests/Bing/Helpers/ValidTest.cs:6` `tests/Bing.Utils.Tests/Helpers/ValidTest.cs:6`
- `Bing/Text`、`Bing/Reflection` 在测试层使用同一业务命名空间，但被测类型分属不同程序集  
  [证据] `tests/Bing.Utils.Tests/Bing/Text/CaseFormatterTest.cs:1` `tests/Bing.Utils.Tests/Bing/Reflection/ReflectionsTest.cs:2`
- `Extensions/Common/ExtensionsTest.*.cs` 为 `partial class`，整文件迁移风险高  
  [证据] `tests/Bing.Utils.Tests/Extensions/Common/ExtensionsTest.DateTime.cs:11` `tests/Bing.Utils.Tests/Extensions/Common/ExtensionsTest.Convert.cs:9`

### 9.3 其他风险
- `Bing.Utils.Tests` 与 `BingUtilsUT` 都引用 `Bing.Tests.Samples`，迁移时可能引入样例依赖耦合  
  [证据] `tests/Bing.Utils.Tests/Bing.Utils.Tests.csproj:20` `tests/BingUtilsUT/BingUtilsUT.csproj:19`
- `FtpClientTest` 使用真实内网地址与凭据，不适合作为稳定回归基线  
  [证据] `tests/Bing.Utils.Tests/Net/FTP/FtpClientTest.cs:25`

---

## 10. 待人工确认项（Manual Review Required）

| 文件路径 | 测试类 | 原因 | 建议处理方式 | 状态 |
|---|---|---|---|---|
| `tests/Bing.Utils.Tests/Bing/Text/StrTest.cs` | `StrTest` | 命名空间在 `Bing.Text`，但实现位于主包 `Bing.Utils` | 明确“按程序集归属”策略后再定 | 待处理 |
| `tests/Bing.Utils.Tests/Text/Formatting/FormattedStringValueExtractor*.cs` | `FormattedStringValueExtractor*` | 文本域但实现位于主包 `Bing.Utils` | 先保留主包测试 | 待确认 |
| `tests/Bing.Utils.Tests/Extensions/Reflections/AssemblyExtensionsTest.cs` | `AssemblyExtensionsTest` | 仅凭类名无法判定主包/Reflection 子包 | 方法级核对源码 | 待处理 |
| `tests/Bing.Utils.Tests/Bing/Collections/CollectionExtensionsTest.cs` | `CollectionExtensionsTest` | 主包/Collections 子包同命名空间混淆风险 | 逐方法判定 | 待处理 |
| `tests/Bing.Utils.Tests/Drawing/CaptchaBuilderTest.cs` | `CaptchaBuilderTest` | 基础 Drawing 是否创建 `Bing.Utils.Drawing.Tests` 未决 | 仓库层决策 | 待处理 |
| `tests/Bing.Utils.Tests/Net/FTP/FtpClientTest.cs` | `FtpClientTest` | `Net` 模块无专属测试项目，且依赖真实环境 | 纳入 P2 专项 | 待处理 |
| `tests/BingUtilsUT/StringUT/*.cs` | 多类 | 主包字符串扩展与 Text 子包混装 | 目录级清点后再拆分 | 待处理 |
| `tests/BingUtilsUT/TypeUT/*.cs` | 多类 | `Types`（主包）与 `TypeVisit`（Reflection 子包）混装 | 先按文件切分 | 待处理 |

---

## 11. 执行记录（迭代更新）

### 11.1 第 1 轮（试点：IdUtils）
- 日期：`2026-02-25`
- 范围：`BingUtilsUT/IdUtilsUT/* -> Bing.Utils.IdUtils.Tests`（P0 子集试点）
- 执行内容：
  - 已将 `tests/BingUtilsUT/IdUtilsUT/GuidTest.cs` 迁移到 `tests/Bing.Utils.IdUtils.Tests/Bing/IdUtils/Legacy/GuidTest.cs`
  - 已将 `tests/BingUtilsUT/IdUtilsUT/RandomIdTest.cs` 迁移到 `tests/Bing.Utils.IdUtils.Tests/Bing/IdUtils/Legacy/RandomIdTest.cs`
  - 已将 `tests/BingUtilsUT/IdUtilsUT/TraceIdTest.cs` 迁移到 `tests/Bing.Utils.IdUtils.Tests/Bing/IdUtils/Legacy/TraceIdTest.cs`
  - 初始暂缓 `tests/Bing.Utils.Tests/Bing/Helpers/IdTest.cs`（与目标项目已有 `IdTest`/`IdSnowflakeTest` 覆盖存在明显重叠）
  - 本轮新增（`Bing.Utils.Tests -> Bing.Utils.IdUtils.Tests` 试点迁移）：
    - 已迁移 `tests/Bing.Utils.Tests/IdGenerators/SnowflakeIdGeneratorTest.cs` -> `tests/Bing.Utils.IdUtils.Tests/Bing/Helpers/Legacy/SnowflakeIdGeneratorTest.cs`
    - 已迁移 `tests/Bing.Utils.Tests/IdGenerators/ObjectIdGeneratorTest.cs` -> `tests/Bing.Utils.IdUtils.Tests/Bing/Helpers/Legacy/ObjectIdGeneratorTest.cs`
    - 已迁移 `tests/Bing.Utils.Tests/IdGenerators/TimestampIdGeneratorTest.cs` -> `tests/Bing.Utils.IdUtils.Tests/Bing/Helpers/Legacy/TimestampIdGeneratorTest.cs`
    - 为目标项目补充最小依赖：`tests/Bing.Utils.IdUtils.Tests/Bing/Tests/TestBase.cs`、`tests/Bing.Utils.IdUtils.Tests/global-usings.cs`（增加 `global using Bing.Tests;`）
    - 已完成 `tests/Bing.Utils.Tests/Bing/Helpers/IdTest.cs` 重复覆盖比对，并执行去重删除（不迁移）；替代覆盖由 `tests/Bing.Utils.IdUtils.Tests/Bing/Helpers/IdSnowflakeTest.cs` 中雪花 ID 唯一性/重置/并发用例承担
- 验证结果：
  - `dotnet test tests/Bing.Utils.IdUtils.Tests/Bing.Utils.IdUtils.Tests.csproj --filter "FullyQualifiedName~GuidTest|FullyQualifiedName~RandomIdTest|FullyQualifiedName~TraceIdTest"` 通过（多目标框架通过，0 失败）
  - `dotnet build tests/BingUtilsUT/BingUtilsUT.csproj -f net8.0` 通过（0 错误，存在既有 warning）
  - `dotnet build tests/Bing.Utils.IdUtils.Tests/Bing.Utils.IdUtils.Tests.csproj -f net8.0` 通过（0 错误）
  - `dotnet build tests/Bing.Utils.Tests/Bing.Utils.Tests.csproj -f net8.0` 通过（0 错误，存在既有 warning）
  - `dotnet test tests/Bing.Utils.IdUtils.Tests/Bing.Utils.IdUtils.Tests.csproj -f net8.0 --filter "FullyQualifiedName~Bing.Utils.Tests.IdGenerators.SnowflakeIdGeneratorTest.Test_Create|FullyQualifiedName~Bing.Utils.Tests.IdGenerators.ObjectIdGeneratorTest.Test_Create|FullyQualifiedName~Bing.Utils.Tests.IdGenerators.TimestampIdGeneratorTest.Test_Create"` 通过（最小验证；筛选实际命中 18 个 `Test_Create*` 用例）
  - `dotnet test tests/Bing.Utils.IdUtils.Tests/Bing.Utils.IdUtils.Tests.csproj -f net8.0 --filter "FullyQualifiedName~Bing.Helpers.IdSnowflakeTest.CreateSnowflakeId_RapidGeneration_ProducesUniqueIds|FullyQualifiedName~Bing.Helpers.IdSnowflakeTest.ResetSnowflakeId_AfterCustomConfiguration_RestoresDefaultBehavior|FullyQualifiedName~Bing.Helpers.IdSnowflakeTest.CreateSnowflakeId_ConcurrentGeneration_ProducesUniqueIds"` 通过（去重替代覆盖验证；命中 3 个用例）
- 遗留问题：
  - `IdUtils` 相关混合测试仍未处理（如 `CheckAndConvEdgeContractTest`），需后续方法级拆分
  - `Legacy` 目录中的 `IdGenerators/*` 与目标项目 `IdTest`/`IdSnowflakeTest` 仍可能存在部分覆盖重叠，需继续去重映射
  - 本轮结论：`部分成功`（已完成 `Bing.Utils.Tests` 中 3 个 `IdGenerators/*` 文件迁移，并完成 `Bing.Helpers.IdTest` 重复覆盖去重删除）

### 11.2 第 2 轮（P0 批次）
- 日期：`2026-02-25`
- 范围：`Bing.Utils.IdUtils（Bing.Utils.Tests 残余审计收尾批次）`
- 执行内容：
  - 按 `test-project-boundary` 规则重新扫描 `tests/Bing.Utils.Tests/**/*`，确认当前 `IdUtils` 相关残余项分类
  - 识别到 `tests/Bing.Utils.Tests/UnitTest1.cs`（混合测试，局部使用 `GuidProvider.Create`）与 `tests/Bing.Utils.Tests/Bing/Helpers/CheckAndConvEdgeContractTest.cs`（混合测试）
  - 确认本轮在 `Bing.Utils.Tests` 范围内 **无新增可直接迁移（文件级）项**
  - 新增批次报告：`docs/quality/migration-reports/2026-02-25-batch-idutils.md`
  - 更新迁移索引：`docs/quality/migration-reports/README.md`
- 验证结果：
  - `dotnet build tests/Bing.Utils.IdUtils.Tests/Bing.Utils.IdUtils.Tests.csproj -f net8.0` 通过（0 错误）
  - `dotnet build tests/Bing.Utils.Tests/Bing.Utils.Tests.csproj -f net8.0` 通过（0 错误）
  - `dotnet test tests/Bing.Utils.IdUtils.Tests/Bing.Utils.IdUtils.Tests.csproj -f net8.0 --filter "FullyQualifiedName~Bing.Helpers.IdSnowflakeTest.CreateSnowflakeId_RapidGeneration_ProducesUniqueIds|FullyQualifiedName~Bing.Helpers.IdSnowflakeTest.ResetSnowflakeId_AfterCustomConfiguration_RestoresDefaultBehavior|FullyQualifiedName~Bing.Helpers.IdSnowflakeTest.CreateSnowflakeId_ConcurrentGeneration_ProducesUniqueIds"` 通过（命中 3 个替代覆盖用例）
- 遗留问题：
  - `UnitTest1.cs` 为历史混合测试文件，方法级拆分成本高，需单独子批次处理
  - `CheckAndConvEdgeContractTest.cs` 为 `Check + Conv` 混合边界契约，需方法级拆分
  - `Legacy/IdGenerators/*` 与目标项目现有 `IdTest`/`IdSnowflakeTest` 仍可能存在部分覆盖重叠，需继续去重映射
  - 本轮结论：`成功`（收尾审计批次；无新增可直接迁移项）

### 11.3 第 3 轮（Mixed Split：IdUtils / Batch-3 / Mixed-Batch-1）
- 日期：`2026-02-25`
- 范围：`Bing.Utils.IdUtils（Bing.Utils.Tests 混合测试方法级拆分，首个子批次）`
- 执行内容：
  - 复审 `IdUtils` 混合测试候选：`UnitTest1.cs`、`CheckAndConvEdgeContractTest.cs`
  - 确认 `UnitTest1.cs` 适合按方法级拆分，拆出 `Test_Id()` 到 `tests/Bing.Utils.IdUtils.Tests/Bing/IdUtils/Legacy/UnitTest1IdUtilsMixedSplitTest.cs`
  - 从 `tests/Bing.Utils.Tests/UnitTest1.cs` 删除 `Test_Id()` 与无用 `using Bing.IdUtils;`
  - 复审后确认 `CheckAndConvEdgeContractTest.cs` 当前不属于 `IdUtils` 混合拆分目标（转入主包/Conv 边界专项）
  - 新增报告：`docs/quality/migration-reports/2026-02-25-mixed-batch-idutils.md`
- 验证结果：
  - `dotnet build tests/Bing.Utils.IdUtils.Tests/Bing.Utils.IdUtils.Tests.csproj -f net8.0` 通过（0 错误）
  - `dotnet build tests/Bing.Utils.Tests/Bing.Utils.Tests.csproj -f net8.0` 通过（0 错误，存在既有 warning）
  - `dotnet test tests/Bing.Utils.IdUtils.Tests/Bing.Utils.IdUtils.Tests.csproj -f net8.0 --filter "FullyQualifiedName~Bing.IdUtils.UnitTest1IdUtilsMixedSplitTest.Test_Id"` 通过（命中 1 个用例）
- 遗留问题：
  - `UnitTest1.cs` 仍为高耦合混合测试类，后续可继续按模块拆分
  - `Legacy/IdGenerators/*` 与目标项目现有测试仍需方法级去重映射
  - 本轮结论：`成功`（已完成 `IdUtils` 混合拆分首个子批次）

### 11.4 第 4 轮（Mixed Split：IdUtils / Batch-3 / Mixed-Batch-2 收尾复审）
- 日期：`2026-02-25`
- 范围：`Bing.Utils.IdUtils（Bing.Utils.Tests/UnitTest1.cs 剩余 IdUtils 方法复审）`
- 执行内容：
  - 复审 `tests/Bing.Utils.Tests/UnitTest1.cs` 在 `Mixed-Batch-1` 后是否仍含 `IdUtils` 相关方法
  - 通过关键词扫描确认已无 `Bing.IdUtils` `using`，且未发现 `GuidProvider` / `GuidStyle` / `Snowflake` / `ObjectId` / `TimestampId` / `TraceIdAccessor` 调用
  - 新增报告：`docs/quality/migration-reports/2026-02-25-mixed-batch-idutils-2.md`
  - 更新迁移索引：`docs/quality/migration-reports/README.md`
- 验证结果：
  - 结构检查通过（`UnitTest1.cs` 中未命中 `IdUtils` 相关关键词）
  - 本轮无测试代码变更，未重复执行编译/测试；沿用 `11.3` 最近有效验证结果
- 遗留问题：
  - `IdUtils` 在 `Bing.Utils.Tests` 的混合拆分目标已基本收尾，后续重点转向 `Legacy/IdGenerators/*` 重复覆盖去重映射
  - `UnitTest1.cs` 的剩余方法如需继续拆分，应按其他模块批次执行（非 `IdUtils`）
  - 本轮结论：`成功`（收尾复审批次；无新增可拆分方法）

### 11.5 第 5 轮（P0：IdUtils / Batch-4 / Legacy 去重 Batch-1）
- 日期：`2026-02-25`
- 范围：`Bing.Utils.IdUtils.Tests/Bing/Helpers/Legacy/*`（ObjectId/TimestampId 去重）
- 执行内容：
  - 删除 `tests/Bing.Utils.IdUtils.Tests/Bing/Helpers/Legacy/ObjectIdGeneratorTest.cs`
  - 删除 `tests/Bing.Utils.IdUtils.Tests/Bing/Helpers/Legacy/TimestampIdGeneratorTest.cs`
  - 保留 `tests/Bing.Utils.IdUtils.Tests/Bing/Helpers/Legacy/SnowflakeIdGeneratorTest.cs`，待确认人工压测脚本定位后进入 Batch-2
  - 新增报告：`docs/quality/migration-reports/2026-02-25-batch-idutils-legacy-dedupe-1.md`
  - 更新迁移索引：`docs/quality/migration-reports/README.md`
- 验证结果：
  - `dotnet build tests/Bing.Utils.IdUtils.Tests/Bing.Utils.IdUtils.Tests.csproj -f net8.0` 通过（0 错误）
  - `dotnet build tests/Bing.Utils.Tests/Bing.Utils.Tests.csproj -f net8.0` 通过（0 错误）
  - `dotnet test tests/Bing.Utils.IdUtils.Tests/Bing.Utils.IdUtils.Tests.csproj -f net8.0 --filter "FullyQualifiedName~Bing.Helpers.IdTest.CreateObjectId_AlwaysGeneratesNewId|FullyQualifiedName~Bing.Helpers.IdTest.CreateObjectId_IgnoresContextId|FullyQualifiedName~Bing.Helpers.IdTest.CreateObjectId_MassGeneration_ProducesUniqueIds|FullyQualifiedName~Bing.Helpers.IdTest.CreateTimestampId_AlwaysGeneratesNewId|FullyQualifiedName~Bing.Helpers.IdTest.CreateTimestampId_IgnoresContextId|FullyQualifiedName~Bing.Helpers.IdTest.CreateTimestampId_ShowsTimeSequence"` 通过（命中 6 个替代覆盖用例）
- 遗留问题：
  - `Legacy/SnowflakeIdGeneratorTest.cs` 是否保留为人工压测脚本待确认
  - 本轮结论：`成功`（Legacy 去重 Batch-1 完成）

### 11.6 第 6 轮（P0：IdUtils / Batch-4 / Legacy 去重 Batch-2）
- 日期：`2026-02-25`
- 范围：`Bing.Utils.IdUtils.Tests/Bing/Helpers/Legacy/SnowflakeIdGeneratorTest.cs`
- 执行内容：
  - 删除 `tests/Bing.Utils.IdUtils.Tests/Bing/Helpers/Legacy/SnowflakeIdGeneratorTest.cs`
  - 新增报告：`docs/quality/migration-reports/2026-02-25-batch-idutils-legacy-dedupe-2.md`
  - 更新迁移索引：`docs/quality/migration-reports/README.md`
- 验证结果：
  - `dotnet build tests/Bing.Utils.IdUtils.Tests/Bing.Utils.IdUtils.Tests.csproj -f net8.0` 通过（0 错误）
  - `dotnet build tests/Bing.Utils.Tests/Bing.Utils.Tests.csproj -f net8.0` 通过（0 错误）
  - `dotnet test tests/Bing.Utils.IdUtils.Tests/Bing.Utils.IdUtils.Tests.csproj -f net8.0 --filter "FullyQualifiedName~Bing.Helpers.IdSnowflakeTest.CreateSnowflakeId_RapidGeneration_ProducesUniqueIds|FullyQualifiedName~Bing.Helpers.IdSnowflakeTest.CreateSnowflakeId_ConcurrentGeneration_ProducesUniqueIds|FullyQualifiedName~Bing.Helpers.IdSnowflakeTest.CreateSnowflakeId_Performance_GeneratesQuickly|FullyQualifiedName~Bing.IdUtils.SnowflakeGeneratorConcurrencyContractTest.NextId_TwitterStyleParallelCalls_ReturnsUniquePositiveIds|FullyQualifiedName~Bing.IdUtils.SnowflakeGeneratorConcurrencyContractTest.NextId_SeataStyleParallelCalls_ReturnsUniquePositiveIds"` 通过（命中 5 个替代覆盖用例）
- 遗留问题：
  - 若团队仍需人工压测脚本资产，需在非 CI 目录重建/迁移（本轮采用删除策略）
  - 本轮结论：`成功`（Legacy 去重 Batch-2 完成；Batch-4 收尾）

---

## 12. 后续优化建议（可选）

1. 抽取共享测试基础设施（如 `tests/Common`）
   - 公共断言扩展
   - 测试数据构造器
   - 时间/随机桩对象
   - 通用 `TestBase`

2. 增加“测试归属审计”周期性检查
   - PR 检查脚本：扫描 `Bing.Utils.Tests` 中明显模块专属命名（如 `TypeVisit*` / `Id*` / `Splitter*`）

3. 建立缺陷回归测试映射（defect-to-test）
   - 缺陷修复必须对应新增回归测试
   - 回归测试随模块归属迁移

4. 明确缺失测试项目策略（建议仓库层决策）
   - 是否新增 `Bing.Utils.Text.Tests`
   - 是否新增 `Bing.Utils.Reflection.Tests`
   - 是否新增 `Bing.Utils.Drawing.Tests`
   - 是否新增 `Bing.Utils.Net.Tests`
