# Bing.Utils 贡献规范（内部协作）

适用范围：`Bing.Utils` 及其工具子包（Collections / DateTime / Drawing / Http / IdUtils / Reflection / Text）。

## 0. 仓库基线（事实）

- 多目标框架基线：`net8.0;net7.0;net6.0;netstandard2.0`。[证据] `common.props:3`
- 语言版本：`latest`。[证据] `common.props:8` [证据] `common.tests.props:3`
- 测试技术栈：`xunit + Shouldly + Moq + coverlet.collector`。[证据] `common.tests.props:12` [证据] `common.tests.props:31` [证据] `common.tests.props:33` [证据] `common.tests.props:25`
- 解决方案测试组织：逻辑分组 `02-tests`，测试工程位于 `tests/*`。[证据] `Bing.Utils.sln:28` [证据] `Bing.Utils.sln:30` [证据] `Bing.Utils.sln:73` [证据] `Bing.Utils.sln:83` [证据] `Bing.Utils.sln:99`

## 1. 命名规范

### 1.1 扩展方法

- 扩展入口类统一为 `public static class XxxExtensions`。[证据] `src/Bing.Utils/Bing/Extensions/ObjectExtensions.cs:9` [证据] `src/Bing.Utils.Collections/Bing/Collections/DictsExtensions.cs:6` [证据] `src/Bing.Utils.DateTime/Bing/Date/DayOfWeekExtensions.cs:8`
- 方法命名优先使用语义前缀：`To*` / `Is*` / `Get*`（动词或判定短语）。[证据] `src/Bing.Utils.DateTime/Bing/Date/DateTimeExtensions.cs:1061` [证据] `src/Bing.Utils.DateTime/Bing/Date/DateTimeExtensions.cs:375` [证据] `src/Bing.Utils.DateTime/Bing/Date/DateTimeExtensions.cs:170`
- 布尔返回值方法优先使用判定式命名（`Is*`/`Has*`/`Can*`）。[证据] `src/Bing.Utils/Bing/Extensions/ObjectExtensions.cs:197` [证据] `src/Bing.Utils/Bing/Extensions/ObjectExtensions.cs:240`
- 可恢复失败场景优先 `Try*` + `out` + `bool`，避免“返回失败标识同时再抛异常”的双语义 API。[证据] `src/Bing.Utils.IdUtils/Bing/IdUtils/ObjectId.cs:354` [证据] `src/Bing.Utils.IdUtils/Bing/IdUtils/ObjectId.cs:364` [证据] `tests/Bing.Utils.IdUtils.Tests/Bing/IdUtils/ObjectIdTest.cs:328`

### 1.2 工具类与状态类

- 无状态公共能力优先 `public static class`（Helper/Provider/Guard）。[证据] `src/Bing.Utils/Bing/Helpers/Check.cs:16` [证据] `src/Bing.Utils.Http/Bing/Net/IpAddressProvider.cs:15`
- 有状态对象使用名词化类型（`Builder` / `Accessor` / `Factory`）。[证据] `src/Bing.Utils.Drawing/Bing/Drawing/CaptchaBuilder.cs:11` [证据] `src/Bing.Utils.IdUtils/Bing/IdUtils/TraceIdAccessor.cs:6` [证据] `src/Bing.Utils/Bing/Utils/Parameters/Parsers/ParameterParserFactory.cs:6`

### 1.3 Options / 枚举

- 配置对象命名保持 `XxxOptions`。[证据] `src/Bing.Utils/Bing/JsonOptions.cs:6` [证据] `src/Bing.Utils.DateTime/Bing/Date/DateTimeOffsetOptions.cs:6`
- 策略枚举保持 `XxxMode` / `XxxStyle` 命名族，避免混乱缩写。[证据] `src/Bing.Utils.Drawing/Bing/Drawing/ThumbnailMode.cs:6` [证据] `src/Bing.Utils.IdUtils/Bing/IdUtils/GuidStyle.cs:6`

### 1.4 异常与日志

