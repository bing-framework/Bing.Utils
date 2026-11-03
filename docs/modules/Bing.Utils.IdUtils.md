# Bing.Utils.IdUtils
## 1. 包职责（Scope）
- 解决的问题
- 提供 ObjectId、Snowflake、UUIDv1、UUIDv7、安全随机标识及时间戳等标识生成与管理能力。[证据] `src/Bing.Utils.IdUtils/Bing/IdUtils/ObjectId.cs:18` [证据] `src/Bing.Utils.IdUtils/Bing/IdUtils/SnowflakeGenerator.cs:8` [指南] [UUIDv1 使用指南](../reference/uuid1-guide.md)、[UUIDv7 使用指南](../reference/uuid7-guide.md)
- 提供统一入口 `Id`，支持上下文 ID、雪花配置与批量生成。[证据] `src/Bing.Utils.IdUtils/Bing/Helpers/Id.cs:9` [证据] `src/Bing.Utils.IdUtils/Bing/Helpers/Id.SnowflakeId.cs:61` [证据] `src/Bing.Utils.IdUtils/Bing/Helpers/Id.SnowflakeId.cs:139`
- 不解决的问题（Out of Scope）
- 不负责外部分布式协调（注册中心、机器号自动分配等需外部系统提供）。

## 2. 核心类型与扩展方法
- 类型/方法签名摘要
- `ObjectId`：`GenerateNewId`、`Parse/TryParse`、`Pack/Unpack`。[证据] `src/Bing.Utils.IdUtils/Bing/IdUtils/ObjectId.cs:273` [证据] `src/Bing.Utils.IdUtils/Bing/IdUtils/ObjectId.cs:339` [证据] `src/Bing.Utils.IdUtils/Bing/IdUtils/ObjectId.cs:354` [证据] `src/Bing.Utils.IdUtils/Bing/IdUtils/ObjectId.cs:308`
- `SnowflakeGenerator.Create(...)`：Twitter/Seata 两种入口。[证据] `src/Bing.Utils.IdUtils/Bing/IdUtils/SnowflakeGenerator.cs:17` [证据] `src/Bing.Utils.IdUtils/Bing/IdUtils/SnowflakeGenerator.cs:24`
- `Uuid7.Generate()`、`Generate(DateTimeOffset)`、`TryGetTimestamp(...)`：按 RFC 9562 生成 UUIDv7 并提取 Unix 毫秒时间戳。[证据] `src/Bing.Utils.IdUtils/Bing/IdUtils/Uuid7.cs:23` [证据] `src/Bing.Utils.IdUtils/Bing/IdUtils/Uuid7.cs:31` [证据] `src/Bing.Utils.IdUtils/Bing/IdUtils/Uuid7.cs:63`
- `SecureRandomId.Create(length, alphabet)`：按调用方字符集生成精确长度的密码学随机文本。[证据] `src/Bing.Utils.IdUtils/Bing/IdUtils/SecureRandomId.cs:30`
- `RandomNonceStrGenerator.Create(...)`：兼容入口，长度小于等于 16 时仍生成 16 字符。[证据] `src/Bing.Utils.IdUtils/Bing/IdUtils/RandomNonceStrGenerator.cs:26`
- `Id`：`CreateObjectId`、`CreateSnowflakeId`、`CreateSnowflakeIds`。[证据] `src/Bing.Utils.IdUtils/Bing/Helpers/Id.cs:131` [证据] `src/Bing.Utils.IdUtils/Bing/Helpers/Id.SnowflakeId.cs:109` [证据] `src/Bing.Utils.IdUtils/Bing/Helpers/Id.SnowflakeId.cs:139`
- 输入输出约定
- `Id.CreateSnowflakeId` 优先读取上下文 `SetId`，否则走生成器。[证据] `src/Bing.Utils.IdUtils/Bing/Helpers/Id.SnowflakeId.cs:111` [证据] `src/Bing.Utils.IdUtils/Bing/Helpers/Id.SnowflakeId.cs:114`
- `CreateObjectId` 总是新生成，不受上下文影响。[证据] `src/Bing.Utils.IdUtils/Bing/Helpers/Id.cs:128` [证据] `src/Bing.Utils.IdUtils/Bing/Helpers/Id.cs:131`
- 边界行为（null、空集合、非法参数）
- `ObjectId` 构造器对 null/长度非法输入抛异常。[证据] `src/Bing.Utils.IdUtils/Bing/IdUtils/ObjectId.cs:122` [证据] `src/Bing.Utils.IdUtils/Bing/IdUtils/ObjectId.cs:170`
- `Uuid7.Generate(timestamp)` 拒绝 Unix Epoch 前时间及超出 48 位毫秒字段的时间；毫秒以下精度会舍弃。[证据] `src/Bing.Utils.IdUtils/Bing/IdUtils/Uuid7.cs:31`
- `SecureRandomId.Create` 要求正长度，以及 2 到 256 个互不重复、非代理 UTF-16 字符组成的字符集。[证据] `src/Bing.Utils.IdUtils/Bing/IdUtils/SecureRandomId.cs:94`
- `RandomNonceStrGenerator.Create(length)` 将所有小于等于 16 的值规范为 16；兼容参数 `forceToAvoidRepetition` 不提供唯一性保证。[证据] `src/Bing.Utils.IdUtils/Bing/IdUtils/RandomNonceStrGenerator.cs:27`
- `CreateSnowflakeIds(count)` 在 `count < 1` 或 `count > 100000` 抛 `ArgumentOutOfRangeException`。[证据] `src/Bing.Utils.IdUtils/Bing/Helpers/Id.SnowflakeId.cs:141` [证据] `src/Bing.Utils.IdUtils/Bing/Helpers/Id.SnowflakeId.cs:142`
- `TryParse` 失败返回 `false` + `ObjectId.Empty`，不抛异常。[证据] `src/Bing.Utils.IdUtils/Bing/IdUtils/ObjectId.cs:354` [证据] `src/Bing.Utils.IdUtils/Bing/IdUtils/ObjectId.cs:363`

