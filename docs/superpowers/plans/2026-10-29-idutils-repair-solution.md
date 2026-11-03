# IdUtils 修复与测试补强方案

日期：2026-10-29
用途：记录 IdUtils 缺陷修复的方案、实施决策和验收依据。本文末尾记录了本轮实施状态。
推荐实施顺序：测试隔离与可控时钟 → 重复 ID 与配置安全 → GUID/COMB 正确性 → UUID 契约补强 → 模块验收。

## 1. 目标、范围与已有证据

目标是消除已经复现的重复发号、静默配置回退、无界等待和 GUID 格式错误，并让单元测试能够稳定发现这些问题。

生产代码范围为 [Bing.Utils.IdUtils](D:/Bing_Framework/Bing.Utils/src/Bing.Utils.IdUtils/Bing.Utils.IdUtils.csproj)，测试统一放在 [Bing.Utils.IdUtils.Tests](D:/Bing_Framework/Bing.Utils/tests/Bing.Utils.IdUtils.Tests/Bing.Utils.IdUtils.Tests.csproj)。按 [测试归属规则](D:/Bing_Framework/Bing.Utils/.agents/skills/test-project-boundary/refs/test-project-boundary.md)，IdUtils 的算法、配置和回归测试均属于该模块；本轮审计未发现主测试项目中有需要迁移的 IdUtils 专属测试。

沿用现有框架范围：生产包 net8.0/net7.0/net6.0/netstandard2.0；测试运行于 net8.0/net7.0/net6.0/net5.0/netcoreapp3.1。本方案不依赖升级框架或增加外部协调服务。

前一轮审查的证据：

- 五个测试目标框架均为 349 通过、0 失败、0 跳过；xUnit 同时报告并丢弃一条重复 test case ID，因此数字不代表全部数据行都执行。
- 临时程序引用前一轮审查当次编译的 net8.0 程序集；通过固定时钟复现雪花同节点双实例及同节点重配置重复。
- 空工厂返回值使节点号从 42 静默切换为 1；并发复现过空引用异常。并发异常次数属于观察结果，不作为未来测试的固定期望。
- 注入接近序列耗尽的状态，复现 TimestampId 同一毫秒重用序列 1；首次带初始时间调用返回 null。
- 请求 GuidVersion.TimeBased 得到版本位 4；PostgreSQL COMB 出现逆序和时间戳往返错误；不匹配括号被 GuidJudge 接受。
- UUIDv7 RFC 示例解析正确；同毫秒 20,000 次并发抽样无重复且布局正确。UUIDv3/v5 与 Python 独立计算结果一致。随机抽样不能证明绝对唯一性。

这些是修复前证据，不是修复后的验收结果。仅编写本文不重跑代码测试；实施后按实际变更范围生成新证据。

## 2. 问题清单与关闭条件

下列源码问题初始状态均为 OPEN_ACTIONABLE。F01 中跨进程节点分配属于部署前提，代码部分与部署部分分别验收。

| 编号 | 优先级 | 问题 | 完成条件 |
| --- | --- | --- | --- |
| F01 | P1 / MUST_FIX | 默认节点固定为 1，生成器可在运行中被替换而丢失状态 | 安全使用路径要求显式节点配置；首次发号后禁止替换/重置生成器；说明跨进程独占节点与重启条件 |
| F02 | P1 / MUST_FIX | TimestampId 耗尽后在同一毫秒重置序列 | 耗尽时推进时间或明确失败；失败及恢复后均不重复 |
| F03 | P2 / SHOULD_FIX | 雪花工厂返回 null 后静默回退及并发空引用 | 配置阶段拒绝 null 结果，保留旧实例；读取使用稳定快照 |
| F04 | P2 / SHOULD_FIX | Twitter 在序列耗尽等待中无界忙等 | 冻结、回拨均有明确结束策略；失败不破坏已分配序列状态 |
| F05 | P2 / SHOULD_FIX | TimeBased 返回 COMB/v4，违背版本语义 | 独立 Uuid1 通过 RFC 固定向量；TimeBased/DceSecurity/未知版本明确异常 |
| F06 | P2 / SHOULD_FIX | PostgreSQL COMB 字节交换位置错误 | 固定向量、进位排序、时间戳往返均正确；旧数据格式处理方式明确 |
| F07 | P2 / SHOULD_FIX | GuidJudge 接受不配对括号 | 采用明确的框架解析契约，非法格式均被拒绝 |
| F08 | P2 / SHOULD_FIX | Twitter 初始 sequence 越界未校验且首次被清零 | 校验 0..4095，首个成功发号使用给定初始序列，后续推进规则明确 |
| F09 | P2 / SHOULD_FIX | TimestampId 首次带初始时间返回 null | 首次调用无论参数是否为空都能初始化，后续遵循单例契约 |

测试补强任务：

| 编号 | 内容 | 完成条件 |
| --- | --- | --- |
| T01 | UUIDv7/v3/v4/v5 的独立契约验证 | 固定向量、版本/变体、默认入口与边界测试完整 |
| T02 | 雪花状态机和部署边界 | 两个 provider 的位布局、溢出、回拨、配置生命周期有确定性测试 |
| T03 | 静态配置测试隔离与任务收尾 | 所有共享配置测试统一隔离；超时被断言；后台任务已退出再清理 |
| T04 | 重复测试 ID | 孤立高代理、低代理等数据分别执行，无重复 ID 警告 |

## 3. 推荐方案与兼容选择

推荐先完成局部缺陷修复，再在一个明确的兼容性变更中收紧公开行为。不要将第一批局部修复描述为“已解决所有分布式唯一性问题”。

