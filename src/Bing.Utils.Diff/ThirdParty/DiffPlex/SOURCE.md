# DiffPlex 来源与修改说明

- 项目：https://github.com/mmanela/diffplex
- 固定标签：`DiffPlex-1.7.0`
- 来源文件：https://github.com/mmanela/diffplex/blob/DiffPlex-1.7.0/DiffPlex/Differ.cs
- 上游许可证：https://github.com/mmanela/diffplex/blob/DiffPlex-1.7.0/License.txt
- 许可证：Apache License 2.0，原文保留在同目录 `LICENSE.txt`。
- 原项目作者及贡献者：Matthew Manela 与 DiffPlex contributors。

本包仅纳入行级最短编辑路径需要的算法和最小内部模型，放在 `Internal/DiffPlex/LineDiffer.cs`。未纳入 UI、字符/词分块器、构建器或日志组件，也不提供上游公共类型。

2026-10-27 适配内容：

1. 命名空间和类型改为内部可见，保留上游双向对角线搜索的编辑路径选择。
2. 将递归子问题处理改为显式工作栈，复用两条线性对角线数组。
3. 在前后缀扫描、搜索及标记循环加入取消与预算检查。
4. 删除上游日志及不使用的重载，使用中文内部异常说明。
5. 上层分行、分块哈希、原始行输出、只读差异块、资源上限和测试时钟由 Bing.Utils 实现。

每份修改后的来源文件均注明来源及变更；本材料与许可证随 NuGet 包一起分发。