## 3. 使用示例（最小可运行）
- 示例1：基础用法
```csharp
var id1 = ObjectId.GenerateNewId();
var id2 = ObjectId.GenerateNewId();
id1.ShouldNotBe(id2);
```
[证据] `tests/Bing.Utils.IdUtils.Tests/Bing/IdUtils/ObjectIdTest.cs:199` [证据] `tests/Bing.Utils.IdUtils.Tests/Bing/IdUtils/ObjectIdTest.cs:203` [证据] `tests/Bing.Utils.IdUtils.Tests/Bing/IdUtils/ObjectIdTest.cs:206`
- 示例2：进阶用法
```csharp
Id.ConfigureSnowflakeId(() => SnowflakeGenerator.Create(workerId: 1, dataCenterId: 1));
var ids = Id.CreateSnowflakeIds(1000);
ids.Length.ShouldBe(1000);
```
[证据] `tests/Bing.Utils.IdUtils.Tests/Bing/Helpers/IdSnowflakeTest.cs:165` [证据] `tests/Bing.Utils.IdUtils.Tests/Bing/Helpers/IdSnowflakeTest.cs:213` [证据] `tests/Bing.Utils.IdUtils.Tests/Bing/Helpers/IdSnowflakeTest.cs:217`
- 示例3：常见错误与修正
```csharp
Should.Throw<ArgumentOutOfRangeException>(() => Id.CreateSnowflakeIds(0));
// 修正：count 取值在 1..100000
```
[证据] `tests/Bing.Utils.IdUtils.Tests/Bing/Helpers/IdSnowflakeTest.cs:229` [证据] `tests/Bing.Utils.IdUtils.Tests/Bing/Helpers/IdSnowflakeTest.cs:235`
- 示例4：按场景选择 UUIDv7 或自定义字符集标识
```csharp
Guid eventId = Uuid7.Generate();
bool hasTimestamp = Uuid7.TryGetTimestamp(eventId, out var createdAtUnixMilliseconds);
string token = SecureRandomId.Create(32, SecureRandomId.AlphaNumericAlphabet);
```
[指南] [UUIDv7 使用指南](../reference/uuid7-guide.md) [测试] `Uuid7Test`、`SecureRandomIdTest`