| 路线 | 质量与剩余问题 | 工作量与速度 | 工具与验证范围 |
| --- | --- | --- | --- |
| A：兼容补丁 | 修复空工厂、等待、TimestampId、校验和端序；若保留默认节点/热重配，F01 仍未关闭；若保留伪 v1，F05 仍未关闭 | 较低，可先交付 | 现有 BCL、xUnit、受控时钟和模块测试 |
| B：完整安全契约，推荐目标 | 在 A 上明确节点配置、启动后冻结、版本支持范围和格式迁移；部署仍须提供独占节点 | 中等，需同步修改调用代码与文档 | 模块测试、调用方构建、公共 API/行为兼容检查 |
| C：增加分布式节点分配及重启恢复服务 | 适用于确有跨主机租约、崩溃恢复要求的业务 | 较高，单独设计和验证 | 外部存储/协调服务、失效节点隔离、跨进程测试；不作为当前基础修复的前提 |

在实施公开行为变更前，应记录以下选择；本文给出推荐，不代表这些变更已经落地：

| 决策 | 推荐 | 兼容影响 |
| --- | --- | --- |
| D01：隐式节点与热重配置 | 安全入口未配置即拒绝发号；首次成功发号后冻结实例，发号进行中拒绝配置/重置 | 依赖默认 worker=1、运行中 Configure/Reset 的调用方需调整；可以集中到一次明确的兼容版本变更 |
| D02：UUIDv1 支持 | 新增独立 Uuid1 API；旧 GuidVersion.TimeBased 入口显式抛 NotSupportedException | 原来收到 COMB 的调用方改用明确的 COMB 入口；需要新时间型 UUID 的调用方可选 Uuid7 |
| D03：PostgreSQL COMB | 新生成采用修正布局；有旧数据解析需求时保留显式旧格式解码 | 不同格式不能靠版本位可靠自动识别，需由数据来源或版本元数据选择 |
| D04：时间回拨与等待 | Twitter/TimestampId 默认拒绝回拨；序列耗尽采用有界等待 | 原来可能长期阻塞的调用现在会失败；调用方应处理明确异常，不能无限重试 |
| D05：初始 sequence | 参数表示第一个成功生成 ID 使用的序列值 | 非零参数从“被忽略”改为生效；仍不能靠不同初始序列保证多个生成器不冲突 |

等待预算应由调用场景的延迟要求决定。实现需要有有限默认值并说明取值依据；本文不把任意毫秒数冒充已验证的生产阈值。测试使用可注入预算与虚拟时间，避免依赖机器速度。

## 4. 雪花生成器修复设计

### 4.1 F01：节点与实例生命周期

主要入口：[Id.SnowflakeId.cs](D:/Bing_Framework/Bing.Utils/src/Bing.Utils.IdUtils/Bing/Helpers/Id.SnowflakeId.cs)、[SnowflakeGenerator.cs](D:/Bing_Framework/Bing.Utils/src/Bing.Utils.IdUtils/Bing/IdUtils/SnowflakeGenerator.cs)。

推荐的 Id 静态入口状态约定：

| 当前状态 | 操作 | 结果 |
| --- | --- | --- |
| 未配置 | CreateSnowflakeId/CreateSnowflakeIds，需要真实发号 | 抛 InvalidOperationException，说明需要配置 |
| 未配置/已配置但未开始 | ConfigureSnowflakeId，返回非空实例 | 配置成功，替换尚未使用的配置 |
| 已配置但未开始 | 首次取得用于发号的实例 | 登记发号调用；成功返回后在配置锁内冻结，失败则撤销登记 |
| 一个或多个发号调用在途 | ConfigureSnowflakeId/ResetSnowflakeId | 立即拒绝；不等待正在执行的 provider |
| 从未成功且所有在途调用均已失败 | ConfigureSnowflakeId/ResetSnowflakeId | 允许修正配置或恢复未配置状态 |
| 已开始 | 再次 ConfigureSnowflakeId | 拒绝；旧实例继续可用 |
| 已开始 | ResetSnowflakeId | 拒绝；不能绕过冻结规则重新开始序列 |
| 未开始 | ResetSnowflakeId | 恢复未配置状态，不能隐式切回节点 1 |

- “开始”以首次成功返回发号结果为界；任何一次成功后永久冻结，即使后续调用失败也不解冻。发号进行中拒绝配置和重置，避免实例切换与生成竞争；仅当此前从未成功且所有在途调用均失败后，才允许重新配置。
- Id.SetId 的上下文覆盖保持独立语义：已有上下文 ID 时单值入口直接返回，不调用 provider，也不计作成功发号；批量入口继续忽略上下文。
- 批量 provider 若在中途抛异常，Id 不返回已部分生成的数组；provider 已消耗的序号不回滚。若这是首次批量调用且整批失败，在没有其他在途调用或既往成功的情况下可以重新配置。
- 配置锁保护状态转换，provider 自己的锁保护序列。不要在每个 ID 的完整生成过程外再加全局配置锁。
- SnowflakeGenerator.Create 仍是显式创建独立实例的工厂。文档要求调用方为同一节点复用实例；不新增全局字典偷偷缓存所有工厂结果。
- 同 worker 的独立实例在相同时间产生冲突属于算法的使用边界。对应测试应暴露该边界或验证入口拒绝替换，不能要求任意两个独立实例自动协调唯一性。
- 去掉“无前提地确保分布式全局唯一”的表述，写清唯一性条件：算法/布局一致、节点身份独占、实例状态连续、时间及重启策略满足约定。
- Twitter 与 Seata 布局不同。两个算法产生的 long 不应直接混入同一个要求唯一的键空间并假设永不冲突。

若当前发布必须保留旧默认行为，应在迁移文档中明确风险并提供显式配置示例；F01 的默认配置/热替换部分保持未关闭，不能仅靠文档宣称修复完成。

跨进程与重启的补充约定：

