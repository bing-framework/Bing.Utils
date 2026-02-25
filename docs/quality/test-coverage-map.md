# 测试覆盖与质量画像

## 1. 测试资产总览

- 解决方案存在逻辑分组 `02-tests`（solution folder），其下测试项目物理路径集中在 `tests/*`，包含分包测试与聚合测试两类。[证据] `Bing.Utils.sln:28` [证据] `Bing.Utils.sln:30` [证据] `Bing.Utils.sln:73` [证据] `Bing.Utils.sln:83` [证据] `Bing.Utils.sln:99`
- 测试基础设施统一为 xUnit + Shouldly + Moq + coverlet（具备单测与覆盖率采集能力）。[证据] `common.tests.props:12` [证据] `common.tests.props:31` [证据] `common.tests.props:33` [证据] `common.tests.props:25`
- 聚合测试工程 `Bing.Utils.Tests` / `BingUtilsUT` 同时引用多个子包，承担回归测试职责。[证据] `tests/Bing.Utils.Tests/Bing.Utils.Tests.csproj:11` [证据] `tests/Bing.Utils.Tests/Bing.Utils.Tests.csproj:19` [证据] `tests/BingUtilsUT/BingUtilsUT.csproj:10` [证据] `tests/BingUtilsUT/BingUtilsUT.csproj:18`
- 分包测试工程已建立于 Collections / DateTime / Http / IdUtils（其中 Http 含 Integration）。[证据] `tests/Bing.Utils.Collections.Tests/Bing.Utils.Collections.Tests.csproj:11` [证据] `tests/Bing.Utils.DateTime.Tests/Bing.Utils.DateTime.Tests.csproj:11` [证据] `tests/Bing.Utils.Http.Tests/Bing.Utils.Http.Tests.csproj:11` [证据] `tests/Bing.Utils.Http.Tests.Integration/Bing.Utils.Http.Tests.Integration.csproj:40` [证据] `tests/Bing.Utils.IdUtils.Tests/Bing.Utils.IdUtils.Tests.csproj:11`

## 2. 子包覆盖热力图

