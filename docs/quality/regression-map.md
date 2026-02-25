# 缺陷驱动回归测试映射

## 1. 机制约定
- 回归分组统一标记：`[Trait("Category", "Regression")]`。  
  [证据] `tests/Bing.Utils.Tests/Bing/IO/FileHelperWriteRegressionTest.cs:6`  
  [证据] `tests/Bing.Utils.Tests/Bing/Helpers/CheckRegressionTest.cs:6`
- 高风险方法统一标记：`[Trait("Risk", "High")]`。  
  [证据] `tests/Bing.Utils.Http.Tests.Integration/Http/HttpClientServiceTest.Regression.cs:16`  
  [证据] `tests/Bing.Utils.Http.Tests/Bing/Helpers/WebRegressionTest.cs:9`
- 缺陷类型统一标记：`[Trait("DefectPattern", "...")]`。  
  [证据] `tests/Bing.Utils.Tests/Bing/IO/PathHelperRegressionTest.cs:14`  
  [证据] `tests/Bing.Utils.IdUtils.Tests/Bing/Helpers/IdRegressionTest.cs:36`

## 2. 缺陷类型 -> 回归测试映射

| 缺陷类型 | 历史缺陷来源 | 受影响代码（当前） | 回归测试（先失败后通过） | 分组 |
|---|---|---|---|---|
| Http 发送前回调判定错误（仅设置 `OnSendAfter` 时空引用） | `a209143` `fix: harden http send-before...` | `src/Bing.Utils.Http/Bing/Http/Clients/HttpRequest.cs:956` | `tests/Bing.Utils.Http.Tests.Integration/Http/HttpClientServiceTest.Regression.cs:17` `Get_OnSendAfterWithoutSendBefore_ShouldReturnCustomResult` | `Category=Regression` `Risk=High` `DefectPattern=Http.HttpRequest.SendBeforeGuard` |
| Http 文件上传路径文件名读取错误（`FilePath` 未用于提取文件名） | `d171c0e` `fix: 修复 HttpRequest 上传文件读取问题` | `src/Bing.Utils.Http/Bing/Http/Clients/HttpRequest.cs:911` | `tests/Bing.Utils.Http.Tests.Integration/Http/HttpClientServiceTest.Regression.cs:40` `Post_FileContentWithPath_ShouldUsePathFileName` | `Category=Regression` `Risk=High` `DefectPattern=Http.HttpRequest.FileContent.PathFileName` |
| 文件异步覆盖写入未截断旧内容 | `a209143` `fix: ... file write async behavior` | `src/Bing.Utils/Bing/IO/FileHelper.Write.cs:269` | `tests/Bing.Utils.Tests/Bing/IO/FileHelperWriteRegressionTest.cs:15` `WriteAsync_OverwriteShorterBytes_ShouldTruncateOldTail` | `Category=Regression` `Risk=High` `DefectPattern=IO.FileWrite.OverwriteTruncate` |
| 目录创建后 `DirectoryInfo.Exists` 缓存未刷新 | `9335af4` `fix(IO): ...EnsureDirectoryExists...缓存问题` | `src/Bing.Utils/Bing/IO/PathHelper.cs:278` | `tests/Bing.Utils.Tests/Bing/IO/PathHelperRegressionTest.cs:15` `EnsureDirectoryExists_NewDirectory_ShouldReturnRefreshedDirectoryInfo` | `Category=Regression` `Risk=High` `DefectPattern=IO.PathHelper.DirectoryInfoCache` |
| 路径分隔符标准化行为不一致 | `9335af4` `fix(IO): ...路径处理优化...` | `src/Bing.Utils/Bing/IO/PathHelper.cs:120` `src/Bing.Utils/Bing/IO/PathHelper.cs:129` | `tests/Bing.Utils.Tests/Bing/IO/PathHelperSeparatorRegressionTest.cs:15` `NormalizePath_WithAltSeparator_ShouldNormalizeToSystemSeparator` | `Category=Regression` `Risk=High` `DefectPattern=IO.PathHelper.SeparatorNormalization` |
| 格式化字符串提取器无分隔符场景字母匹配错误 | `c4cd2fa` `fix: 修复字符串提取器...` | `src/Bing.Utils/Bing/Text/Formatting/FormattedStringValueExtractor.cs:31` `src/Bing.Utils/Bing/Text/Formatting/FormattedStringValueExtractor.cs:64` | `tests/Bing.Utils.Tests/Text/Formatting/FormattedStringValueExtractorRegressionTest.cs:17` `Extract_NoSeparatorAndLetterTokens_ShouldKeepDynamicValueComplete` | `Category=Regression` `Risk=High` `DefectPattern=Text.FormattedString.NoSeparatorLetterMatch` |
| `Conv.ToDictionary` 显示名映射与空输入契约 | `4497839` `fix: Conv.ToDictionary() 增加获取显示名称` | `src/Bing.Utils/Bing/Helpers/Conv.cs:1521` `src/Bing.Utils/Bing/Helpers/Conv.cs:1552` | `tests/Bing.Utils.Tests/Bing/Helpers/ConvToDictionaryRegressionTest.cs:17` `ToDictionary_UseDisplayNameTrue_ShouldUseAttributeNameAsKey`；`tests/Bing.Utils.Tests/Bing/Helpers/ConvToDictionaryRegressionTest.cs:41` `ToDictionary_InputNull_ShouldReturnEmptyDictionary` | `Category=Regression` `Risk=High` `DefectPattern=Helpers.Conv.ToDictionary.*` |
| `Conv.ToRMB` 空输入触发空引用异常 | `eefc3f2` `fix: 修复转换人民币大写金额空异常` | `src/Bing.Utils/Bing/Helpers/Conv.cs:1821` `src/Bing.Utils/Bing/Helpers/Conv.cs:1823` | `tests/Bing.Utils.Tests/Bing/Helpers/ConvToRmbRegressionTest.cs:15` `ToRMB_NullInput_ShouldReturnNull` | `Category=Regression` `Risk=High` `DefectPattern=Helpers.Conv.ToRMB.NullInput` |
| 时间工具多线程串值问题（线程上下文隔离） | `e6c5867` `fix: 优化 时间操作 多线程问题` | `src/Bing.Utils/Bing/Helpers/Time.cs:15` `src/Bing.Utils/Bing/Helpers/Time.cs:64` | `tests/Bing.Utils.Tests/Bing/Helpers/TimeRegressionTest.cs:23` `SetTime_ParallelContexts_ShouldRemainIsolated` | `Category=Regression` `Risk=High` `DefectPattern=DateTime.Time.AsyncLocalIsolation` |
| 时间范围起止无序输入导致区间语义错误 | `3a3b4a6` `fix: 优化 时间范围` | `src/Bing.Utils/Bing/Date/DateTimeRange.cs:82` `src/Bing.Utils/Bing/Date/DateTimeRange.cs:84` | `tests/Bing.Utils.Tests/Bing/Date/DateTimeRangeRegressionTest.cs:15` `Ctor_StartGreaterThanEnd_ShouldNormalizeRangeOrder` | `Category=Regression` `Risk=High` `DefectPattern=DateTime.DateTimeRange.NormalizeStartEnd` |
| 反射读取可空枚举描述为空 | `1d401e4` `fix: nullable enum get description empty` | `src/Bing.Utils/Bing/Reflection/Reflections/Reflections.Description.cs:51` | `tests/Bing.Utils.Tests/Bing/Reflection/ReflectionsDescriptionRegressionTest.cs:17` `GetDescription_NullableEnumMember_ShouldReturnMemberDescription` | `Category=Regression` `Risk=High` `DefectPattern=Reflection.Reflections.NullableEnumDescription` |
| Id 生成器未配置时异常类型不明确（契约硬化） | `a5bdb15` `增强 ID 生成器的线程安全性和测试覆盖`（待确认：提交信息非 `fix` 前缀） | `src/Bing.Utils.IdUtils/Bing/Helpers/Id.cs:100` `src/Bing.Utils.IdUtils/Bing/Helpers/Id.cs:101` | `tests/Bing.Utils.IdUtils.Tests/Bing/Helpers/IdRegressionTest.cs:37` `CreateLong_NoGeneratorConfigured_ShouldThrowInvalidOperationException` | `Category=Regression` `Risk=High` `DefectPattern=IdUtils.Id.UnconfiguredLongGenerator` |
| `Check.NotNull` 空值参数名契约丢失 | `5964544` `fix: 更新Check操作方法` | `src/Bing.Utils/Bing/Helpers/Check.cs:87` `src/Bing.Utils/Bing/Helpers/Check.cs:93` | `tests/Bing.Utils.Tests/Bing/Helpers/CheckRegressionTest.cs:15` `NotNull_NullValue_ShouldThrowArgumentNullExceptionWithParamName` | `Category=Regression` `Risk=High` `DefectPattern=Helpers.Check.NotNull.ParamNameContract` |
| `Web.GetParam` 表单读取异常时未回退到请求头 | `a77edf4` `fix: 调整Web请求，优化Cookie携带` | `src/Bing.Utils.Http/Bing/Helpers/Web.cs:410` `src/Bing.Utils.Http/Bing/Helpers/Web.cs:424` `src/Bing.Utils.Http/Bing/Helpers/Web.cs:433` | `tests/Bing.Utils.Http.Tests/Bing/Helpers/WebRegressionTest.cs:24` `GetParam_FormReadThrows_ShouldFallbackToHeader` | `Category=Regression` `Risk=High` `DefectPattern=Http.Web.GetParam.FormFallback` |
| `Env.SetDevelopment` 不应覆盖已存在环境值 | `c8d4214` `fix: 环境操作 调整为 Env 缩写，防止与 System 冲突`（待确认：主要为命名冲突修复） | `src/Bing.Utils/Bing/Helpers/Env.cs:680` `src/Bing.Utils/Bing/Helpers/Env.cs:682` `src/Bing.Utils/Bing/Helpers/Env.cs:688` | `tests/Bing.Utils.Tests/Bing/Helpers/EnvRegressionTest.cs:16` `SetDevelopment_EnvironmentAlreadySet_ShouldKeepOriginalValue` | `Category=Regression` `Risk=High` `DefectPattern=Helpers.Env.SetDevelopment.NoOverride` |

