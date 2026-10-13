# Bing.Utils 文档

## 快速入口

- [项目概览](00-overview.md)
- [能力地图](00-capability-map.md)
- [文档与实现盘点](00-documentation-audit.md)
- [分类模块目录](01-module-catalog.md)
- [模块文档索引](modules/README.md)
- [API 参考说明](reference/api-reference.md)
- [安全与密码学专题](security.md)
- [质量与验证资料](quality/)
- [反射验证状态](quality/reflection-validation-evidence.md)：当前候选的测试、发布结果与历史证据边界。
- [反射失败契约迁移](quality/reflection-contract-migration.md)：异常契约变化及已迁移的消费者测试。
- [反射性能证据](quality/reflection-performance-evidence.md)：历史逐轮测量与显式预算用法。
- [反射裁剪证据](quality/reflection-trim-evidence.md)：trimming/NativeAOT 部署命令和支持边界。
- [反射实施状态](quality/reflection-todo-status.md)：当前剩余问题与历史任务记录。
- [NativeAOT 基线复核](quality/reflection-nativeaot-baseline-review.md)：未通过门禁的精确诊断及待确认提案。
- [贡献指南](CONTRIBUTING.md)

## 维护约定

- 模块标准入口统一使用 `docs/modules/Bing.Utils*.md` 命名，并与 `src` 下项目一一对应。
- 每份模块文档至少说明功能、核心逻辑和文件位置、接口示例、依赖配置、限制及待确认项。
- 未在代码或测试中确认的能力必须标记为“待实现/待确认”，不得根据项目名推断实现。
- `docs/quality`、专题文档和小写旧模块文档用于保存迁移、审查与深度实现资料，不代替模块入口。
