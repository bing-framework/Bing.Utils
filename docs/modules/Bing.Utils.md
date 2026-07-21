# Bing.Utils
## 1. 包职责（Scope）
- 解决的问题
- 提供基础设施通用能力：IO、异常、基础类型扩展、Json 配置等，是多个子包共同依赖的核心包。[证据] `src/Bing.Utils/Bing.Utils.csproj:3` [证据] `src/Bing.Utils/Bing.Utils.csproj:16` [证据] `src/Bing.Utils/Bing.Utils.csproj:18`
- `FileHelper` 提供文件读取/转换等基础 IO 能力。[证据] `src/Bing.Utils/Bing/IO/FileHelper.cs:11` [证据] `src/Bing.Utils/Bing/IO/FileHelper.Read.cs:95`
- `BooleanExtensions` 提供布尔条件守卫与链式逻辑组合。[证据] `src/Bing.Utils/Bing/Extensions/Bases/BooleanExtensions.cs:7` [证据] `src/Bing.Utils/Bing/Extensions/Bases/BooleanExtensions.cs:49`
- 不解决的问题（Out of Scope）
- 不面向单一垂直领域（如专用 HTTP 客户端、图像引擎、ID 算法实现细节），这些由子包承载。[证据] `Bing.Utils.sln:22` [证据] `Bing.Utils.sln:24` [证据] `Bing.Utils.sln:26` [证据] `Bing.Utils.sln:87`

## 2. 核心类型与扩展方法
- 类型/方法签名摘要
- `public static partial class FileHelper`，典型方法：`ReadToString(string filePath)`、`ReadToStringAsync(string filePath)`。[证据] `src/Bing.Utils/Bing/IO/FileHelper.Read.cs:6` [证据] `src/Bing.Utils/Bing/IO/FileHelper.Read.cs:95` [证据] `src/Bing.Utils/Bing/IO/FileHelper.Read.cs:118`
- `public static class BooleanExtensions`，典型方法：`MustTrue(this bool)`、`MustFalse(this bool)`、`And`、`Or`。[证据] `src/Bing.Utils/Bing/Extensions/Bases/BooleanExtensions.cs:49` [证据] `src/Bing.Utils/Bing/Extensions/Bases/BooleanExtensions.cs:63` [证据] `src/Bing.Utils/Bing/Extensions/Bases/BooleanExtensions.cs:78`
- `public static class ExceptionHelper`，提供 `PrepareForRethrow`/`Unwrap`。[证据] `src/Bing.Utils/Bing/Exceptions/ExceptionHelper.cs:8` [证据] `src/Bing.Utils/Bing/Exceptions/ExceptionHelper.cs:14` [证据] `src/Bing.Utils/Bing/Exceptions/ExceptionHelper.cs:48`
- 输入输出约定
- 文件读取 API 对“文件不存在”返回空值/空字符串而非抛异常（不同方法返回值略有差异）。[证据] `src/Bing.Utils/Bing/IO/FileHelper.Read.cs:17` [证据] `src/Bing.Utils/Bing/IO/FileHelper.Read.cs:105`
- 布尔守卫 API 对不满足条件直接抛出 `ArgumentException`。[证据] `src/Bing.Utils/Bing/Extensions/Bases/BooleanExtensions.cs:52` [证据] `src/Bing.Utils/Bing/Extensions/Bases/BooleanExtensions.cs:66`
- 边界行为（null、空集合、非法参数）
- `ReadToString`：文件不存在返回 `string.Empty`。[证据] `src/Bing.Utils/Bing/IO/FileHelper.Read.cs:104` [证据] `src/Bing.Utils/Bing/IO/FileHelper.Read.cs:105`
- `ReadToBytes(Stream)`：`stream == null` 或不可读时返回 `null`。[证据] `src/Bing.Utils/Bing/IO/FileHelper.Read.cs:31` [证据] `src/Bing.Utils/Bing/IO/FileHelper.Read.cs:34`
- `MustTrue/MustFalse`：非法布尔值抛 `ArgumentException`。[证据] `src/Bing.Utils/Bing/Extensions/Bases/BooleanExtensions.cs:52` [证据] `src/Bing.Utils/Bing/Extensions/Bases/BooleanExtensions.cs:66`