## 4. 性能与线程安全说明
- 是否分配敏感
- `ObjectId` 为结构体，单次生成分配较低；批量生成可用 `CreateSnowflakeIds` 减少调用开销。[证据] `src/Bing.Utils.IdUtils/Bing/IdUtils/ObjectId.cs:18` [证据] `src/Bing.Utils.IdUtils/Bing/Helpers/Id.SnowflakeId.cs:124`
- UUIDv7 的随机字段和 `SecureRandomId` 使用密码学随机数；自定义字符串采用拒绝采样，避免字符索引分布偏差。两者都不承诺全局唯一。[证据] `src/Bing.Utils.IdUtils/Bing/IdUtils/Uuid7.cs:31` [证据] `src/Bing.Utils.IdUtils/Bing/IdUtils/SecureRandomId.cs:67`
- 是否线程安全
- `ObjectId` 生成使用 `Interlocked.Increment` 保证并发唯一性。[证据] `src/Bing.Utils.IdUtils/Bing/IdUtils/ObjectId.cs:289`
- `Id.SnowflakeId` 使用 `lock` + `volatile` 保护生成器实例切换。[证据] `src/Bing.Utils.IdUtils/Bing/Helpers/Id.SnowflakeId.cs:17` [证据] `src/Bing.Utils.IdUtils/Bing/Helpers/Id.SnowflakeId.cs:33` [证据] `src/Bing.Utils.IdUtils/Bing/Helpers/Id.SnowflakeId.cs:65`
- 是否可并发调用
- 可并发调用，且已有并发唯一性测试覆盖。[证据] `tests/Bing.Utils.IdUtils.Tests/Bing/IdUtils/ObjectIdTest.cs:685` [证据] `tests/Bing.Utils.IdUtils.Tests/Bing/IdUtils/ObjectIdTest.cs:710`

## 5. 异常与日志策略
- 抛出哪些异常
- 参数越界、null 输入等抛 `ArgumentOutOfRangeException` / `ArgumentNullException`。[证据] `src/Bing.Utils.IdUtils/Bing/IdUtils/ObjectId.cs:150` [证据] `src/Bing.Utils.IdUtils/Bing/IdUtils/ObjectId.cs:381`
- 什么时候返回默认值而不是抛异常
- `ObjectId.TryParse` 失败返回默认空对象，不抛异常。[证据] `src/Bing.Utils.IdUtils/Bing/IdUtils/ObjectId.cs:361` [证据] `src/Bing.Utils.IdUtils/Bing/IdUtils/ObjectId.cs:364`

## 6. 与其他子包的关系
- 依赖的包
- 依赖 `Bing.Utils`。[证据] `src/Bing.Utils.IdUtils/Bing.Utils.IdUtils.csproj:11`
- 被哪些包复用
- 当前 `src` 层未见其他子包直接引用 `Bing.Utils.IdUtils`。

## 7. 测试映射
- 对应测试项目/测试类/关键用例
- 项目：`tests/Bing.Utils.IdUtils.Tests`。[证据] `tests/Bing.Utils.IdUtils.Tests/Bing.Utils.IdUtils.Tests.csproj:11`
- 类：`ObjectIdTest`（构造、解析、并发、性能）、`IdSnowflakeTest`（配置、批量、边界）。[证据] `tests/Bing.Utils.IdUtils.Tests/Bing/IdUtils/ObjectIdTest.cs:9` [证据] `tests/Bing.Utils.IdUtils.Tests/Bing/Helpers/IdSnowflakeTest.cs:10`
- 类：`Uuid7Test`（时间戳布局、版本和变体校验）、`SecureRandomIdTest`（字符集、拒绝采样、边界和分块）。[证据] `tests/Bing.Utils.IdUtils.Tests/Bing/IdUtils/Uuid7Test.cs:13` [证据] `tests/Bing.Utils.IdUtils.Tests/Bing/IdUtils/SecureRandomIdTest.cs:10`
- 未覆盖风险点
- 跨进程/跨机器时钟回拨场景未见系统性自动化覆盖（TODO）。

## 8. 版本与兼容性注意事项
- 跟随公共多目标框架策略。[证据] `common.props:3`
- 雪花算法参数（workerId/dataCenterId）应与部署拓扑一致配置，迁移环境需复核 ID 冲突风险。[证据] `src/Bing.Utils.IdUtils/Bing/IdUtils/SnowflakeGenerator.cs:14` [证据] `src/Bing.Utils.IdUtils/Bing/IdUtils/SnowflakeGenerator.cs:23`
- UUIDv7 包含可读的生成时间戳，适合需要时间排序特征的 GUID；同一毫秒内不保证单调递增。跨系统交换原始 UUID 字节时注意 RFC 网络字节序与 .NET `Guid.ToByteArray()` 的字段字节序差异，见 [UUIDv7 使用指南](../reference/uuid7-guide.md)。
- `SecureRandomId` 的默认 AlphaNumeric 字符集为 62 个字符，随机文本长度精确；`RandomNonceStrGenerator` 保留最小长度 16 的旧约定。随机性不等于唯一性，需唯一约束时由存储或调用方负责检测和处理冲突。


