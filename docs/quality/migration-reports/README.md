# 测试迁移报告索引（Migration Reports Index）

## 1. 目的
本目录用于归档每一轮测试重归类迁移的验证报告，便于：
- 跟踪迁移进度（试点 / P0 / P1 / P2）
- 审计每轮执行范围与验证结果
- 快速定位遗留问题与风险项
- 支持回滚与后续优化决策

---

## 2. 关联文档
- `AGENTS.md`
- `.agents/skills/test-project-boundary/SKILL.md`
- `.agents/skills/test-project-boundary/refs/test-project-boundary.md`
- `docs/quality/test-reclassification-plan.md`

> 注：若规则文档已迁移至 Skill 内，以上以 Skill 内版本为准。

---

## 3. 命名规范（报告文件）
建议每轮报告使用以下命名格式：

`YYYY-MM-DD-<scope>-<batch>.md`

示例：
- `2026-02-25-idutils-pilot.md`
- `2026-02-26-p0-text-collections.md`
- `2026-02-27-p1-datetime-reflection.md`
- `2026-02-28-p2-http-drawing.md`

命名建议：
- `scope`：本轮主要模块或范围（如 `idutils` / `text-collections`）
- `batch`：阶段标识（如 `pilot` / `p0` / `p1` / `p2`）

---

## 4. 迁移阶段总览

### 4.1 阶段定义
- **Pilot（试点）**：先处理一个边界清晰模块，验证流程与规则执行效果
- **P0**：边界清晰、收益高、风险低模块（优先）
- **P1**：中等复杂模块 + 混合测试拆分
- **P2**：复杂协作模块 / 集成测试整理

### 4.2 当前进度概览
| 阶段 | 状态 | 已完成轮次 | 备注 |
|---|---|---:|---|
| Pilot | `进行中` | `1` | 已完成 `IdUtils` 试点首轮与 `IdTest` 去重（部分成功） |
| P0 | `进行中` | `5` | 已完成 `IdUtils` 残余审计、混合拆分收尾与 Legacy 去重 Batch-1/2；待转向下一模块或其他去重项 |
| P1 | `TODO` | `TODO` | `DateTime` / `Reflection` / 混合测试拆分 |
| P2 | `TODO` | `TODO` | `Http` / `Drawing*` / Integration |

---

## 5. 报告索引（按时间倒序或阶段分组）

> 建议每新增一轮报告后，更新本节。

### 5.1 Pilot（试点）
| 日期 | 报告文件 | 模块/范围 | 类型 | 结果 | 摘要 |
|---|---|---|---|---|---|
| `2026-02-25` | `2026-02-25-idutils-pilot.md` | `IdUtils（Bing.Utils.Tests -> Bing.Utils.IdUtils.Tests）` | Pilot | `部分成功` | 已迁移 `IdGenerators/*` 3 个文件并通过编译/最小测试；已完成 `Bing.Helpers.IdTest` 重复覆盖比对与去重删除 |

---

### 5.2 P0（边界清晰模块）
| 日期 | 报告文件 | 模块/范围 | 类型 | 结果 | 摘要 |
|---|---|---|---|---|---|
| `2026-02-25` | `2026-02-25-batch-idutils.md` | `IdUtils（Bing.Utils.Tests 残余审计）` | P0 | `成功` | 完成连续运行批次：确认无新增文件级直迁项；剩余项均为混合测试/应保留项，并已更新规划与索引 |
| `2026-02-25` | `2026-02-25-mixed-batch-idutils.md` | `IdUtils（Mixed Split / UnitTest1）` | P0 | `成功` | 完成 `UnitTest1` 方法级拆分首批：将 `Test_Id` 迁入 `Bing.Utils.IdUtils.Tests`，并通过编译与最小测试验证 |
| `2026-02-25` | `2026-02-25-mixed-batch-idutils-2.md` | `IdUtils（Mixed Split 收尾复审）` | P0 | `成功` | 完成 `UnitTest1` 剩余 IdUtils 方法复审：无新增可拆分项；关闭 `IdUtils` Mixed-Batch-2 |
| `2026-02-25` | `2026-02-25-batch-idutils-legacy-dedupe-1.md` | `IdUtils（Legacy 去重 Batch-1）` | P0 | `成功` | 删除 `ObjectId/TimestampId` 两个高重叠 Legacy 测试文件，并用 `IdTest` 6 个替代覆盖用例完成验证 |
| `2026-02-25` | `2026-02-25-batch-idutils-legacy-dedupe-2.md` | `IdUtils（Legacy 去重 Batch-2）` | P0 | `成功` | 删除 `Snowflake` Legacy 测试文件，并用 `IdSnowflakeTest + SnowflakeGeneratorConcurrencyContractTest` 5 个替代覆盖用例完成验证 |
| `TODO` | `TODO` | `TODO` | P0 | `TODO` | `TODO` |

