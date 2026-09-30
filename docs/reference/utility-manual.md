# Bing.Utils 常用工具类使用手册

> 面向刚接手本项目的开发者。手册只收录**日常开发高频使用**的工具类，并按功能分类组织。
> 所有类名、方法签名、异常类型均取自当前仓库源码（`version.props` 版本 `1.5.0`），示例均已通过编译与运行验证。

## 约定与前提

| 项 | 值 | 依据 |
|---|---|---|
| 目标框架 | `net8.0`、`net7.0`、`net6.0`、`netstandard2.0` | `common.props:3` |
| 命名空间规则 | 与包同名去掉 `Bing.Utils.` 前缀后仍在 `Bing.*` 下，例如包 `Bing.Utils.Text` → `Bing.Text.*` | 各 `src/<包>/Bing/**` 目录 |
| 示例形态 | .NET 8 控制台顶级语句，复制到 `Program.cs` 即可运行 | — |
| 证据标注 | 文中「源码：`path:line`」指该结论在源码中的位置，便于自行核对 | — |

**包与命名空间对照**

| NuGet 包 | 主要命名空间 | 本文对应章节 |
|---|---|---|
| `Bing.Utils` | `Bing.Helpers`、`Bing.Text`、`Bing.Extensions`、`Bing.IO`、`Bing.Date`、`Bing.Http` | 2、3、5、6、7 |
| `Bing.Utils.Collections` | `Bing.Collections` | 4 |
| `Bing.Utils.DateTime` | `Bing.Date` | 5 |
| `Bing.Utils.Text` | `Bing.Text.Pinyin`、`Bing.Text.Chinese`、`Bing.Text.Joiners`、`Bing.Text.Splitters`、`Bing.Text.Truncation` | 3 |
| `Bing.Utils.Http` | `Bing.Http.Clients`、`Bing.Net`、`Bing.Helpers` | 9 |
| `Bing.Utils.Security` | `Bing.Security.*` | 8 |
| `Bing.Utils.Security.Gm` | `Bing.Security.Gm` | 8 |
| `Bing.Utils.IdUtils` | `Bing.Helpers`、`Bing.IdUtils` | 10 |

## 目录

