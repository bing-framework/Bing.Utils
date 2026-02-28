# 多模块批量迁移验证报告（Batch-2）

## 1. 执行范围与规则摘要

- 日期：`2026-02-26`
- 执行模式：`multi-module-batch-controller`（连续运行，承接 Batch-1）
- 本轮目标模块（继承上一轮多模块批次）：
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
- 执行原则（本轮）：
  - 继续只处理文件级直迁项
  - 混合/待确认项只记录，不强迁
  - 不修改 `src/*`

## 2. 子批次说明（连续运行）

- 本轮子批次：`Collections / Batch-2`
- 承接 `Batch-1`：继续处理 `BingUtilsUT/CollUT` 剩余高置信度直迁文件
- 本轮实际执行范围：
  - `tests/BingUtilsUT/CollUT/ArrayCopyTests.cs`
  - `tests/BingUtilsUT/CollUT/ArrayEmptyTests.cs`
  - `tests/BingUtilsUT/CollUT/ArrayToTests.cs`

## 3. 各目标模块迁移清单（本轮审计结果）

### 3.1 `Bing.Utils.Collections -> Bing.Utils.Collections.Tests`（本轮执行）

#### 已执行
- `tests/BingUtilsUT/CollUT/ArrayCopyTests.cs`
  - 归属依据：主断言目标为 `Arrays.Copy` 与数组复制扩展（`ArraysExtensions.Copy`），位于 `Bing.Utils.Collections` [证据] `tests/Bing.Utils.Collections.Tests/Bing/Collections/Legacy/ArrayCopyTests.cs:23` `src/Bing.Utils.Collections/Bing/Collections/Arrays.Copy.cs:22` `src/Bing.Utils.Collections/Bing/Collections/ArraysExtensions.Copy.cs:6`
- `tests/BingUtilsUT/CollUT/ArrayEmptyTests.cs`
  - 归属依据：主断言目标为 `Arrays.Empty<T>()`，位于 `Bing.Utils.Collections` [证据] `tests/Bing.Utils.Collections.Tests/Bing/Collections/Legacy/ArrayEmptyTests.cs:15` `src/Bing.Utils.Collections/Bing/Collections/Arrays.cs:6`
- `tests/BingUtilsUT/CollUT/ArrayToTests.cs`
  - 归属依据：主断言目标为 `Arrays.ToArraySafety*` 系列，位于 `Bing.Utils.Collections` [证据] `tests/Bing.Utils.Collections.Tests/Bing/Collections/Legacy/ArrayToTests.cs:25` `src/Bing.Utils.Collections/Bing/Collections/Arrays.cs:60` `src/Bing.Utils.Collections/Bing/Collections/Arrays.cs:105` `src/Bing.Utils.Collections/Bing/Collections/Arrays.cs:179`

#### 状态更新
- `BingUtilsUT/CollUT` 目录已清空（`ArrayShortcutTests` 已于 Batch-1 迁移，本轮完成剩余 3 个文件迁移）

### 3.2 其他目标模块（本轮仅状态继承，未执行）

- `Text`：待确认/创建 `Bing.Utils.Text.Tests` 后执行 `P1` 批次
- `Reflection`：待确认/创建 `Bing.Utils.Reflection.Tests` 后执行 `P1` 批次
- `DateTime`：本轮未新增高置信度文件级直迁项
- `Drawing`（基础）：待确认是否创建 `Bing.Utils.Drawing.Tests`
- `Drawing.ImageSharp` / `Drawing.SkiaSharp`：专属项目状态稳定，本轮无待迁项
- `Http`：`WebTest` 需重复覆盖比对后再处理

## 4. 实施变更明细（本轮）

### 4.1 文件迁移
- `tests/BingUtilsUT/CollUT/ArrayCopyTests.cs`
  -> `tests/Bing.Utils.Collections.Tests/Bing/Collections/Legacy/ArrayCopyTests.cs`
- `tests/BingUtilsUT/CollUT/ArrayEmptyTests.cs`
  -> `tests/Bing.Utils.Collections.Tests/Bing/Collections/Legacy/ArrayEmptyTests.cs`
- `tests/BingUtilsUT/CollUT/ArrayToTests.cs`
  -> `tests/Bing.Utils.Collections.Tests/Bing/Collections/Legacy/ArrayToTests.cs`

### 4.2 最小修复
- 命名空间统一：
  - `BingUtilsUT.CollUT` -> `Bing.Collections.Legacy`
- `ArrayToTests.cs` 增补：
  - `using System.Collections.Generic;`
  - 原因：目标项目 `global-usings.cs` 不包含 `List<T> / IEnumerable<T> / IReadOnlyList<T>` 所需命名空间
- 未修改：
  - 测试方法命名
  - 断言语义
  - 生产代码 `src/*`

## 5. 验证结果（按模块汇总）

### 5.1 `Bing.Utils.Collections.Tests`
- `dotnet build tests/Bing.Utils.Collections.Tests/Bing.Utils.Collections.Tests.csproj -f net8.0`：通过
- `dotnet test tests/Bing.Utils.Collections.Tests/Bing.Utils.Collections.Tests.csproj -f net8.0 --filter "FullyQualifiedName~Bing.Collections.Legacy.ArrayCopyTests|FullyQualifiedName~Bing.Collections.Legacy.ArrayEmptyTests|FullyQualifiedName~Bing.Collections.Legacy.ArrayToTests"`：通过（命中 `10` 个用例）

### 5.2 `Bing.Utils.Tests`
- `dotnet build tests/Bing.Utils.Tests/Bing.Utils.Tests.csproj -f net8.0`：通过（存在既有 warning）

### 5.3 结构检查
- 源文件已清理（3 个 `CollUT` 文件不存在）
- 目标文件存在（`Bing.Utils.Collections.Tests/Bing/Collections/Legacy/` 下 3 个文件）
- `BingUtilsUT/CollUT` 目录已无文件（清空）
- 修改范围符合约束：仅 `tests/*` 与 `docs/quality/*`

## 6. 风险与遗留事项

- `Collections` 后续不建议继续按目录名粗迁移 `tests/Bing.Utils.Tests/Bing/Collections/*`；需按被测 API 实现所在程序集逐文件判断（主包与子包同命名空间并存）
- 多模块批量迁移后续推进仍依赖 `Text` / `Reflection` / `Drawing` 目标测试项目策略决策

## 7. 本轮结论

- 结论：`成功`
- 说明：已完成多模块批量迁移第 2 个子批次（`Collections / Batch-2`），`BingUtilsUT/CollUT` 历史目录文件级直迁收尾完成。

## 8. 下一步建议（默认）

1. 继续多模块批量迁移：转入 `Text/Reflection` 的“项目策略确认 + P1 首批候选审计”
2. 或在 `Collections` 方向执行主包目录复审（`tests/Bing.Utils.Tests/Bing/Collections/*` 逐文件判定）