## 3. 使用示例（最小可运行）
- 示例1：基础用法
```csharp
var filePath = Common.GetPhysicalPath("/Samples/FileSample.txt");
var text = FileHelper.ReadToString(filePath);
Assert.Equal("test", text);
```
[证据] `tests/Bing.Utils.Tests/Bing/IO/FileHelperTest.cs:16` [证据] `tests/Bing.Utils.Tests/Bing/IO/FileHelperTest.cs:17`
- 示例2：进阶用法
```csharp
bool ok = true;
ok.MustTrue();
var result = ok.And(true).Or(false);
Assert.True(result);
```
[证据] `tests/Bing.Utils.Tests/Bing/Extensions/Base/BooleanExtensionsTest.cs:15` [证据] `tests/Bing.Utils.Tests/Bing/Extensions/Base/BooleanExtensionsTest.cs:86` [证据] `tests/Bing.Utils.Tests/Bing/Extensions/Base/BooleanExtensionsTest.cs:176`
- 示例3：常见错误与修正
```csharp
bool ok = false;
Assert.Throws<ArgumentException>(() => ok.MustTrue());
// 修正：先确保条件成立，或改用 MustFalse
```
[证据] `tests/Bing.Utils.Tests/Bing/Extensions/Base/BooleanExtensionsTest.cs:29` [证据] `tests/Bing.Utils.Tests/Bing/Extensions/Base/BooleanExtensionsTest.cs:35`

## 4. 性能与线程安全说明
- 是否分配敏感
- `ReadToBytes`/`ReadToString` 会创建缓冲区或读取整文件，属于分配敏感路径。[证据] `src/Bing.Utils/Bing/IO/FileHelper.Read.cs:20` [证据] `src/Bing.Utils/Bing/IO/FileHelper.Read.cs:106`
- 是否线程安全
- `BooleanExtensions` 为纯函数，无共享可变状态。[证据] `src/Bing.Utils/Bing/Extensions/Bases/BooleanExtensions.cs:78` [证据] `src/Bing.Utils/Bing/Extensions/Bases/BooleanExtensions.cs:89`
- `FileHelper` 本身不维护全局状态，但文件系统对象并发访问仍受外部环境约束（路径冲突、锁竞争由调用方处理）。[证据] `src/Bing.Utils/Bing/IO/FileHelper.Read.cs:19` [证据] `src/Bing.Utils/Bing/IO/FileHelper.Read.cs:145`
- 是否可并发调用
- 纯计算类可并发调用；涉及同一路径 IO 时建议调用方串行化关键写操作（待确认：仓库未给出统一并发约束文档）。[证据] `src/Bing.Utils/Bing/IO/FileHelper.Read.cs:145`

## 5. 异常与日志策略
- 抛出哪些异常
- `MustTrue/MustFalse` 抛 `ArgumentException`。[证据] `src/Bing.Utils/Bing/Extensions/Bases/BooleanExtensions.cs:52` [证据] `src/Bing.Utils/Bing/Extensions/Bases/BooleanExtensions.cs:66`
- `ExceptionHelper.Unwrap` 在传入 `null` 时抛 `ArgumentNullException`。[证据] `src/Bing.Utils/Bing/Exceptions/ExceptionHelper.cs:50`
- 什么时候返回默认值而不是抛异常
- 文件不存在时，`ReadToString` 返回空字符串；`ReadToBytes` 返回 `null`。[证据] `src/Bing.Utils/Bing/IO/FileHelper.Read.cs:17` [证据] `src/Bing.Utils/Bing/IO/FileHelper.Read.cs:105`

