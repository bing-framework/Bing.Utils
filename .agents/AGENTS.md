# AGENTS.md

## 仓库定位
这是一个多子包的 .NET 基础设施/工具类库仓库（Bing.Utils），包含多个模块及对应测试项目（如 `Bing.Utils.IdUtils.Tests`、`Bing.Utils.Text.Tests` 等）。

常见任务包括：
- 文档补全与整理（`docs/*`）
- 单元测试/集成测试补全（`02-tests/*`）
- 测试迁移与测试归属整理
- 小范围工程结构优化（不改变生产行为）

---

## 默认工作规则
1. 先分析再修改；结构性任务（迁移/重构）必须先输出计划或清单。
2. 优先小步修改，保持现有代码风格与测试风格一致。
3. 不确定项标记 `TODO` / 待确认，不臆测。
4. 修改后尽量执行最小验证（编译/测试），并输出结果。
5. 默认使用中文输出（代码、路径、类型名除外）。

---

## 修改范围约束
默认仅允许修改以下目录（除非用户明确授权）：
- `docs/*`
- `02-tests/*`

默认不要修改：
- `01-src/*`（生产代码）

如果任务要求必须修改生产代码：
- 使用最小变更原则
- 不改变既有行为语义（除非任务明确要求）
- 输出影响说明

---

## 测试归属与迁移规则（重要）
仓库测试归属与迁移规则由 Skill 管理，请优先使用对应 Skill，并遵循其规则来源文档。

Skill 路径：
- `.agents/skills/test-project-boundary/SKILL.md`

规则文档路径（Skill 内）：
- `.agents/skills/test-project-boundary/refs/test-project-boundary.md`

摘要规则：
1. `Bing.Utils.Tests` 仅承载主包测试、通用/共享测试、真实跨模块测试。
2. 模块专属测试必须放在 `Bing.Utils.<Module>.Tests`。
3. 按“主要断言目标”判断测试归属，不按引用数量判断。
4. 混合测试类优先按测试方法粒度拆分（安全可行时）。
5. 迁移测试时保持断言语义与测试意图不变。

执行测试相关任务前：
- 先读取并简要总结上述规则（或显式使用 `$test-project-boundary` skill）
- 先输出分类清单/迁移计划，再执行修改

---

## 文档任务规则
1. 文档优先基于源码、注释、测试事实生成。
2. 注释与实现冲突时，以实现为准，并标记冲突点。
3. 新增文档默认放在 `docs/` 对应目录下。
4. 输出结构清晰（标题、步骤、清单、TODO）。

---

## 输出格式建议
根据任务类型，优先按以下结构输出（可裁剪）：
1. 规则摘要（本次使用的规则/Skill）
2. 计划 / 分类清单
3. 实施变更（修改文件清单）
4. 验证结果（编译/测试/检查）
5. TODO / 风险 / 后续建议

---

## 任务提示词模板（Task Controller Prompts）
仓库内可复用的总控提示词模板存放于：
- `docs/quality/prompts/*`

执行批次任务时，优先使用对应总控提示词模板，并结合 `docs/quality/test-reclassification-plan.md` 当前状态运行。

## Prompt 模板入口
测试重归类相关总控提示词模板位于：
- `docs/quality/prompts/audit-only-controller.prompt.md`
- `docs/quality/prompts/multi-module-batch-controller.prompt.md`
- `docs/quality/prompts/multi-module-mixed-split-controller.prompt.md`

执行批次任务时，优先使用上述模板，并结合 `docs/quality/test-reclassification-plan.md` 当前状态运行。
