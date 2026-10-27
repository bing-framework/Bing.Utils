# 通用工具主线 B 方案实施计划

用户已批准实施全部四项。执行遵循 AGENTS.md、GOAL_RULES.md 和测试归属规则；不提交、不发布，不改变版本号。

## 1. 目标与固定选择

| 模块 | 实现方式 | 放置位置 |
|---|---|---|
| KeyedLock | 自研按 key 隔离与回收机制 | Bing.Utils |
| SingleFlight | 自研共享任务与等待取消机制 | Bing.Utils |
| Cron | 固定依赖 Cronos 0.13.0 | 新增 Bing.Utils.Cron |
| Diff | DiffPlex-1.7.0 必要算法源码的最小适配，加入取消与预算 | 新增 Bing.Utils.Diff |

- 核心不新增依赖，保持现有目标框架。新包为 netstandard2.0/net8.0，专属测试为 net6.0/net8.0；net6 实际消费 netstandard2.0 产物。
- 新接口有中文 XML 注释、示例、取消/异常/生命周期契约。保留工作区已有修改，不迁移 HTTP 缓存、不修改旧 AsyncLock。
- 不包含发布、提交、分布式锁、调度器、缓存 TTL、字符级 Diff、三方合并。

## 2. 并发工具

### KeyedLock

在 Bing.Threading 增加 KeyedAsyncLock<TKey>。构造接收可选 IEqualityComparer<TKey>，默认使用类型默认比较器。

- LockAsync(key, cancellationToken) 返回可释放引用类型租约。
- TryLockAsync(key, timeout, cancellationToken) 超时返回空租约，取消抛 OperationCanceledException。
- 租约重复释放幂等。同实例同 key 互斥，不同 key 独立；非重入，不保证严格 FIFO。
- 短临界区保护条目表；每条目拥有 SemaphoreSlim 和引用计数。登记引用后才能等待，取消/超时/释放归还引用；持有者、等待者全部退出后核对条目身份并回收，避免同 key 两把有效锁。
- 组件不提供销毁正在使用锁的接口；租约释放，空闲条目自动清理。

### SingleFlight

在 Bing.Threading 增加 SingleFlight<TKey,TValue>。构造固定 Func<TKey,CancellationToken,Task<TValue>> 工厂、可选比较器和生命周期取消令牌。

- RunAsync(key, cancellationToken) 合并正在执行的同 key 操作。
- 调用方令牌只取消自己的等待；工厂只接收生命周期令牌，执行超时由工厂自行配置。
- 不缓存完成结果；成功/失败/取消后回收，之后可重新执行。共享结果对象，保留工厂异常和任务取消状态。
- 预取消不创建任务；处理同步工厂异常、空任务、全部等待者退出后工厂失败。
- 先登记启用异步 continuation 的共享 TaskCompletionSource，再在锁外启动工厂。发布、完成和回收保持一致；按条目身份清理。

## 3. Cron 与大文本 Diff

### Cron

提供 Bing.Date.Cron.CronSchedule：Parse/TryParse、自有五段/六段格式枚举（默认五段）；GetNextOccurrence 接收 DateTimeOffset、显式 TimeZoneInfo、inclusive（默认 false），无匹配返回 null；GetOccurrences 必须指定有限起止时间和结果上限，区间 [from,to)，达到上限正常结束，支持枚举取消。统一返回 UTC DateTimeOffset。

保持 Cronos 匹配和 DST 语义，明确月日与星期同时指定为 AND，不声明 Quartz 七段兼容。解析错误有中文说明，不伪造字符位置。

### Diff

同步入口 TextDiffResult TextDiff.Compare(string oldText, string newText, TextDiffOptions options = null, CancellationToken cancellationToken = default)。

用户批准默认限制：每份 200,000 逻辑行、16,000,000 UTF-16 代码单元，单次 30 秒。可配置，但预算必须为有限正值。主要验收 10 万行稀疏增删改和追加；高差异输入预算耗尽明确失败；真实计算支持取消。