## 6. 与其他子包的关系
- 依赖的包
- 外部依赖：`Newtonsoft.Json`、`AspectCore.Extensions.Reflection`、`Microsoft.Extensions.Logging.Abstractions` 等。[证据] `src/Bing.Utils/Bing.Utils.csproj:16` [证据] `src/Bing.Utils/Bing.Utils.csproj:18`
- 被哪些包复用
- `Bing.Utils.Collections`、`Bing.Utils.DateTime`、`Bing.Utils.Drawing`、`Bing.Utils.Http`、`Bing.Utils.IdUtils`、`Bing.Utils.Reflection`、`Bing.Utils.Text` 直接引用本包。[证据] `src/Bing.Utils.Collections/references.props:4` [证据] `src/Bing.Utils.DateTime/Bing.Utils.DateTime.csproj:11` [证据] `src/Bing.Utils.Drawing/Bing.Utils.Drawing.csproj:15` [证据] `src/Bing.Utils.Http/references.props:4` [证据] `src/Bing.Utils.IdUtils/Bing.Utils.IdUtils.csproj:11` [证据] `src/Bing.Utils.Reflection/dependency.props:3` [证据] `src/Bing.Utils.Text/Bing.Utils.Text.csproj:11`

## 7. 测试映射
- 对应测试项目/测试类/关键用例
- 项目：`tests/Bing.Utils.Tests`（聚合主测），`tests/BingUtilsUT`（补充回归）。[证据] `tests/Bing.Utils.Tests/Bing.Utils.Tests.csproj:19` [证据] `tests/BingUtilsUT/BingUtilsUT.csproj:18`
- 类：`FileHelperTest`（读文件）、`BooleanExtensionsTest`（守卫与逻辑）。[证据] `tests/Bing.Utils.Tests/Bing/IO/FileHelperTest.cs:8` [证据] `tests/Bing.Utils.Tests/Bing/Extensions/Base/BooleanExtensionsTest.cs:7`
- 未覆盖风险点
- `JsonOptions` 等配置对象缺少直连测试示例（TODO）。[证据] `src/Bing.Utils/Bing/JsonOptions.cs:6`

## 8. 版本与兼容性注意事项
- 主库统一多目标框架：`net8.0;net7.0;net6.0;netstandard2.0`。[证据] `common.props:3`
- 主库不启用 `EnableUnsafeBinaryFormatterSerialization`，也不添加 BinaryFormatter 兼容包；最终应用自行决定是否为受控历史迁移承担兼容性配置。
- `net8.0` 测试项目仅为执行历史兼容回归测试而显式启用该开关。[证据] `tests/Bing.Utils.Tests/Bing.Utils.Tests.csproj:25`

### 对象与字节转换选择

| 场景 | 推荐 API | 约束 |
| --- | --- | --- |
| 普通 .NET 对象图二进制转换 | `Serialize.ToDataContractBytes` / `Serialize.FromDataContractBytes` | 多态类型必须通过 `knownTypes` 显式注册；读取受固定对象图和 XML 配额限制。 |
| 跨平台缓存、消息传递、可调试存储 | `Json.ToBytes` / `Json.ToObject` | 采用 JSON 字节，不新增另一套 JSON API。 |
| 不包含托管引用的结构体内存布局转换 | `Serialize.StructToBytes` / `Serialize.BytesToStruct` | 仅适用于内存布局互转，不承诺跨平台或跨目标框架持久化兼容。 |
| 受控历史数据迁移 | `Serialize.ToLegacyBinary` / `Serialize.FromLegacyBinary` | 仅限历史迁移，已过时，禁止作为新数据默认格式。 |

### Legacy BinaryFormatter 迁移

`BinaryFormatter` 不安全。不得用于网络输入、用户上传文件、不可信消息队列，也不得作为未知格式的自动探测或回退方案；Binder 或类型白名单不能彻底修复其安全问题。

```csharp
var oldValue = Serialize.FromLegacyBinary<LegacyOrder>(legacyBytes);
var newBytes = Serialize.ToDataContractBytes(oldValue);
```

- 旧数据只读取一次，转换后立即写入新格式；不要再写入新的 BinaryFormatter 数据。
- 迁移完成后删除或明确标记历史数据，避免新代码继续依赖该格式。
- .NET 6、7、8 的最终应用如确需迁移，应自行评估并配置 `EnableUnsafeBinaryFormatterSerialization`；.NET 9 及以后不得依赖内置实现，是否引入兼容包由最终应用负责。