1. Twitter 节点身份为 dataCenterId + workerId；Seata 为 workerId。节点号须在共享 ID 键空间内分配，不能依赖 IP/MAC 截断或随机挑选来获得确定唯一性。
2. 节点租约需要处理旧持有者暂停后恢复的情况，确保旧实例无法继续发号。仅有租约到期时间不等于旧实例已被隔离。
3. 相同节点重启时，时钟回拨或此前逻辑时间超前仍可能使区间重叠。需要重启安全保证的部署应使用可靠高水位/预留区间，或能证明不会重叠的节点轮换策略。
4. 高水位应在使用对应区间前可靠持久化，不能只在正常关机时保存最后 ID。选择等待物理时钟越过水位或跳到新预留区间时，应与具体算法契约一致。
5. 这些协调能力由宿主或专门服务提供；基础包不引入 Redis/数据库依赖来掩盖部署前提。

### 4.2 F03：配置结果校验和快照读取

兼容补丁也可以先完成此项：

- 工厂参数为 null：ArgumentNullException，参数名 generatorFactory。
- 工厂返回 null：InvalidOperationException，指出返回了无效生成器。
- 工厂抛异常或返回 null：旧配置和生命周期状态都不改变。
- 在局部变量中得到并验证候选实例后，再在受保护的配置阶段发布。
- getter 从字段读取一次局部快照并返回该快照；需要初始化时在锁内复查。不能检查字段非空后，再读取一个可能被其他线程改变的字段。
- 若把用户工厂移到锁外执行，发布前必须重新检查生命周期状态；不要顺带改变“工厂执行次数/执行时机”而没有测试。

验收：先配置节点 42、尚未发号时提交空工厂结果，配置立即失败；随后发号仍来自原实例，不能切回 1。工厂抛异常同样保留原配置。用受控同步点验证配置与读取竞争；压力抽样只作为补充。

### 4.3 F04/F08：Twitter 等待与序列状态

修改 [TwitterSnowflakeProvider.cs](D:/Bing_Framework/Bing.Utils/src/Bing.Utils.IdUtils/Bing/IdUtils/SnowflakeImplements/Providers/TwitterSnowflakeProvider.cs)。

推荐规则：

- workerId/dataCenterId 各为 0..31；sequence 为 0..4095，越界在构造阶段拒绝。
- 第一个成功 ID 使用传入的初始 sequence；同一毫秒随后递增；进入新的毫秒重置为 0。
- 同一毫秒已发出 4095 后，下次请求等待更大的时间戳。初始 sequence=4095 时只能先发出该值，再等待下一毫秒。
- 当前时间小于最后成功发号时间时拒绝发号；等待循环内也执行相同的回拨策略。
- 时间冻结时只能等待有限时间。用单调计时计算预算，用短暂退让降低忙等；不能用可能回拨的 UTC 时间计算超时。
- 检查相对纪元时间在 41 位范围内，避免符号位溢出或时间字段截断；这属于位布局边界补强。
- 兼容补丁可保留原回拨异常类型；新增等待超时使用 TimeoutException。若统一异常类型，作为明确的契约变更处理。

必须遵守的状态提交原则：

~~~text
进入实例锁
  读取最后成功状态
  在局部变量计算候选 timestamp / sequence
  如需等待，执行有界等待和回拨检查
  校验候选时间、位宽及序列
  构造候选 ID
  一次性提交新的 timestamp / sequence
  返回 ID
退出实例锁
~~~

不能先把字段 sequence 从 4095 改成 0，再在等待超时时抛异常。否则下一次在同一毫秒调用可能从 1 继续，重发已经使用过的值。失败后状态必须仍代表最后一个成功发出的 ID。

### 4.4 Seata：保留逻辑时间语义，补齐边界

参考 [SeataSnowflakeProvider.cs](D:/Bing_Framework/Bing.Utils/src/Bing.Utils.IdUtils/Bing/IdUtils/SnowflakeImplements/Providers/SeataSnowflakeProvider.cs)。

- Seata 的低 53 位是逻辑时间与序列的整体计数器；运行中不要求每个 ID 的时间字段等于当前物理时间。
- 保留单实例计数递增语义，不因为 Twitter 采用回拨拒绝就给 Seata 套用同样的异常预期。
- 验证 workerId=0/1023 的高 10 位布局；序列从 4095 进位时逻辑时间增加，worker 位不变。
- 用受控时钟验证物理时间冻结、前跳、回拨时计数器不会重置或重用。
- 当前一次 Sleep(5) 是节流行为，不代表保证物理时间已经追上逻辑时间。若需要改变节流策略，单独声明行为及性能影响。
- 增加有效时间范围和计数空间耗尽检查，耗尽时明确失败，不能通过低 53 位掩码静默回绕到旧 ID。
- 不对跨 worker 的 long 数值顺序承诺全局时间排序。时间提取、重启水位必须按 Seata 布局解释。

## 5. TimestampId 修复设计

修改 [TimestampId.cs](D:/Bing_Framework/Bing.Utils/src/Bing.Utils.IdUtils/Bing/IdUtils/TimestampId.cs)。

### 5.1 F02：序列耗尽和回拨

推荐保持现有输出形式“时间戳 + 四位十进制序列”，将有效序列明确为 0001..9999：

- 新毫秒从 1 开始；同毫秒依次发出至 9999。
- 9999 已发出后，下一次必须等待时间推进；冻结时有界失败，回拨时明确失败。
- 超时/回拨失败不能把序列清零或改成 1；恢复到相同旧毫秒时仍不能重用序列。
- 时间、序列、ID 构造使用同一份已锁定状态；时间戳和序列作为局部结果一起返回。
- 以私有同步对象保护状态；增加每实例可控时钟与等待依赖，避免用反射改字段作为主要长期验收手段。
- public GetUniqueTimeStamp 的旧参数没有承担有效状态管理。修复时保留签名并明确内部状态为准，避免顺带引入由调用者任意重置水位的能力。

