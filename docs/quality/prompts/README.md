# 测试重归类 Prompt 模板索引（精简版）

## 1. 目的
本目录用于存放“测试重归类/迁移”场景的总控提示词模板（Controller Prompts），用于驱动 Codex 按统一流程执行：

- 审计清单（不改代码）
- 多模块批量迁移（仅可直接迁移项）
- 多模块混合测试拆分（方法级）
- 最小修复
- 编译/测试验证
- 报告生成
- 索引更新
- 规划文档执行记录更新

目标：
- 减少重复编写 prompt
- 降低漏步骤风险
- 提高批次执行一致性与可审计性
- 支持多模块持续迁移，而非单模块临时处理

---

## 2. 关联规则与文档（执行前必读）
- `AGENTS.md`
- `.agents/skills/test-project-boundary/SKILL.md`
- `.agents/skills/test-project-boundary/refs/test-project-boundary.md`
- `docs/quality/test-reclassification-plan.md`
- `docs/quality/migration-reports/README.md`

> 所有 prompt 模板默认依赖上述规则与文档。若路径调整，请同步更新模板内容。

---

## 3. 使用方式（推荐）

### 3.1 方式 A：手动复制模板内容执行
1. 打开对应 `.prompt.md` 文件
2. 按当前批次修改日期/范围（如需要）
3. 将内容粘贴给 Codex 执行

### 3.2 方式 B：让 Codex读取模板后执行（推荐）
示例（批量迁移）：

```text
请读取并执行 `docs/quality/prompts/multi-module-batch-controller.prompt.md`。

本轮目标模块：
- Bing.Utils.IdUtils -> Bing.Utils.IdUtils.Tests
- Bing.Utils.Text -> Bing.Utils.Text.Tests
- Bing.Utils.Collections -> Bing.Utils.Collections.Tests

将 `YYYY-MM-DD` 替换为今天日期，并按“连续运行”策略执行。
如果总量过大，自动拆分为子批次并先完整完成第一个子批次（含报告与索引更新）。
```

### 3.3 连续运行建议（减少中断）

建议在 Codex 较高自治模式下使用（如 Agent / Agent Full Access / CLI `--full-auto`），以减少“每一步确认”的中断。

若仍有频繁确认，可在执行时追加说明：

* 将任务视为端到端连续任务
* 混合测试与待确认项仅写入 TODO，不中断
* 若给出“下一步建议”，默认执行建议中的第1项

---

## 4. Prompt 模板总览（仅保留 3 个）

| 文件                                              | 场景               | 输入前提        | 主要产出                    | 连续执行 | 备注          |
| ----------------------------------------------- | ---------------- | ----------- | ----------------------- | ---- | ----------- |
| `audit-only-controller.prompt.md`               | 只审计不改代码          | 有或没有规划文档都可  | 审计结果 + 批次规划建议 + 文档草案    | 是    | 新批次前推荐先跑    |
| `multi-module-batch-controller.prompt.md`       | 多模块批量迁移（仅可直接迁移项） | 已有规划文档/审计结果 | 批量迁移结果 + 报告 + 索引 + 规划更新 | 是    | 主力模板        |
| `multi-module-mixed-split-controller.prompt.md` | 多模块混合测试拆分（方法级）   | 已有混合测试清单    | 拆分结果 + 专项报告 + 索引 + 规划更新 | 是    | 高风险专项，建议小批次 |

---

## 5. 模板详细说明

### 5.1 `audit-only-controller.prompt.md`

**用途**

* 执行“仅审计，不改代码”的测试归属审计
* 输出可直接迁移项 / 混合测试 / 待人工确认项
* 给出迁移批次规划建议（P0 / P1 / P2 / Batch）
* 生成或更新规划文档草案（默认先展示，不直接改代码）

**适用时机**

* 新阶段启动前（如准备开始 P1 / P2）
* 当前规划文档需要刷新
* 批量迁移前想先盘点范围和风险
* 不确定某批次该先迁什么模块时

**默认不处理**

* 不修改 `tests/*`
* 不修改 `src/*`
* 不进行实际迁移/拆分

**关键产出**

* 当前测试分布概览
* 归类审计结果（按目标测试项目分组）
* 迁移批次规划建议
* `docs/quality/test-reclassification-plan.md` 更新草案（可选落盘）

---

### 5.2 `multi-module-batch-controller.prompt.md`

**用途**

* 执行“多模块批量迁移”（仅处理可直接迁移项）
* 将 `Bing.Utils.Tests` 中明确属于多个目标模块的测试迁移到对应模块专属测试项目
* 自动支持子批次拆分（如 Batch-1 / Batch-2）

**适用时机**

* 已有审计结果或规划文档
* 希望一次处理多个模块（而不是单模块反复执行）
* 想完成“迁移 + 验证 + 报告 + 索引 + 规划更新”的闭环

**默认不处理**

* 混合测试类（需拆分）
* 待人工确认项
* 与本轮目标模块无关的测试
* 生产代码 (`src/*`)

**关键产出**

* `docs/quality/migration-reports/YYYY-MM-DD-batch-<modules>.md`
* 更新 `docs/quality/migration-reports/README.md`
* 更新 `docs/quality/test-reclassification-plan.md`（批次状态 / 执行记录）

---

### 5.3 `multi-module-mixed-split-controller.prompt.md`

**用途**

* 执行“多模块混合测试拆分专项”
* 对混合测试类按测试方法粒度拆分
* 将不同模块的方法迁移到对应模块测试项目

**适用时机**

* 批量迁移后，剩余大量混合测试类
* 需要专项处理高耦合测试类
* 规划文档或历史报告已标记混合测试清单