| 子包 | 测试深度 | 判定依据 | 备注 |
|---|---|---|---|
| `Bing.Utils` | 高 | 聚合工程直接引用 + 大量基础类测试（`FileHelperTest`、`BooleanExtensionsTest`、`CheckTest`）。[证据] `tests/Bing.Utils.Tests/Bing.Utils.Tests.csproj:19` [证据] `tests/Bing.Utils.Tests/Bing/IO/FileHelperTest.cs:8` [证据] `tests/Bing.Utils.Tests/Bing/Extensions/Base/BooleanExtensionsTest.cs:7` [证据] `tests/Bing.Utils.Tests/Bing/Helpers/CheckTest.cs:7` | 基础守卫/扩展覆盖较密集 |
| `Bing.Utils.Collections` | 中 | 有独立测试工程，但当前核心集中在 `ArraysTest` 单类。[证据] `tests/Bing.Utils.Collections.Tests/Bing.Utils.Collections.Tests.csproj:11` [证据] `tests/Bing.Utils.Collections.Tests/Bing/Collections/ArraysTest.cs:10` | `Dicts/ReadOnlyDicts/Colls` 覆盖偏弱 |
| `Bing.Utils.DateTime` | 高 | 独立测试工程 + 多测试类，覆盖转换、偏移、农历、星座等分支。[证据] `tests/Bing.Utils.DateTime.Tests/Bing.Utils.DateTime.Tests.csproj:11` [证据] `tests/Bing.Utils.DateTime.Tests/Bing/Date/Extensions/DateTimeExtensionsToTest.cs:10` [证据] `tests/Bing.Utils.DateTime.Tests/Bing/Date/DateUtils/DateTimeCalcTest.cs:9` | 包含边界与异常路径 |
| `Bing.Utils.Drawing` | 低 | 仅见 `CaptchaBuilderTest` 冒烟式用例，断言强度有限（主要输出/保存）。[证据] `tests/Bing.Utils.Tests/Bing.Utils.Tests.csproj:13` [证据] `tests/Bing.Utils.Tests/Drawing/CaptchaBuilderTest.cs:5` [证据] `tests/Bing.Utils.Tests/Drawing/CaptchaBuilderTest.cs:37` | 缺少像素级/异常/并发保障 |
| `Bing.Utils.Drawing.ImageSharp` | 低 | 存在独立包，但测试工程引用清单未见该包。[证据] `src/Bing.Utils.Drawing.ImageSharp/Bing.Utils.Drawing.ImageSharp.csproj:3` [证据] `tests/Bing.Utils.Tests/Bing.Utils.Tests.csproj:11` [证据] `tests/Bing.Utils.Tests/Bing.Utils.Tests.csproj:20` | 当前判定为“无直接测试”（推断） |
| `Bing.Utils.Drawing.SkiaSharp` | 低 | 存在独立包，但测试工程引用清单未见该包。[证据] `src/Bing.Utils.Drawing.SkiaSharp/Bing.Utils.Drawing.SkiaSharp.csproj:3` [证据] `tests/Bing.Utils.Tests/Bing.Utils.Tests.csproj:11` [证据] `tests/Bing.Utils.Tests/Bing.Utils.Tests.csproj:20` | 当前判定为“无直接测试”（推断） |
| `Bing.Utils.Http` | 高 | 独立单测 + Integration 双层覆盖，含 Web/IPv4/IPv6/Cookie/IpProvider/HTTP 调用链路。 [证据] `tests/Bing.Utils.Http.Tests/Bing.Utils.Http.Tests.csproj:11` [证据] `tests/Bing.Utils.Http.Tests.Integration/Bing.Utils.Http.Tests.Integration.csproj:40` [证据] `tests/Bing.Utils.Http.Tests/Bing/Helpers/WebTest.cs:12` [证据] `tests/Bing.Utils.Http.Tests.Integration/Http/HttpClientServiceTest.Get.cs:8` | 兼顾单测与端到端行为 |
| `Bing.Utils.IdUtils` | 高 | 独立测试工程，覆盖 ObjectId/Guid/Snowflake/并发与性能场景。 [证据] `tests/Bing.Utils.IdUtils.Tests/Bing.Utils.IdUtils.Tests.csproj:11` [证据] `tests/Bing.Utils.IdUtils.Tests/Bing/IdUtils/ObjectIdTest.cs:9` [证据] `tests/Bing.Utils.IdUtils.Tests/Bing/Helpers/IdSnowflakeTest.cs:10` | 唯一性与线程安全覆盖较强 |
| `Bing.Utils.Reflection` | 中 | 主要依赖聚合测试（`TypeUT` + `ReflectionsTest`），无独立测试工程。 [证据] `tests/Bing.Utils.Tests/Bing.Utils.Tests.csproj:17` [证据] `tests/BingUtilsUT/BingUtilsUT.csproj:16` [证据] `tests/BingUtilsUT/TypeUT/TypeIsNumericTypeTest.cs:6` [证据] `tests/Bing.Utils.Tests/Bing/Reflection/ReflectionsTest.cs:8` | 类型判定覆盖好，契约化不足 |
| `Bing.Utils.Text` | 中 | 依赖聚合测试（`SplitterUT` + `StringSimilarityTest`），无独立测试工程。 [证据] `tests/Bing.Utils.Tests/Bing.Utils.Tests.csproj:18` [证据] `tests/BingUtilsUT/BingUtilsUT.csproj:17` [证据] `tests/BingUtilsUT/SplitterUT/SplitterTest.cs:5` [证据] `tests/Bing.Utils.Tests/Bing/Text/Similarity/StringSimilarityTest.cs:4` | 主流程覆盖可用，异常边界不足 |

## 3. 功能-测试映射清单