此类型仍只有进程内单实例范围的保证，没有节点字段，不能由本次修复扩展成分布式唯一 ID。

### 5.2 F09：单例初始化和时间基准

- 初始化判断应基于“单例是否存在”，而不是“参数是否为 null”。
- 首次初始化使用传入时间；null/DateTime.MinValue 按旧默认规则使用 Unix Epoch。
- 后续调用返回同一实例并保持首次时间基准，不能运行中更换基准。保留原先后续参数被忽略的行为并写清文档。
- 同时初始化时只有一个实例发布；不让失败的候选初始化破坏已存在的实例。
- 统一在 UTC 下计算和比较时间：UTC 原样使用，Local 转 UTC；Unspecified 推荐明确按 UTC 解释以保持原先按 ticks 计算的兼容倾向。
- 初始时间晚于当前 UTC 时拒绝；校验位置和异常类型需要同步更新测试。推荐初始化时抛 ArgumentOutOfRangeException。
- 首次调用测试必须运行在隔离的单例状态中。可使用单独进程，或在专门的非并行测试夹具中保存/还原单例；不能依赖测试执行顺序。

## 6. UUID/GUID 与 COMB 修复设计

### 6.1 F05：分开 UUID 版本与 COMB 风格

修改 [GuidProvider.cs](D:/Bing_Framework/Bing.Utils/src/Bing.Utils.IdUtils/Bing/IdUtils/GuidProvider.cs)、[GuidVersion.cs](D:/Bing_Framework/Bing.Utils/src/Bing.Utils.IdUtils/Bing/IdUtils/GuidVersion.cs)。

最终选择：

- 新增 Uuid1.Generate() 和 Uuid1.Generate(DateTimeOffset)，按 RFC 9562 实现 UUIDv1。
- GuidVersion.TimeBased 明确抛 NotSupportedException，提示迁移到 Uuid1.Generate()；不忽略 namespace/name 参数。
- 需要旧 COMB 输出的调用方使用 GuidProvider.Create(CombStyle.NormalStyle)；不要仅将版本位改成 1 来伪造 UUIDv1。
- 枚举中未实现的 DceSecurity 明确抛 NotSupportedException；未知枚举值抛 ArgumentOutOfRangeException，避免随机回退掩盖调用错误。
- UUIDv1 默认使用密码学随机 node 并设置 multicast bit，不读取本机 MAC；进程内维护最后时间戳、clock sequence 和 node，并覆盖官方固定向量。
- v3/v5 保持规定的 MD5/SHA-1 名称算法；不能替换成其他哈希后仍称为同一 UUID 版本。

关联参数契约建议：名称型入口对 null name 显式抛 ArgumentNullException，并测试 ParamName；空 byte[] 与 null 分开处理。这会替换现有固化 NullReferenceException 的测试预期。随机版本忽略 namespace/name 的既有行为是否改变，应另行明确，不能把参数校验无差别前移。

### 6.2 T01：UUIDv7 保留现有随机模式

参考 [Uuid7.cs](D:/Bing_Framework/Bing.Utils/src/Bing.Utils.IdUtils/Bing/IdUtils/Uuid7.cs) 与 [使用指南](D:/Bing_Framework/Bing.Utils/docs/reference/uuid7-guide.md)。

- 保留 48 位 Unix 毫秒、版本 7、RFC 变体及 74 位密码学随机字段。
- 保留指定 DateTimeOffset 入口与 .NET 混合端序转换，不在本次增加默认的单调计数器。
- 明确“时间前缀可排序”不等于“同毫秒严格递增”，也不保证物理时钟回拨后仍递增。
- 补无参入口的版本/变体及调用前后时间窗口断言；时间窗口测试属于真实时钟集成检查，不能用重试掩盖失败。
- 如需确定性验证随机位保留，增加 internal 随机填充委托，由公开入口仍使用原密码学随机源；全零、全一及两种固定模式应分别得到正确布局。
- 同毫秒并发抽样可保留为冒烟验证，不宣称其证明绝对唯一性或密码学强度。
- DateTimeOffset.MaxValue 是当前生成入口能表示的上界；完整 48 位最大值只能用手工构造的 UUID 测解析，不能编造无法构造的 DateTimeOffset 输入。

### 6.3 F06：PostgreSQL COMB 字节布局

修改 [PostgreSqlCombProvider.cs](D:/Bing_Framework/Bing.Utils/src/Bing.Utils.IdUtils/Bing/IdUtils/CombImplements/Providers/PostgreSqlCombProvider.cs)。

将交换前两个 GUID 字段的方法修正为以下核心操作；当前调用使用完整 16 字节数组：

~~~csharp
// 调整 .NET Guid 前两个字段，使规范字符串前缀与时间字节一致。
Array.Reverse(input, 0, 4);
Array.Reverse(input, 4, 2);
~~~

该变换应可逆，正向/反向应用相同操作恢复原字节。不能再反转从索引 4 开始的四个字节，避免混入第三字段。

固定验收向量：

| 输入 | 值 |
| --- | --- |
| 原始 Guid | 00112233-4455-6677-8899-aabbccddeeff |
| 第一时间戳 Unix ms | 0x010203FFFEFF，即 1108168670975 |
| 修正后的第一 Guid | 010203ff-feff-6677-8899-aabbccddeeff |
| 第二时间戳 Unix ms | 0x010204000000 |
| 修正后的第二 Guid | 01020400-0000-6677-8899-aabbccddeeff |
| 排序与往返 | 第一值小于第二值；第一值解码回 1108168670975 |

