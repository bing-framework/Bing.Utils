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
- [TODO 执行记录](quality/todo-execution.md)：既定清单当前状态、本轮交付及剩余依赖。
- [依赖注入模块](modules/Bing.Utils.DependencyInjection.md)：显式接口批量注册、受控扫描及开放/复杂泛型示例。
- [依赖注入执行记录](quality/dependency-injection-execution.md)：DI-01～DI-04 范围及编译、模块测试和打包证据。
- [图像性能与反射决策](quality/performance-scope-decisions.md)：固定负载、校准预算、执行顺序及反射剩余项边界。
- [图像性能执行记录](quality/image-performance-execution.md)：质量 P2-1 的 8 场景负载、冒烟、校准及预算验收用法与证据。
- [六类边界检查清单](quality/boundary-test-checklist.md)：补测时统一记录输入、溢出、并发和性能边界的适用性、契约与证据。
- [测试协作模板索引](quality/prompts/README.md)：审计、迁移、补测与风格规范化的既有模板入口。
- [反射验证状态](quality/reflection-validation-evidence.md)：当前候选的测试、发布结果与历史证据边界。
- [反射失败契约迁移](quality/reflection-contract-migration.md)：异常契约变化及已迁移的消费者测试。
- [反射性能证据](quality/reflection-performance-evidence.md)：历史逐轮测量与显式预算用法。
- [反射预算校准](quality/reflection-budget-execution.md)：本机四项冻结预算、正式确认结果与策略适用边界。
- [反射预算校准](quality/reflection-budget-execution.md)：本机四项冻结预算、正式确认结果与策略适用边界。
- [反射裁剪证据](quality/reflection-trim-evidence.md)：trimming/NativeAOT 部署命令和支持边界。
- [反射实施状态](quality/reflection-todo-status.md)：当前剩余问题与历史任务记录。
- [NativeAOT 基线复核](quality/reflection-nativeaot-baseline-review.md)：批准的精确诊断范围与严格门禁复核结果。
- [反射验证目录与基线](quality/reflection-validation-layout.md)：已迁移的集成验证入口、基线职责与历史路径映射。
- [贡献指南](CONTRIBUTING.md)

## 维护约定

- 模块标准入口统一使用 `docs/modules/Bing.Utils*.md` 命名，并与 `src` 下项目一一对应。
- 每份模块文档至少说明功能、核心逻辑和文件位置、接口示例、依赖配置、限制及待确认项。
- 未在代码或测试中确认的能力必须标记为“待实现/待确认”，不得根据项目名推断实现。
- `docs/quality`、专题文档和小写旧模块文档用于保存迁移、审查与深度实现资料，不代替模块入口。
