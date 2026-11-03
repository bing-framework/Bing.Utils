# UUIDv1 使用指南

`Bing.IdUtils.Uuid1` 按 RFC 9562 的 UUIDv1 字段布局生成基于时间的 UUID。时间字段是从 Gregorian epoch（1582-10-15 UTC）开始计算的 100 纳秒间隔，版本位为 `1`，变体位为 RFC 变体 `10`。

```csharp
using Bing.IdUtils;

Guid current = Uuid1.Generate();
Guid importedTime = Uuid1.Generate(new DateTimeOffset(2024, 1, 2, 3, 4, 5, TimeSpan.Zero));
```

节点字段在进程首次使用时由密码学随机源生成，并设置 multicast 位，因此不会把本机 MAC 地址写入 UUID。时钟序列同样在进程内随机初始化；时间回拨时生成器推进时钟序列，同一时间字段重复时使用逻辑 100 纳秒递增，以避免当前进程内的重复值。

生成器分别保存上次成功调用的物理时间和已编码的逻辑时间。相同物理时间重复调用，或物理时间向前恢复但仍落后于逻辑时间时，clock sequence 保持不变；只有物理时间相对上次成功调用倒退时才推进 clock sequence。无参入口在状态锁内采样 UTC 时间，避免线程排队造成采样顺序倒置。

这些状态只在当前进程内有效。UUIDv1 不提供跨进程、跨主机的节点协调或重启恢复保证；需要分布式唯一性时，部署方必须确保节点身份和时钟策略不冲突。

`Uuid1.Generate(DateTimeOffset)` 拒绝 Gregorian epoch 之前以及超出 UUIDv1 60 位时间字段的时刻。`.NET Guid` 的前三个字段使用混合端序，跨系统交换原始字节时应使用规范 UUID 字符串，或自行转换为 RFC 网络字节序。

`GuidProvider.Create(namespace, name, GuidVersion.TimeBased)` 和 `GuidVersion.DceSecurity` 保持显式不支持，分别抛出 `NotSupportedException`；名称型版本使用 `GuidVersion.NameBasedMd5`（v3）或 `GuidVersion.NameBasedSha1`（v5）。