前一轮错误实现生成第一值 ff030201-6677-feff-8899-aabbccddeeff，随后值反而更小；解码为 1108168671230。这组非对称字节可同时发现端序与往返错误。

兼容处理：

- 已作为主键使用的 GUID 不应因修复而批量重写，外键关系继续以既存值为准。
- 新格式和旧格式若混存，不能承诺整个历史集合立即变成时间顺序。
- 需要读取旧嵌入时间时，显式选择旧格式逆变换；不能把原错误函数再调用一次当成旧格式正确解码。
- 两种格式没有可靠自带的版本标记。选择依据应来自数据生成版本/来源，不能猜测；没有旧时间读取需求时不新增无用兼容层。
- PostgreSQL 排序验证使用 RFC 网络字节序或规范十六进制字符串。不要用 .NET 混合端序 byte[] 的直接比较替代数据库 UUID 顺序。
- 只有声明真实数据库排序兼容时，再添加对应数据库集成验收；修复字节函数本身先用固定向量验证。

### 6.4 F07：GUID 格式校验

修改 [GuidJudge.cs](D:/Bing_Framework/Bing.Utils/src/Bing.Utils.IdUtils/Bing/IdUtils/GuidJudge.cs)。

推荐将有效字符串定义为 Guid.TryParse 能成功解析的输入：

~~~csharp
public static bool IsValid(string guidStr) =>
    !string.IsNullOrWhiteSpace(guidStr) && Guid.TryParse(guidStr, out _);
~~~

测试 N/D/B/P/X 格式、大小写、空/null/纯空白、不匹配括号、错误长度和非法字符。若必须拒绝前后空白或只允许部分格式，使用显式检查和 TryParseExact，并在文档中记录；不能依赖当前正则的偶然行为。

### 6.5 其他风格的适用边界

- EquifaxStyle 只编码到分钟，同一分钟可能得到相同值；不能作为通用“每次调用唯一”的 GUID 生成器使用。
- NoRepeatMode.On 的时间戳递增可能被具体格式的时间精度截断，不能由名字推导出所有风格/所有输入都绝不重复。
- COMB、自定义 Sequential 风格与标准 UUID 版本分别描述，不对全部 GuidStyle 强行套用 v4/v7 位布局断言。
- 这些属于契约说明与选型约束；不要为它们顺带重写全部风格算法。

## 7. 测试设计与文件安排

### 7.1 最小测试支撑

现有 assembly-config 已允许 Bing.Utils.IdUtils.Tests 访问 internal 成员，优先利用这一能力：

- 时钟与等待依赖按实例注入，不新增进程全局可变时钟。
- 需要的最小依赖为 UTC 毫秒读取、单调时间/已耗时读取、等待或退让动作；可用 internal 委托或小型内部类型。
- 兼容 netstandard2.0，不直接把仅新框架可用的 TimeProvider 放进公共签名。
- Seata 初始化读取时钟前先建立依赖，避免基类/构造阶段调用尚未初始化的派生状态。
- 状态机测试先推进到边界，必要时使用 internal 测试入口；尽量替换直接修改私有字段的反射测试。
- 超时测试由虚拟单调时钟推进，外层再有测试进程超时保护。外层超时不能替代算法自己的期限验收。

### 7.2 回归矩阵

以下文件均位于 tests/Bing.Utils.IdUtils.Tests。标注“新增”的文件为建议文件，尚未创建。

| 关联 | 文件 | 用例示例与关键期望 |
| --- | --- | --- |
| F01 | Bing/Helpers/IdSnowflakeTest.cs | 未配置拒绝真实发号；首次发号后 Configure/Reset 被拒绝且旧实例继续；固定时间重配不能悄悄重发 |
| F01/T02 | Bing/IdUtils/SnowflakeProviderBoundaryTest.cs | 同身份双实例冲突边界；不同身份编码不同节点位；不将独立实例误测成自动协调服务 |
| F03 | Bing/Helpers/IdSnowflakeTest.cs | null 委托、null 返回值、抛异常工厂分别验证；旧实例保留；受控配置/读取竞争无空引用 |
| F04 | Bing/IdUtils/SnowflakeProviderBoundaryTest.cs | 普通溢出推进；等待中回拨失败；冻结超时；失败后恢复仍不重用 sequence |
| F08 | 同上 | sequence=-1/4096 拒绝；0/7/4095 首次值；4095 后等待；跨毫秒归零 |
| T02 | Bing/IdUtils/SeataSnowflakeProviderBoundaryTest.cs（新增） | worker 0/1023、位布局、序列进位、物理时钟冻结/前跳/回拨、低 53 位耗尽拒绝 |
| F02 | Bing/IdUtils/TimestampIdBoundaryTest.cs（新增） | 同毫秒 1..9999；耗尽后冻结/前进/回拨；异常后状态未回退；并发唯一性 |
| F09 | Bing/IdUtils/RandomAndTimestampTest.cs | 隔离状态下首次传入过去时间；首次无参；并发初始化；后续参数不改变基准；未来基准拒绝 |
| F05 | Bing/IdUtils/GuidProviderAndExtensionsTest.cs、Uuid1Test.cs（新增） | Uuid1 RFC 固定向量；TimeBased/DceSecurity 明确异常；版本、变体和回拨逻辑 |
| F06 | Bing/IdUtils/GuidProviderCombOverloadsTest.cs | 固定正确字符串、时间戳往返、进位逆序回归、未嵌入字段保留；不能只比较两次输出相同 |
| F07 | Bing/IdUtils/GuidJudgeTest.cs | N/D/B/P/X；不匹配/单边括号；空白策略；非法字符、长度 |
| T01 | Bing/IdUtils/Uuid7Test.cs | 默认入口窗口；RFC 示例；毫秒以下截断；不同时区表示同一瞬间；空 Guid 与全部非 RFC 变体拒绝；随机字段映射 |
| T01 | Bing/IdUtils/GuidProviderAndExtensionsTest.cs | v3/v5 固定向量；v4 版本/变体；名称 null 与空数组区别 |
| T03 | Bing/Helpers 下共享 Id 配置的测试类 | 同一非并行 collection；每例恢复状态；超时失败；任务退出后才清理 |
| T04 | Bing/IdUtils/SecureRandomIdTest.cs | 非法字符集六类均被发现和执行，孤立高/低代理分别覆盖，无重复 case ID |

