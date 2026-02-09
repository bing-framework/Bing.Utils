# Copilot Instructions for Bing.Utils

你正在协助维护 `Bing.Utils`（.NET 工具类库，多 NuGet 子包）。

## 1) 目标
- 输出生产可用、可测试、可维护的 C# 代码与文档。
- 优先遵循现有项目结构：`src/`、`tests/`、`benchmarks/`、`docs/`。
- 避免引入不必要的第三方依赖，尤其在核心工具包中。

## 2) 分层与依赖规则
- 抽象/契约层不依赖具体实现。
- Core 不反向依赖 Integrations。
- 测试项目仅用于验证，不被生产代码引用。
- 可选能力（如图像引擎）放在独立包，不污染核心包依赖树。

## 3) 命名规范
- 项目名：`Bing.Utils.{Capability}`
- 接口：`I*`
- 扩展类：`*Extensions`
- 异步方法：`*Async`
- 测试类：`{TypeName}Tests`
- 测试方法：`Method_State_Expected`

## 4) 代码风格
- 开启并保持 Nullable 友好。
- 公共 API 必须有 XML 注释（`<summary>`、`<param>`、`<returns>`、`<exception>`）。
- 优先 `TryXxx` 模式替代异常控制流（高频路径）。
- 所有输入参数做 guard clause。
- 关注跨平台行为（路径、编码、时区）。

## 5) 测试要求
- 每个新增公共方法必须有对应测试。
- 至少覆盖：正常路径、边界值、异常路径。
- 性能敏感代码补 benchmark（如已有基准工程则追加用例）。

## 6) 文档要求
- 变更时同步更新 `docs/`。
- API 变更必须写明兼容性影响（Breaking / Non-breaking）。
- 示例代码可直接复制运行。

## 7) 提交建议
- 提交信息建议：`feat:` / `fix:` / `refactor:` / `docs:` / `test:` / `perf:`。
- 单次 PR 聚焦一个主题，避免“超大混合提交”。

## 8) 禁止事项
- 不要编造不存在的项目/类型/依赖。
- 不要跨层直接访问不应依赖的实现。
- 不要在核心包引入重量级外部库（除非明确要求）。