---

### 5.3 P1（中等复杂 / 混合测试拆分）
| 日期 | 报告文件 | 模块/范围 | 类型 | 结果 | 摘要 |
|---|---|---|---|---|---|
| `TODO` | `TODO` | `DateTime + Reflection` | P1 | `TODO` | `TODO` |
| `TODO` | `TODO` | `Mixed test split` | P1 | `TODO` | `TODO` |

---

### 5.4 P2（复杂协作 / 集成测试）
| 日期 | 报告文件 | 模块/范围 | 类型 | 结果 | 摘要 |
|---|---|---|---|---|---|
| `TODO` | `TODO` | `Http + Drawing` | P2 | `TODO` | `TODO` |

---

## 6. 关键指标（可选，持续更新）

> 本节用于长期观察迁移效果，可按需维护。

### 6.1 归类迁移统计
| 指标 | 数值 | 说明 |
|---|---:|---|
| 已迁移测试文件数 | `TODO` | 累计 |
| 已迁移测试类数 | `TODO` | 累计 |
| 已拆分混合测试类数 | `TODO` | 累计 |
| 待人工确认项数 | `TODO` | 当前未处理 |
| 遗留混合测试类数 | `TODO` | 当前未处理 |

### 6.2 风险收敛情况
- [ ] `Bing.Utils.Tests` 中模块专属测试显著减少
- [ ] 模块专属测试项目边界更清晰
- [ ] 混合测试拆分策略稳定
- [ ] 共享测试辅助类抽取方案已落地（如适用）
- [ ] CI 可按模块分层执行测试（如已配置）

---

## 7. 常见执行流程（供复用）

### 7.1 每轮迁移建议流程
1. 阅读规则（`AGENTS.md` + Skill 规则）
2. 读取 `docs/quality/test-reclassification-plan.md`
3. 输出本轮迁移清单（可直接迁移 / 混合 / 待确认）
4. 执行迁移与最小修复
5. 运行最小验证（编译 / 测试）
6. 生成迁移验证报告（本目录）
7. 更新本索引（增加一条报告记录）
8. 更新规划文档执行记录章节

### 7.2 禁止事项（提醒）
- 不要一次性全量迁移全仓库
- 不要跳过“迁移清单”直接改代码
- 不要强行迁移混合测试类
- 不要在未授权情况下修改生产代码

---

## 8. 后续优化事项（长期）
- [ ] 建立 `tests/Common` 共享测试基础设施（如适用）
- [ ] 增加测试归属审计脚本/检查项（防止回流）
- [ ] 建立 defect-to-test 回归映射
- [ ] 为模块专属测试项目补充 README（测试范围、运行方式、约束）
- [ ] 将迁移结果同步回 `docs/quality/test-reclassification-plan.md`

---

## 9. 维护说明
- 本文件为迁移报告索引，记录“每轮报告入口与结果摘要”
- 详细执行内容请查看各轮报告文件
- 若规则文档位置发生变化，请优先更新 `AGENTS.md` 与 Skill 内引用路径