v3/v5 使用前一轮已与 Python 对照的固定向量，作为跨实现参照：

| namespace | UTF-8 名称 | 版本 | 预期 |
| --- | --- | --- | --- |
| 6ba7b810-9dad-11d1-80b4-00c04fd430c8 | www.widgets.com | 3 | 3d813cbb-47fb-32ba-91df-831e1593ac29 |
| 同上 | www.widgets.com | 5 | 21f7f8de-8051-5b89-8680-0195ef798b6a |

UUIDv7 使用 RFC 9562 示例 017f22e2-79b0-7cc3-98c4-dc0c0c07398f，对应 Unix 毫秒 1645557742000。期望常量独立保存，不能调用被测编码函数生成期望值。

### 7.3 必须调整的现有测试

1. SnowflakeProviderBoundaryTest 的普通溢出输入包含“等待中回拨后恢复”。采用回拨拒绝策略后，拆成正常溢出和回拨失败两例。
2. 移除 TimeGenCalls 精确等于 4100/4 等与实现轮询次数绑定的断言。改断言结果、位布局、状态恢复及预算耗尽。
3. GuidProviderAndExtensionsTest 中 name=null 预期 NullReferenceException 的用例，随参数契约修复改为 ArgumentNullException。
4. 生命周期冻结会改变 IdSnowflakeTest 中运行中多次配置及 Reset 的预期。安全入口测试应断言拒绝；测试清理使用隔离夹具，不为测试保留可在生产绕过冻结的公开重置通道。
5. TimestampId 未来基准若改为初始化阶段拒绝，更新原来在 GetId 才断言异常的用例。
6. 将同一 Id 静态配置的测试放入共同 xUnit collection；需要时设置 DisableParallelization=true。清理放在 finally/Dispose，保存还原测试前状态；独立 UUID 算法测试仍可并行。
7. 对有超时的 Task.WaitAll 检查 bool 结果。超时后先通知任务停止并等待退出，再恢复全局状态；不可让遗留任务污染下一用例。不可取消的故障探针放到可终止的独立进程。
8. 对孤立高/低代理数据增加唯一 case 名或改为独立 Fact。xUnit 日志中不得再出现重复 ID 被丢弃的提示，不能只检查 skipped=0。
9. Legacy 中只断言非空的测试可以作为冒烟保留，但不能替代上述验收。

## 8. 分步 TODO、依赖与协作

### 阶段 A：建立可重复的回归测试基础

- [ ] 建立问题到测试的映射，并冻结 D01..D05 的实现选择。
- [ ] 修复共享配置测试隔离、超时任务收尾及重复 case ID。
- [ ] 增加最小 internal 时钟/等待测试入口。
- [ ] 将已复现问题转成能在旧实现上失败的测试；抽样与确定性用例分开。
- [ ] 只运行对应 net8.0 精准用例，记录失败原因与原问题一致。

### 阶段 B：修复重复 ID、配置与等待

- [ ] 完成 F03 快照与配置结果校验。
- [ ] 按 D01 完成 F01 生命周期；同步更新默认入口文档和测试。
- [ ] 完成 F04/F08；验证超时、回拨异常后的状态仍安全。
- [ ] 完成 F02/F09；验证 TimestampId 首次初始化及序列耗尽。
- [ ] 补 Seata 位布局、逻辑时钟、耗尽边界。
- [ ] 运行雪花/TimestampId 精准测试，再运行 net8.0 模块完整测试。

### 阶段 C：修复 GUID/COMB 并补标准契约

- [x] 按 D02 完成 F05，新增 Uuid1 并明确不支持的旧版本入口。
- [x] 完成 F06 正确布局；旧格式不自动识别，文档已说明迁移影响。
- [x] 完成 F07 输入格式校验。
- [ ] 补 v3/v4/v5 固定向量与位布局；补 Uuid7 默认入口及边界。
- [ ] 更新模块说明与 UUIDv7 指南，区分标准 UUID、COMB、自定义风格。
- [ ] 运行对应精准测试，再运行模块完整测试。

### 阶段 D：兼容与收口

- [ ] 运行五个现有测试目标框架。
- [ ] 构建生产包全部四个目标，确认 netstandard2.0 不被新测试支撑破坏。
- [ ] 公共 API/行为变化涉及的实际调用方完成构建及迁移验证。
- [ ] 如宣称数据库排序兼容或跨进程恢复保证，完成对应真实组件验证；否则明确能力边界。
- [ ] 独立审查本次改动及失败恢复路径，逐项记录关闭依据。
- [ ] 发布说明列出默认节点、热重配、异常类型、TimeBased 与 COMB 布局变化。

协作建议：主维护者负责生命周期、唯一性条件和兼容选择；边界清晰的测试数据整理、GUID 校验修复、固定向量补充可交给 luna_worker。雪花状态转换与异常后的状态提交必须集中设计。最终由独立审查者核对缺陷与验收映射，避免只凭总通过数收口。

## 9. 验证命令与证据规则

在仓库根目录使用 PowerShell，文本输出统一 UTF-8：

~~~powershell
[Console]::InputEncoding = [System.Text.UTF8Encoding]::new($false)
[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)
$OutputEncoding = [System.Text.UTF8Encoding]::new($false)

