# Bing.Utils.Diff

独立的同步行级 Diff 包，支持 `netstandard2.0;net8.0`，无运行时第三方包依赖。最短编辑路径内核改编自 DiffPlex `DiffPlex-1.7.0`，必要源码均为内部类型，Apache-2.0 许可和修改说明随包分发。

入口为 `Bing.Text.Diff.TextDiff.Compare`，返回原始行与不可变差异块。默认每份输入最多 200000 行、16000000 个 UTF-16 单元，整个调用预算 30 秒；扫描、哈希、算法和结果构建均检查真实取消和预算。

```csharp
var result = Bing.Text.Diff.TextDiff.Compare("a\nb", "a\nc", cancellationToken: token);
```

公开入口位于 `src/Bing.Utils.Diff/TextDiff.cs`，适配算法位于 `Internal/DiffPlex/LineDiffer.cs`。显式工作栈替代递归，两条线性工作数组复用；高差异输入可能预算失败，不返回部分结果。首版没有字符级、三方合并或异步包装。

专属测试位于 `tests/Bing.Utils.Diff.Tests`，net6 使用 netstandard2.0 产物，net8 使用 net8 产物。完整比较语义、可调边界和异常代码见 [通用工具指南](../reference/general-utilities-guide.md)，验证证据见 [实施记录](../quality/general-utilities-execution.md)。