- [1. 能力速查表](#1-能力速查表)
- [2. 类型转换与参数校验](#2-类型转换与参数校验)
- [3. 字符串与文本处理](#3-字符串与文本处理)
- [4. 集合处理](#4-集合处理)
- [5. 日期时间](#5-日期时间)
- [6. 文件与 IO](#6-文件与-io)
- [7. 序列化](#7-序列化)
- [8. 加解密、摘要与签名](#8-加解密摘要与签名)
- [9. HTTP 请求与网络](#9-http-请求与网络)
- [10. Id 与唯一标识生成](#10-id-与唯一标识生成)
- [11. 重复能力对照与推荐选择](#11-重复能力对照与推荐选择)
- [12. 常见坑速查](#12-常见坑速查)

---

## 1. 能力速查表

| 我要做什么 | 推荐入口 | 包 |
|---|---|---|
| 把字符串/对象安全转成 int、decimal、bool、Guid | `Conv` | `Bing.Utils` |
| 方法入口做参数断言 | `Check` | `Bing.Utils` |
| 判断手机号/邮箱/身份证/URL 是否合法 | `Valid` | `Bing.Utils` |
| 读取枚举的名称、值、描述 | `Enums` | `Bing.Utils` |
| 字符串拼接、截断、首字母大小写、隐藏手机号 | `Bing.Helpers.Str` | `Bing.Utils` |
| 从字符串里提取数字/中文、按标记裁剪 | `Bing.Extensions.StringExtensions` | `Bing.Utils` |
| 集合按分隔符拼接、按分隔符切分 | `Joiner` / `Splitter` | `Bing.Utils.Text` |
| 汉字转拼音（含多音字）、简繁互转 | `PinyinUtil` / `ChineseConverter` | `Bing.Utils.Text` |
| 手机号、车牌、邮箱脱敏 | `Format` | `Bing.Utils` |
| 字典取值/累加/批量合并 | `DictsExtensions` | `Bing.Utils.Collections` |
| 去重、合并、判空 | `CollsExtensions` / `CollJudge` | `Bing.Utils.Collections` |
| 工作日推移、月初月末、月差 | `DateTimeExtensions` / `DateTimeFactory` | `Bing.Utils.DateTime` |
| Unix 秒/毫秒时间戳互转 | `DateTimeHelper` | `Bing.Utils` |
| 读写文件、文件哈希 | `FileHelper` | `Bing.Utils` |
| 目录创建与清理、路径拼接与防穿越 | `DirectoryHelper` / `PathHelper` | `Bing.Utils` |
| 字节数格式化（KB/MB/GB） | `FileSize` / `FileSizeHelper` | `Bing.Utils` |
| GZip 压缩、ZIP 打包 | `Compression` | `Bing.Utils` |
| JSON 序列化 | `Json` | `Bing.Utils` |
| XML 生成、对象 XML 序列化 | `Xml` / `Serialize` | `Bing.Utils` |
| 摘要、HMAC | `Hashing` / `Hmac` | `Bing.Utils.Security` |
| 对称加密（短数据 / 大文件） | `AesGcmEncryption` / `AesGcmStreamEncryption` | `Bing.Utils.Security` |
| 口令散列存储 | `Pbkdf2PasswordHasher` | `Bing.Utils.Security` |
| RSA / ECDSA 签名、请求签名 | `RsaSignature` / `EcdsaSignature` / `VersionedRequestSigner` | `Bing.Utils.Security` |
| 国密 SM2 / SM3 / SM4 | `Sm2*` / `Sm3` / `Sm4GcmEncryption` | `Bing.Utils.Security.Gm` |
| 发 HTTP 请求（链式） | `HttpClientService` + `HttpRequest<TResult>` | `Bing.Utils.Http` |
| 读当前请求的 IP、UA、Cookie | `IpAddressProvider` / `Web` | `Bing.Utils.Http` |
| 生成业务 Id、雪花 Id、ObjectId | `Id` / `SnowflakeGenerator` / `ObjectId` | `Bing.Utils.IdUtils` |

---

## 2. 类型转换与参数校验

### 2.1 `Conv`（类型转换）

**用途与适用场景**：把配置字符串、`object`、数据库读取值安全转成强类型。转换失败**不抛异常**，返回默认值或 `null`，适合解析外部输入。源码：`src/Bing.Utils/Bing/Helpers/Conv.cs`。

**主要方法**

| 方法 | 返回 | 说明 |
|---|---|---|
| `ToInt(object input)` / `ToInt(object input, int defaultValue)` | `int` | 失败返回 `0` 或指定默认值；小数按 `AwayFromZero` 四舍五入 |
| `ToIntOrNull(object input)` | `int?` | 失败或超出 `int` 范围返回 `null` |
| `ToDecimal(object input, int? digits = null)` | `decimal` | `digits` 指定小数位；失败返回 `0` |
| `ToDecimal(object input, decimal defaultValue, int? digits = null, MidpointRounding mode = MidpointRounding.AwayFromZero)` | `decimal` | 带默认值与舍入模式 |
| `ToBool(object input)` / `ToBool(object input, bool defaultValue)` | `bool` | 见下方字符串规则 |
| `ToDate(object input, DateTime defaultValue = default)` / `ToDateOrNull(object input, DateTime? defaultValue = null)` | `DateTime` / `DateTime?` | 用当前区域性 `DateTime.TryParse` |
| `ToGuid(object input)` / `ToGuidOrNull(object input)` | `Guid` / `Guid?` | 失败返回 `Guid.Empty` / `null` |
| `ToBytes(string input)` | `byte[]` | 默认 UTF-8 |
| `To<T>(object input)` / `TryTo<T>(object input, out T result)` | `T` / `bool` | 泛型转换 |

**示例**

```csharp
using Bing.Helpers;

var port = Conv.ToInt("8080", 80);        // 8080
var badPort = Conv.ToInt("abc", 80);      // 80（转换失败回落默认值）
var price = Conv.ToDecimal("19.90");      // 19.90
var nothing = Conv.ToIntOrNull("");       // null
var date = Conv.ToDateOrNull("2026-01-01");

Console.WriteLine($"{port} {badPort} {price} {nothing} {date:yyyy-MM-dd}");
// 输出：8080 80 19.90  2026-01-01

Console.WriteLine($"{Conv.ToBool("YES")} {Conv.ToBool("off")} {Conv.ToBool("0")} {Conv.ToBool(2)}");
// 输出：True False False True
```

**常见误用与注意事项**

- `ToBool` 的字符串规则是固定的：`"1"`、`"是"` 与不区分大小写的 `"ok"/"yes"/"y"/"on"/"enable"/"enabled"/"t"/"true"` 为 `true`；`"0"`、`"否"`、`"不"` 与不区分大小写的 `"no"/"fail"/"n"/"off"/"disable"/"disabled"/"f"/"false"` 为 `false`；数值类型**非零即 true**。源码：`Conv.cs:1013`。
- 无法识别的字符串（如 `"abc"`）走 `ToBool(input)` 时静默返回 `false`，不会报错；需要区分"未识别"时请用 `ToBoolOrNull`。
- `ToDecimal` / `ToDateOrNull` 使用**当前区域性**解析，`19,90` 这类逗号小数在中文环境下会被误判，跨区域部署请显式指定 `CultureInfo`。
- 全部转换方法都不抛异常（除自定义 `converter` 为 `null` 时抛 `ArgumentNullException`），不要把 `Conv` 当校验器用；需要校验请接 `Valid`。

### 2.2 `Check`（方法入口断言）

**用途与适用场景**：在方法入口对入参做守卫，失败立即抛参数异常。源码：`src/Bing.Utils/Bing/Helpers/Check.cs`。

**主要方法**

| 方法 | 返回 | 失败时抛出 |
|---|---|---|
| `NotNull<T>(T value, string parameterName)` | `T`（返回原值，可链式赋值） | `ArgumentNullException` |
| `NotNull(string value, string parameterName, int maxLength = int.MaxValue, int minLength = 0)` | `string` | `ArgumentException` |
| `NotNullOrWhiteSpace(string value, string parameterName, int maxLength = int.MaxValue, int minLength = 0)` | `string` | `ArgumentException` |
| `NotNullOrEmpty(string value, string parameterName, ...)` | `string` | `ArgumentException` |
| `Range(int value, string parameterName, int minimumValue, int maximumValue = int.MaxValue)` | `int` | `ArgumentException` |
| `Required<T>(T value, Func<T, bool> assertionFunc, string message)` / `Required<T, TException>(...)` | `void` | `Exception` / `TException` |
| `FileExists(string fileName, string parameterName = null)` / `DirectoryExists(...)` | `void` | `ArgumentNullException`、`FileNotFoundException`、`DirectoryNotFoundException` |

**示例**

```csharp
using Bing.Helpers;

public static Order Get(string orderId, int pageSize)
{
    Check.NotNullOrWhiteSpace(orderId, nameof(orderId));
    Check.Range(pageSize, nameof(pageSize), 1, 100);
    // ...
}

// 实测：Check.NotNullOrWhiteSpace("  ", "port")   -> ArgumentException(paramName: "port")
// 实测：Check.Range(500, "pageSize", 1, 100)      -> ArgumentException("参数 pageSize 的值 500 超出范围 [1, 100]")
// 实测：var name = Check.NotNull("bing", "name"); // 返回 "bing"
```

**常见误用与注意事项**

- 泛型版 `Check.NotNull<T>` 抛 **`ArgumentNullException`**，字符串版 `Check.NotNull(string, ...)` 抛 **`ArgumentException`** —— 两者异常类型不同，测试断言别写错。源码：`Check.cs:90`、`Check.cs:126`。
- `Required<T>` 默认抛出的是 `Exception` 基类，不是 `ArgumentException`；需要具体异常请用 `Required<T, TException>`，且该异常类型必须提供 `(string message)` 构造函数（内部用反射创建）。源码：`Check.cs:54`、`Check.cs:35`。

### 2.3 `Valid`（格式校验）

**用途与适用场景**：校验外部输入格式，**全部返回 bool，不抛异常**，适合在 DTO 校验、导入数据清洗中使用。源码：`src/Bing.Utils/Bing/Helpers/Valid.cs`。

**主要方法**：`IsMobileNumber(string)`、`IsEmail(string, bool isRestrict = false)`、`IsIdCard(string)`、`IsGuid(string)`、`IsUrl(string)` / `IsUri(string)`、`IsChinese(string)`、`IsDate(string, bool isRegex = false)`、`IsLengthStr(string, int minLength, int maxLength)`、`IsInteger` / `IsDecimal` / `IsNumber`。

**示例**

```csharp
using Bing.Helpers;

Console.WriteLine($"{Valid.IsMobileNumber("13800138000")} {Valid.IsEmail("dev@bing.test")} {Valid.IsGuid(Guid.NewGuid().ToString())}");
// 输出：True True True
```

**常见误用与注意事项**

- 所有方法对 `null` / 空串统一返回 `false`（`IsEmpty` 语义：`null`、`string.IsNullOrWhiteSpace`、空集合均视为空），源码：`Valid.cs:42`。
- `IsPhoneNumber` 已标记 `[Obsolete("建议使用 IsMobileNumber 方法，此方法的正则表达式已过时")]`，源码：`Valid.cs:109`，新代码请用 `IsMobileNumber`。

### 2.4 `Enums`（枚举操作）

**用途与适用场景**：按值/名称/描述读取枚举信息，或把外部值解析为枚举。源码：`src/Bing.Utils/Bing/Helpers/Enums.cs`。

**主要方法**

| 方法 | 返回 |
|---|---|
| `Parse<TEnum>(object member)` | `TEnum` |
| `GetName<TEnum>(object member)` / `GetName(Type type, object member)` | `string` |
| `GetValue<TEnum>(object member)` | `int` |
| `GetDescription<TEnum>(object member)` | `string`（读 `DescriptionAttribute`） |
| `GetItems<TEnum>()` | `List<Item>`（`Value` / `Text` / `Description`） |
| `GetDictionary<TEnum>()` | `IDictionary<int, string>` |
| `HasValue<TEnum>(TEnum value)` / `HasValue<TEnum>(int value)` | `bool` |

**示例**

```csharp
using Bing.Helpers;

Console.WriteLine(Enums.GetName<OrderStatus>(OrderStatus.Paid));            // Paid
Console.WriteLine(Enums.GetDescription<OrderStatus>(OrderStatus.Paid));     // 已付款
Console.WriteLine(Enums.Parse<OrderStatus>("3"));                           // Paid
Console.WriteLine(string.Join(',', Enums.GetItems<OrderStatus>().Select(i => i.Text))); // 待付款,已付款

public enum OrderStatus
{
    [System.ComponentModel.Description("待付款")] Pending = 1,
    [System.ComponentModel.Description("已付款")] Paid = 3,
}
```

**常见误用与注意事项**

- 同名类 `Bing.Helpers.Enum` 已整类标记 `[Obsolete("请使用 Enums 静态类，下个版本将会弃用!")]`（源码：`Enum.cs:11`），两个类是**代码复制**关系，不要再用旧的。
- 传入空值且枚举非可空类型时抛 `ArgumentNullException(paramName: "member")`；传入非枚举类型抛 `InvalidOperationException("类型 X 不是枚举")`。源码：`Enums.cs:31`、`Enums.cs:348`。

---

## 3. 字符串与文本处理

### 3.1 `Bing.Helpers.Str`（字符串静态工具 + 生成器）

**用途与适用场景**：集合拼接、首字母大小写、截断、手机号隐藏、随机串。源码：`src/Bing.Utils/Bing/Helpers/Str.cs`（静态部分）与 `Str.Builder.cs`（生成器实例部分）。

**主要方法**

| 方法 | 返回 | 说明 |
|---|---|---|
| `Join<T>(IEnumerable<T> list, string quotes = "", string separator = ",")` | `string` | 集合转字符串，可给元素加引号 |
| `FirstLower(string value)` / `FirstUpper(string value)` | `string` | 首字母大小写 |
| `Truncate(string text, int length, int endChatCount = 0, string endChar = ".")` | `string` | 见下方截断对比 |
| `GetHideMobile(string value)` | `string` | 手机号隐藏（前 3 + `******` + 后 3） |
| `GenerateNonceStr()` | `string` | `Guid.NewGuid().ToString("N")` |
| `PinYin(string chineseText)` / `FullPinYin(string text)` | `string` | 旧拼音入口，无分隔符参数 |
| `new Str().Append(...)` / `AppendLine(...)` / `RemoveEnd(...)` | `Str` | 链式字符串生成器 |

**示例**

```csharp
using Bing.Helpers;

Console.WriteLine(Bing.Helpers.Str.Join(new[] { 1, 2, 3 }, "'"));      // '1','2','3'
Console.WriteLine(Bing.Helpers.Str.FirstLower("UserName"));            // userName
Console.WriteLine(Bing.Helpers.Str.Truncate("abcdefghij", 5));         // abcde
Console.WriteLine(Bing.Helpers.Str.GetHideMobile("13812345678"));      // 138******678
Console.WriteLine(new Bing.Helpers.Str().AppendLine("a").Append("b"));
```

**常见误用与注意事项**

- ⚠ **命名冲突**：`Bing.Helpers.Str` 与 `Bing.Text.Str` 同名。若同时 `using Bing.Helpers;` 与 `using Bing.Text;`，写 `Str.Join(...)` 会编译报错 `CS0104`（实测）。请写全 `Bing.Helpers.Str` 或只引入需要的命名空间。
- 截断语义：`Truncate(text, length)` 在 `endChatCount = 0` 时**不加省略号**（实测 `"abcdefghij"` → `"abcde"`），需要 `...` 请用 3.6 节的扩展或截断器。
- 拼音请用 `PinyinUtil`（见 3.4），`Str.PinYin` / `Str.FullPinYin` 没有分隔符参数，且不支持多音字消歧。

### 3.2 `Bing.Extensions.StringExtensions`（字符串扩展方法）

**用途与适用场景**：从字符串中提取数字/字母/中文、按标记裁剪、批量替换。源码：`src/Bing.Utils/Bing/Extensions/Bases/StringExtensions*.cs`。

**主要方法**

| 方法 | 返回 |
|---|---|
| `ExtractNumbers(this string value)` / `ExtractLetters` / `ExtractChinese` | `string` |
| `FilterChars(this string value, Predicate<char> predicate)` | `string` |
| `TrimToMaxLength(this string value, int maxLength)` / `(…, int maxLength, string suffix)` | `string` |
| `GetBefore(this string value, string x)` / `GetBetween` / `GetAfter` | `string` |
| `ReplaceAll(this string value, IEnumerable<string> oldValues, string newValue)` | `string` |
| `ExtractAround(this string value, int index, int left, int right)` | `string` |

**示例**

```csharp
using Bing.Extensions;

Console.WriteLine("订单号 ORD-2026-0001".ExtractNumbers());   // 20260001（实测）
```

**常见误用与注意事项**

- `ExtractAround` 的索引越界抛 `IndexOutOfRangeException("参数索引值超出字符串的最大长度")`（源码：`StringExtensions.cs:29`），调用前先判长度。
- `ReplaceAll(oldValues, newValues)` 两个序列长度不一致时抛 `ArgumentOutOfRangeException`（源码：`StringExtensions.cs:509`）。
- 本类与 `Bing.Text.StringExtensions` 同名且都在 `Bing.Utils` 包内，只引入需要的那个命名空间。

### 3.3 `Joiner` / `Splitter`（拼接与切分）

**用途与适用场景**：可控地拼接或切分字符串（跳过 null、去空、去空白、限制段数）。源码：`src/Bing.Utils.Text/Bing/Text/Joiners/Joiner.cs`、`src/Bing.Utils.Text/Bing/Text/Splitters/Splitter.cs`。

**主要方法**

| 类 | 方法 |
|---|---|
| `Joiner` | `On(string|char)`、`Join(IEnumerable<string>)`、`Join(string, params string[])`、`Join<T>(IEnumerable<T>, Func<T,string>)`、`SkipNulls()`、`UseForNull(string)`、`WithKeyValueSeparator(char|string)` |
| `Splitter` | `On(char|string|Regex)`、`OnPattern(string)`、`FixedLength(int)`、`Split(string)`、`SplitToList(string)`、`SplitToArray(string)`、`OmitEmptyStrings()`、`TrimResults()`、`Limit(int)` |

**示例**

```csharp
using Bing.Text.Joiners;
using Bing.Text.Splitters;

Console.WriteLine(Joiner.On(",").SkipNulls().Join(new[] { "bing", null, "utils" }));  // bing,utils
Console.WriteLine(string.Join('|', Splitter.On(',').TrimResults().OmitEmptyStrings().SplitToList("a, ,b,,c")));
// 实测输出：a||b|c  —— 只含空格的段不会被 OmitEmptyStrings 去掉
```

**常见误用与注意事项**

- ⚠ 实测表明 `OmitEmptyStrings()` **无法过滤只含空格的段**（`"a, ,b,,c"` 仍得到 4 段，其中第 2 段为空串）。需要彻底去空格段请自行追加 `.Where(s => !string.IsNullOrWhiteSpace(s))`。
- `Splitter.FixedLength(length)` 的 `length < 0` 抛 `ArgumentOutOfRangeException`（源码：`Splitter.cs:291`）。

### 3.4 `PinyinUtil` / `ChineseConverter`（拼音与简繁）

**用途与适用场景**：搜索索引、首字母检索、繁简转换。拼音词典与简繁词表以内嵌资源随包分发（`character.gz` / `phrases.gz` / `s2t.gz` / `t2s.gz`）。源码：`src/Bing.Utils.Text/Bing/Text/Pinyin/PinyinUtil*.cs`、`.../Chinese/ChineseConverter.cs`。

**主要方法**

| 类 | 方法 | 返回 |
|---|---|---|
| `PinyinUtil` | `GetPinyin(string text, string separator = "")` | `string` |
| | `GetInitials(string text, string separator = "")` | `string` |
| | `GetContextualPinyin(string text, string separator = "")` | `string`（按词组消歧多音字） |
| | `GetPinyinWithTone(string text, string separator = "")` | `string`（带声调） |
| `ChineseConverter` | `ToTraditional(string text)` / `ToSimplified(string text)` | `string` |

**示例**

```csharp
using Bing.Text.Chinese;
using Bing.Text.Pinyin;

Console.WriteLine(PinyinUtil.GetPinyin("你好"));              // NiHao
Console.WriteLine(PinyinUtil.GetInitials("你好"));            // nh
Console.WriteLine(PinyinUtil.GetContextualPinyin("重庆"));    // ChongQing（多音字按词组消歧）
Console.WriteLine(ChineseConverter.ToTraditional("软件开发")); // 軟體開發
```

**常见误用与注意事项**

- ⚠ `GetPinyin("重庆")` 返回 **`ZhongQing`**（逐字常见读音，实测），多音字场景必须用 `GetContextualPinyin`，否则会出现错误的搜索索引。
- `separator` 为 `null` 时抛 `ArgumentNullException(paramName: "separator")`（源码：`PinyinUtil.cs:23`）；`text` 为 `null` 时返回 `null`（与 `Bing.Helpers.Str` 返回空串的行为不同）。
- 需要减小包体积时可用 `-p:BingTextExternalOnly=true` 构建 `Bing.Utils.Text.External` 变体，该变体**不包含** `ChineseConverter`，且 `GetContextualPinyin` / `GetPinyinWithTone` 走条件编译被排除。

### 3.5 `Format`（脱敏）

**用途与适用场景**：手机号、车牌、VIN、邮箱等敏感信息脱敏。源码：`src/Bing.Utils/Bing/Helpers/Format.cs`。

**主要方法**：`EncryptPhoneOfChina(string phone)`、`EncryptPlateNumberOfChina(string plateNumber)`、`EncryptVinCode(string vinCode)`、`EncryptString(string input, int prefixLength, int suffixLength, char maskChar = '*', int maskLength = 6)`、`FormatMoney(decimal money, bool isEncrypt = false)`。

**示例**

```csharp
using Bing.Helpers;

Console.WriteLine(Format.EncryptPhoneOfChina("13812345678"));                 // 138******78（实测）
Console.WriteLine(Format.EncryptString("bing-utils@example.com", 4, 4));      // bing******.com（实测）
```

**常见误用与注意事项**

- 输入长度不足时**返回 `string.Empty`** 而不报错（手机号 < 5、车牌 < 4、VIN < 6 均如此），日志里出现空串不要以为是 Bug，源码：`Format.cs:33`、`Format.cs:69`、`Format.cs:104`。
- `EncryptString` 的 `prefixLength` / `suffixLength` 为负数时抛 `ArgumentException("前缀和后缀长度不能为负数")`（源码：`Format.cs:215`）。
- 若需要按长度自动分档脱敏，可用 `Bing.Text.StringExtensions.Mask()`（实测 `"13812345678".Mask()` → `138****5678`），但两者掩码规则不同，同一个项目里不要混用。

### 3.6 截断能力三选一（重复能力）

| 入口 | 语义 | 实测/源码 |
|---|---|---|
| `Bing.Helpers.Str.Truncate(text, length, endChatCount = 0, endChar = ".")` | 取前 `length` 位，`endChatCount = 0` 时**不加省略号** | `Str.Truncate("abcdefghij", 5)` → `abcde` |
| `Bing.Text.StringTruncateExtensions.Truncate(this string, int maxLength, ...)` | 结果**总长恰为 maxLength**（`maxLength - 3` 正文 + `...`） | `"hello world".Truncate(8)` → `hello...` |
| `StringTruncators.ByLength` 等 5 种截断器 | 支持按长度/文本元素/字符数/单词数/行数截断，可指定从左侧截断与省略符 | `src/Bing.Utils.Text/Bing/Text/Truncation/StringTruncators.cs` |

推荐：日常 UI 截断用 `Bing.Text` 的 `Truncate` 扩展（总长可控）；纯截断不需要省略号用 `Str.Truncate`；涉及中文/emoji 或按单词、行数截断才用 `StringTruncators`。

---

## 4. 集合处理

### 4.1 `DictsExtensions` / `Dicts`（字典）

**用途与适用场景**：缓存读写、配置合并、按 key 累加。源码：`src/Bing.Utils.Collections/Bing/Collections/DictsExtensions.cs`、`Dicts.cs`。

**主要方法**

| 方法 | 返回 | 说明 |
|---|---|---|
| `GetValueOrDefault(this IDictionary<TKey,TValue>, TKey key, TValue defaultValue)` | `TValue` | 推荐用法 |
| `GetValueOrAdd(this Dictionary<TKey,TValue>, TKey key, TValue value)` / `(…, Func<TKey,TValue>)` | `TValue` | 不存在则写入再返回 |
| `AddValueOrUpdate(this Dictionary, TKey, Func<TKey,TValue> insertFunc, Func<TKey,TValue,TValue> updateFunc)` | `void` | 存在则按委托更新 |
| `AddRange(this Dictionary, Dictionary)` | `void` | 批量合并 |
| `SetValue(this Dictionary, TKey, TValue)` | `void` | 存在则覆盖 |

**示例**

```csharp
using Bing.Collections;

var cache = new Dictionary<string, int> { ["bing"] = 1 };

var hit = cache.GetValueOrDefault("bing", 0);        // 1
var miss = cache.GetValueOrDefault("missing", -1);   // -1
var created = cache.GetValueOrAdd("counter", 100);   // 100，并写入字典
cache.AddValueOrUpdate("bing", _ => 1, (_, old) => old + 1); // bing -> 2
```

**常见误用与注意事项**

- ⚠ `GetValueOrDefault(key)` 的单参重载与 .NET 自带 `CollectionExtensions.GetValueOrDefault` 易产生二义性，**统一使用带 `defaultValue` 的重载**。
- `AddValueOrUpdate` / `AddValueOrDo` 的委托参数为 `null` 时抛 `ArgumentNullException`（源码：`Dicts.cs:31`、`Dicts.cs:51`）。
- 这些扩展不提供并发保护，`Dictionary` 多线程读写仍需自行加锁或改用并发集合。

### 4.2 `CollsExtensions` / `Colls` / `CollJudge`（序列）

**用途与适用场景**：去重、合并、计数判断、判空。源码：`src/Bing.Utils.Collections/Bing/Collections/Colls*.cs`、`CollJudge.cs`。

**主要方法**

| 方法 | 返回 |
|---|---|
| `CollJudge.IsNullOrEmpty(IEnumerable)` / `IsNullOrEmpty<T>(IEnumerable<T>)` | `bool` |
| `ContainsAtLeast<T>(this ICollection<T>, int count)` | `bool` |
| `RemoveDuplicates(this IList<T>)` / `RemoveDuplicates<T,TCheck>(this IList<T>, Func<T,TCheck>)` | `void` |
| `RemoveIf(this IList<T>, Func<T,bool>)` | `void` |
| `Merge(this IEnumerable<T>, IEnumerable<T>)` | `IEnumerable<T>` |
| `IndexOf<T>(this IEnumerable<T>, T)` | `int` |

**示例**

```csharp
using Bing.Collections;

var names = new List<string> { "a", "b", "a", "c" };
names.RemoveDuplicates();                                   // 原地去重 -> a,b,c
Console.WriteLine(CollJudge.IsNullOrEmpty(Array.Empty<string>()));   // True
Console.WriteLine(new[] { 1, 2, 3 }.ContainsAtLeast(2));             // True
```

**常见误用与注意事项**

- `RemoveDuplicates` 是**原地修改**传入的 `IList<T>`，不是返回新集合。
- `Colls.ContainsAtLeast` 与 `CollJudge.ContainsAtLeast` 实现完全重复，优先用职责更单一的 `CollJudge`。
- `OrderByShuffle` 内部按时间播种 `Random`（源码：`Colls.cs:255`），高频调用不适合做安全随机，随机性要求高的场景请用 `SecurityRandom`。

### 4.3 `Arrays` / `ArraysExtensions`

**用途与适用场景**：空数组复用、安全转换、数组复制。源码：`src/Bing.Utils.Collections/Bing/Collections/Arrays.cs`。

**主要方法**：`Empty<T>()`（返回共享空数组，实测 `Length == 0`）、`ToArraySafety<TElement>(IEnumerable<TElement>)`、`AreEqual(Array, Array)`、`Enumerate<T>(T[])`、`ReverseEnumerate<T>(T[])`、`Copy` 系列扩展。

**示例**

```csharp
using Bing.Collections;

int[] empty = Arrays.Empty<int>();     // 复用共享空数组，避免每次 new
Console.WriteLine(empty.Length);       // 0
```

**常见误用与注意事项**

- `Arrays.GetLength(null)` 抛 `ArgumentNullException`，而 XML 注释写的是 `NullReferenceException`（源码：`Arrays.cs:206`），**以实际异常为准**。
- `ArraysShortcutExtensions` 的 `Sort` / `Reverse` / `Clear` / `SetByte` 会**原地修改数组**。

---

## 5. 日期时间

### 5.1 `DateTimeExtensions` / `DateTimeFactory`（推荐组合）

**用途与适用场景**：工作日计算、月/季度推移、起止时刻、年龄与月差。源码：`src/Bing.Utils.DateTime/Bing/Date/DateTimeExtensions.cs`、`DateTimeFactory.cs`。

**主要方法**

| 类 | 方法 | 返回 |
|---|---|---|
| `DateTimeExtensions` | `AddBusinessDays(this DateTime, int days)` | `DateTime`（跳过周六日） |
| | `IsWeekend()` / `IsWeekday()` / `IsToday()` / `IsLeapYear()` | `bool` |
| | `GetMonthDiff(this DateTime, DateTime)` / `GetTotalMonthDiff(...)` | `int` / `double` |
| | `BeginningOfDay()` / `EndOfDay()` / `FirstDayOfMonth()` / `LastDayOfMonth()` | `DateTime` |
| | `Round(this DateTime, RoundTo rt)` / `RoundUp(TimeSpan)` / `RoundDown(TimeSpan)` | `DateTime` |
| `DateTimeFactory` | `Now()` / `UtcNow()` / `Create(y, m, d, ...)` | `DateTime` |
| | `CreateLastDayOfMonth(int year, int month)` / `CreateFirstDayOfMonth(...)` | `DateTime` |
| | `CreateByWeek(int year, int month, DayOfWeek dayOfWeek, int occurrence)` | `DateTime` |

**示例**

```csharp
using Bing.Date;

var day = new DateTime(2026, 5, 1);
Console.WriteLine(day.IsWeekend());                 // False（周五）
Console.WriteLine(day.AddBusinessDays(1));          // 2026-05-04（跳过周末）
Console.WriteLine(day.BeginningOfDay());            // 2026-05-01 00:00:00
Console.WriteLine(day.EndOfDay());                  // 2026-05-01 23:59:59
Console.WriteLine(DateTimeFactory.CreateLastDayOfMonth(2026, 2)); // 2026-02-28
Console.WriteLine(new DateTime(2026, 1, 1).GetMonthDiff(new DateTime(2026, 7, 1))); // 6
```

**常见误用与注意事项**

- `Round` 传入非法 `RoundTo` 值抛 `ArgumentOutOfRangeException(nameof(rt))`；`RoundUp/RoundDown` 的间隔必须为正，否则抛 `ArgumentOutOfRangeException`（源码：`DateTimeExtensions.cs:1035`、`Rounding.cs:68`）。
- ⚠ `DateTimeOffsetExtensions.IsSameMonth` **只比较 Month 不比较 Year**（源码：`DateTimeOffsetExtensions.cs:111`），跨年比较请用 `DateTimeExtensions.IsSameMonth`。
- `Bing.Date.DateTimeExtensions` 在 `Bing.Utils` 与 `Bing.Utils.DateTime` **两个包里都有同名 partial 类**（分属不同程序集），方法集是并集，写在哪个包里要对应清楚。

### 5.2 `DateTimeHelper` / `TimeStamp` / `UnixTimeStamp`（时间戳）

**用途与适用场景**：Unix 秒/毫秒互转、时间戳对象化。源码：`src/Bing.Utils/Bing/Date/DateTimeHelper.cs`、`TimeStamp.cs`、`TimeStamp.UnixTimeStamp.cs`。

**主要方法**

| 类 | 方法 | 返回 |
|---|---|---|
| `DateTimeHelper` | `ConvertDateTimeToUnixTime(DateTime, TimestampDigit digit = TimestampDigit.Millisecond)` | `long` |
| | `ConvertUnixTimestampToDateTime(long, TimestampDigit digit = TimestampDigit.Millisecond)` | `DateTime` |
| | `Current()` / `CurrentSeconds()` | `long`（毫秒 / 秒） |
| `TimeStamp` | `new TimeStamp(DateTime)`、`ToTimestamp()`、`ToDateTime()` | 单位 = **.NET Ticks** |
| `UnixTimeStamp` | `new UnixTimeStamp(DateTime)`、`ToTimestamp()`、`ToIso8601String()` | 单位 = **秒** |

**示例**

```csharp
using Bing.Date;

var utc = new DateTime(2026, 9, 30, 12, 0, 0, DateTimeKind.Utc);

var ms = DateTimeHelper.ConvertDateTimeToUnixTime(utc, TimestampDigit.Millisecond); // 1790769600000
var sec = DateTimeHelper.ConvertDateTimeToUnixTime(utc, TimestampDigit.Second);     // 1790769600
Console.WriteLine(DateTimeHelper.ConvertUnixTimestampToDateTime(ms, TimestampDigit.Millisecond));
// 实测输出：2026-09-30 20:00:00  ← 注意：该方法内部硬编码转 GMT+8

var unix = new UnixTimeStamp(utc);
Console.WriteLine($"{unix.ToTimestamp()} {unix.ToIso8601String()}");  // 1790769600 2026-09-30T12:00:00Z
```

**常见误用与注意事项**

- ⚠ **往返不对称**（实测）：`ConvertDateTimeToUnixTime(12:00Z)` → `1790769600000`，再 `ConvertUnixTimestampToDateTime` 得到 `20:00`，因为该方法内部执行 `TimeZoneInfo.ConvertTimeFromUtc(..., GMT8)`（源码：`DateTimeHelper.cs:499`）。需要严格 UTC 往返请用 `UnixTimeStamp` 或 `DateTimeOffset` 自行换算。
- `TimeStamp` 的单位是 **Ticks**，`UnixTimeStamp` 的单位是 **秒**，两者不可混用（实测同一个时间：`639263664000000000` vs `1790769600`）。
- `TimeStamp.NowTimeStamp` / `UtcNowTimeStamp` 属性已标记 `[Obsolete("请使用 TimeStamp.Now() 静态方法替代此属性")]`（源码：`TimeStamp.cs:426`、`433`）。
- `DateTimeHelper.GetWeekRange(int year, int weekIndex, ...)` 在参数非法时抛的是 `Exception`（而非参数异常），源码：`DateTimeHelper.cs:279`，不建议在新代码中使用。

---

## 6. 文件与 IO

### 6.1 `FileHelper`（文件读写）

**用途与适用场景**：文本/字节读写、删除、文件信息、哈希。源码：`src/Bing.Utils/Bing/IO/FileHelper*.cs`。

**主要方法**

| 方法 | 返回 | 说明 |
|---|---|---|
| `ReadToString(string filePath)` / `ReadToStringAsync(...)` | `string` | 文件不存在返回 `string.Empty`（实测） |
| `ReadToBytes(string)` / `ReadToMemoryStream(string)` | `byte[]` / `MemoryStream` | 文件不存在返回 `null` |
| `Write(string filePath, string content)` / `Write(string filePath, byte[] content)` | `void` | **自动创建目录**（实测） |
| `Exists(string path)` / `DeleteIfExists(string filePath)` | `bool` | 删除不存在的文件返回 `false` |
| `GetFileSize(string filePath)` | `FileSize` | 路径为空白抛 `ArgumentNullException` |
| `GetSha256(string file)` / `GetSha512(string file)` | `string` | 大写无分隔十六进制 |

**示例**

```csharp
using Bing.Helpers;
using Bing.IO;

var path = PathHelper.GetPhysicalPath("manual/sample.txt");
FileHelper.Write(path, "bing utils");                       // 目录不存在时自动创建
Console.WriteLine(FileHelper.ReadToString(path));           // bing utils
Console.WriteLine(FileHelper.ReadToString("not-exists"));   // ''（空串，不抛异常）
Console.WriteLine(FileHelper.DeleteIfExists(path));         // True
```

**常见误用与注意事项**

- 读文件**不抛异常**：`ReadToString` 返回 `string.Empty`，`ReadToBytes` / `ReadToMemoryStream` 返回 `null`（源码：`FileHelper.Read.cs:104`、`Read.cs:16`）。用返回值判空而不是 try/catch。
- `SaveFile(...)` 系列**吞掉所有异常只返回 bool**（源码：`FileHelper.Write.cs:29`），且当路径不含目录部分时直接静默返回（源码：`FileHelper.Write.cs:124`），排查"文件没写出来"时优先看这两点。
- `GetMd5` / `GetSha1` 已标记 `[Obsolete("MD5/SHA-1 仅用于兼容非安全校验；安全完整性校验请使用 Bing.Security.Hashing.Hashing 的 SHA-2 API。")]`（源码：`FileHelper.Hash.cs:16`、`72`），完整性校验请用 8.2 节的 `Hashing`。
- 文档契约是**绝对路径**（XML 注释均写"文件的绝对路径"），相对路径是否可用取决于进程当前目录；拼接路径请用 `PathHelper`。

### 6.2 `DirectoryHelper` / `PathHelper`

**用途与适用场景**：目录创建清理、相对路径转物理路径、安全拼接。源码：`src/Bing.Utils/Bing/IO/DirectoryHelper.cs`、`PathHelper.cs`。

**主要方法**

| 类 | 方法 | 返回 |
|---|---|---|
| `DirectoryHelper` | `CreateDirectory(string path)` / `CreateIfNotExists(string)` | `DirectoryInfo` |
| | `DeleteIfExists(string, bool recursive = false)` / `Delete(string, bool isDeleteRoot = true)` | `bool` |
| | `GetFiles(string, string pattern = "*", bool includeChildPath = false)` | `string[]` |
| `PathHelper` | `GetPhysicalPath(string relativePath, string basePath = null)` | `string` |
| | `SafeCombinePaths(string basePath, string relativePath)` | `string` |
| | `GetPathInfo(string path)` / `GetUniqueFilePath(string)` / `EnsureDirectoryExists(string)` | `PathInfo` / `string` / `DirectoryInfo` |

**示例**

```csharp
using Bing.IO;

var physical = PathHelper.GetPhysicalPath("manual/sample.txt");   // 基于应用基目录
Console.WriteLine(PathHelper.SafeCombinePaths(AppContext.BaseDirectory, "manual/a.txt"));
Console.WriteLine(PathHelper.GetPathInfo("/logs/app.log").FileName);   // app.log
DirectoryHelper.CreateIfNotExists(PathHelper.GetPhysicalPath("manual"));
```

**常见误用与注意事项**

- ⚠ `DirectoryHelper.CreateDirectory(path)` 用 `Path.HasExtension(path)` 猜测"这是文件还是目录"（源码：`DirectoryHelper.cs:30`）。传入**无扩展名的文件路径**会被当作目录创建；推荐改用 `PathHelper.EnsureDirectoryExists`，语义明确。
- ⚠ 不要用 `FileHelper.JoinPath`：它强制使用 `\` 并 `.ToLower()`（源码：`FileHelper.cs:229`），在 Linux 上不可用且丢失大小写；请改用 `PathHelper.SafeCombinePaths`，它带路径穿越防护。
- `SafeCombinePaths` 的 `relativePath` 含 `..` 或穿越出 basePath 时抛 `ArgumentException`（源码：`PathHelper.cs:184`、`191`）。

### 6.3 `FileSize` / `FileSizeHelper`

**用途与适用场景**：字节数格式化与解析（上传限制、日志打印）。源码：`src/Bing.Utils/Bing/IO/FileSize.cs`、`FileSizeHelper.cs`。

**主要方法**：`FileSize.FromBytes(long)`、`Parse(string)` / `TryParse(...)`、`GetSize(FileSizeUnit unit, int precision = 2)`、`ToString(FileSizeUnit unit, int precision = 2)`、`GetOptimalUnit()`；`FileSizeHelper.Format(long, int precision = 2)`、`AutoFormat(long, int precision = 2)`、`Convert(long, FileSizeUnit, int)`。

**示例**

```csharp
using Bing.IO;

var size = FileSize.FromBytes(1536);
Console.WriteLine(size);                              // 1.5 KB（实测）
Console.WriteLine(size.ToString(FileSizeUnit.K));     // 1.5 KB
Console.WriteLine(FileSizeHelper.AutoFormat(5L * 1024 * 1024 * 1024)); // 5 GB（实测）
```

**常见误用与注意事项**

- 单位采用 **1024 进制**（`KiloByteSize = 1024`，源码：`FileSize.cs:13`），不是磁盘厂商的 1000 进制。
- `FileSizeUnit` 的成员名是 `Byte / K / M / G / T / P`，**不是** `KiloByte / MegaByte`。
- 构造或 `FromBytes` 传入负数抛 `ArgumentOutOfRangeException`；减法结果为负、除数为 0 也会抛（源码：`FileSize.cs:54`、`241`、`269`）。

### 6.4 `Compression`（GZip / ZIP）

**用途与适用场景**：字节或字符串 GZip 压缩、目录打包为 ZIP。源码：`src/Bing.Utils/Bing/Helpers/Compression.cs`。

**主要方法**

| 方法 | 返回 | 说明 |
|---|---|---|
| `Compress(byte[] data, CompressionLevel = Optimal)` / `Decompress(byte[])` | `byte[]` | GZip |
| `Compress(string value, Encoding encoding = null)` / `Decompress(string, Encoding)` | `string` | 输出/输入为 **Base64 字符串** |
| `CompressAsync` / `DecompressAsync` | `Task<byte[]>` / `Task<string>` | 异步版本 |
| `Zip(string sourceDir, string zipFile)` / `UnZip(string zipFile, string targetDir)` | `void` | 标准 ZIP（文件夹） |
| `IsValidGZipData(byte[] data)` | `bool` | 只校验魔数 `0x1F 0x8B` |

**示例**

```csharp
using System.Text;
using Bing.Helpers;

var raw = Encoding.UTF8.GetBytes(string.Concat(Enumerable.Repeat("bing-utils ", 50)));
var gzip = Compression.Compress(raw);
Console.WriteLine($"{raw.Length} -> {gzip.Length} {Compression.IsValidGZipData(gzip)}");
// 实测：550 -> 39 True
Console.WriteLine(Encoding.UTF8.GetString(Compression.Decompress(gzip)).Length); // 550
```

**常见误用与注意事项**

- 字符串重载输出的是 **Base64**，不是原始字节（源码：`Compression.cs:208`），跨系统传输时不要再做一次 Base64。
- `Compress(Stream)` / `Decompress(Stream)` 会读 `stream.Length`，**不可 Seek 的流（如网络流）会抛异常**（源码：`Compression.cs:330`）。
- 失败时抛 `InvalidOperationException("压缩操作失败", ex)`（源码：`Compression.cs:60`），异常里带 `InnerException`；`Zip` 源目录不存在抛 `DirectoryNotFoundException`。
- 旧类 `Bing.Helpers.GZip` 已整类标记 `[Obsolete("建议使用 Compression 类获得更完整的功能和更好的异常处理")]`（源码：`GZip.cs:6`），且它没有 `Zip/UnZip`。

### 6.5 `StreamHelper` / `PooledMemoryStream` / `LargeMemoryStream`

**用途与适用场景**：字符串转流、高频字节缓冲、超过 2GB 的大内存流。源码：`src/Bing.Utils/Bing/IO/StreamHelper.cs`、`PooledMemoryStream.cs`、`LargeMemoryStream.cs`。

**主要方法**：`StreamHelper.GenerateStreamFromString(string content, Encoding encoding = null)`（默认 UTF-8，`content` 为 `null` 抛 `ArgumentNullException`）；`PooledMemoryStream` / `LargeMemoryStream` 提供 `GetSpan()`、`GetMemory()`、`ToArray()`、`WriteTo(Stream)` 等。

**常见误用与注意事项**

- ⚠ 两个内存流都基于 `ArrayPool<byte>.Shared`，**必须 `Dispose`**（或用 `using`）才会把租借的数组归还，否则失去池化意义（源码：`PooledMemoryStream.cs:290`、`LargeMemoryStream.cs:275`）。
- `PooledMemoryStream.Dispose` 之后再读写一律抛 `ObjectDisposedException`（源码：`PooledMemoryStream.cs:341`）。
- `LargeMemoryStream.Seek(offset, SeekOrigin.End)` 的实现与 .NET 标准语义相反（源码：`LargeMemoryStream.cs:151`），仅在确实需要 >2GB 内存流时使用。

---

## 7. 序列化

### 7.1 `Json`（推荐，基于 System.Text.Json）

**用途与适用场景**：对象 ↔ JSON。源码：`src/Bing.Utils/Bing/Helpers/Json.cs`（纯 `System.Text.Json`，**不是** Newtonsoft）。

**主要方法**

| 方法 | 返回 |
|---|---|
| `ToJson<T>(T value, JsonSerializerOptions options = null, bool removeQuotationMarks = false, bool toSingleQuotes = false)` | `string` |
| `ToJson<T>(T value, JsonOptions options)` | `string` |
| `ToObject<T>(string json, JsonSerializerOptions options = null)` / `ToObject(string, Type, ...)` | `T` / `object` |
| `ToBytes<T>(T value, JsonSerializerOptions options = null)` | `byte[]` |
| `ToJsonAsync<T>(...)` / `ToObjectAsync<T>(...)` | `Task<string>` / `Task<T>` |

**默认行为（实测 + 源码）**

- 属性名保持**原样（PascalCase）**，不驼峰、不缩进。
- **忽略 null 值**：`Json.ToJson(new OrderSample { Amount = 1m })` → `{"Amount":1}`（实测）。
- 中文**不转义**（`JavaScriptEncoder.Create(UnicodeRanges.All)`）。
- 反序列化**大小写不敏感**（`PropertyNameCaseInsensitive = true`，源码：`Json.cs:217`）。

**示例**

```csharp
using Bing.Helpers;

var order = new OrderSample { Id = "ORD-1", Amount = 19.90m };
var json = Json.ToJson(order);          // {"Id":"ORD-1","Amount":19.90}
var back = Json.ToObject<OrderSample>(json);

public sealed class OrderSample
{
    public string Id { get; set; }
    public decimal Amount { get; set; }
}
```

**常见误用与注意事项**

- ⚠ `removeQuotationMarks` / `toSingleQuotes` 是**序列化后的字符串替换**（源码：`Json.cs:83`），会把内容里的引号一起破坏，只适合生成非严格 JSON 的展示文本。
- `Json.ToBytes` 的默认配置与 `ToJson` **不同**（不忽略 null、不大小写不敏感，源码：`Json.cs:340`），混用时注意差异。
- 需要 `JObject` / LINQ to JSON 时才用 `Bing.Utils.Json.JsonHelper`（Newtonsoft 实现，`src/Bing.Utils/Json/JsonHelper.cs`）；两者不要在同一条数据链上混用。

### 7.2 `Xml`（XML 生成器）

**用途与适用场景**：构造简单 XML 报文。源码：`src/Bing.Utils/Bing/Helpers/Xml.Builder.cs`、`Xml.Tools.cs`。

**主要方法**：`new Xml()` / `new Xml(string xml)`、`AddNode(string name, object value = null, XmlNode parent = null)`、`AddCDataNode(...)`、`UpdateNode(...)` / `DeleteNode(...)`、`SelectNodes(string xpath)`、`ToString()`；静态工具 `ToDocument(string)`、`ToElements(string)`、`Validate(string xmlFile, string schemaFile)`、`LoadFileToDocumentAsync(string)`。

**示例**

```csharp
using Bing.Helpers;

var node = new Xml();
node.AddNode("orderId", "ORD-1");
Console.WriteLine(node);   // <xml><orderId>ORD-1</orderId></xml>（实测）
```

**常见误用与注意事项**

- 无参构造的根节点固定为 `<xml>`（源码：`Xml.Builder.cs:44`），需要自定义根节点请传入 XML 字符串构造。
- `UpdateNode` / `DeleteNode` 找不到节点时**静默返回**（源码：`Xml.Builder.cs:142`、`159`），不要依赖它报错。
- `AddCDataNode` 的节点名是随机 GUID（源码：`Xml.Builder.cs:128`），需要稳定节点名时不要用它。

### 7.3 `Serialize`（对象序列化）

**用途与适用场景**：对象 XML 序列化、DataContract 二进制、结构体内存布局。源码：`src/Bing.Utils/Bing/Helpers/Serialize*.cs`。

**主要方法**

| 方法 | 返回 | 说明 |
|---|---|---|
| `ToXml(object data, Encoding encoding = null)` / `FromXml<T>(string xml, Encoding = null)` | `string` / `T` | `XmlSerializer`，缩进输出 |
| `ToDataContractBytes<T>(T data, IEnumerable knownTypes = null)` / `FromDataContractBytes<T>(byte[], ...)` | `byte[]` / `T` | DataContract Binary XML，保留对象引用 |
| `StructToBytes<T>(T data)` / `BytesToStruct<T>(byte[])` where `T : struct` | `byte[]` / `T` | 仅支持**不含托管引用**的结构体 |

**示例**

```csharp
using Bing.Helpers;

var order = new OrderSample { Id = "ORD-1", Amount = 19.90m };

var xml = Serialize.ToXml(order);
Console.WriteLine(Serialize.FromXml<OrderSample>(xml).Id);   // ORD-1

var bytes = Serialize.ToDataContractBytes(order);
Console.WriteLine(Serialize.FromDataContractBytes<OrderSample>(bytes).Id); // ORD-1
```

**常见误用与注意事项**

- `LegacyBinary` 系列的 8 个方法（含 `ToBinary` / `ToLegacyBinary` / `...File`）**全部标记 `[Obsolete]`**，提示原文："BinaryFormatter 不安全，仅用于受控历史数据迁移。新数据请使用 ToDataContractBytes 或 Json.ToBytes。"（源码：`Serialize.LegacyBinary.cs:18` 等）。基于 `BinaryFormatter`，.NET 9+ 不可用。
- `ToBytes<T>` / `FromBytes<T>`（结构体版）同样已过时，请改用 `StructToBytes` / `BytesToStruct`；结构体含 `string`、数组、类等引用字段时会抛 `ArgumentException`（源码：`Serialize.Struct.cs:151`）。
- 结构体内存布局**受运行时和平台影响**，不要用于跨进程通信或长期存储（源码 remarks：`Serialize.Struct.cs:61`），这类场景用 `Json.ToBytes`。

---

## 8. 加解密、摘要与签名

> 完整的安全边界、算法范围与迁移说明见 [docs/security.md](../security.md)。

### 8.1 旧 API 迁移对照（务必先看）

| 遗留入口（包 `Bing.Utils`） | 推荐入口 | `Obsolete` 提示原文摘要 |
|---|---|---|
| `Encrypt.Md5By16/Md5By32`、`Encrypt.Sha1`、FileHelper.GetMd5/GetSha1 | `Bing.Security.Hashing.Hashing`（SHA-256/384/512） | "此密码 API 仅用于兼容旧数据，请迁移到 Bing.Utils.Security 的认证加密、SHA-2/HMAC-SHA2 或 RSA-OAEP/PSS API。" |
| `Encrypt.AesEncrypt/AesDecrypt`、`Encrypt.DesEncrypt/DesDecrypt` | `AesGcmEncryption` | 同上；旧 CBC/3DES 密文无认证标签，需显式迁移 |
| `StringExtensions.EncryptToString/DecryptFromString/EncryptToBytes/DecryptFromBytes` | `AesGcmEncryption` | "此 3DES API 仅用于兼容旧数据，请迁移到 Bing.Security.Cryptography.AesGcmEncryption。" |
| `Encrypt.HmacMd5/HmacSha1` | `Hmac`（HMAC-SHA256/384/512） | 同第一条 |
| `SignManager`、`Encrypt.RsaSign/Rsa2Sign` | `VersionedRequestSigner`（`BRS1.RSA-PSS-SHA256`） | "旧签名协议使用 RSA PKCS#1 v1.5；新代码请使用 Bing.Security.Canonicalization.VersionedRequestSigner 的 BRS1 入口。" |
| `Bing.Helpers.GZip`（整类） | `Compression` | "建议使用 Compression 类获得更完整的功能和更好的异常处理" |

> 说明：`docs/security.md` 迁移表中列出的 `Encrypt.RsaEncrypt/RsaDecrypt` 在**当前源码中不存在**（`Encrypt.cs` 只有签名/验签方法）；RSA 加解密请使用新包的 `RsaEncryption`。

### 8.2 `Hashing` / `Hmac`

**用途与适用场景**：完整性校验、消息认证码。源码：`src/Bing.Utils.Security/Bing/Security/Hashing/Hashing.cs`、`Authentication/Hmac.cs`。

**主要方法**

| 类 | 方法 | 返回 |
|---|---|---|
| `Hashing` | `Compute(ReadOnlySpan<byte>, HashAlgorithmType = Sha256)` | `byte[]` |
| | `ComputeHex(string, HashAlgorithmType = Sha256, Encoding = null)` | `string`（小写十六进制） |
| | `VerifyHex(string value, string expectedHash, ...)` | `bool`（固定时间比较） |
| | `ComputeAsync(Stream, ...)` / `ComputeFileHexAsync(string, ...)` | `Task<byte[]>` / `Task<string>` |
| `Hmac` | `ComputeHex(string, ReadOnlySpan<byte> key, HmacAlgorithmType = Sha256, ...)` | `string` |
| | `VerifyHex(ReadOnlySpan<byte> value, string expectedMac, ReadOnlySpan<byte> key, ...)` | `bool` |
| | `VerifyBase64Url(...)` / `ComputeAsync(Stream, ...)` | `bool` / `Task<byte[]>` |

**示例**

```csharp
using Bing.Security.Hashing;

var digest = Hashing.ComputeHex("bing-utils", HashAlgorithmType.Sha256);
Console.WriteLine(Hashing.VerifyHex("bing-utils", digest));   // True
```

**常见误用与注意事项**

- 只支持 SHA-256/384/512，传入其他值抛 `ArgumentOutOfRangeException("不支持的 SHA-2 算法。")`（源码：`Hashing.cs:133`）。
- `ComputeAsync(Stream)` **不会关闭调用方的流**（源码：`Hashing.cs:86`），需自行 `Dispose`。
- HMAC 的十六进制验签与 Base64Url 验签是**两个独立入口**（`VerifyHex` / `VerifyBase64Url`），不要用错编码格式（源码：`Hmac.cs:102`、`126`）。

### 8.3 `AesGcmEncryption` / `AesGcmStreamEncryption`

**用途与适用场景**：短数据（配置、令牌、字段级）加密与大文件加密。源码：`src/Bing.Utils.Security/Bing/Security/Cryptography/AesGcmEncryption.cs`、`AesGcmStreamEncryption.cs`。

**主要方法**

| 类 | 方法 | 返回 |
|---|---|---|
| `AesGcmEncryption` | `Encrypt(ReadOnlySpan<byte> plaintext, ReadOnlySpan<byte> key, ReadOnlySpan<byte> associatedData = default)` | `AesGcmPayload` |
| | `Decrypt(AesGcmPayload payload, ReadOnlySpan<byte> key, ReadOnlySpan<byte> associatedData = default)` | `byte[]` |
| `AesKeyGenerator` | `Generate(AesKeySize keySize = AesKeySize.Size256)` | `byte[]` |
| `AesGcmStreamEncryption` | `EncryptFileAsync(string sourcePath, string destinationPath, ReadOnlyMemory<byte> key, AesGcmStreamOptions options = null, CancellationToken = default)` | `Task` |
| | `DecryptFileAsync(string sourcePath, string destinationPath, ReadOnlyMemory<byte> key, CancellationToken = default)` | `Task` |

**示例**

```csharp
using System.Text;
using Bing.Security.Cryptography;
using Bing.Security.Keys;

var key = AesKeyGenerator.Generate();                        // 默认 256 位
var payload = AesGcmEncryption.Encrypt(Encoding.UTF8.GetBytes("secret"), key);
Console.WriteLine(Encoding.UTF8.GetString(AesGcmEncryption.Decrypt(payload, key)));   // secret

// 大文件：先写临时文件、整流校验通过后再原子替换
await AesGcmStreamEncryption.EncryptFileAsync("data.bin", "data.bin.enc", key);
await AesGcmStreamEncryption.DecryptFileAsync("data.bin.enc", "data.bin.dec", key);
```

**常见误用与注意事项**

- 密钥必须是 16/24/32 字节，否则抛 `ArgumentException`（实测消息："AES-GCM 密钥必须为 16、24 或 32 字节。(Parameter 'key')"）；流加密**只支持 32 字节**密钥（源码：`AesGcmStreamEncryption.cs:161`）。
- ⚠ **认证失败（密文/Nonce/标签/AAD/密钥任一不匹配）抛 `CryptographicException`**（实测：篡改密文后解密抛该异常），不是返回 `null`，务必 try/catch。
- **同一密钥下 Nonce 必须唯一**：库为单次加密生成随机 Nonce，不要把 `payload` 的字段跨密钥流程复用。
- 流式 API 仅在 `net6.0+` 可用（`#if NET6_0_OR_GREATER`），且只支持 `BSS2` 格式，`BSS1` 密文会抛 `NotSupportedException`。

### 8.4 `Pbkdf2PasswordHasher`（口令存储）

**用途与适用场景**：用户口令散列存储与验证。源码：`src/Bing.Utils.Security/Bing/Security/Passwords/Pbkdf2PasswordHasher.cs`。

**主要方法**：构造函数 `Pbkdf2PasswordHasher(Pbkdf2PasswordHasherOptions options = null)`、`Hash(string password)` → `string`、`Verify(string password, string encodedHash)` → `PasswordVerificationResult`（`Failed` / `Success` / `SuccessRehashNeeded`）。

**示例**

```csharp
using Bing.Security.Passwords;

var hasher = new Pbkdf2PasswordHasher();
var encoded = hasher.Hash("P@ssw0rd");
Console.WriteLine(encoded.Split('$')[0] + "$" + encoded.Split('$')[1] + "$" + encoded.Split('$')[2]);
// BSP1$PBKDF2-SHA256$310000（实测，默认 31 万次迭代）
Console.WriteLine(hasher.Verify("P@ssw0rd", encoded));  // Success
Console.WriteLine(hasher.Verify("bad", encoded));       // Failed
```

**常见误用与注意事项**

- 构造即强校验参数范围（源码：`Pbkdf2PasswordHasher.cs:109`）：迭代次数 **100000 ~ 1000000**（默认 310000）、盐 **16 ~ 64** 字节（默认 16）、哈希 **32 ~ 64** 字节（默认 32），越界抛 `ArgumentOutOfRangeException`。
- 待验证记录超过 512 字符或迭代次数低于 100000 时，`Verify` 返回 `Failed` 而**不抛异常**（源码：`Pbkdf2PasswordHasher.cs:71`、`74`）——不要把它当成"参数错了"的信号去重试。
- `SuccessRehashNeeded` 表示需要用当前参数重新散列，应借此平滑升级迭代次数。

### 8.5 `RsaKeyGenerator` / `RsaSignature` / `VersionedRequestSigner`

**用途与适用场景**：非对称签名、对外接口的请求签名。源码：`src/Bing.Utils.Security/Bing/Security/Keys/RsaKeyGenerator.cs`、`Signatures/RsaSignature.cs`、`Canonicalization/VersionedRequestSigner.cs`。

**主要方法**

| 类 | 方法 | 返回 |
|---|---|---|
| `RsaKeyGenerator` | `Generate(int keySize = 3072)` | `RsaKeyPair`（`PublicKeyPem` / `PrivateKeyPem`） |
| `RsaSignature` | `Sign(ReadOnlySpan<byte> data, string privateKeyPem)` | `byte[]` |
| | `Verify(ReadOnlySpan<byte> data, ReadOnlySpan<byte> signature, string publicKeyPem)` | `bool` |
| `VersionedRequestSigner` | `Sign(IEnumerable<CanonicalParameter> parameters, RSA privateKey)` | `string`（形如 `BRS1.xxx`） |
| | `Verify(IEnumerable<CanonicalParameter> parameters, string signature, RSA publicKey)` | `bool` |

**示例**

```csharp
using System.Security.Cryptography;
using System.Text;
using Bing.Security.Canonicalization;
using Bing.Security.Keys;
using Bing.Security.Signatures;

var pair = RsaKeyGenerator.Generate(2048);
var signature = RsaSignature.Sign(Encoding.UTF8.GetBytes("contract"), pair.PrivateKeyPem);
Console.WriteLine(RsaSignature.Verify(Encoding.UTF8.GetBytes("contract"), signature, pair.PublicKeyPem));  // True
Console.WriteLine(RsaSignature.Verify(Encoding.UTF8.GetBytes("contract!"), signature, pair.PublicKeyPem)); // False

using var rsa = RSA.Create(2048);
var parameters = new[] { new CanonicalParameter("orderId", "ORD-1"), new CanonicalParameter("amount", 19.9m) };
var signed = VersionedRequestSigner.Sign(parameters, rsa);
Console.WriteLine($"{signed.Split('.')[0]} {VersionedRequestSigner.Verify(parameters, signed, rsa)}"); // BRS1 True
```

**常见误用与注意事项**

- **验签失败返回 `false`，不抛异常**（实测：内容被改返回 `False`）；而畸形 PEM / 密钥强度不足（< 2048 位）抛 `ArgumentException`。
- RSA 密钥长度必须 ≥ 2048 且为 256 的倍数（源码：`RsaKeyGenerator.cs:19`），各处复用前都会再校验一次。
- `CanonicalParameterSerializer` 的参数值类型有白名单，且 `DateTimeKind.Unspecified`、NaN / Infinity 会被拒绝（源码：`CanonicalParameterSerializer.cs:125`、`138`）。
- RSA/ECDSA 签名入口与 `VersionedRequestSigner` 仅在 `net6.0+` 编译（条件编译）。

### 8.6 国密 `Sm3` / `Sm4GcmEncryption` / `Sm2*`

**用途与适用场景**：需要国密合规的场景（SM2 加密与签名、SM3 摘要、SM4 分组加密）。源码：`src/Bing.Utils.Security.Gm/Bing/Security/Gm/*.cs`。

**主要方法**

| 类 | 方法 | 返回 |
|---|---|---|
| `Sm3` | `Compute(byte[])` / `Compute(Stream)` / `ComputeAsync(Stream, ...)` | `byte[]` / `Task<byte[]>` |
| | `ComputeHex(byte[])` / `ComputeBase64(byte[])` / `ComputeFileHexAsync(string)` | `string` / `Task<string>` |
| `HmacSm3` | `ComputeHex(byte[] key, byte[] data)` / `Verify(byte[] key, byte[] data, byte[] expectedMac)` | `string` / `bool` |
| `Sm4GcmEncryption` | `Encrypt(byte[] plaintext, byte[] key, byte[] associatedData = null)` / `Decrypt(Sm4GcmPayload, byte[] key, byte[] aad = null)` | `Sm4GcmPayload` / `byte[]` |
| `Sm2KeyGenerator` / `Sm2Encryption` / `Sm2Signature` | `Generate()` / `Encrypt(byte[], string publicKeyPem)` / `Sign(byte[], string privateKeyPem, byte[] userId = null)` | `Sm2KeyPair` / `byte[]` |

**示例**

```csharp
using System.Security.Cryptography;
using System.Text;
using Bing.Security.Gm;

Console.WriteLine(Sm3.ComputeHex(Encoding.UTF8.GetBytes("国密")));

var key = new byte[Sm4GcmEncryption.KeySize];   // 固定 16 字节
RandomNumberGenerator.Fill(key);
var payload = Sm4GcmEncryption.Encrypt(Encoding.UTF8.GetBytes("sm4 secret"), key);
Console.WriteLine(Encoding.UTF8.GetString(Sm4GcmEncryption.Decrypt(payload, key)));  // sm4 secret
```

**常见误用与注意事项**

- SM4 密钥必须**恰好 16 字节**（`Sm4GcmEncryption.KeySize`，源码：`Sm4GcmEncryption.cs:18`），否则抛 `ArgumentException`。
- SM2 密钥只接受 `sm2p256v1` 曲线，公钥必须是单个 `PUBLIC KEY`（SPKI）PEM、私钥必须是单个 `PRIVATE KEY`（PKCS#8）PEM；曲线点或私钥标量不合格一律抛 `ArgumentException`（源码：`Sm2KeySerializer.cs:147`、`159`）。
- SM2 默认用户标识为 `"1234567812345678"`，自定义时长度必须在 1~8191 字节；**验签时 userId 必须与签名时一致**，否则返回 `false`。
- 国密包依赖 `BouncyCastle.Cryptography`，但公共 API 不暴露其类型。

---

## 9. HTTP 请求与网络

### 9.1 `HttpClientService` + `HttpRequest<TResult>`（发送 HTTP 请求）

**用途与适用场景**：链式构造 GET/POST/PUT/DELETE 请求、自动序列化结果。源码：`src/Bing.Utils.Http/Bing/Http/Clients/HttpClientService.cs`、`HttpRequest.cs`（接口 `IHttpClient` / `IHttpRequest` 在主包 `Bing.Utils` 的 `Bing.Http` 命名空间下）。

**主要方法**

| 方法 | 返回 |
|---|---|
| `HttpClientService.Get(string url)` / `Get<TResult>(string url)` / `Post` / `Put` / `Delete`（`TResult : class`） | `IHttpRequest<TResult>` |
| `Header(string key, string value)` / `Header(IDictionary<string,string>)` | `IHttpRequest<TResult>` |
| `BearerToken(string token)` / `ContentType(...)` / `Encoding(...)` / `Timeout(int|TimeSpan)` | `IHttpRequest<TResult>` |
| `QueryString(string, string)` / `Content(object)` / `JsonContent(object)` / `XmlContent(string)` / `FileContent(...)` | `IHttpRequest<TResult>` |
| `OnFail(Action<HttpResponseMessage, object>)` / `OnSuccess(...)` / `OnComplete(...)` | `IHttpRequest<TResult>` |
| `GetResultAsync(CancellationToken = default)` | `Task<TResult>` |
| `GetStreamAsync(...)` / `WriteAsync(string filePath, ...)` | `Task<byte[]>` / `Task`（下载到文件） |

**示例**（实测通过：本地 `HttpListener` 返回 200 / 400）

```csharp
using Bing.Http.Clients;

using var httpClient = new HttpClient { BaseAddress = new Uri("https://api.example.com") };

// 200：Content-Type 为 application/json 时自动反序列化为目标类型
var order = await new HttpClientService().SetHttpClient(httpClient)
    .Get<OrderDto>("/order")
    .Header("X-Trace-Id", "manual-sample")
    .GetResultAsync();

// 400：结果返回 null，并回调 OnFail —— 不会抛异常
var failed = await new HttpClientService().SetHttpClient(httpClient)
    .Get<string>("/bad")
    .OnFail((response, _) => Console.WriteLine($"状态码 {(int)response.StatusCode}"))
    .GetResultAsync();
```

**常见误用与注意事项**

- ⚠ **HTTP 状态码非 2xx 不会抛异常**，结果返回 `default/null`，要感知失败必须挂 `OnFail`（源码：`HttpRequest.cs:1112`；实测 400 时 `result == null` 且 `OnFail` 被回调）。网络不可达等传输层故障仍会抛 `HttpRequestException`。
- ⚠ 结果转换只对 **Content-Type 为 `application/json`** 的响应做反序列化，其他类型一律返回 `null`（源码：`HttpRequest.cs:1150`）；非 JSON 响应用 `OnConvert` 或 `OnSendAfter` 自行处理。
- ⚠ 调用 `IgnoreSsl()`、`Certificate(path, password)`、`UseCookies(bool)` 中任意一个，客户端必须通过 `services.AddHttpClient(...).UseBingRequestIsolation(...)` 注册，否则抛 `NotSupportedException`（源码：`HttpRequest.cs:936`）。
- `SetHttpClient(client)` 注入的客户端由**调用方负责释放**；用 `IHttpClientFactory` 构造时，工厂创建的客户端由本次发送拥有并释放（源码：`HttpRequest.cs:926`）。
- 包内**没有** `HttpHelper` 类，也没有 `Bing.Http.Clients.WebClient`（`WebClient.cs`、`HttpRequestBase.cs`、`IRequest.cs` 等文件已整体注释，编译期不存在），不要按这些名字搜索。

### 9.2 `IpAddressProvider` / `IpValidator`

**用途与适用场景**：获取当前请求 IP、本机 IP、公网 IP，以及 IP 合法性判断。源码：`src/Bing.Utils.Http/Bing/Net/IpAddressProvider.cs`、`IpValidator.cs`。

**主要方法**

| 方法 | 返回 |
|---|---|
| `IpAddressProvider.GetIp()` | `string`（优先级：手动值 → 请求上下文远程 IP → 本机局域网 IP） |
| `SetIp(string ip)` / `Reset()` | `void` |
| `GetAllLocalIps(bool includeIPv6 = false, bool includeLoopback = false)` | `List<string>` |
| `GetPublicIpAsync(TimeSpan? timeout = null, bool useParallel = false)` | `Task<string>`（访问外部服务，全部失败返回 `null`） |
| `IpValidator.IsValid(string ip)` / `IsValidIPv4` / `IsValidIPv6` / `IsInnerIp` / `IsLocalIp` | `bool` |

**示例**

```csharp
using Bing.Net;

IpAddressProvider.SetIp("192.168.1.100");   // 在管道或测试中注入当前上下文 IP
Console.WriteLine(IpAddressProvider.GetIp());                        // 192.168.1.100（实测）
Console.WriteLine(IpAddressProvider.GetAllLocalIps().Count);         // 3（实测，随机器变化）
Console.WriteLine($"{IpValidator.IsValid("192.168.1.100")} {IpValidator.IsInnerIp("192.168.1.100")}"); // True True
```

**常见误用与注意事项**

- `SetIp` 会校验格式，非法值抛 `ArgumentException("无效的IP地址格式: ...")`（源码：`IpAddressProvider.cs:35`）；传 `null` / 空串可用于清空。
- IP 缓存基于 `AsyncLocal`，是**异步流级**而非线程级，跨 `Task` 边界需注意值是否传递。
- `GetIp()` 在未注入 `Web.HttpContextAccessor` 时会静默退化为"本机局域网 IP"，不会报错——线上拿错 IP 多源于此。
- `GetPublicIpAsync` 会真实访问外部服务，默认超时 5 秒，失败返回 `null`；不要在请求主链路中同步等待它。

### 9.3 `Web`（服务端请求上下文）

**用途与适用场景**：在 ASP.NET Core 请求处理中读取 URL、Header、Body、UA，或向当前响应写出内容。源码：`src/Bing.Utils.Http/Bing/Helpers/Web.cs`。

**主要成员**：`HttpContextAccessor`（必须先赋值）、`Request` / `Response`、`Url` / `Host` / `Browser` / `Body` / `QueryString` / `ContentType`、`GetParam(string name)`、`GetBodyAsync()`、`UrlEncode/UrlDecode`、`Write` / `WriteAsync`、`DownloadAsync`、`LocalIpAddress`。

**示例**

```csharp
// 应用启动阶段注入访问器
Web.HttpContextAccessor = app.Services.GetRequiredService<IHttpContextAccessor>();

// 之后任意位置读取当前请求信息
Console.WriteLine($"{Web.Host} {Web.QueryString} {Web.Browser}");
```

**常见误用与注意事项**

- ⚠ 未注入 `HttpContextAccessor` 时行为**不统一**（实测）：`Body` / `GetParam` 返回空串，`Host` 退化为 `Dns.GetHostName()`，而 `IsLocal` 直接抛 `ArgumentNullException(paramName: "connection")`，`Write` 系列抛 `InvalidOperationException("HTTP 响应对象不可用")`。
- `Url` 在 `NETSTANDARD2_1` 下直接抛 `NotSupportedException`（源码：`Web.cs:219`）。
- 它**不是** HTTP 客户端：主动发起请求请用 9.1 的 `HttpClientService`。
- 静态构造会设置 `ServicePointManager.DefaultConnectionLimit = 200`（源码：`Web.cs:358`），属于进程级副作用。

---

## 10. Id 与唯一标识生成

### 10.1 `Id`（统一入口）

**用途与适用场景**：业务 Id、TraceId、雪花 Id。源码：`src/Bing.Utils.IdUtils/Bing/Helpers/Id*.cs`。

**主要方法**

| 方法 | 返回 | 是否需要预配置 |
|---|---|---|
| `CreateObjectId()` | `string`（24 位十六进制） | 否 |
| `CreateTimestampId()` | `string`（时间戳 + 序号） | 否 |
| `CreateSnowflakeId()` / `CreateSnowflakeIds(uint count)` | `long` / `long[]` | 否（默认 workerId=1，**生产必须配置**） |
| `CreateGuid()` / `CreateSimpleGuid()` | `Guid` / `string` | 否 |
| `CreateLong()` / `CreateString()` | `long` / `string` | **是**，未配置抛 `InvalidOperationException` |
| `ConfigureLong(Func<long>)` / `ConfigureString(Func<string>)` / `ConfigureSnowflakeId(Func<ISnowflakeId>)` | `void` | — |

**示例**

```csharp
using Bing.Helpers;

Console.WriteLine(Id.CreateObjectId());      // 6abd060c47f6da8b0439ddf0（实测）
Console.WriteLine(Id.CreateTimestampId());   // 17907727482920001
Console.WriteLine(Id.CreateSimpleGuid());    // bcf1553b90764ee390800d4306935d77

Id.ConfigureSnowflakeId(() => SnowflakeGenerator.Create(workerId: 1, dataCenterId: 1));
Console.WriteLine(Id.CreateSnowflakeId());   // 2105279611708510208
```

**常见误用与注意事项**

- ⚠ `CreateLong()` / `CreateString()` **必须先** `ConfigureLong` / `ConfigureString`，否则抛 `InvalidOperationException`（源码：`Id.cs:101`、`119`）。这是最容易踩的坑。
- ⚠ 多实例部署必须调用 `Id.ConfigureSnowflakeId(() => SnowflakeGenerator.Create(workerId, dataCenterId))` 分配**唯一的 workerId**；默认 `workerId = 1` 在多机会重复发号。
- `CreateObjectId()` / `CreateTimestampId()` 不受 `SetId` 影响，始终生成新值（源码：`Id.cs:131`、`141`）。
- `[Obsolete] Id.Configure(Func<ISnowflakeId>)` 已过时，请用 `ConfigureSnowflakeId`（源码：`Id.SnowflakeId.cs:154`）。
- 时钟回拨时雪花算法抛 `ApplicationException("时间戳必须大于上一次生成ID的时间戳...")`（源码：`TwitterSnowflakeProvider.cs:160`）。

### 10.2 `ObjectId` / `SnowflakeGenerator` / `GuidProvider` / `RandomIdGenerator`

| 类 | 典型入口 | 返回 | 适用 |
|---|---|---|---|
| `ObjectId` | `ObjectId.GenerateNewStringId()`、`Parse(string)` / `TryParse(string, out ObjectId)`、`CreationTime` | `string` / `ObjectId` | 需要带创建时间的短字符串 Id |
| `SnowflakeGenerator` | `Create(long workerId, long dataCenterId, long sequence = 0)`（Twitter）/ `Create(long workerId)`（Seata） | `ISnowflakeId` | 分布式 long 型 Id |
| `GuidProvider` | `Create(GuidStyle style = BasicStyle, ...)`、`Create(CombStyle style, ...)` | `Guid` | 数据库主键（可按库选顺序风格） |
| `RandomIdGenerator` | `Create(int length, string dict = AllWords)` | `string` | 邀请码、短码 |

**示例**

```csharp
using Bing.IdUtils;

var id = ObjectId.GenerateNewStringId();
Console.WriteLine(ObjectId.Parse(id).CreationTime);      // 解析出创建时间
Console.WriteLine(ObjectId.TryParse(id, out _));         // True

Console.WriteLine(GuidProvider.Create());                          // 随机 GUID
Console.WriteLine(GuidProvider.Create(GuidStyle.SequentialAsEndStyle)); // 适合 SQL Server 主键的顺序 GUID
Console.WriteLine(RandomIdGenerator.Create(12, RandomIdGenerator.NanoWords)); // xp82zGI3izNk
```

**常见误用与注意事项**

- GUID 风格要按数据库选：`SequentialAsEndStyle`（SQL Server）、`SequentialAsStringStyle`（MySQL / PostgreSQL）、`SequentialAsBinaryStyle`（Oracle），枚举注释已标注适用库；跨系统唯一标识用 `BasicStyle`。
- `RandomIdGenerator.Create` 内部使用共享的 `System.Random` 静态实例且无锁，高并发下不适合作为安全随机；安全场景请用 `SecurityRandom`。
- `ObjectId.Parse` 传入非 24 字符抛 `ArgumentOutOfRangeException`；需要容错用 `TryParse`。

---

## 11. 重复能力对照与推荐选择

| 能力 | 重复入口 | 推荐 | 原因 |
|---|---|---|---|
| JSON 序列化 | `Bing.Helpers.Json`（System.Text.Json） vs `Bing.Utils.Json.JsonHelper`（Newtonsoft） | `Bing.Helpers.Json` | 与 `Serialize`、`FileHelper` 等生态一致，默认注册时间/长整型转换器、中文不转义；只有需要 `JObject` 时才用 `JsonHelper` |
| 二进制序列化 | `Serialize.ToLegacyBinary*` / `ToBinary*`（全 `Obsolete`） vs `ToDataContractBytes` | `ToDataContractBytes` | 前者基于 `BinaryFormatter`，.NET 9+ 不可用，且明文提示仅用于历史数据迁移 |
| GZip 压缩 | `GZip`（整类 `Obsolete`） vs `Compression` | `Compression` | 后者有 `Zip/UnZip`、`IsValidGZipData`、压缩级别、异步全套，异常带 `InnerException` |
| 目录创建 | `DirectoryHelper.CreateDirectory` vs `PathHelper.EnsureDirectoryExists` | `PathHelper.EnsureDirectoryExists` | 前者用 `Path.HasExtension` 猜目录，无扩展名文件路径会被误判 |
| 路径拼接 | `FileHelper.JoinPath` vs `PathHelper.SafeCombinePaths` | `SafeCombinePaths` | 前者强制 `\` 且小写化，Linux 不可用；后者有路径穿越防护 |
| 文件哈希 | `FileHelper.GetMd5/GetSha1`（`Obsolete`） vs `Hashing`（Security 包） | `Hashing` | MD5/SHA-1 仅保留非安全校验用途 |
| 枚举工具 | `Bing.Helpers.Enum`（整类 `Obsolete`） vs `Enums` | `Enums` | 前者提示"下个版本将会弃用"，且是代码复制 |
| 拼音转换 | `Str.PinYin/FullPinYin` vs `PinyinUtil` | `PinyinUtil` | 后者支持分隔符、多音字消歧与声调 |
| 字符串截断 | `Str.Truncate` / `StringExtensions.Truncate` / `StringTruncators` | 见 3.6 节对比 | 语义差异明显，按是否需要省略号与计数单位选择 |
| 短信/手机号脱敏 | `Str.GetHideMobile` / `Format.EncryptPhoneOfChina` / `StringExtensions.Mask` | `Format` 系列 | 规则明确且可控；三者掩码位数不同，勿混用 |
| Unix 时间戳 | `DateTimeHelper` vs `UnixTimeStamp` | 简单换算用 `DateTimeHelper`，严格 UTC 往返用 `UnixTimeStamp` | `ConvertUnixTimestampToDateTime` 硬编码 GMT+8（实测） |
| 请求签名 | `SignManager`（`Obsolete`） / `CanonicalRequestSigner` / `VersionedRequestSigner` | `VersionedRequestSigner` | 签名自带协议版本前缀并纳入签名输入，且不回退旧协议 |
| Id 生成 | `Id.CreateObjectId` / `CreateTimestampId` / `SnowflakeGenerator` / `GuidProvider` | 见第 10 章 | 按是否需要时序、是否 long 型、是否做数据库主键选择 |

---

## 12. 常见坑速查

1. `Str` 同名冲突：`Bing.Helpers.Str` 与 `Bing.Text.Str` 同时引入会编译报 `CS0104`（实测）。
2. `Id.CreateLong()` / `CreateString()` 未配置就调用 → `InvalidOperationException`（源码：`Id.cs:101`）。
3. `DateTimeHelper.ConvertUnixTimestampToDateTime` 会把结果转 GMT+8，与 `ConvertDateTimeToUnixTime` **不互逆**（实测 12:00Z → 20:00）。
4. `DictsExtensions.GetValueOrDefault(key)` 单参重载易与 BCL 冲突，统一用带默认值重载。
5. `Splitter.OmitEmptyStrings()` 过滤不掉"只含空格"的段（实测）。
6. `PinyinUtil.GetPinyin("重庆")` 得到 `ZhongQing`，多音字必须用 `GetContextualPinyin`。
7. `FileHelper` 读不存在的文件**不抛异常**（字符串返回空串、字节返回 `null`）；`SaveFile` 吞异常只返回 `false`。
8. `DirectoryHelper.CreateDirectory` 会把无扩展名的文件路径当目录创建（源码：`DirectoryHelper.cs:30`）。
9. HTTP 请求**非 2xx 不抛异常**，结果返回 `null`，要挂 `OnFail` 才能感知（实测）。
10. AES-GCM / SM4-GCM **认证失败抛 `CryptographicException`**（实测），而**验签失败返回 `false`**，两者的错误处理完全不同。
11. `PooledMemoryStream` / `LargeMemoryStream` 必须 `Dispose` 才能归还 `ArrayPool` 数组。
12. `Web` 未注入 `HttpContextAccessor` 时：部分属性返回空值、部分抛异常（`IsLocal` 抛 `ArgumentNullException`，实测）。

---

## 附录：源码与测试索引

| 分类 | 源码目录 | 主要测试目录 |
|---|---|---|
| 核心工具 | `src/Bing.Utils/Bing/Helpers`、`Bing/Text`、`Bing/IO`、`Bing/Date` | `tests/Bing.Utils.Tests`、`tests/BingUtilsUT` |
| 集合 | `src/Bing.Utils.Collections/Bing/Collections` | `tests/Bing.Utils.Collections.Tests` |
| 日期时间 | `src/Bing.Utils.DateTime/Bing/Date` | `tests/Bing.Utils.DateTime.Tests` |
| 文本 | `src/Bing.Utils.Text/Bing/Text` | `tests/Bing.Utils.Text.Tests` |
| HTTP / 网络 | `src/Bing.Utils.Http/Bing/Http`、`Bing/Net` | `tests/Bing.Utils.Http.Tests`、`tests/Bing.Utils.Http.Tests.Integration` |
| 安全 | `src/Bing.Utils.Security/Bing/Security` | `tests/Bing.Utils.Security.Tests` |
| 国密 | `src/Bing.Utils.Security.Gm/Bing/Security/Gm` | `tests/Bing.Utils.Security.Gm.Tests` |
| Id | `src/Bing.Utils.IdUtils` | `tests/Bing.Utils.IdUtils.Tests` |

> 本手册中所有「实测」结论来自在 .NET 8 控制台工程中编译并运行上述 API；如需复现，可新建 `net8.0` 控制台项目并引用对应的 `src/<包>/<包>.csproj`。
