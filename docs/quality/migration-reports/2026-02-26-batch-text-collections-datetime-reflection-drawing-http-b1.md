# 多模块批量迁移验证报告（Batch-1）

## 1. 执行范围与规则摘要

- 日期：`2026-02-26`
- 执行模式：`multi-module-batch-controller`（连续运行）
- 本轮目标模块：
  - `Bing.Utils.Text -> Bing.Utils.Text.Tests`
  - `Bing.Utils.Collections -> Bing.Utils.Collections.Tests`
  - `Bing.Utils.DateTime -> Bing.Utils.DateTime.Tests`
  - `Bing.Utils.Reflection -> Bing.Utils.Reflection.Tests`
  - `Bing.Utils.Drawing -> Bing.Utils.Drawing.Tests`
  - `Bing.Utils.Drawing.ImageSharp -> Bing.Utils.Drawing.ImageSharp.Tests`
  - `Bing.Utils.Drawing.SkiaSharp -> Bing.Utils.Drawing.SkiaSharp.Tests`
  - `Bing.Utils.Http -> Bing.Utils.Http.Tests`
- 规则来源：
  - `AGENTS.md`
  - `.agents/skills/test-project-boundary/refs/test-project-boundary.md`
  - `docs/quality/test-reclassification-plan.md`
- 关键规则（本轮执行）：
  - 按“主要断言目标”判定归属
  - 仅处理文件级直迁项
  - 混合测试/待确认项只记录 TODO，不强行迁移
  - 不修改 `src/*`

## 2. 子批次拆分说明（连续运行）

- 拆分原因：
  - 目标模块数量较多（8 个）
  - `Bing.Utils.Text.Tests` / `Bing.Utils.Reflection.Tests` / `Bing.Utils.Drawing.Tests` 当前未发现测试项目（需先做项目策略决策）[证据] `docs/quality/test-reclassification-plan.md:97` `docs/quality/test-reclassification-plan.md:99` `docs/quality/test-reclassification-plan.md:100`
  - 多个模块以混合测试为主（`Text` / `Reflection` / `DateTime` 扩展 partial）
- 本轮实际执行子批次：`Collections / Batch-1`
- 本轮执行范围（代码变更）：
  - `tests/BingUtilsUT/CollUT/ArrayShortcutTests.cs`（文件级直迁）

## 3. 各目标模块迁移清单（本轮审计结果）

### 3.1 `Bing.Utils.Collections -> Bing.Utils.Collections.Tests`（本轮执行）

#### 已执行
- `tests/BingUtilsUT/CollUT/ArrayShortcutTests.cs`
  - 归属依据：测试 `Array` 快捷扩展（`BinarySearch/Clear/FindAll/...`），主断言目标对应 `Bing.Utils.Collections` 的 `ArraysShortcutExtensions` [证据] `tests/Bing.Utils.Collections.Tests/Bing/Collections/Legacy/ArrayShortcutTests.cs:19` `src/Bing.Utils.Collections/Bing/Collections/ArraysShortcutExtensions.cs:17`

#### 未执行（待后续）
- `tests/BingUtilsUT/CollUT/ArrayCopyTests.cs`
- `tests/BingUtilsUT/CollUT/ArrayEmptyTests.cs`
- `tests/BingUtilsUT/CollUT/ArrayToTests.cs`
  - 原因：需逐文件确认被测 API 所在程序集（避免误迁）

#### 应保留（状态复核）
- `tests/Bing.Utils.Tests/Collections/EnumerableExtensionsTest.cs`
  - 原因：`ChunkBy` 实现位于主包 `Bing.Utils`，不属于 `Bing.Utils.Collections` [证据] `src/Bing.Utils/Bing/Collections/Extensions/Enumerable/Extensions.Enumerable.cs:19`

### 3.2 `Bing.Utils.Text -> Bing.Utils.Text.Tests`（审计，未执行）

- 当前状态：未发现 `Bing.Utils.Text.Tests` 项目（待项目策略确认）[证据] `docs/quality/test-reclassification-plan.md:97`
- 高置信度文件级候选（待项目存在后执行）：
  - `tests/Bing.Utils.Tests/Bing/Text/CaseFormatter*.cs`
  - `tests/Bing.Utils.Tests/Bing/Text/JoinerTest.cs`
  - `tests/Bing.Utils.Tests/Bing/Text/Splitter*.cs`
  - `tests/BingUtilsUT/SplitterUT/*.cs`
- 混合项（后续专项）：
  - `tests/Bing.Utils.Tests/Bing/Text/*`
  - `tests/BingUtilsUT/StringUT/*`

### 3.3 `Bing.Utils.DateTime -> Bing.Utils.DateTime.Tests`（审计，未执行）

- 当前状态：专属测试项目已存在，覆盖较强；本轮未识别新增高置信度文件级直迁项 [证据] `docs/quality/test-reclassification-plan.md:90`
- 混合项：
  - `tests/Bing.Utils.Tests/Extensions/Common/ExtensionsTest.DateTime.cs`（`partial`，需方法级拆分）[证据] `tests/Bing.Utils.Tests/Extensions/Common/ExtensionsTest.DateTime.cs:11`

### 3.4 `Bing.Utils.Reflection -> Bing.Utils.Reflection.Tests`（审计，未执行）