- 参数异常类型保持语义一致：
- `ArgumentNullException`：空引用参数。[证据] `src/Bing.Utils/Bing/Helpers/Check.cs:93` [证据] `src/Bing.Utils.Http/Bing/Helpers/Web.cs:488`
- `ArgumentException`：格式/语义非法。[证据] `src/Bing.Utils/Bing/Helpers/Check.cs:126` [证据] `src/Bing.Utils.Http/Bing/Helpers/Web.cs:413`
- `ArgumentOutOfRangeException`：范围越界。[证据] `src/Bing.Utils.IdUtils/Bing/IdUtils/ObjectId.cs:344` [证据] `src/Bing.Utils.IdUtils/Bing/Helpers/Id.SnowflakeId.cs:142`
- `InvalidOperationException`：状态不允许当前操作。[证据] `src/Bing.Utils.Http/Bing/Helpers/Web.cs:563`
- 测试必须断言 `ParamName`/消息关键字，防止异常语义漂移。[证据] `tests/Bing.Utils.Http.Tests/Bing/Helpers/WebTest.cs:450` [证据] `tests/Bing.Utils.Http.Tests/Bing/Helpers/WebTest.cs:451` [证据] `tests/Bing.Utils.IdUtils.Tests/Bing/Helpers/IdSnowflakeTest.cs:321` [证据] `tests/Bing.Utils.IdUtils.Tests/Bing/Helpers/IdSnowflakeTest.cs:322`
- 需要日志时统一走 `LogHelper`（`LogInformation/LogWarning/LogError`），不要在库层直接 `Console` 输出。[证据] `src/Bing.Utils/Bing/Logging/LogHelper.cs:331` [证据] `src/Bing.Utils/Bing/Logging/LogHelper.cs:338` [证据] `src/Bing.Utils/Bing/Logging/LogHelper.cs:345` [证据] `tests/Bing.Utils.Tests/Bing/Logging/LogHelperTest.cs:103`

## 2. 新增工具方法准入清单（Gate）

新增 `public` API 前必须全部满足：

- 可泛化：不是业务专有逻辑；至少能服务两个以上调用场景或被多个子包复用。[证据] `tests/Bing.Utils.Tests/Bing.Utils.Tests.csproj:11` [证据] `tests/Bing.Utils.Tests/Bing.Utils.Tests.csproj:19`
- 可测试：可写稳定单测（Happy Path / 非法输入 / 边界值），不依赖不可控环境状态。[证据] `tests/Bing.Utils.IdUtils.Tests/Bing/IdUtils/ObjectIdTest.cs:307` [证据] `tests/Bing.Utils.IdUtils.Tests/Bing/IdUtils/ObjectIdTest.cs:328` [证据] `tests/Bing.Utils.Http.Tests/Bing/Helpers/WebTest.cs:447`
- 语义清晰：命名、返回值、异常类型一致且可预期。[证据] `src/Bing.Utils.IdUtils/Bing/IdUtils/ObjectId.cs:339` [证据] `src/Bing.Utils.IdUtils/Bing/IdUtils/ObjectId.cs:354`
- 边界明确：`null`/空/越界/冲突等分支必须有定义（抛异常或返回默认值）。[证据] `src/Bing.Utils/Bing/Helpers/Check.cs:87` [证据] `src/Bing.Utils/Bing/Helpers/Check.cs:392`
- 不破坏已有语义：避免同名 API 行为变化、默认值变化、异常类型变化（见第 5 节）。[证据] `src/Bing.Utils/Bing/Date/TimeStamp.cs:426` [证据] `src/Bing.Utils.IdUtils/Bing/Helpers/Id.SnowflakeId.cs:154`

## 3. 性能基线要求

- 热路径避免无必要分配；优先复用现有池化设施（`ArrayPool<T>`）。[证据] `src/Bing.Utils/Bing/IO/PooledMemoryStream.cs:48` [证据] `src/Bing.Utils/Bing/IO/PooledMemoryStream.cs:61` [证据] `src/Bing.Utils/Bing/IO/LargeMemoryStream.cs:224` [证据] `src/Bing.Utils/Bing/IO/LargeMemoryStream.cs:179`
- 对字符串/二进制高频处理，优先提供 `Span<T>` / `ReadOnlySpan<T>` 友好实现。[证据] `src/Bing.Utils/Bing/IO/PooledMemoryStream.cs:120` [证据] `src/Bing.Utils/Bing/Text/Strings/Strings.StringBuilder.cs:408`
- 公开声明“线程安全/可并发”时，必须给出实现依据（`lock`/不可变/原子）并配并发测试。[证据] `src/Bing.Utils.IdUtils/Bing/IdUtils/ModelIdAccessor.cs:96` [证据] `src/Bing.Utils.IdUtils/Bing/IdUtils/ModelIdAccessor.cs:138` [证据] `tests/Bing.Utils.IdUtils.Tests/Bing/IdUtils/ModelIdAccessorTest.cs:302` [证据] `tests/Bing.Utils.IdUtils.Tests/Bing/Helpers/IdSnowflakeTest.cs:449`
- 性能敏感改动建议附 benchmark 对比（基准工程已存在）。[证据] `benchmarks/Bing.Utils.Benchmark/Bing.Utils.Benchmark.csproj:11` [证据] `benchmarks/Bing.Utils.Benchmark/Bing.Utils.Benchmark.csproj:19`