## 本轮修复后的契约

### 雪花 ID

`Id.CreateSnowflakeId` 和 `Id.CreateSnowflakeIds` 不再隐式创建 `workerId=1` 的生成器。应用启动阶段必须显式调用 `Id.ConfigureSnowflakeId`，并为节点分配独占的 `workerId`/`dataCenterId`。首次成功单值或批量发号后，生成器实例被冻结；发号进行中禁止配置或重置，provider 失败且没有成功结果时可以修正配置。工厂参数为 null、工厂返回 null 或工厂抛异常时，配置不会发布半成品状态。

工厂委托在配置锁外执行，允许其等待其他线程访问 `Id`。发布候选前会再次检查生命周期及配置版本：执行期间出现成功发号、仍有在途调用，或其他调用已经配置/重置时，抛出 `InvalidOperationException`，保留当前状态。

若同一时刻有多个发号调用，只有在所有在途调用都失败、且此前从未有调用成功返回后，才可重新配置或重置；任何一次成功后，后续失败都不会解除冻结。批量调用抛异常时不返回部分数组，provider 已消耗的序号不会回滚。`Id.SetId` 命中上下文值时不调用 provider，也不算成功发号。

Twitter 提供程序校验节点和初始序列范围，首次发号使用传入序列；序列耗尽时使用单调计时器进行最多 1 秒的有界等待，回拨和超时都会明确失败且不提交未成功的状态。Seata 使用独立的逻辑时间布局，节点范围为 0..1023，并在 41 位逻辑时间与 12 位序列空间耗尽时拒绝继续发号。两种布局不能混入同一个全局键空间后假设自然全局排序。

雪花唯一性依赖节点 ID 独占、实例状态连续以及安全的重启和时钟策略。本包不提供跨进程节点租约、高水位持久化或外部协调服务。

### TimestampId

`TimestampId.GetInstance` 首次调用无论时间基准是否为 null 都会初始化；后续调用返回同一实例并忽略新的时间基准。时间统一按 UTC 计算，未来初始时间在初始化阶段抛 `ArgumentOutOfRangeException`。同一毫秒内序列范围为 `0001..9999`，耗尽后等待时间推进；冻结时以有界 `TimeoutException` 失败，回拨抛 `InvalidOperationException`，失败不会清零或重用序列状态。

等待达到预算后，即使 UTC 时间已经推进也会抛出 `TimeoutException`，本次调用不提交时间戳或序列。等待预算用于故障保护，不是线程调度或业务延迟 SLA。实例使用私有锁保护发号状态。

### GUID、UUID 和 COMB

需要 UUIDv1 时使用 `Uuid1.Generate()` 或 `Uuid1.Generate(DateTimeOffset)`。UUIDv1 使用 Gregorian epoch、100 ns 时间字段、14 位 clock sequence 和随机 multicast node，不读取本机 MAC。`GuidVersion.TimeBased` 与 `GuidVersion.DceSecurity` 明确抛 `NotSupportedException`；未知 `GuidVersion` 抛 `ArgumentOutOfRangeException`。v3/v5 保持 RFC 名称型算法，v4 使用 `Guid.NewGuid()` 并验证版本和变体位。

UUIDv1 与 UUIDv7 的时间字段不同：v1 使用 Gregorian epoch 的 100 ns 字段并带逻辑时钟状态，v7 使用 Unix epoch 毫秒和随机字段；v7 同一毫秒内不保证严格递增。COMB、Sequential 和 Equifax 风格是存储布局，不是标准 UUID 版本。Equifax 风格只能编码到分钟，不能承诺每次调用唯一；随机 ID 的碰撞概率也不等于绝对唯一性。

PostgreSQL COMB 新生成值使用修正后的前 4 字节、后 2 字节交换布局，旧 GUID 不重写、不自动识别旧格式，旧数据与新数据可能存在不同排序布局。

采用 Unix 日期策略的 COMB 入口拒绝 Unix Epoch（1970-01-01）前的日期，检查发生在舍弃毫秒以下精度之前，避免负时间被截断成无法往返的无符号时间字段。GUID 的大端/小端字节入口要求非 null 且长度恰为 16，分别使用 `ArgumentNullException` 和 `ArgumentException` 拒绝非法输入。