**默认策略**

* 先确认混合测试清单
* 先输出方法级拆分方案，再执行修改
* 高风险项暂缓并记录 TODO
* 可自动拆分为 Mixed-Batch-1 / Mixed-Batch-2

**默认不处理**

* 纯可直接迁移项（除非明确授权）
* 待人工确认项（只记录 TODO）
* 生产代码 (`src/*`)

**关键产出**

* `docs/quality/migration-reports/YYYY-MM-DD-mixed-batch-<modules>.md`
* 更新 `docs/quality/migration-reports/README.md`
* 更新 `docs/quality/test-reclassification-plan.md`（混合测试状态 / 执行记录）

---

## 6. 推荐执行顺序（精简版实践）

### 路线 A（稳妥，推荐）

1. `audit-only-controller.prompt.md`（先盘点）
2. `multi-module-batch-controller.prompt.md`（先做可直接迁移）
3. `multi-module-mixed-split-controller.prompt.md`（再做混合拆分）
4. 回到 `audit-only-controller.prompt.md`（复核下一批次）

### 路线 B（推进优先）

1. `multi-module-batch-controller.prompt.md`
2. `multi-module-batch-controller.prompt.md`（下一子批次）
3. `multi-module-mixed-split-controller.prompt.md`（集中清理混合类）

> 若你当前重点是“快速推进抽离成果”，优先路线 B；若你更关注边界稳定与后续维护，优先路线 A。

---

## 7. 模板维护规范（建议）

1. 每次修改模板时更新版本号（如 `v1.0 -> v1.1`）
2. 模板中只写“通用流程与约束”，不要写某次具体执行结果
3. 日期统一使用占位符：`YYYY-MM-DD`
4. 若规则路径变更，先更新 `AGENTS.md` 与 Skill，再更新模板
5. 若新增模板（例如专项去重、专项验证），先在本索引登记用途与产出

---

## 8. 常见问题（FAQ）

### Q1：总控提示词放在 `AGENTS.md` 吗？

不放。

* `AGENTS.md`：长期规则（项目级）
* `SKILL.md`：通用流程（能力级）
* `*.prompt.md`：执行级模板（本目录）

### Q2：为什么只保留这 3 个模板？

因为你的主场景已经升级为“多模块迁移抽离”，这 3 个模板正好覆盖完整闭环：

* 审计
* 批量迁移
* 混合拆分

模板更少，维护成本更低，也更不容易选错。

### Q3：如果 Codex 仍然频繁确认怎么办？

* 提升自治模式（如 Agent / Agent Full Access / CLI `--full-auto`）
* 在执行时补充“连续运行 + 默认决策”说明
* 让模板自动拆分子批次（Batch-1 / Batch-2）
* 将高风险项统一写 TODO，不阻塞整轮任务

### Q4：模板能否让 Codex直接读取执行？

可以，推荐这样做。示例：

```text
请读取并执行 `docs/quality/prompts/audit-only-controller.prompt.md`。
按“连续运行”策略执行，并先输出规划文档更新草案，不要修改测试代码。
```

### Q5：什么时候用批量迁移，什么时候用混合拆分？

* **批量迁移**：处理“可直接迁移（文件级）”项
* **混合拆分**：处理“需按方法级拆分”的混合测试类

通常先批量迁移，再混合拆分。

---

## 9. 快速调用示例（可直接复制）

### 9.1 审计（不改代码）

```text
请读取并执行 `docs/quality/prompts/audit-only-controller.prompt.md`。

本轮目标模块：
- Bing.Utils.IdUtils
- Bing.Utils.Text
- Bing.Utils.Collections
- Bing.Utils.DateTime
- Bing.Utils.Reflection

按“连续运行”策略执行，仅输出审计结果与规划文档更新草案；不要修改测试代码。
```

### 9.2 多模块批量迁移（主力）

```text
请读取并执行 `docs/quality/prompts/multi-module-batch-controller.prompt.md`。

本轮目标模块：
- Bing.Utils.IdUtils -> Bing.Utils.IdUtils.Tests
- Bing.Utils.Text -> Bing.Utils.Text.Tests
- Bing.Utils.Collections -> Bing.Utils.Collections.Tests
- Bing.Utils.DateTime -> Bing.Utils.DateTime.Tests
- Bing.Utils.Reflection -> Bing.Utils.Reflection.Tests

将 `YYYY-MM-DD` 替换为今天日期，并按“连续运行”策略执行。
如果总量过大，自动拆分为子批次并先完整完成第一个子批次（含报告与索引更新）。
```

### 9.3 多模块混合测试拆分专项

```text
请读取并执行 `docs/quality/prompts/multi-module-mixed-split-controller.prompt.md`。

本轮目标模块：
- Bing.Utils.IdUtils -> Bing.Utils.IdUtils.Tests
- Bing.Utils.Text -> Bing.Utils.Text.Tests
- Bing.Utils.Collections -> Bing.Utils.Collections.Tests

将 `YYYY-MM-DD` 替换为今天日期，并按“连续运行”策略执行。
若混合测试类数量较多，请自动拆分子批次并先处理 1~3 个代表性测试类。
```

---

## 10. 后续扩展（可选，不必现在做）

当前仅保留 3 个模板已足够支撑主流程。若后续确有需要，再考虑补充专项模板：

* [ ] `dedup-matrix-controller.prompt.md`（重复测试映射/去重专项）
* [ ] `verification-only-controller.prompt.md`（只做编译/测试验证与报告）
* [ ] `report-only-controller.prompt.md`（只补报告与索引）