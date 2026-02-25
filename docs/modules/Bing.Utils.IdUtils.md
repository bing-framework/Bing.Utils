# Bing.Utils.IdUtils
## 1. 包职责（Scope）
- 解决的问题
- 提供 ObjectId、Snowflake、时间戳等标识生成与管理能力。[证据] `src/Bing.Utils.IdUtils/Bing.Utils.IdUtils.csproj:3` [证据] `src/Bing.Utils.IdUtils/Bing/IdUtils/ObjectId.cs:18` [证据] `src/Bing.Utils.IdUtils/Bing/IdUtils/SnowflakeGenerator.cs:8`
- 提供统一入口 `Id`，支持上下文 ID、雪花配置与批量生成。[证据] `src/Bing.Utils.IdUtils/Bing/Helpers/Id.cs:9` [证据] `src/Bing.Utils.IdUtils/Bing/Helpers/Id.SnowflakeId.cs:61` [证据] `src/Bing.Utils.IdUtils/Bing/Helpers/Id.SnowflakeId.cs:139`
- 不解决的问题（Out of Scope）
- 不负责外部分布式协调（注册中心、机器号自动分配等需外部系统提供）。

## 2. 核心类型与扩展方法
- 类型/方法签名摘要
- `ObjectId`：`GenerateNewId`、`Parse/TryParse`、`Pack/Unpack`。[证据] `src/Bing.Utils.IdUtils/Bing/IdUtils/ObjectId.cs:273` [证据] `src/Bing.Utils.IdUtils/Bing/IdUtils/ObjectId.cs:339` [证据] `src/Bing.Utils.IdUtils/Bing/IdUtils/ObjectId.cs:354` [证据] `src/Bing.Utils.IdUtils/Bing/IdUtils/ObjectId.cs:308`
- `SnowflakeGenerator.Create(...)`：Twitter/Seata 两种入口。[证据] `src/Bing.Utils.IdUtils/Bing/IdUtils/SnowflakeGenerator.cs:17` [证据] `src/Bing.Utils.IdUtils/Bing/IdUtils/SnowflakeGenerator.cs:24`
- `Id`：`CreateObjectId`、`CreateSnowflakeId`、`CreateSnowflakeIds`。[证据] `src/Bing.Utils.IdUtils/Bing/Helpers/Id.cs:131` [证据] `src/Bing.Utils.IdUtils/Bing/Helpers/Id.SnowflakeId.cs:109` [证据] `src/Bing.Utils.IdUtils/Bing/Helpers/Id.SnowflakeId.cs:139`
- 输入输出约定
- `Id.CreateSnowflakeId` 优先读取上下文 `SetId`，否则走生成器。[证据] `src/Bing.Utils.IdUtils/Bing/Helpers/Id.SnowflakeId.cs:111` [证据] `src/Bing.Utils.IdUtils/Bing/Helpers/Id.SnowflakeId.cs:114`
- `CreateObjectId` 总是新生成，不受上下文影响。[证据] `src/Bing.Utils.IdUtils/Bing/Helpers/Id.cs:128` [证据] `src/Bing.Utils.IdUtils/Bing/Helpers/Id.cs:131`
- 边界行为（null、空集合、非法参数）
- `ObjectId` 构造器对 null/长度非法输入抛异常。[证据] `src/Bing.Utils.IdUtils/Bing/IdUtils/ObjectId.cs:122` [证据] `src/Bing.Utils.IdUtils/Bing/IdUtils/ObjectId.cs:170`
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
Id.ConfigureSnowflakeId(() => SnowflakeGenerator.Create(1));
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

## 4. 性能与线程安全说明
- 是否分配敏感
- `ObjectId` 为结构体，单次生成分配较低；批量生成可用 `CreateSnowflakeIds` 减少调用开销。[证据] `src/Bing.Utils.IdUtils/Bing/IdUtils/ObjectId.cs:18` [证据] `src/Bing.Utils.IdUtils/Bing/Helpers/Id.SnowflakeId.cs:124`
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
- 未覆盖风险点
- 跨进程/跨机器时钟回拨场景未见系统性自动化覆盖（TODO）。

## 8. 版本与兼容性注意事项
- 跟随公共多目标框架策略。[证据] `common.props:3`
- 雪花算法参数（workerId/dataCenterId）应与部署拓扑一致配置，迁移环境需复核 ID 冲突风险。[证据] `src/Bing.Utils.IdUtils/Bing/IdUtils/SnowflakeGenerator.cs:14` [证据] `src/Bing.Utils.IdUtils/Bing/IdUtils/SnowflakeGenerator.cs:23`