| 功能点 | 测试项目 | 测试类 | 关键用例 |
|---|---|---|---|
| 数组安全转换/枚举（Collections） | `Bing.Utils.Collections.Tests` | `ArraysTest` | `ToArraySafety_InvalidCastFromArrayWithCount_ThrowInvalidCastException`、`GetLength_WithNullArray_ThrowArgumentNullException`、`ReverseEnumerate_WithNullArray_ReturnEmptyEnumerable`。[证据] `tests/Bing.Utils.Collections.Tests/Bing/Collections/ArraysTest.cs:213` [证据] `tests/Bing.Utils.Collections.Tests/Bing/Collections/ArraysTest.cs:347` [证据] `tests/Bing.Utils.Collections.Tests/Bing/Collections/ArraysTest.cs:622` |
| 时区与纪元转换（DateTime） | `Bing.Utils.DateTime.Tests` | `DateTimeExtensionsToTest` | `ToUtc_ConvertLocalToUtc_SetsKindToUtc`、`ToCst_ReturnsChineseStandardTime`、`ToEpochTimeSpan_BeforeEpoch_ReturnsNegativeTimespan`。[证据] `tests/Bing.Utils.DateTime.Tests/Bing/Date/Extensions/DateTimeExtensionsToTest.cs:23` [证据] `tests/Bing.Utils.DateTime.Tests/Bing/Date/Extensions/DateTimeExtensionsToTest.cs:84` [证据] `tests/Bing.Utils.DateTime.Tests/Bing/Date/Extensions/DateTimeExtensionsToTest.cs:127` |
| 日期偏移与周历法（DateTime） | `Bing.Utils.DateTime.Tests` | `DateTimeCalcTest` | `OffsetByWeek_InvalidWeekAtMonth_ThrowsArgumentException`、`OffsetByMonths_RelativelyMode_HandlesMonthBoundaries`、`OffsetByYears_HandlesLeapYears`。[证据] `tests/Bing.Utils.DateTime.Tests/Bing/Date/DateUtils/DateTimeCalcTest.cs:182` [证据] `tests/Bing.Utils.DateTime.Tests/Bing/Date/DateUtils/DateTimeCalcTest.cs:364` [证据] `tests/Bing.Utils.DateTime.Tests/Bing/Date/DateUtils/DateTimeCalcTest.cs:472` |
| Web 上下文与 Cookie/下载（Http） | `Bing.Utils.Http.Tests` | `WebTest`、`CookieHelperTest` | `AccessToken_ShouldReturnToken_WhenBearerAuthorizationExists`、`GetParam_ShouldThrowArgumentException_WhenNameIsNullOrWhiteSpace`、`DownloadFileAsync_ShouldThrowFileNotFoundException_WhenFileNotExists`、`WriteCookie_ShouldThrowArgumentNullException_WhenHttpContextIsNull`。[证据] `tests/Bing.Utils.Http.Tests/Bing/Helpers/WebTest.cs:173` [证据] `tests/Bing.Utils.Http.Tests/Bing/Helpers/WebTest.cs:447` [证据] `tests/Bing.Utils.Http.Tests/Bing/Helpers/WebTest.cs:1095` [证据] `tests/Bing.Utils.Http.Tests/Bing/Helpers/CookieHelperTest.cs:133` |
| IP 地址验证与转换（Http） | `Bing.Utils.Http.Tests` | `IPv4ValidatorTest`、`IPv6ConverterTest`、`IpAddressProviderTest` | `IsInSameSubnet_InvalidParams_ThrowsArgumentException`、`ToBytes_InvalidIPv6_ThrowsArgumentException`、`GetPublicIpAsync_WithShortTimeout_HandlesTimeoutGracefully`。[证据] `tests/Bing.Utils.Http.Tests/Bing/Net/IPv4/IPv4ValidatorTest.cs:463` [证据] `tests/Bing.Utils.Http.Tests/Bing/Net/IPv6/IPv6ConverterTest.cs:66` [证据] `tests/Bing.Utils.Http.Tests/Bing/Net/IpAddressProviderTest.cs:453` |
| HTTP 客户端链路（Http Integration） | `Bing.Utils.Http.Tests.Integration` | `HttpClientServiceTest` | `Test_Get_Header_1`、`Test_Get_Cookie_2`、`Test_Post_FileContent_4`、`Test_Put_2`、`Test_Delete_2`。[证据] `tests/Bing.Utils.Http.Tests.Integration/Http/HttpClientServiceTest.Get.cs:70` [证据] `tests/Bing.Utils.Http.Tests.Integration/Http/HttpClientServiceTest.Get.cs:172` [证据] `tests/Bing.Utils.Http.Tests.Integration/Http/HttpClientServiceTest.Post.cs:107` [证据] `tests/Bing.Utils.Http.Tests.Integration/Http/HttpClientServiceTest.Put.cs:26` [证据] `tests/Bing.Utils.Http.Tests.Integration/Http/HttpClientServiceTest.Delete.cs:23` |
| ObjectId 解析与唯一性（IdUtils） | `Bing.Utils.IdUtils.Tests` | `ObjectIdTest` | `Parse_InvalidLength_ThrowsArgumentOutOfRangeException`、`TryParse_InvalidString_ReturnsFalse`、`ConcurrentGeneration_ProducesUniqueObjectIds`。[证据] `tests/Bing.Utils.IdUtils.Tests/Bing/IdUtils/ObjectIdTest.cs:296` [证据] `tests/Bing.Utils.IdUtils.Tests/Bing/IdUtils/ObjectIdTest.cs:328` [证据] `tests/Bing.Utils.IdUtils.Tests/Bing/IdUtils/ObjectIdTest.cs:685` |
| Snowflake 生成器配置与并发（IdUtils） | `Bing.Utils.IdUtils.Tests` | `IdSnowflakeTest` | `CreateSnowflakeIds_InvalidCount_ThrowsArgumentOutOfRangeException`、`ConfigureSnowflakeId_NullGenerator_ThrowsArgumentNullException`、`CreateSnowflakeId_ConcurrentGeneration_ProducesUniqueIds`。[证据] `tests/Bing.Utils.IdUtils.Tests/Bing/Helpers/IdSnowflakeTest.cs:232` [证据] `tests/Bing.Utils.IdUtils.Tests/Bing/Helpers/IdSnowflakeTest.cs:318` [证据] `tests/Bing.Utils.IdUtils.Tests/Bing/Helpers/IdSnowflakeTest.cs:384` |
| Guid/索引生成并发行为（IdUtils） | `Bing.Utils.IdUtils.Tests` | `IdGuidTest`、`ModelIdAccessorTest` | `Configure_ConcurrentCalls_IsThreadSafe`、`GetNextIndex_ConcurrentAccess_ProducesUniqueValues`。[证据] `tests/Bing.Utils.IdUtils.Tests/Bing/Helpers/IdGuidTest.cs:85` [证据] `tests/Bing.Utils.IdUtils.Tests/Bing/IdUtils/ModelIdAccessorTest.cs:302` |
| 类型判定与反射访问（Reflection） | `BingUtilsUT`、`Bing.Utils.Tests` | `TypeIsNumericTypeTest`、`TypeIsTupleTest`、`CreateInterfaceTest`、`ReflectionsTest` | `Types.IsNumericType(...IgnoreNullable)`、`Types.IsTupleType(null)`、`CreateInstance_With_Two_Params_With_WrongSort`、`GetPropertyValueByPath(...NotExists)`。[证据] `tests/BingUtilsUT/TypeUT/TypeIsNumericTypeTest.cs:64` [证据] `tests/BingUtilsUT/TypeUT/TypeIsTupleTest.cs:83` [证据] `tests/BingUtilsUT/TypeUT/CreateInterfaceTest.cs:90` [证据] `tests/Bing.Utils.Tests/Bing/Reflection/ReflectionsTest.cs:467` |
| 程序集元数据读取（Reflection） | `Bing.Utils.Tests` | `AssemblyExtensionsTest` | `Test_GetFileVersion_1`、`Test_GetProductVersion_1`。[证据] `tests/Bing.Utils.Tests/Extensions/Reflections/AssemblyExtensionsTest.cs:14` [证据] `tests/Bing.Utils.Tests/Extensions/Reflections/AssemblyExtensionsTest.cs:25` |
| Splitter 主流程（Text） | `BingUtilsUT` | `SplitterTest`、`MapSplitterTest`、`FixedLengthSplitterTest` | `Limit`、`TrimResults`、`WithKeyValueSeparator`、`FixedLength(...).SplitToList`。[证据] `tests/BingUtilsUT/SplitterUT/SplitterTest.cs:94` [证据] `tests/BingUtilsUT/SplitterUT/MapSplitterTest.cs:31` [证据] `tests/BingUtilsUT/SplitterUT/FixedLengthSplitterTest.cs:34` |
| 相似度算法（Text） | `Bing.Utils.Tests` | `StringSimilarityTest` | `Test_EvaluateSimilarity_Same`、`Test_EvaluateSimilarity_Any75`。[证据] `tests/Bing.Utils.Tests/Bing/Text/Similarity/StringSimilarityTest.cs:10` [证据] `tests/Bing.Utils.Tests/Bing/Text/Similarity/StringSimilarityTest.cs:61` |
| 验证码生成（Drawing） | `Bing.Utils.Tests` | `CaptchaBuilderTest` | `Test_GetCode_NumberAndLetter`、`Test_GetCode_ChineseChar`、`Test_CreateImage`（当前以执行/输出为主）。[证据] `tests/Bing.Utils.Tests/Drawing/CaptchaBuilderTest.cs:16` [证据] `tests/Bing.Utils.Tests/Drawing/CaptchaBuilderTest.cs:30` [证据] `tests/Bing.Utils.Tests/Drawing/CaptchaBuilderTest.cs:37` |
| 基础守卫/扩展（Bing.Utils） | `Bing.Utils.Tests` | `FileHelperTest`、`BooleanExtensionsTest`、`CheckTest` | `Test_ReadToString`、`MustTrue_ValueIsFalse_ThrowArgumentException`、`NotNull_NullValue_ThrowsArgumentNullException`。[证据] `tests/Bing.Utils.Tests/Bing/IO/FileHelperTest.cs:14` [证据] `tests/Bing.Utils.Tests/Bing/Extensions/Base/BooleanExtensionsTest.cs:29` [证据] `tests/Bing.Utils.Tests/Bing/Helpers/CheckTest.cs:90` |