## 3. 执行方式（CI/本地）
- 仅执行回归分组：
  - `dotnet test tests/Bing.Utils.Tests/Bing.Utils.Tests.csproj --filter "Category=Regression"`
  - `dotnet test tests/Bing.Utils.Http.Tests/Bing.Utils.Http.Tests.csproj --filter "Category=Regression"`
  - `dotnet test tests/Bing.Utils.Http.Tests.Integration/Bing.Utils.Http.Tests.Integration.csproj --filter "Category=Regression"`
  - `dotnet test tests/Bing.Utils.IdUtils.Tests/Bing.Utils.IdUtils.Tests.csproj --filter "Category=Regression"`
- 通过标准：
  - 所有 `Category=Regression` 用例通过；
  - 不依赖外网（当前回归用例均为本地/内存/TestServer 场景）。

## 4. 待确认
- `a5bdb15` 为“风险修复型增强”而非 `fix` 前缀提交，已纳入回归映射；后续可按团队口径确认是否单独归档到“稳定性增强”分组。
- `c8d4214` 主要描述为命名冲突规避，`SetDevelopment` 回归用例属于高风险契约补强，建议后续在发布说明中明确其与历史缺陷的对应关系。
- 仍有更早 `fix/bug/hotfix` 历史尚未逐条映射，建议按模块分批补齐，避免一次性扩张导致维护噪声。