- 当前状态：未发现 `Bing.Utils.Reflection.Tests` 项目（待项目策略确认）[证据] `docs/quality/test-reclassification-plan.md:98`
- 高置信度文件级候选（待项目存在后执行）：
  - `tests/Bing.Utils.Tests/Bing/Reflection/AssemblyVisitAndTypeVisitGuardTest.cs`
  - `tests/Bing.Utils.Tests/Bing/Reflection/TypeVisit*.cs`
  - `tests/Bing.Utils.Tests/Bing/Reflection/TypeMetaVisitExtensionsAdditionalContractTest.cs`
  - `tests/BingUtilsUT/TypeUT/CreateInterfaceTest.cs`

### 3.5 `Bing.Utils.Drawing -> Bing.Utils.Drawing.Tests`（审计，未执行）

- 当前状态：未发现基础 Drawing 专属测试项目（待项目策略确认）[证据] `docs/quality/test-reclassification-plan.md:100`
- 候选：
  - `tests/Bing.Utils.Tests/Drawing/CaptchaBuilderTest.cs`（待决定是否新增 `Bing.Utils.Drawing.Tests`）[证据] `tests/Bing.Utils.Tests/Drawing/CaptchaBuilderTest.cs:7` `src/Bing.Utils.Drawing/Bing/Drawing/CaptchaBuilder.cs:11`

### 3.6 `Bing.Utils.Drawing.ImageSharp -> Bing.Utils.Drawing.ImageSharp.Tests`（审计，未执行）

- 状态复核：专属项目已存在，当前未发现 `Bing.Utils.Tests` 中高置信度待迁项（本轮无动作）

### 3.7 `Bing.Utils.Drawing.SkiaSharp -> Bing.Utils.Drawing.SkiaSharp.Tests`（审计，未执行）

- 状态复核：专属项目已存在，当前未发现 `Bing.Utils.Tests` 中高置信度待迁项（本轮无动作）

### 3.8 `Bing.Utils.Http -> Bing.Utils.Http.Tests`（审计，未执行）

- 状态复核：
  - `tests/Bing.Utils.Tests/Helpers/WebTest.cs` 为历史禁用文件，语义上属于 `Http`，但目标项目已存在活跃 `WebTest`，应先做重复覆盖比对 [证据] `tests/Bing.Utils.Tests/Helpers/WebTest.cs:12` `tests/Bing.Utils.Http.Tests/Bing/Helpers/WebTest.cs:11` `src/Bing.Utils.Http/Bing/Helpers/Web.cs:17`
  - `tests/Bing.Utils.Tests/Bing/Helpers/UrlTest.cs` / `tests/Bing.Utils.Tests/Parameters/UrlParameterBuilderTest.cs` 应保留主包测试（实现位于 `Bing.Utils`）[证据] `src/Bing.Utils/Bing/Helpers/Url.cs:9` `src/Bing.Utils/Bing/Utils/Parameters/UrlParameterBuilder.cs:10`

## 4. 实施变更明细（本轮仅 Batch-1）

### 4.1 文件迁移
- `tests/BingUtilsUT/CollUT/ArrayShortcutTests.cs`
  -> `tests/Bing.Utils.Collections.Tests/Bing/Collections/Legacy/ArrayShortcutTests.cs`

### 4.2 最小修复
- 命名空间：
  - `BingUtilsUT.CollUT` -> `Bing.Collections.Legacy`
- 未修改：
  - 测试方法命名
  - 断言语义
  - 生产代码 `src/*`

## 5. 验证结果（按模块汇总）

### 5.1 `Bing.Utils.Collections.Tests`
- `dotnet build tests/Bing.Utils.Collections.Tests/Bing.Utils.Collections.Tests.csproj -f net8.0`：通过
- `dotnet test tests/Bing.Utils.Collections.Tests/Bing.Utils.Collections.Tests.csproj -f net8.0 --filter "FullyQualifiedName~Bing.Collections.Legacy.ArrayShortcutTests"`：通过（命中 `8` 个用例）

### 5.2 `Bing.Utils.Tests`
- `dotnet build tests/Bing.Utils.Tests/Bing.Utils.Tests.csproj -f net8.0`：通过（存在既有 warning）

### 5.3 结构检查
- 源文件已清理：`tests/BingUtilsUT/CollUT/ArrayShortcutTests.cs` 不存在
- 目标文件存在：`tests/Bing.Utils.Collections.Tests/Bing/Collections/Legacy/ArrayShortcutTests.cs`
- 修改范围符合约束：仅 `tests/*` 与 `docs/quality/*`

## 6. 风险与遗留事项

- `BingUtilsUT/CollUT` 其余文件仍需逐文件判定归属（`ArrayCopy/ArrayEmpty/ArrayTo`）
- `Text` / `Reflection` / `Drawing` 基础模块仍受“目标测试项目未创建”约束，需先完成项目策略决策
- `Http` 历史 `WebTest` 需做重复覆盖比对后再处理，避免重复迁移

## 7. 本轮结论

- 结论：`成功`
- 说明：已完成多模块批量迁移首个子批次（`Collections / Batch-1`），并完成验证、索引与规划文档更新；其余模块已完成审计分流，待后续子批次继续执行。

## 8. 下一步建议（默认）

1. 执行 `Collections / Batch-2`：复审并迁移 `BingUtilsUT/CollUT` 其余高置信度文件（仅文件级直迁）
2. 如需跨更大收益，先决策是否创建 `Bing.Utils.Text.Tests` / `Bing.Utils.Reflection.Tests` / `Bing.Utils.Drawing.Tests`