## 4. 边界场景缺口

1. `Bing.Utils.Drawing.ImageSharp` 缺少直接测试（高风险）
- 包存在公开 API `ImageSharpHelper.SetOpacity`，包含 `null` 与范围异常分支。[证据] `src/Bing.Utils.Drawing.ImageSharp/Bing/Drawing/ImageSharpHelper.cs:21` [证据] `src/Bing.Utils.Drawing.ImageSharp/Bing/Drawing/ImageSharpHelper.cs:24` [证据] `src/Bing.Utils.Drawing.ImageSharp/Bing/Drawing/ImageSharpHelper.cs:26`
- 现有测试工程引用清单未见该包（推断未覆盖）。[证据] `tests/Bing.Utils.Tests/Bing.Utils.Tests.csproj:11` [证据] `tests/Bing.Utils.Tests/Bing.Utils.Tests.csproj:20`

2. `Bing.Utils.Drawing.SkiaSharp` 缺少直接测试（高风险）
- 包中 `SkiaSharpHelper` / `SKEncodedImageFormatExtensions` 含 `ArgumentNullException`、`ArgumentOutOfRangeException` 和格式转换分支。[证据] `src/Bing.Utils.Drawing.SkiaSharp/Bing/Drawing/SkiaSharpHelper.cs:19` [证据] `src/Bing.Utils.Drawing.SkiaSharp/Bing/Drawing/SkiaSharpHelper.cs:24` [证据] `src/Bing.Utils.Drawing.SkiaSharp/Bing/Drawing/SkiaSharpHelper.Load.cs:47` [证据] `src/Bing.Utils.Drawing.SkiaSharp/Bing/Drawing/Extensions/SKEncodedImageFormatExtensions.cs:16`
- 现有测试工程引用清单未见该包（推断未覆盖）。[证据] `tests/Bing.Utils.Tests/Bing.Utils.Tests.csproj:11` [证据] `tests/Bing.Utils.Tests/Bing.Utils.Tests.csproj:20`