## 4. 文档与测试门槛

- 新增 `public API` 必须补齐 XML 注释：`summary/param/returns/exception`。构建已产出 XML 文档文件。[证据] `common.props:22` [证据] `common.props:26` [证据] `src/Bing.Utils.IdUtils/Bing/IdUtils/ObjectId.cs:332` [证据] `src/Bing.Utils.IdUtils/Bing/IdUtils/ObjectId.cs:337`
- 必须提供至少 1 个最小示例，优先直接引用测试用例。模板已要求 API、测试策略、使用示例。[证据] `docs/TEMPLATE.module.md:13` [证据] `docs/TEMPLATE.module.md:45` [证据] `docs/TEMPLATE.module.md:56`
- 新增/变更 API 时同步更新：
- `docs/modules/*.md`
- `docs/reference/api-reference.md`
- 测试最低门槛：
- 正常路径
- 非法输入（异常语义）
- 边界值（空、极值、临界）
- 并发或集成行为涉及时，追加并发测试或 integration 测试。[证据] `Bing.Utils.sln:83` [证据] `tests/Bing.Utils.IdUtils.Tests/Bing/Helpers/IdSnowflakeTest.cs:384` [证据] `tests/Bing.Utils.IdUtils.Tests/Bing/Helpers/IdSnowflakeTest.cs:416`
- 推荐本地验证命令：

```bash
dotnet test Bing.Utils.sln -c Release
```

## 5. 兼容性策略

- 版本遵循 `Major.Minor.Patch` 结构（仓库版本属性已拆分维护）。[证据] `version.props:3` [证据] `version.props:4` [证据] `version.props:5` [证据] `version.props:7`
- 视为 Breaking Change：
- public 签名变更（参数类型/顺序/返回类型）
- 默认行为变化导致结果不同
- 异常类型或异常触发条件变化
- public 类型/命名空间重命名或移除
- 弃用流程采用“先标记再迁移”：优先 `[Obsolete("迁移建议", false)]`，给出替代 API。[证据] `src/Bing.Utils/Bing/Date/TimeStamp.cs:426` [证据] `src/Bing.Utils/Bing/Date/TimeStamp.cs:433` [证据] `src/Bing.Utils.IdUtils/Bing/Helpers/Id.SnowflakeId.cs:154`

## 6. PR 检查清单（可复制）

```markdown
## PR Checklist

- [ ] 变更为基础设施通用能力，不包含业务语义
- [ ] 命名符合规范（Extensions / Builder|Accessor|Factory / Options / Mode|Style）
- [ ] 新增 public API 具备 XML 注释（summary/param/returns/exception）
- [ ] 新增 public API 已补示例（优先测试代码）
- [ ] 已覆盖 Happy Path / 非法输入 / 边界值
- [ ] 涉及并发时已补并发测试
- [ ] 涉及集成行为时已补 integration 测试或标注 TODO 风险
- [ ] 性能敏感改动已给出 benchmark 或分配对比
- [ ] 已更新 docs/modules 与 docs/reference/api-reference.md
- [ ] 本地已执行：`dotnet test Bing.Utils.sln -c Release`
- [ ] 若涉及弃用，已给出 Obsolete 迁移说明
```

## 7. 待确认项

- TODO：`Obsolete` 保留周期（至少 1 个 Minor 还是按发布季度）需在发布流程中固化。
- TODO：性能回归门槛（例如分配增幅/吞吐下降阈值）当前仓库未形成统一数值标准。
- TODO：日志分类命名（`categoryName` 规范）尚无统一文档约定。[证据] `src/Bing.Utils/Bing/Logging/LogHelper.cs:101`