- 固定来源 DiffPlex-1.7.0，仅纳入必要行级算法/模型，使用内部命名空间；保留来源、许可证及修改说明，随 NuGet 打包第三方归属材料。
- 可取消行扫描替代一次性 Split；分行、哈希、前后缀、对角线搜索、结果汇总均检查取消/预算。字符扫描最多每 4096 个代码单元、算法最多每 1024 次迭代检查；阶段/子问题边界额外检查。
- 显式工作栈替代无界递归，复用线性工作数组，不创建行数乘积矩阵。
- 单调时钟预算覆盖扫描至结果构建；协作预算不是硬实时保证。
- 取消抛 OperationCanceledException；字数/行数/耗时超限抛 TextDiffLimitExceededException 并区分稳定错误代码；不返回部分结果。
- 结果包含原始行集合、不可变差异块（新增/删除/替换，旧新起始索引/行数），索引 0 起始；插入/删除一侧长度可为 0；不逐项返回未变化行。
- CRLF/LF/CR 均为分隔符，保留末尾空行，空文本为 0 行。默认大小写/空白敏感，可选忽略大小写、行首尾空白；输出保留原文。
- 不提供伪异步包装；调用方自行安排后台执行，令牌进入真实循环。

## 4. 交付单元

| 单元 | 交付内容 | 完成条件 |
|---|---|---|
| U0 | 保存计划、建立项目/测试项目、固定契约/来源 | 引用清晰，核心无新增依赖 |
| U1 | KeyedLock 与测试 | 互斥、取消、回收竞争正确 |
| U2 | SingleFlight 与测试 | 合并、取消隔离、异常后重试正确 |
| U3 | Cron 与测试 | 语法、时间边界和 DST 正确 |
| U4 | Diff 与大文本验证 | 正确性、规模、真实取消和预算正确 |
| U5 | 文档、消费者、打包、最终回归 | 验收有明确证据 |

主代理负责并发核心、Diff 与集成；luna_worker 可负责契约已固定的 Cron、独立夹具或文档核对。新增项目加入解决方案，扩展 CapabilityConsumer 验证四个消费目标，更新能力地图、模块索引和指南，不增加插件框架。

## 5. 验收

- 并发测试以屏障/信号控制交错，不依赖固定休眠。记录同 key/不同 key 的实际并发及工厂调用次数。覆盖预取消、等待取消、超时、重复释放、回收竞争、异常/取消后重试和资源清理。
- Cron 覆盖五/六段、非法输入、闰年、月末、区间边界、数量限制、无匹配、DST 跳时/重复，使用可控时区。
- Diff 覆盖空/相同/增删改/重复行/中文/emoji/换行/末尾空行；小型语料与原版对照，差异可重建目标。
- 10 万行固定语料分别追加 1000 行，及分散 100 处插入/删除/修改，在默认预算内得到完整正确结果。验证 20 万行/1600 万代码单元边界及超限拒绝。
- 受控内部时钟/检查点验证中途取消和超时；高差异按预算退出。记录耗时/分配，不宣称未经验证的性能或内存上限。
- 顺序：git diff --check，受影响项目构建/精准测试，三个对应项目完整测试，最后一次全解决方案构建/回归。

```powershell
dotnet build Bing.Utils.sln -c Release
dotnet build build/CapabilityConsumer/CapabilityConsumer.csproj -c Release
dotnet test Bing.Utils.sln -c Release --no-build
```

新包另做本地 pack 与包消费，验证目标框架/依赖/README/许可，不发布。仅 OPEN_ACTIONABLE 触发修复，按影响范围验证；不因文档或外部门禁重复全量测试。

## 6. 执行结果

2026-10-27：U0–U5 全部完成。全量回归累计 66905 通过、90 跳过、0 失败；构建、四目标消费者、本地打包和实际包消费均通过。证据与边界见 [execution.md](execution.md)。
