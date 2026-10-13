# Bing.Utils.Reflection.Tests.Integration

该目录承载反射裁剪、NativeAOT、程序集加载和依赖图集成验证，不属于生产包依赖，也不合入现有 xUnit 测试项目。

- `hosts/`：主反射、外部插件加载、第三方插件和未 root 模型宿主。
- `fixtures/`：模型、依赖图、强名称冲突以及 RuntimeCycle 编译夹具；RuntimeCycle 构建脚本与输入保留在对应夹具目录。
- `tools/`：NativeAOT 基线/源码映射/警告检查、插件 staging 和多插件容量工具。
- `baselines/`：当前诊断基线、allowlist、非仓库责任清单及未改动的历史快照。

当前目录映射、基线职责和运行入口见[反射验证目录与基线](../../docs/quality/reflection-validation-layout.md)。

NativeAOT 诊断批准范围见[NativeAOT 基线复核](../../docs/quality/reflection-nativeaot-baseline-review.md)。迁移后 24 个项目构建、脚本自测、fixture/framework 插件 smoke、NativeAOT 与五项严格门禁通过，执行范围及限制见[验证证据](../../docs/quality/reflection-validation-evidence.md)。
