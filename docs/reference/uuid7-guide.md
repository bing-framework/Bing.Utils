# UUIDv7 使用指南

`Bing.IdUtils.Uuid7` 按 RFC 9562 生成 UUIDv7。前 48 位编码 Unix Epoch 毫秒，版本字段设为 7、RFC 变体字段设为 `10`，其余 74 位由密码学安全随机数填充。生成器不承诺同一毫秒内单调递增或全局唯一。

```csharp
using Bing.IdUtils;

Guid current = Uuid7.Generate();
Guid importedTime = Uuid7.Generate(DateTimeOffset.FromUnixTimeMilliseconds(1645557742000));

if (Uuid7.TryGetTimestamp(current, out var unixTimeMilliseconds))
{
    DateTimeOffset createdAt = DateTimeOffset.FromUnixTimeMilliseconds(unixTimeMilliseconds);
}
```

指定时间入口按 Unix 毫秒编码，毫秒以下精度会被舍弃。Unix Epoch 之前的时间无效；时间戳必须适合 UUID 的无符号 48 位字段。`TryGetTimestamp` 只接受 RFC 变体的版本 7 标识，输出完整 48 位 Unix 毫秒数，其他 UUID 返回 `false` 并将输出设为零。

UUID 字符串使用 RFC 字段顺序，可按固定时间戳前缀进行字典序排序。RFC 的 UUID 字节布局为网络大端序，而 .NET `Guid.ToByteArray()` 对前三个字段使用混合端序；跨系统交换原始字节时应显式转换字节序，或使用规范 UUID 字符串。

字段布局和版本/变体位遵循 [RFC 9562 第 5.7 节](https://www.rfc-editor.org/rfc/rfc9562#section-5.7) 与第 4 节。
