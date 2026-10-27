# Bing.Utils.Diff

支持取消和协作计算预算的同步行级文本比较，目标框架为 `netstandard2.0`、`net8.0`，不依赖 DiffPlex NuGet 包。

```csharp
using Bing.Text.Diff;

var result = TextDiff.Compare("第一行\n旧内容", "第一行\n新内容", new TextDiffOptions
{
    MaxDuration = TimeSpan.FromSeconds(30),
    IgnoreCase = false,
    IgnoreLeadingTrailingWhitespace = false
}, cancellationToken);

foreach (var block in result.Blocks)
    Console.WriteLine($"{block.Kind}: 旧 {block.OldStartLine}+{block.OldLineCount}，新 {block.NewStartLine}+{block.NewLineCount}");
```

每份输入默认最多 200,000 行、16,000,000 个 UTF-16 代码单元；整个调用默认预算 30 秒。三项都可调整，必须为正值。输入限制以及扫描、哈希、前后缀、对角线搜索和结果构建共用一次预算。字符扫描每最多 4096 个代码单元、算法循环每最多 1024 步检查取消及时间；这不是硬实时中断保证。

取消抛 `OperationCanceledException`。超限抛 `TextDiffLimitExceededException`，`Code` 分别为 `diff.input_characters`、`diff.input_lines`、`diff.duration`；不返回部分结果。空参数或无效选项分别抛 `ArgumentNullException`、`ArgumentOutOfRangeException`。

`OldLines`、`NewLines` 保存原始行，`Blocks` 只包含插入、删除或替换块，全部只读。索引从 0 开始。CRLF、LF、CR 都是分隔符，换行样式不参与比较；末尾换行保留空行，空文本为 0 行。默认大小写和空白敏感；忽略设置只影响匹配，不修改原始输出。启用忽略设置时，差异块表达的是比较语义上的变化，不能用于还原被忽略的原文差别；完整目标原文行始终可从 `NewLines` 读取。

适合日志追加和稀疏行修改。高差异输入可能耗尽预算。调用不会创建后台任务；需要 UI 后台运行时由调用方安排线程，并把取消令牌传入 `Compare`。结果持有原始行字符串，使用完成后按普通托管对象释放引用即可。

内部最短编辑路径算法改编自 DiffPlex `DiffPlex-1.7.0`（Apache-2.0）。来源、许可证及修改说明随包放在 `ThirdParty/DiffPlex/`。未提供字符级 Diff、三方合并或伪异步 API。
