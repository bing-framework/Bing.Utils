# Bing.Utils.Cron

独立的 Cron 解析与触发时间计算包，支持 `netstandard2.0;net8.0`。仅依赖固定版本 Cronos `[0.13.0]`，不依赖核心包，不包含调度器。

核心入口位于 `src/Bing.Utils.Cron/Bing/Date/Cron/CronSchedule.cs`；自有 `CronExpressionFormat` 区分五段与六段格式。解析失败提供中文说明，时间计算要求显式时区，结果统一 UTC；有界枚举支持取消，达到数量上限正常结束。

```csharp
var schedule = Bing.Date.Cron.CronSchedule.Parse("0 9 * * 1-5");
var next = schedule.GetNextOccurrence(DateTimeOffset.UtcNow, TimeZoneInfo.Utc);
```

月日和星期同时指定使用 AND；保留 Cronos 的 DST 规则。无匹配结果返回 null，不声明 Quartz 七段兼容，不提供 H 字段随机种子。专属测试位于 `tests/Bing.Utils.Cron.Tests`，在 net6/net8 上验证，net6 消费 netstandard2.0 产物。

完整示例、异常与生命周期说明见 [通用工具指南](../reference/general-utilities-guide.md)。
