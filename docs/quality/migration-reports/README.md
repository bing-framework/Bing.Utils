# 测试迁移报告索引

本目录保存测试项目边界治理产生的迁移记录，用于追踪测试归属、迁移范围和验证结果。报告只反映生成时的仓库状态，不替代当前源码、测试项目或模块文档。

## 现有报告

| 日期 | 批次 | 覆盖模块 | 报告 |
| --- | --- | --- | --- |
| 2026-02-26 | B1 | Text、Collections、DateTime、Reflection、Drawing、Http | [查看报告](2026-02-26-batch-text-collections-datetime-reflection-drawing-http-b1.md) |
| 2026-02-26 | B2 | Text、Collections、DateTime、Reflection、Drawing、Http | [查看报告](2026-02-26-batch-text-collections-datetime-reflection-drawing-http-b2.md) |

## 使用说明

- 先从报告中的“迁移清单”和“验证结果”定位历史调整，再以当前 `tests` 目录和项目文件为最终依据。
- 新增报告应使用 `YYYY-MM-DD-batch-<scope>-<batch>.md` 形式命名，并同步更新本索引。
- 原索引曾引用但当前目录中不存在的历史报告无法核验，统一标记为**待确认**，不在此处补造记录。

## 已知限制与注意事项

- 报告是历史快照，文件路径、测试数量和验证结论可能随后续提交变化。
- 本索引不判断迁移是否仍符合当前测试边界规则；需要审计时应重新扫描项目引用和测试内容。
- 2026-02-26 之前是否存在未纳入仓库的迁移报告属于**待确认**。