3. `Bing.Utils.Drawing` 仅冒烟，未覆盖异常与稳定性
- 源码存在明确异常分支：`GetCode(length<=0)`、`CreateImage(code空)`。[证据] `src/Bing.Utils.Drawing/Bing/Drawing/CaptchaBuilder.cs:118` [证据] `src/Bing.Utils.Drawing/Bing/Drawing/CaptchaBuilder.cs:121` [证据] `src/Bing.Utils.Drawing/Bing/Drawing/CaptchaBuilder.cs:216` [证据] `src/Bing.Utils.Drawing/Bing/Drawing/CaptchaBuilder.cs:219`
- 现有测试主要调用正常路径并输出结果，缺少异常断言和内容正确性断言。[证据] `tests/Bing.Utils.Tests/Drawing/CaptchaBuilderTest.cs:16` [证据] `tests/Bing.Utils.Tests/Drawing/CaptchaBuilderTest.cs:37`

4. `Bing.Utils.Collections` 覆盖集中于 `Arrays`，其他工具类覆盖不足
- 源码公开了 `Dicts` / `ReadOnlyDicts` / `Colls` 等工具类。[证据] `src/Bing.Utils.Collections/Bing/Collections/Dicts.cs:6` [证据] `src/Bing.Utils.Collections/Bing/Collections/ReadOnlyDicts.cs:28` [证据] `src/Bing.Utils.Collections/Bing/Collections/Colls.cs:64`
- 独立测试工程当前核心为 `ArraysTest`（待确认是否已有等效迁移测试）。[证据] `tests/Bing.Utils.Collections.Tests/Bing/Collections/ArraysTest.cs:10`