Set-Location -LiteralPath 'D:/Bing_Framework/Bing.Utils'
$idUtilsTests = 'tests/Bing.Utils.IdUtils.Tests/Bing.Utils.IdUtils.Tests.csproj'
~~~

每次代码测试前记录实际变更文件、公开契约、运行路径、provider、TFM、影响调用方与风险；文档单独改动只做格式、链接和 UTF-8 检查。

L1，雪花与时间戳精准验证：

~~~powershell
dotnet test $idUtilsTests -f net8.0 --filter 'FullyQualifiedName~Snowflake|FullyQualifiedName~Timestamp'
~~~

L1，GUID/UUID 与测试数据验证：

~~~powershell
dotnet test $idUtilsTests -f net8.0 --filter 'FullyQualifiedName~Guid|FullyQualifiedName~Uuid7|FullyQualifiedName~SecureRandomId'
~~~

L2，阶段收口：

~~~powershell
dotnet test $idUtilsTests -f net8.0 --logger 'console;verbosity=minimal'
~~~

最终模块验证与多目标构建：

~~~powershell
dotnet test $idUtilsTests --logger 'console;verbosity=minimal'
dotnet build 'src/Bing.Utils.IdUtils/Bing.Utils.IdUtils.csproj' -c Release
git diff --check -- src/Bing.Utils.IdUtils tests/Bing.Utils.IdUtils.Tests docs/modules/Bing.Utils.IdUtils.md docs/reference/uuid7-guide.md
~~~

可选跨语言向量核对，不替代 .NET 测试：

~~~powershell
python -c "import uuid; print(uuid.uuid3(uuid.NAMESPACE_DNS, 'www.widgets.com')); print(uuid.uuid5(uuid.NAMESPACE_DNS, 'www.widgets.com'))"
~~~

检查完整测试日志中的重复 ID、超时、任务异常和跳过原因；测试数会随新增用例变化，不固定要求仍为 349。

每条证据至少记录：对应问题、源码候选版本/工作区范围、命令、框架、通过失败跳过数、关键断言和结果。修改范围不变时复用已有结果；修改等待或锁等热路径后，仅在确有性能评估需求时做针对性前后对比，不因 Markdown 改动运行基准或全仓测试。未批准的性能阈值不自动成为修复循环条件。

遵循 GOAL_RULES：只有 OPEN_ACTIONABLE 存在时继续修复；外部环境或兼容决策缺失分别记录，不能通过重复本地测试把它们伪装为完成。独立审查没有新问题时停止，避免无进展循环。

## 10. 最终验收清单

- [ ] F01..F09 的每项都有明确结论、对应测试和证据；尚未采用的兼容变更保持显式未完成。
- [ ] Twitter/TimestampId 超时或回拨异常后，不会再次发出已成功分配的 ID。
- [ ] 空工厂或失败配置不改变已生效节点与实例。
- [ ] 安全入口不会在运行中无声切换生成器或退回默认节点。
- [ ] Seata 测试遵循其逻辑时间设计，节点与耗尽边界正确。
- [ ] UUIDv7/v3/v4/v5 的版本、变体、字节序及独立向量通过。
- [ ] TimeBased 不再伪装成已支持 UUIDv1。
- [ ] PostgreSQL COMB 固定向量、排序进位和时间戳往返通过，旧格式使用方式明确。
- [ ] GUID 校验的格式/空白策略与实际解析一致。
- [ ] 所有共享状态测试隔离，无遗留任务，无重复测试 ID 被丢弃。
- [ ] 五个测试框架和四个生产目标通过；公开行为变更对应的调用方迁移完成。
- [ ] 文档准确区分进程内保证、部署前提、随机碰撞概率、逻辑时间与数据库排序。

