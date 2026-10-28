# Bing.Utils.Templates.Scriban

提供受限的 Scriban 模板编译与渲染入口，支持条件、循环、泛型字典和 DTO 公共只读属性。每次渲染均创建独立上下文，默认严格变量；不暴露 include 或宿主方法，并在渲染期间限制输出。组合示例见 `docs/integration-capabilities.md`。

目标框架：`netstandard2.0;net8.0`。