5. `Bing.Utils.Text` 缺少异常边界（固定长度/字典键冲突）
- `Splitter.FixedLength` 对 `length<=0` 会抛 `ArgumentOutOfRangeException`，现有 `SplitterUT` 主要覆盖正向流程。[证据] `src/Bing.Utils.Text/Bing/Text/Splitters/Splitter.cs:288` [证据] `src/Bing.Utils.Text/Bing/Text/Splitters/Splitter.cs:291` [证据] `tests/BingUtilsUT/SplitterUT/FixedLengthSplitterTest.cs:14`
- `SplitToDictionary` 直接 `ToDictionary`，重复键行为未见专门用例（待确认）。[证据] `src/Bing.Utils.Text/Bing/Text/Splitters/Splitter.Map.cs:59` [证据] `tests/BingUtilsUT/SplitterUT/MapSplitterTest.cs:47`

6. `Bing.Utils.IdUtils` 的时钟回拨/Provider 细节分支覆盖不足（待确认）
- `TwitterSnowflakeProvider` 存在时间回拨异常分支与序列溢出等待逻辑。[证据] `src/Bing.Utils.IdUtils/Bing/IdUtils/SnowflakeImplements/Providers/TwitterSnowflakeProvider.cs:159` [证据] `src/Bing.Utils.IdUtils/Bing/IdUtils/SnowflakeImplements/Providers/TwitterSnowflakeProvider.cs:160` [证据] `src/Bing.Utils.IdUtils/Bing/IdUtils/SnowflakeImplements/Providers/TwitterSnowflakeProvider.cs:168`
- 当前 `IdSnowflakeTest` 已覆盖并发与性能，但未见针对“回拨异常”的明确用例命名（待确认）。[证据] `tests/Bing.Utils.IdUtils.Tests/Bing/Helpers/IdSnowflakeTest.cs:384` [证据] `tests/Bing.Utils.IdUtils.Tests/Bing/Helpers/IdSnowflakeTest.cs:492`

