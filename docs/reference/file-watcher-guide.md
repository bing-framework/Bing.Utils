# 文件监控

`FileWatcher` 使用 `FileSystemWatcher` 监控目录及其子目录。旧构造函数、`EventHandler` 委托字段以及 `AddPath`、`DeletePath`、`Start`、`Stop` 方法继续可用。`Stop` 保留原有的清空目录约定，`StopAsync` 也会清空目录；再次启动前应重新 `AddPath`。`DeletePath` 可单独移除不再需要的目录。

```csharp
var root = Path.GetFullPath("data");
await using var watcher = new FileWatcher(new[] { root });
watcher.EventHandler += (sender, args) =>
{
    Console.WriteLine($"{args.ChangeTypes}: {args.FullPath}");
};
watcher.Error += (sender, args) =>
{
    Console.Error.WriteLine($"监控目录 {args.Path} 发生错误：{args.Exception}");
};

watcher.Start();
// 完成监控后等待后台检查和已进入的回调收尾。
await watcher.StopAsync();
```

`Stop()` 只发起停止，不等待后台任务或正在执行的回调。`StopAsync()` 会关闭当前运行代次的通知入口，并等待后台任务和已进入的回调结束；它返回后不会再开始该代次的新回调。待 debounce 的事件会在停止时丢弃。不要在事件回调中同步等待 `StopAsync()` 或 `DisposeAsync()`，因为该回调本身属于停止屏障的一部分；阻塞的回调会延长停止时间。

默认不合并事件，以保持原有逐通知回调行为。需要合并短时间内同一路径的重复通知时，可配置 debounce 窗口及待处理事件上限：

```csharp
var options = new FileWatcherOptions
{
    DebounceInterval = TimeSpan.FromMilliseconds(200),
    MaxPendingEvents = 2048,
    InternalBufferSize = 16 * 1024
};
await using var watcher = new FileWatcher(new[] { root }, options);
```

debounce 会按路径合并变更标记，不承诺保留原始通知次数和顺序。超过待处理上限或底层通知缓冲区溢出时，`Error` 会报告错误并请求目录快照重扫。文件事件处理器抛出的异常会通知 `Error`；`Error` 处理器自身抛出的异常会被忽略，避免递归错误通知。重扫只比较前后快照并报告可识别的创建、删除和修改；它不能恢复溢出期间每一次变化、短暂创建后删除的文件或原始重命名序列。快照遍历跳过符号链接和其他 reparse point，不递归进入链接目录。文件系统事件仍可能因平台和权限产生重复、合并或遗漏，调用方应按当前文件状态设计处理逻辑。

`ScanInterval` 默认 10 秒，只控制已添加目录消失或重新出现时的检测间隔；普通文件变化由操作系统通知驱动。`ScanInterval` 和 `DebounceInterval` 必须大于零，且不超过运行时定时器可等待的范围。快照重扫会遍历监控目录下的普通文件及目录，目录规模较大时应将其视为溢出后的恢复操作。重扫差异直接逐项交付，不再次进入 debounce 队列，避免差异数量超过队列上限时丢失恢复事件。

`FileWatcher` 实现 `IDisposable` 和 `IAsyncDisposable`。同步释放会发起停止；使用 `await using` 或 `DisposeAsync()` 可等待后台任务和在途回调收尾。目录路径在添加时规范化；不存在的目录不会被添加，目录在监控期间暂时消失后再次出现时会自动重新挂载。
