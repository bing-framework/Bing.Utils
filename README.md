# Bing.Utils
[![GitHub license](https://img.shields.io/badge/license-MIT-blue.svg)](https://mit-license.org/)
[![Ask DeepWiki](https://deepwiki.com/badge.svg)](https://deepwiki.com/bing-framework/Bing.Utils)

Bing.Utils 是一个基于`.net core`平台下的工具库，旨在提升小型团队的开发输出能力，由常用公共操作类（工具类、帮助类）、分层架构基类，第三方组件封装，第三方业务接口封装等组成。

## 目录导航

- [Nuget Packages](#nuget-packages)
- [安装步骤](#安装步骤)
  - [环境要求](#环境要求)
  - [安装依赖](#安装依赖)
  - [必要配置说明](#必要配置说明)
- [使用示例](#使用示例)
  - [前期准备](#前期准备)
  - [类型转换 Conv](#类型转换-conv)
  - [Json 序列化 Json](#json-序列化-json)
  - [Id 生成 IdUtils](#id-生成-idutils)
  - [日期时间 DateTime](#日期时间-datetime)
  - [集合扩展 Collections](#集合扩展-collections)
  - [文本处理 Text](#文本处理-text)
  - [图片处理 Drawing](#图片处理-drawing)
  - [安全与密码学 Security](#安全与密码学-security)
  - [网络与 Http](#网络与-http)
- [安全与密码学](#安全与密码学)
- [开发环境与依赖](#开发环境与依赖)
- [框架开发流程](#框架开发流程)
- [作者](#作者)
- [贡献与反馈](#贡献与反馈)
- [贡献指南](#贡献指南)
  - [Fork 与分支流程](#fork-与分支流程)
  - [提交规范](#提交规范)
  - [代码风格要求](#代码风格要求)
  - [测试要求](#测试要求)
  - [PR 提交流程](#pr-提交流程)
- [免责声明](#免责声明)
- [开源地址](#开源地址)
- [License](#license)

## Nuget Packages

|包名称|Nuget版本|下载数|
|---|---|---|
|Bing.Utils|[![Bing.Utils](https://img.shields.io/nuget/v/Bing.Utils.svg)](https://www.nuget.org/packages/Bing.Utils/)|[![Bing.Utils](https://img.shields.io/nuget/dt/Bing.Utils.svg)](https://www.nuget.org/packages/Bing.Utils/)|
|Bing.Utils.Text|[![Bing.Utils.Drawing](https://img.shields.io/nuget/v/Bing.Utils.Text.svg)](https://www.nuget.org/packages/Bing.Utils.Text/)|[![Bing.Utils.Text](https://img.shields.io/nuget/dt/Bing.Utils.Text.svg)](https://www.nuget.org/packages/Bing.Utils.Text/)|
|Bing.Utils.Collections|[![Bing.Utils.Collections](https://img.shields.io/nuget/v/Bing.Utils.Collections.svg)](https://www.nuget.org/packages/Bing.Utils.Collections/)|[![Bing.Utils.Collections](https://img.shields.io/nuget/dt/Bing.Utils.Collections.svg)](https://www.nuget.org/packages/Bing.Utils.Collections/)|
|Bing.Utils.Reflection|[![Bing.Utils.Reflection](https://img.shields.io/nuget/v/Bing.Utils.Reflection.svg)](https://www.nuget.org/packages/Bing.Utils.Reflection/)|[![Bing.Utils.Reflectionn](https://img.shields.io/nuget/dt/Bing.Utils.Reflection.svg)](https://www.nuget.org/packages/Bing.Utils.Reflection/)|
|Bing.Utils.IdUtils|[![Bing.Utils.IdUtils](https://img.shields.io/nuget/v/Bing.Utils.IdUtils.svg)](https://www.nuget.org/packages/Bing.Utils.IdUtils/)|[![Bing.Utils.IdUtils](https://img.shields.io/nuget/dt/Bing.Utils.IdUtils.svg)](https://www.nuget.org/packages/Bing.Utils.IdUtils/)|
|Bing.Utils.DateTime|[![Bing.Utils.DateTime](https://img.shields.io/nuget/v/Bing.Utils.DateTime.svg)](https://www.nuget.org/packages/Bing.Utils.DateTime/)|[![Bing.Utils.DateTime](https://img.shields.io/nuget/dt/Bing.Utils.DateTime.svg)](https://www.nuget.org/packages/Bing.Utils.DateTime/)|
|Bing.Utils.Drawing|[![Bing.Utils.Drawing](https://img.shields.io/nuget/v/Bing.Utils.Drawing.svg)](https://www.nuget.org/packages/Bing.Utils.Drawing/)|[![Bing.Utils.Drawing](https://img.shields.io/nuget/dt/Bing.Utils.Drawing.svg)](https://www.nuget.org/packages/Bing.Utils.Drawing/)|
|Bing.Utils.Drawing.ImageSharp|[![Bing.Utils.Drawing.ImageSharp](https://img.shields.io/nuget/v/Bing.Utils.Drawing.ImageSharp.svg)](https://www.nuget.org/packages/Bing.Utils.Drawing.ImageSharp/)|[![Bing.Utils.Drawing.ImageSharp](https://img.shields.io/nuget/dt/Bing.Utils.Drawing.ImageSharp.svg)](https://www.nuget.org/packages/Bing.Utils.Drawing.ImageSharp/)|
|Bing.Utils.Drawing.SkiaSharp|[![Bing.Utils.Drawing.SkiaSharp](https://img.shields.io/nuget/v/Bing.Utils.Drawing.SkiaSharp.svg)](https://www.nuget.org/packages/Bing.Utils.Drawing.SkiaSharp/)|[![Bing.Utils.Drawing.SkiaSharp](https://img.shields.io/nuget/dt/Bing.Utils.Drawing.SkiaSharp.svg)](https://www.nuget.org/packages/Bing.Utils.Drawing.SkiaSharp/)|
|Bing.Utils.Http|[![Bing.Utils.Http](https://img.shields.io/nuget/v/Bing.Utils.Http.svg)](https://www.nuget.org/packages/Bing.Utils.Http/)|[![Bing.Utils.Http](https://img.shields.io/nuget/dt/Bing.Utils.Http.svg)](https://www.nuget.org/packages/Bing.Utils.Http/)|
|Bing.Utils.Security|安全与密码学基础能力|详见 [安全与密码学文档](docs/security.md)|
|Bing.Utils.Security.Gm|SM2、SM3、SM4 国密扩展|详见 [安全与密码学文档](docs/security.md)|

## 安装步骤

### 环境要求

| 项目 | 要求 | 说明 |
|---|---|---|
| .NET SDK | 8.0 或更高 | CI 固定使用 `8.0.x`（见 `.github/workflows/dotnet.yml`）；仓库未提供 `global.json`，不锁定 SDK 版本 |
| 目标框架 | `net8.0`、`net7.0`、`net6.0`、`netstandard2.0` | 由 `common.props` 的 `TargetFrameworks` 统一定义 |
| 操作系统 | Windows / Linux / macOS | CI 在三个平台同时验证；其中 `Bing.Utils.Drawing` 依赖 `System.Drawing.Common`，非 Windows 平台行为受限 |
| IDE | Visual Studio 2022（17.3+）或 VS Code + C# 扩展 | 解决方案文件为 Visual Studio Version 17 格式 |

> 多目标框架包含 `net6.0` 与 `net7.0`，首次还原时 SDK 会自动下载对应的目标框架包，请保证可以联网执行一次还原。

### 安装依赖

**方式一：作为 NuGet 包引用（推荐）**

按需安装即可，各包之间已通过 NuGet 依赖关联：

```bash
dotnet add package Bing.Utils
dotnet add package Bing.Utils.Collections
dotnet add package Bing.Utils.DateTime
dotnet add package Bing.Utils.IdUtils
dotnet add package Bing.Utils.Reflection
dotnet add package Bing.Utils.Text
dotnet add package Bing.Utils.Http
dotnet add package Bing.Utils.Security
dotnet add package Bing.Utils.Security.Gm
# 图像处理三选一，跨平台场景推荐 ImageSharp 或 SkiaSharp
dotnet add package Bing.Utils.Drawing
dotnet add package Bing.Utils.Drawing.ImageSharp
dotnet add package Bing.Utils.Drawing.SkiaSharp
```

**方式二：从源码构建**

```bash
git clone https://github.com/bing-framework/Bing.Utils.git
cd Bing.Utils
dotnet restore ./Bing.Utils.sln
dotnet build ./Bing.Utils.sln -c Release --no-restore
dotnet test ./Bing.Utils.sln -c Release --no-build
```

构建单个项目或指定目标框架：

```bash
dotnet build ./src/Bing.Utils/Bing.Utils.csproj -c Release
dotnet build ./src/Bing.Utils.Drawing.ImageSharp/Bing.Utils.Drawing.ImageSharp.csproj -f netstandard2.0 --no-restore
```

**打包产物**

- Windows：`Publish.bat`，脚本会在仓库根目录 `nuget_packages/` 生成包，并提示输入 Nuget Key 后推送到 nuget.org。
- 跨平台：`build/BuildScript.cs` 是 FlubuCore 脚本，提供 `clean`、`restore`、`build`、`test`、`unit.test`、`integration.test`、`pack`、`nuget.publish` 目标（CI 发布即使用 `nuget.publish`）。

```bash
dotnet tool install --global FlubuCore.Tool --version 8.0.0
flubu build -c=Release
flubu test
flubu pack
```

### 必要配置说明

1. **目标框架**：新增类库请在 `.csproj` 中引入公共配置 `<Import Project="..\..\common.props" />`，不要自行硬编码 `TargetFrameworks`；`Bing.Utils.Text` 是唯一定向 `netstandard2.0` 的包。
2. **版本号**：统一修改 `version.props` 的 `VersionMajor` / `VersionMinor` / `VersionPatch`，不要在单个 `.csproj` 写死版本；包的作者、仓库地址、标签等元信息集中在 `asset/props/package.props`。
3. **输出目录**：`common.props` 将 Debug 产物输出到 `output/debug/`，Release 产物输出到 `output/release/`；打包产物输出到仓库根目录 `nuget_packages/`。
4. **SourceLink**：`asset/props/sourcelink.env.props` 通过 `DotNetCore.SourceLink.Environment` 从 CI 环境变量读取仓库信息，本地构建无需任何额外配置。
5. **Text 词库瘦身开关**：不需要内置拼音 / 简繁词典时，使用该开关会产出不含内嵌资源的 `Bing.Utils.Text.External`（`ChineseConverter` 会被排除）：

   ```bash
   dotnet build ./src/Bing.Utils.Text/Bing.Utils.Text.csproj -c Release -p:BingTextExternalOnly=true
   ```

   该变体可由 `build/Test-TextExternalPackage.ps1` 做消费端校验。
6. **图像处理后端三选一**（三者的 `Identify` / `Process` / `Save` 用法一致，仅入口类名不同）：

   | 包 | 底层 | 适用场景 |
   |---|---|---|
   | `Bing.Utils.Drawing` | `System.Drawing.Common` | Windows 桌面 / 服务端；Linux、Docker 需额外系统依赖且行为受限 |
   | `Bing.Utils.Drawing.ImageSharp` | `SixLabors.ImageSharp` | 纯托管、跨平台，通用场景推荐 |
   | `Bing.Utils.Drawing.SkiaSharp` | `SkiaSharp` | 原生实现、跨平台，适合容器化部署 |

7. **依赖安全**：CI 会执行依赖漏洞审计并与 `build/nuget-vulnerability-baseline.json` 基线比对，禁止新增高危或严重漏洞。本地提交前可先执行：

   ```powershell
   pwsh ./build/Test-NuGetAudit.ps1
   ```

## 使用示例

### 前期准备

以下示例均使用 .NET 8 控制台程序，复制后可直接在 `Program.cs` 中运行：

```bash
mkdir BingQuickStart && cd BingQuickStart
dotnet new console -f net8.0
```

涉及文件的示例请在项目目录准备对应文件（例如一张 `input.png`）。每个小节开头的安装命令即为该示例所需的最小依赖。

### 类型转换 Conv

安装：`dotnet add package Bing.Utils`

```csharp
using Bing.Helpers;

// 转换失败时回退默认值，避免到处写 try/catch
var port = Conv.ToInt("8080", 80);          // 8080
var badPort = Conv.ToInt("abc", 80);        // 80
var price = Conv.ToDecimal("19.90");        // 19.90
var nullable = Conv.ToIntOrNull("");        // null
var birthday = Conv.ToDateOrNull("2026-01-01");

Console.WriteLine($"{port} {badPort} {price} {nullable} {birthday:yyyy-MM-dd}");
```

### Json 序列化 Json

安装：`dotnet add package Bing.Utils`

```csharp
using System.Text.Json;
using Bing.Helpers;

var json = Json.ToJson(new Dictionary<string, object> { ["name"] = "Bing", ["port"] = 8080 });
var dict = Json.ToObject<Dictionary<string, object>>(json);
var pretty = Json.ToJson(dict, new JsonSerializerOptions { WriteIndented = true });

Console.WriteLine(pretty);
// {
//   "name": "Bing",
//   "port": 8080
// }
```

### Id 生成 IdUtils

安装：`dotnet add package Bing.Utils.IdUtils`

```csharp
using Bing.Helpers;

var objectId = Id.CreateObjectId();        // MongoDB 风格 24 位十六进制 Id
var seqId = Id.CreateTimestampId();        // 带时序的字符串 Id，例如 17907704898120001
var snowflakeId = Id.CreateSnowflakeId();  // long 型雪花 Id

// 需要自定义 Id 策略时，先注册生成委托再取 Id
Id.ConfigureString(() => Guid.NewGuid().ToString("N"));
var customId = Id.CreateString();

Console.WriteLine($"{objectId} {seqId} {snowflakeId} {customId}");
```

### 日期时间 DateTime

安装：`dotnet add package Bing.Utils.DateTime`

```csharp
using Bing.Date;

var laborDay = new DateTime(2026, 5, 1);

var isRestDay = laborDay.IsWeekend();           // False（2026-05-01 为周五）
var nextWorkday = laborDay.AddBusinessDays(1);  // 2026-05-04（自动跳过周末）
var monthDiff = new DateTime(2026, 1, 1).GetMonthDiff(new DateTime(2026, 7, 1)); // 6

Console.WriteLine($"{isRestDay} {nextWorkday:yyyy-MM-dd} {monthDiff}");
```

### 集合扩展 Collections

安装：`dotnet add package Bing.Utils.Collections`

```csharp
using Bing.Collections;

var cache = new Dictionary<string, int> { ["bing"] = 1 };

var hit = cache.GetValueOrDefault("bing", 0);       // 1
var miss = cache.GetValueOrDefault("missing", -1);  // -1
var created = cache.GetValueOrAdd("counter", 100);  // 100，同时写入字典
cache.AddValueOrUpdate("bing", _ => 1, (_, old) => old + 1); // bing -> 2

Console.WriteLine($"{hit} {miss} {created} {cache["bing"]}");
```

### 文本处理 Text

安装：`dotnet add package Bing.Utils.Text`（该包定向 `netstandard2.0`，可被 .NET 6 及以上项目直接引用，词典以内嵌资源形式随包分发）

```csharp
using Bing.Text.Chinese;
using Bing.Text.Pinyin;

var pinyin = PinyinUtil.GetPinyin("你好");                // NiHao
var initials = PinyinUtil.GetInitials("你好");            // nh
var polyphone = PinyinUtil.GetContextualPinyin("重庆");   // ChongQing（按词组判定多音字）
var traditional = ChineseConverter.ToTraditional("软件开发"); // 軟體開發

Console.WriteLine($"{pinyin} {initials} {polyphone} {traditional}");
```

> `GetPinyin` 按单字常见读音取值，`GetContextualPinyin` 会结合词组消歧，处理多音字场景请优先使用后者。

### 图片处理 Drawing

安装：`dotnet add package Bing.Utils.Drawing.ImageSharp`（跨平台推荐；Windows 平台可换成 `Bing.Utils.Drawing`，容器化场景可换成 `Bing.Utils.Drawing.SkiaSharp`）

```csharp
using Bing.Drawing;

// 入口类名随后端不同：
//   Bing.Utils.Drawing                -> ImageHelper
//   Bing.Utils.Drawing.ImageSharp     -> ImageSharpHelper
//   Bing.Utils.Drawing.SkiaSharp      -> SkiaSharpHelper
var info = ImageSharpHelper.Identify("input.png");
Console.WriteLine($"{info.Width}x{info.Height} {info.MimeType}");

// 生成 800x600 等比缩略图并写出到文件
var thumbnail = ImageSharpHelper.Process("input.png", new ImageProcessOptions
{
    AutoOrient = true,
    Resize = new ImageResizeOptions { Width = 800, Height = 600, Mode = ImageResizeMode.Contain },
});
ImageSharpHelper.Save(thumbnail, "thumbnail.png");
```

### 安全与密码学 Security

安装：`dotnet add package Bing.Utils.Security`，国密能力需额外安装 `Bing.Utils.Security.Gm`。算法范围与边界详见 [安全与密码学文档](docs/security.md)。

```csharp
using System.Text;
using Bing.Security.Cryptography;
using Bing.Security.Gm;
using Bing.Security.Hashing;
using Bing.Security.Keys;
using Bing.Security.Passwords;

// 摘要
var sha512 = Hashing.ComputeHex("bing-utils", HashAlgorithmType.Sha512);

// AES-256-GCM 认证加密：库会为单次加密生成随机 Nonce
var key = AesKeyGenerator.Generate();       // 默认 256 位
var payload = AesGcmEncryption.Encrypt(Encoding.UTF8.GetBytes("secret"), key);
var plain = Encoding.UTF8.GetString(AesGcmEncryption.Decrypt(payload, key)); // secret

// 口令散列（PBKDF2-HMAC-SHA256），迭代次数可通过 Pbkdf2PasswordHasherOptions 调整
var hasher = new Pbkdf2PasswordHasher();
var encodedHash = hasher.Hash("P@ssw0rd");
var result = hasher.Verify("P@ssw0rd", encodedHash); // PasswordVerificationResult.Success

// 国密 SM3 摘要
var sm3 = Sm3.ComputeHex(Encoding.UTF8.GetBytes("国密"));
```

### 网络与 Http

安装：`dotnet add package Bing.Utils.Http`

```csharp
using Bing.Net;

// 在管道或测试中注入当前线程的 IP，便于链路透传
IpAddressProvider.SetIp("192.168.1.100");
var ip = IpAddressProvider.GetIp();               // 192.168.1.100
var localIps = IpAddressProvider.GetAllLocalIps(); // 本机全部 IPv4 地址

Console.WriteLine($"{ip} {string.Join('|', localIps)}");
```

`Bing.Utils.Http` 中的 `Web` 用于读取当前请求上下文，需在 ASP.NET Core 启动阶段注入访问器后才能使用：

```csharp
// Program.cs
builder.Services.AddHttpContextAccessor();

using var scope = app.Services.CreateScope();
Web.HttpContextAccessor = scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>();

// 之后可在任意位置读取当前请求信息
var host = Web.Host;
var userAgent = Web.Browser;
```

## 安全与密码学

安全与国密包的算法范围、依赖边界和使用限制见 [docs/security.md](docs/security.md)。

```powershell
dotnet add package Bing.Utils.Security
dotnet add package Bing.Utils.Security.Gm
```

两个包支持 `netstandard2.0`、`net6.0`、`net7.0` 和 `net8.0`；国密扩展依赖 BouncyCastle，但公共 API 不暴露其类型。

## 开发环境与依赖

在项目开发和部署过程中，我们使用了以下工具和组件：

- **开发工具**
  - [Visual Studio 2022](https://visualstudio.microsoft.com/zh-hans/vs/)
  - [Resharper Ultimate](https://www.jetbrains.com/resharper/)

> 如果没有标注版本号，则采用最新版本。

## 框架开发流程

我们的开发流程包括以下步骤：

- 1. 搜集：收集常用的工具和组件。
- 2. 整理：对收集的资源进行分类和整理。
- 3. 集成：将整理后的资源集成到框架中。
- 4. 封装：对集成的功能进行封装，提供简洁的接口。

## 作者

简玄冰

## 贡献与反馈
如果你在阅读或使用Bing中任意一个代码片断时发现Bug，或有更佳实现方式，请通知我们。
- **功能完善**：目前，许多功能仅建立了基本结构，细节特性尚未完全实现。如果某个类无法满足您的需求，请告诉我们。
- **提交方式**：您可以通过 Github 的 Issue 或 Pull Reuqest 向我们提交问题和代码。如果您更喜欢使用 QQ 交流，请加入我们的交流群。
- **代码风格**：对于您提交的代码，如果我们决定采纳，可能会进行相应的重构，以统一代码风格。
- **贡献者名单**：对于热心的贡献者，我们会将您的名字列入贡献者名单。

## 贡献指南

参与协作前请先阅读 [docs/CONTRIBUTING.md](docs/CONTRIBUTING.md)，其中包含带源码出处的命名与兼容性约定。以下流程是提交代码的硬性要求。

### Fork 与分支流程

1. 在 GitHub 点击右上角 **Fork**，把仓库复制到自己的账号下。
2. 克隆自己的 Fork，并添加上游仓库：

   ```bash
   git clone https://github.com/<你的账号>/Bing.Utils.git
   cd Bing.Utils
   git remote add upstream https://github.com/bing-framework/Bing.Utils.git
   ```

3. 从最新的 `main` 拉出工作分支（CI 只在 `main` 分支推送时触发）：

   ```bash
   git fetch upstream
   git checkout -b feature/idutils-objectid-parse upstream/main
   ```

4. 分支命名使用 `类型/作用域-简述`：

   | 前缀 | 用途 |
   |---|---|
   | `feature/` | 新增能力 |
   | `fix/` | 缺陷修复，建议带上 Issue 号，例如 `fix/123-snowflake-clock-back` |
   | `perf/` | 性能改动 |
   | `docs/` | 文档改动 |
   | `test/` | 仅补充测试 |
   | `chore/` | 构建、依赖、脚本类改动 |

5. 提交前同步上游，避免冲突：

   ```bash
   git fetch upstream
   git rebase upstream/main
   ```

### 提交规范

提交信息采用 Conventional Commits 风格，作用域填写包名（去掉 `Bing.Utils.` 前缀）：

```text
<类型>(<作用域>): <中文或英文简述>

<正文：说明动机、实现方式与影响范围>

Closes #123
```

- 类型：`feat`、`fix`、`docs`、`refactor`、`perf`、`test`、`build`、`ci`、`chore`、`revert`。
- 主题行控制在 72 字符以内，使用祈使句，结尾不加句号。
- 一个提交只做一件事，不要把重构、格式化与功能改动混在一个提交里。
- 例如：`feat(security-gm): 新增 SM2withSM3 签名握手示例`、`fix(datetime): 修正跨月 GetMonthDiff 计算偏差`。

### 代码风格要求

- 格式化以仓库根目录下的 `.editorconfig` 与 `Bing.Utils.sln.DotSettings` 为准，提交与主线保持一致；注释与文档统一使用简体中文。
- 扩展入口类统一命名为 `XxxExtensions`；方法优先使用 `To*` / `Is*` / `Get*` 语义前缀，可恢复的失败场景优先设计为 `Try*` + `out` + `bool`。
- 无状态公共能力使用 `public static class`（Helper / Provider / Guard），有状态对象使用 `Builder` / `Accessor` / `Factory` 等名词化后缀。
- 配置对象命名为 `XxxOptions`，策略枚举使用 `XxxMode` / `XxxStyle` 命名族。
- 异常语义保持一致：空引用 `ArgumentNullException`、格式或语义非法 `ArgumentException`、越界 `ArgumentOutOfRangeException`、状态不允许 `InvalidOperationException`。
- 类库层禁止直接 `Console` 输出，需要日志时统一走 `LogHelper`。
- 新增 `public` API 必须补齐 XML 注释（`summary` / `param` / `returns` / `exception`），并同步更新 `docs/modules/*.md` 与 `docs/reference/api-reference.md`。
- 不允许破坏既有 `public` 行为；确需调整时先 `[Obsolete("迁移建议", false)]` 标记并给出替代 API。
- `Bing.Utils` 不依赖任何上层模块，模块依赖只能单向向下。

### 测试要求

- 测试技术栈固定为 `xunit` + `Shouldly` + `Moq` + `coverlet.collector`，配置集中在 `common.tests.props`，不要自行替换。
- 单元测试放在 `tests/<Module>.Tests`，依赖外部资源时才新增 `tests/<Module>.Tests.Integration`；不要另起新的目录体系。
- 测试方法名使用英文 `Method_State_Expected`，注释使用中文说明测试目的，结构遵循 Arrange / Act / Assert。
- 每个新增 `public` API 至少覆盖：正常路径、非法输入（断言异常类型与参数名）、边界值（空值、极值、临界）；涉及并发时补充并发测试，涉及外部资源时补充集成测试并说明可重复运行的条件。
- 本地提交前必须通过全量测试：

  ```bash
  dotnet build ./Bing.Utils.sln -c Release
  dotnet test ./Bing.Utils.sln -c Release
  ```

- 性能敏感改动请附上 `benchmarks/Bing.Utils.Benchmark` 的对比数据：

  ```bash
  dotnet run -c Release --project benchmarks/Bing.Utils.Benchmark -- --filter *<BenchmarkClass>*
  ```

- 依赖变更请确认 `pwsh ./build/Test-NuGetAudit.ps1` 未引入新的高危或严重漏洞。

### PR 提交流程

1. 推送分支到自己的 Fork：`git push origin feature/idutils-objectid-parse`。
2. 在 GitHub 发起 Pull Request，目标分支选择上游的 `main`。
3. 按 PR 模板逐项填写：变更说明、影响的模块与包、测试与验证结果、是否需要发版。
4. 等待并检查 CI（`.github/workflows/dotnet.yml`）在 Windows / Linux / macOS 三个平台全部通过；若失败，在本分支补充提交修复，不要强制推送覆盖历史（除非明确需要整理）。
5. 根据 Review 意见补充提交；合并前请自查以下清单：

   ```markdown
   - [ ] 变更为基础设施通用能力，不包含业务语义
   - [ ] 命名符合规范（Extensions / Builder|Accessor|Factory / Options / Mode|Style）
   - [ ] 新增 public API 具备 XML 注释，并已更新 docs/modules 与 docs/reference/api-reference.md
   - [ ] 已覆盖正常路径 / 非法输入 / 边界值，必要时补充并发或集成测试
   - [ ] 未破坏既有 public API，或已给出 Obsolete 迁移说明
   - [ ] 本地已执行 `dotnet test ./Bing.Utils.sln -c Release` 且全部通过
   - [ ] 涉及依赖变更时已执行依赖漏洞审计
   ```

6. 由维护者使用 Squash Merge 合入 `main`；合入后 CI 会自动打包并发布 NuGet 包。

## 免责声明
- **Bug 风险**：尽管我们对代码进行了严格审查，并在自己的项目中使用，但仍可能存在未知的 Bug。如果您的生产系统因此受到影响，Bing 团队不承担责任。
- **API 兼容性**：出于成本考虑，我们不保证已发布的 API 保持兼容。每次更新代码时，请注意可能的变更。

## 开源地址
[https://github.com/bing-framework/Bing.Utils](https://github.com/bing-framework/Bing.Utils)

## License

**MIT**

> 这意味着你可以在任意场景下使用 Bing 应用框架而不会有人找你要钱。

> Bing 会尽量引入开源免费的第三方技术框架，如有意外，还请自行了解。