## 5. 补测计划（P0/P1/P2）

### P0（本迭代必须补）

1. `Drawing.ImageSharp` 新增 `ImageSharpHelperTest`
- 建议位置：`tests/Bing.Utils.Tests/Drawing/ImageSharpHelperTest.cs`
- 必测用例：
- `SetOpacity_NullImage_ThrowsArgumentNullException`
- `SetOpacity_OutOfRange_ThrowsArgumentOutOfRangeException`
- `SetOpacity_ValidInput_ReturnsClonedImageAndAppliesAlpha`
- 目标：补齐当前“无直接测试”的公开 API 风险面。

2. `Drawing.SkiaSharp` 新增 `SkiaSharpHelperTest` + `SKEncodedImageFormatExtensionsTest`
- 建议位置：`tests/Bing.Utils.Tests/Drawing/SkiaSharpHelperTest.cs`
- 必测用例：
- `SetOpacity_NullImage_ThrowsArgumentNullException`
- `SetOpacity_InvalidOpacity_ThrowsArgumentOutOfRangeException`
- `FromStream_NullStream_ThrowsArgumentNullException`
- `GetMimeType_AllSupportedFormats_ReturnExpected`
- 目标：覆盖空参、范围、格式映射三类高频故障。

3. `Drawing` 强化 `CaptchaBuilderTest` 的断言
- 建议在现有文件补充：
- `GetCode_LengthLessOrEqualZero_ThrowsArgumentOutOfRangeException`
- `CreateImage_EmptyCode_ThrowsArgumentNullException`
- `CreateImage_ValidCode_ReturnsBitmapWithExpectedSize`
- 目标：把“可运行”升级为“可保证”。

### P1（下个迭代补齐）

1. `Collections` 增加 `Dicts/ReadOnlyDicts/Colls` 测试组
- 建议位置：`tests/Bing.Utils.Collections.Tests/Bing/Collections/DictsTest.cs`、`ReadOnlyDictsTest.cs`、`CollsTest.cs`
- 用例方向：
- 空字典/空集合输入
- 键不存在/重复键行为
- 只读集合转换与边界长度

2. `Text` 增加异常与冲突行为测试
- 建议位置：`tests/BingUtilsUT/SplitterUT/SplitterBoundaryTest.cs`
- 用例方向：
- `Splitter.FixedLength(0/-1)` 抛异常
- `SplitToDictionary` 重复键冲突行为（异常或覆盖策略）
- `WithKeyValueSeparator` 异常输入（空分隔符）行为

3. `IdUtils` 增加 Snowflake Provider 细节场景
- 建议位置：`tests/Bing.Utils.IdUtils.Tests/Bing/IdUtils/SnowflakeProviderBoundaryTest.cs`
- 用例方向：
- 时间回拨触发 `ApplicationException`
- 序列溢出跨毫秒行为
- `workerId/datacenterId` 越界参数

### P2（质量提升项）

1. `Http Integration` 增加失败链路
- 非 2xx、超时、取消令牌、异常中间件返回体一致性。
- 可在 `HttpClientServiceTest.*` 增补 `*_Failure_*` / `*_Timeout_*` 用例。

2. `Reflection` 增加契约化回归
- 对 `TypeVisit` / `TypeReflections` 建立“行为快照用例”（null、泛型嵌套、继承链极端场景）。

3. 横向引入“边界清单驱动”策略
- 统一模板：`null/empty/invalid/overflow/concurrent/perf` 六类检查，避免新 API 仅有 happy-path 单测。