参考：[RFC 9562 UUIDv7](https://www.rfc-editor.org/rfc/rfc9562.html#section-5.7)、[RFC 9562 示例](https://www.rfc-editor.org/rfc/rfc9562.html#appendix-A.6)、[Apache Seata 算法说明](https://seata.apache.org/zh-cn/blog/seata-analysis-UUID-generator/)。标准规定与本库现有自定义风格分别适用，不将自定义 GUID 风格等同于标准 UUID 版本。

## 实施状态（2026-10-29）

本文方案已按“完整安全契约”路线实施：雪花配置生命周期、Twitter/Seata 边界、TimestampId 可控时钟与序列耗尽、UUIDv1、GuidProvider 版本异常、名称型 GUID 参数校验、PostgreSQL COMB 字节布局和 GuidJudge 解析均已落地。测试已补充静态状态隔离、确定性时钟、RFC 固定向量、并发唯一性和超时状态保持用例。

与初始建议不同，UUIDv1 已提供独立的 `Uuid1` API；`GuidVersion.TimeBased` 继续明确拒绝，避免忽略 namespace/name 参数。PostgreSQL COMB 只修正新生成布局，不增加旧格式自动解码。剩余跨进程节点分配、重启高水位和外部协调仍属于宿主部署责任。

### 雪花生命周期契约收口

- Implementation：完成首次成功发号后冻结、在途调用隔离、全失败后恢复配置、工厂重入发布检查；局部独立审查未发现可执行缺陷。
- Changed Scope：`Id.SnowflakeId.cs`、`IdSnowflakeTest.cs` 及四份相关文档；公开签名、provider 算法、包配置不变。
- L0：差异检查通过；Release 的 net8.0/net7.0/net6.0/netstandard2.0 均构建成功，0 错误；74 条警告来自基础项目 Bing.Utils。
- L1：net8.0 生命周期精准测试 30/30 通过。
- L2：标准多目标命令测试通过，net8.0/net7.0/net6.0/net5.0/netcoreapp3.1 各 389/389，0 失败、0 跳过，无重复 xUnit case ID 警告。
- Reused Evidence：测试后的生产文件修改仅为 XML 注释，复用上述行为测试结果。
- Completed：契约修复、文档同步和验证；Open Actionable：无；Blocked Approval / Blocked External：无。
- Not Applicable：跨项目集成、全解决方案测试和性能门禁；Accepted Limitations：节点协调与重启持久化由宿主负责。
- Verified Boundaries / Deferred：无新增；No-Progress Check：CHANGED；Next Action：STOP；Goal Status：COMPLETED。

### 复审问题修复（2026-10-29）

本轮编号 R01..R06 对应后续复审的六项问题，区别于原方案 F01..F09。基线为 `bdb8193` 上的当前工作区实现；保留其他模块已有修改。执行一次 Fix → 独立只读 Review，独立复核确认以下六项均可关闭。

| 编号 | 状态 | 实现与验证依据 |
|---|---|---|
| R01 | CLOSED | 有效大/小端 GUID 字节入口统一校验非 null、恰好 16 字节；15/17 字节及 null 测试通过，未知 style 保留原行为 |
| R02 | CLOSED | Unix COMB 在毫秒截断前拒绝 Epoch 前时间；-1 tick/-1ms 拒绝、Epoch 和 DateTime.MaxValue 往返测试通过 |
| R03 | CLOSED | 雪花工厂锁外执行，发布时复查生命周期和配置版本；跨线程重入、重置、并发重配和在途调用测试通过 |
| R04 | CLOSED | TimestampId 在接受时间推进前检查预算，使用私有锁；预算内/到期/超时及失败后状态保持测试通过 |
| R05 | CLOSED | UUIDv1 分开维护物理时间和逻辑时间，在锁内采样当前 UTC；重复时刻、回拨恢复、sequence 回绕、60 位边界和 RFC 向量通过 |
| R06 | CLOSED | UUIDv1/TimestampId 并发测试在 finally 中等待所有任务退出，再清理状态；不再留下超时后的后台发号任务 |

影响分析：生产改动限于 `Id.SnowflakeId`、`TimestampId`、`Uuid1`、`Uuid7`、`GuidProvider` 和 `UnixDateTimeStrategy`；测试全部归属 `Bing.Utils.IdUtils.Tests`。公开签名、包版本、目标框架和依赖图不变；异常行为按上述输入与生命周期契约收紧。并发状态与共享 Unix 日期策略属于中等风险，验证覆盖模块完整测试和全部生产目标。没有跨模块集成或全解决方案测试需求。

验证记录：

- L1：新增精准用例在旧实现上出现 8 个预期失败、1 个通过；修复后 Snowflake/Timestamp/UUID/GuidProvider 精准测试 183/183 通过。
- L2：net8.0/net7.0/net6.0/net5.0/netcoreapp3.1 各 409/409，通过数由 389 增至 409；0 失败、0 跳过，没有重复 xUnit case ID 警告。
- L0：Release 的 net8.0/net7.0/net6.0/netstandard2.0 全部构建成功，0 错误；41 条警告来自基础包，本次 IdUtils 无编译警告。范围内差异及 UTF-8 检查通过。
- 独立复核：`verify_idutils_review_fixes`（luna_worker / GPT-6 Luna）只读检查六项修复和 UUID 字节布局，未发现新的 OPEN_ACTIONABLE。
- 原始日志：`%TEMP%/idutils-review-fixes-tests-20261029.log`、`%TEMP%/idutils-review-fixes-build-20261029.log`。

本轮还执行不改变 UUID 布局的分配优化：UUIDv1 使用字段构造函数；UUIDv7 在 .NET 6+ 使用栈缓冲，在 netstandard2.0 保留单数组兼容路径。后者由 net5.0/netcoreapp3.1 测试覆盖。

性能复测使用上一轮相同的 Case 测量方法（Release、.NET 8.0.31、Windows x64、Ultra 9 185H、禁用分层编译，每项预热后 5 轮，每轮 262144 次），只执行受影响的三条 UUID 路径。复用已有基线，不重建 before；下表吞吐为百万次/秒，4 线程为合计吞吐。

| 路径 | 每次分配：修复前 → 后 | 单线程吞吐：前 → 后 | 4 线程吞吐：前 → 后 |
|---|---|---|---|
| UUIDv1 生成 | 80B → 0B | 8.170 → 6.866 | 2.565 → 3.092 |
| UUIDv7 生成 | 120B → 0B | 2.848 → 4.050 | 5.451 → 5.957 |
| UUIDv7 时间戳解析 | 80B → 0B | 23.939 → 108.895 | 40.972 → 188.851 |

UUIDv1 增加正确的物理时钟状态处理后，本轮单线程吞吐较基线下降约 16%，因此只确认分配改善及本机测量结果，不宣称所有路径吞吐提升；上述微基准不作为生产 SLA。基线位于 `%TEMP%/idutils-review-7525bff047/benchmark.log`，本轮位于 `%TEMP%/idutils-review-fixes-perf-896ecaa552/benchmark.log`。

本轮状态：Implementation 6/6；本地验证门禁 3/3（五目标测试、四目标构建、独立复核）；Completed 为 R01..R06、私有锁和 UUID 分配优化；Open Actionable、Blocked Approval、Blocked External 均无。Accepted Limitations 为原有节点协调与重启责任；Verified Boundaries 为已有雪花休眠粒度测量；Deferred 为 Twitter/Seata 等待策略与冻结后快速路径专项调优（涉及节流与生命周期计数机制，本轮未改变）。L3/L4、外部发布和正式性能阈值不适用。No-Progress Check：CHANGED；Next Action：STOP；Goal Status：COMPLETED。
