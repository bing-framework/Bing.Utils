# src 中文 XML 注释补全计划

> 历史计划与执行记录：下列全量扫描、构建和测试数字属于各批次记录时的候选，不代表当前工作树重新执行。机械扫描候选为零也不能证明文案语义全部正确。2026-10-13 的定向修订见末尾补遗。

## 目标与范围

按 `chinese-comments` 技能补全、修正 `src/**` 手写 C# XML 文档，只改注释，不改代码逻辑、成员签名或运行时行为。扫描排除 `bin`、`obj`、`*.g.cs`、`*.generated.cs`、`*.Designer.cs`、EF Core Migration、ModelSnapshot、自动生成客户端/代理代码及第三方源码。

Roslyn 扫描纳入 774 个源文件，合并默认、.NET 8 和 .NET Standard 条件分支并按物理文件去重。类型声明统计含 `partial` 分部；48 个无摘要候选类声明是分部声明，须核对同类型其余分部后再定缺失。扫描统计用于定位，继承与文案仍须结合代码人工确认。

## 问题统计

| 对象 | 数量 | 缺失候选 |
|---|---:|---:|
| class | 798 | 51 |
| interface | 49 | 0 |
| struct、record、delegate | 20 | 0 |
| enum / 枚举成员 | 66 / 318 | 0 / 3 |
| 构造函数 | 234 | 4 |
| 方法 | 5,976 | 39 |
| 属性 / 索引器 | 911 / 6 | 3 / 0 |
| 字段与常量 | 780 | 41 |
| 运算符及转换运算符 | 104 | 0 |
| **总计** | **9,262** | **141** |

另有 private 方法 35/692、internal 方法 2/94、protected 方法 0/62、static 方法 34/4,605 缺少摘要候选；private 字段 30/512、readonly 字段 11/301、static 字段 10/186、static readonly 字段 9/148、const 27/290。分类存在交叉。

文案与标签扫描发现：215 个构造函数摘要模板候选；720 个属性摘要缺访问语义前缀；4 个单行 summary；78 个超过 80 字符的摘要待人工复核；缺少 `<param>` 813 项、`<typeparam>` 26 项、`<returns>` 3,249 项；构造函数或无结果方法存在 31 个不适用 `<returns>`；2 个参数标签引用不存在的参数；已有 247 个 `<inheritdoc />` 待验证；173 个 override/显式实现未使用 inheritdoc 的候选待确认。

已确认的签名标签错误位于 `Bing.Utils.Http/Bing/Http/Clients/HttpRequest.cs`：`DownloadToResultAsync` 记录了不存在的 `withResult`，`DownloadToDetailedAsync` 记录了不存在的 `ownsDestination`，两处均漏记 `nested`。

## 分批实施清单

1. **接口、抽象类和公共契约**：检查 `Bing.Utils`、`Bing.Utils.Http`、`Bing.Utils.IdUtils`、`Bing.Utils.Net`、`Bing.Utils.Reflection`、`Bing.Utils.Text` 及其他模块的接口和抽象成员；先完善上游参数、泛型参数和返回契约。
2. **DTO、Entity、Options 和枚举**：覆盖全部数据承载类型；重点检查 `Bing.Utils/Bing/Item.cs`、`Bing.Utils.Drawing.Shared/Bing/Drawing`、HTTP `Caching`、Security `Passwords` 与 `Cryptography`，补全默认值、范围、单位及枚举成员语义。
3. **构造函数和属性文案统一**：覆盖所有源项目；核对访问器并统一构造函数模板和获取/设置用语，处理 `Item`、Comments、日期时间、HTTP 及图像选项。
4. **实现类、重写成员和重载方法**：核查所有接口/抽象/override 实现；重点为 IdUtils `CombImplements/Providers`、`CombImplements/Strategies`、`TraceIdAccessor*` 和 HTTP `Caching`、`NetworkInformation`；去除重复契约，核对重载摘要。
5. **private、internal、static 辅助方法**：重点覆盖图像 `Internal`、ImageSharp/SkiaSharp、HTTP `Clients`、IdUtils、Security/Gm、核心 `Helpers` 与 `Reflection`；依据实现说明用途、输入输出、单位和副作用。
6. **字段、常量、缓存键和配置键**：覆盖全部字段；重点检查图像 JPEG/PNG 清理器、`TimestampId`、Security 常量、HTTP `Caching`、Comments 缓存及核心 `Const`；说明用途、作用域、生命周期、边界及默认值。
7. **最终审计和构建验证**：复扫目标框架条件分支与全部标签；检查 XML、cref、继承来源、重载一致性，并验证 C# 非注释 token 未变。

## 文案与继承规范

- 独立 `<summary>` 使用三行格式。实例构造函数写作 `初始化一个 <see cref="当前类型"/> 类型的实例。`；静态构造函数描述静态初始化职责。
- 属性按实际可访问方式以“获取”“设置”“获取或设置”“获取或初始化”起句。
- 类型摘要只说明核心职责；冗长边界、策略、兼容性和副作用移入合法 `<remarks>`。方法摘要聚焦用途；同语义重载保持一致或高度一致。
- 非 `void`、非无结果 `Task`/`ValueTask` 成员补充返回说明；bool 说明 true/false 含义，可空值说明 null 条件。
- 上游契约有效时实现、显式实现、override 优先使用 `/// <inheritdoc />`；差异只追加 remarks。上游契约缺失时先修上游。构造函数、字段、常量和普通私有方法独立注释。
- 所有 `<param>`、`<typeparam>` 名称须与签名完全一致；检查 XML 转义和 cref。无法从代码确认的业务语义不猜测，记录成员和证据缺口。

## 风险与验证

- 处理 `partial` 类型时合并核对分部，避免错误地复制类型摘要。
- `Bing.Utils.Drawing.Shared` 被 ImageSharp 和 SkiaSharp 链接编译；共享物理文件只改一次，并构建两个消费项目。
- 实际目标框架和预处理符号可能影响语义；至少构建 net8.0、net7.0、net6.0、netstandard2.0 可用项目，基线失败单独记录。
- Release 配置屏蔽 `CS1591`；Debug 构建开启 XML 文档，并把文档输出指向 `obj`。
- 构建前记录 `ChangedFiles`、`ChangedProjects`、公共契约/运行路径、目标框架、打包影响、受影响依赖及风险级别；优先运行直接受影响项目的 L0 检查，不运行无关的全量测试或基准。

PowerShell 验证命令（仓库根目录）：

```powershell
[Console]::InputEncoding = [System.Text.UTF8Encoding]::new($false)
[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)
$OutputEncoding = [System.Text.UTF8Encoding]::new($false)

$projects = Get-ChildItem ./src -Recurse -Filter '*.csproj' |
    Where-Object { $_.BaseName -eq $_.Directory.Name }
foreach ($project in $projects) {
    foreach ($framework in @('net8.0', 'net7.0', 'net6.0', 'netstandard2.0')) {
        dotnet build $project.FullName -c Debug -f $framework `
            '-p:GenerateDocumentationFile=true' `
            "-p:DocumentationFile=obj/comments-audit/$framework/$($project.BaseName).xml" `
            '-p:NoWarn=NETSDK1138' --nologo
        if ($LASTEXITCODE -ne 0) { throw "构建失败：$($project.Name) / $framework" }
    }
}
git diff --check
```

## 前序实施记录

- 完成接口/抽象契约、DTO 与 Options、构造函数及属性文案、实现和重写成员、私有辅助方法及枚举成员的补充与校正；字段和常量由批次 6 单独审计。涉及 `Bing.Utils`、Collections、Comments、DateTime、Drawing 及其 ImageSharp/SkiaSharp 适配器、Guard、HTTP、IdUtils、Net、Reflection、Security/Gm、Text 等 14 个源项目。
- 修复 HTTP 下载方法的参数标签；COMB 实现改用继承注释并明确 SQL 300 单位/秒及 Unix 毫秒语义；补充图像清理器、SM3、JSON、Comments、缓存和私有辅助成员说明；将雪花 WorkerId 位布局移入 remarks。
- 对 216 个变更 C# 文件进行 Roslyn 非注释 token 序列比较，差异为 0；非分部声明缺少 summary/inheritdoc 候选为 0。15 个机械属性前缀候选经人工复核，均为只读语义、有效 inheritdoc 或扫描文本解析误报。
- 14 个受影响项目在 Debug 下分别针对 `net8.0`、`net7.0`、`net6.0`、`netstandard2.0` 构建通过，共 56 组；生成的 100 个 XML 文档均可解析，`git diff --check` 通过。
- 构建仍报告若干仓库既有 XML 文档与分析器警告；本轮没有修改运行时代码、签名、项目配置或依赖。最终复核以实际变更 diff 与 Roslyn token 比较为准。

## 批次 2 执行记录

- 覆盖 DTO、Options 和枚举重点文件：`Item`、JSON/HTTP 重试与缓存选项、图像元数据与验证码选项、AES-GCM 流选项、PBKDF2 选项，以及日期格式、区间、反射选项、HTTP 请求结果和查询操作符枚举。
- 根据实现补充缓存容量的正数约束、估算大小非负约束、重试默认值与有效范围、密码派生参数默认值/字节单位，以及 `Item.Disabled` 的 true/false/null 含义。修正 `CaptchaOptions` 各后端实际支持范围和 `FontWidth` 固定默认值的说明。
- 修正 `RangeMode.OpenClose` / `CloseOpen` 边界说明、数学表达式 `EOF` 成员含义、反射选项歧义策略、`DateTimeOutputStyles` 长短格式的区域设置语义、`TimeFormatType` 的格式类别描述，并将模式字符串细节移入 remarks；整理 `HttpRequestCompletion` 的空白 XML 行。`Operator.In/NotIn` 的 XML 摘要已中文化；其 `Description` 特性仍是运行时元数据，不在本注释批次修改。
- Roslyn 扫描确认 66 个枚举和 318 个成员均有摘要；59 个 DTO/Options 等候选类型的摘要及数据成员无缺失。剩余 7 项均是 `HttpRequest<TResult>` partial 服务声明，列入第四批而非数据模型。
- 245 个当前变更 C# 文件的非注释 token 序列比较无差异。核心、HTTP、Security、ImageSharp、SkiaSharp 5 个受影响项目的 Debug `net8.0` 文档构建通过。构建存在其他既有 XML/分析器和 ImageSharp 依赖警告；批次目标文件未遗留新增 XML 诊断。`Types.Of.cs` 的 nullable `cref` 初次诊断已改为 `<c>int?</c>` 并复建通过。

## 历史批次执行状态

| 批次 | 状态 | 本轮记录 |
|---|---|---|
| 1. 接口、抽象类和公共契约 | 已完成 | 补齐接口/抽象契约中 16 个缺失参数标签和 246 个缺失返回标签；Text Joiner/Splitter 共 66 项（含最后复核补齐的 2 项）、HTTP 请求与下载能力、FTP、参数解析/格式化、Try 结果、签名、转换、雪花及 COMB 契约均已复扫。Roslyn 合约审计的 summary、param、typeparam、returns 缺失候选为 0。 |
| 2. DTO、Entity、Options 和枚举 | 已完成 | 补充/校正文档默认值、范围、单位、可空语义与后端适用范围；复核 66 个枚举和 318 个成员，修复已确认的语义错误。Roslyn 数据类型摘要候选 59 个，无缺失项；7 个 HTTP partial 服务类型摘要缺失项转入后续实现类批次。 |
| 3. 构造函数和属性文案统一 | 已完成 | 统一 26 个构造函数摘要及 7 组属性摘要；Roslyn 复扫的构造函数模板、属性/索引器访问语义、属性摘要缺失和长摘要候选均为 0。 |
| 4. 实现类、重写成员和重载方法 | 已完成 | 59 个文件中的 170 个显式实现/override 候选改用 inheritdoc；另修正 1 个隐式 ITraceIdMaker 实现并清理 2 处重复 inheritdoc。最终扫描候选与重复项均为 0。 |
| 5. private、internal、static 辅助方法 | 已完成 | 已按 Batch 5 范围完成定向审计和注释补齐；全量候选复算列入最终审计。 |
| 6. 字段、常量、缓存键和配置键 | 已完成 | 复核全部纳入源码的字段摘要候选；补齐 BSS2 常量 11 项及 IPv6 生成器 .NET 6+ 分支的加密随机数生成器字段 1 项。其他候选均已有摘要。 |
| 7. 最终审计和构建验证 | 已完成 | 复核全量注释差异、排除范围、字段候选、条件编译和四框架构建；详见批次 7 执行记录。 |

## 批次 3 执行记录

- 覆盖 `Bing.Utils`、`Bing.Utils.Http`、`Bing.Utils.Security` 中本批涉及的 18 个物理 C# 文件；统一 26 个实例构造函数摘要为 `初始化一个 <see cref="当前类型"/> 类型的实例。`，并修正 7 组属性摘要的访问语义，包括条件编译分支、init-only 与只读属性。未修改成员签名或实现。
- 对 774 个纳入文件重新进行 Roslyn 条件分支扫描；构造函数模板候选、属性前缀候选、索引器前缀候选、缺失属性/索引器摘要及超过 80 字的属性摘要均为 0。新增 inheritdoc 数量为 0。
- Roslyn 比较当前 247 个变更 C# 文件在默认、net8.0、net7.0、net6.0 和 netstandard2.0 符号集下的非注释 token，差异为 0。
- `Bing.Utils.Security` 的四个目标框架 Debug 文档构建通过，生成 XML 均可解析；`Bing.Utils` 的隔离 `net8.0` Debug 构建通过，XML 可解析。`Bing.Utils.Http` 在隔离输出构建时，MSBuild 的项目框架发现阶段返回失败，日志无警告或错误诊断，未生成 XML，因此该项目及框架仍待后续重试。核心 `net8.0` 构建报告 189 条既有 XML/分析器/过时 API 警告；日志含源文件中已有的空白 XML 注释格式和 cref 诊断，需在最终审计区分基线与新增问题。
- 构建输出放入 `ai_docs/.verification` 临时目录，以避开仓库 `obj` 中 DLL/XML 的访问拒绝。首次还原亦遇到 NuGet 配置锁访问拒绝；后续使用已有还原资产运行。未修改项目配置、运行时代码或依赖。

## 批次 4 执行记录

- 覆盖实现/重写候选位于 `Bing.Utils`、`Bing.Utils.Collections`、`Bing.Utils.Http`、`Bing.Utils.IdUtils`、`Bing.Utils.Text` 的 59 个物理文件，按当前 Roslyn 条件分支扫描处理 170 个非 inheritdoc 候选；把显式接口实现、抽象成员实现和 override 的契约文档改为 `<inheritdoc />`。另将 `TraceIdAccessor.DefaultTraceIdMaker.Create()` 改为继承 `ITraceIdMaker.Create()` 契约，并删除 SQL/Unix COMB 策略中重复的 inheritdoc。
- 保留实现特有的 remarks、example 和 exception；为 DateTimeRange/CodeTimer 格式、文件大小格式、MSSQL/PostgreSQL COMB 字节布局、临时路径释放行为及显式已知类型解析补充 remarks。HTTP Caching 子范围复核确认实现、Dispose 和重载契约已有有效 inheritdoc，无需修改。
- 复扫 774 个源文件及默认、net8.0、net7.0、net6.0、netstandard2.0 条件符号：override/显式实现缺失 inheritdoc 候选为 0，重复 inheritdoc 为 0。由于预处理分支与已完成批次会影响候选去重，本轮扫描为 170 项，计划基线为 173 项；最终当前源码扫描为 0，差异不构成遗留项。
- Roslyn 对 263 个变更 C# 文件在 5 组符号集下比较非注释 token，共 1,315 组，差异为 0；`git diff --check` 通过。IdUtils、Collections、HTTP、Text 及其核心依赖的 Debug/net8.0 文档构建通过；0 错误，警告数分别包括核心依赖的既有 XML/分析器警告，最高为 HTTP 构建 514 条。隔离 intermediate path 构建因 MSBuild 框架发现阶段无诊断失败，改用常规项目引用解析后通过。生成的 `Bing.Utils.xml` 与 `Bing.Utils.IdUtils.net8.0.xml` 均可解析。

本轮使用的计划文件为 `ai_docs/plans/chinese-comments-plan.md`；用户指定的 `docs/plans/chinese-comments-plan.md` 在工作树中不存在。未创建第二份计划副本。批次 1 的六个受影响项目均完成 Debug `net8.0` 构建；`Bing.Utils`、`Bing.Utils.Http`、`Bing.Utils.IdUtils` 构建成功但保留仓库 XML/分析器警告，`Bing.Utils.Net`、`Bing.Utils.Reflection`、`Bing.Utils.Text` 为零警告。237 个变更 C# 文件的 Roslyn 非注释 token 对比为零差异，`git diff --check` 通过。

验收要求：纳入范围的缺失、错误、重复、过时或无意义文案均已处理或说明理由；所有标签与签名及继承契约一致；XML 文档构建无新增警告；diff 仅包含注释及本计划；排除文件未被触及；非注释 token、签名和公开 API 保持不变。

## 批次 5 执行记录

- 在原有共享图像几何、ImageSharp/SkiaSharp 编码及 BSS2 helper 工作基础上，扩展并完成对 HTTP 断点续传与响应流、ImageSharp/SkiaSharp 像素和格式 helper、共享图像缓冲/元数据清理/验证码/连通区域处理、IdUtils 时间戳与 GUID helper、核心 Helpers 和 Reflection helper 的定向审核。涉及文件以 `HttpRequest.ResumableDownload.cs`、`ResponseBodyReadStream.cs`、`ImageSharpHelper.cs`、`SkiaSharpHelper.cs`、`GrayImageBuffer.cs`、`PngMetadataSanitizer.cs`、`JpegMetadataSanitizer.cs`、`CaptchaCodeGenerator.cs`、`FastPathMatcher.cs`、`Env.cs`、`Encrypt.cs`、`TypeReflections.*`、IdUtils 的 Snowflake/GUID provider 为主。
- 补齐私有、内部及静态辅助方法的摘要、实际参数、泛型参数和返回值说明；修正下载恢复候选判定、If-Range 选择、图像字节/尺寸转换、颜色矩阵、验证码绘制、元数据清理边界、时间戳单位及反射返回语义。修正 `ValidateAmount` 对非负值而非比例的描述、两个 HTTP 下载方法过时的参数标签，以及 `InitProcess`、Env 平台 helper 和响应流 XML 格式问题。无新增 inheritdoc；未改动方法签名、运行时代码、字段、常量或配置键。
- 按正确项目路径检查 `Bing.Utils.Security.Gm`，未发现本批需处理的辅助方法标签缺口。共享源码只编辑物理文件一次，并由 ImageSharp、SkiaSharp 消费项目共同构建验证。
- 原始 588 项 `<param>`/`<typeparam>`/`<returns>` 数值是实施前的机械候选基线，不再作为当前剩余项计数；本批按候选集中目录和文件逐项核对实现与签名。全量 774 文件的候选复算、条件编译分支扫描及非注释 token 对比留在批次 7 执行，不据此提前开始批次 6。
- 最终构建通过：`Bing.Utils`、`Bing.Utils.Http`、`Bing.Utils.Drawing.ImageSharp`、`Bing.Utils.Drawing.SkiaSharp`、`Bing.Utils.IdUtils`、`Bing.Utils.Reflection` 均完成 Debug `net8.0` 文档构建；6 份 XML 均可解析。最终筛查未发现本批文件的 XML 文档编译诊断；仓库仍有既有加密 API 过时警告和 ImageSharp 依赖漏洞警告。`git diff --check` 通过。
- Batch 5 完成；字段、常量、缓存键和配置键仍未开始，按顺序留待下一批处理。

## 批次 6 执行记录

- 按字段、常量、缓存键和配置键范围复核所有纳入的手写 C# 字段；重点复核图像、时间戳、Security、HTTP 缓存、Comments 缓存及核心常量。扫描并人工排除属性和 ReSharper 辅助注释造成的候选误报后，确认 12 项字段摘要缺失：BSS2 格式常量 11 项，以及 IPv6 生成器 `NET6_0_OR_GREATER` 分支中的加密随机数生成器字段 1 项。
- 为 BSS2 版本、算法和记录类型标识，以及头、盐、Nonce 前缀、记录头、最终记录数据和认证数据长度补充三行摘要；长度均明确为字节。用途及长度组成已根据 `AesGcmStreamV2`、`Bss2AssociatedData` 和 `Bss2NonceSequence` 的读写位置核对。为 IPv6 生成器的两个条件分支统一补充随机数用途摘要，并修正旧框架分支已有 summary 的三行格式。
- 其余字段候选均已有 summary；未发现其他缺失字段、缓存键或配置键注释。未新增 inheritdoc，未修改字段、常量、配置键或缓存键的值和行为。待确认项：无。
- 目标项目 Debug `net8.0` 文档构建通过：`Bing.Utils.Security` 为 0 警告、0 错误；`Bing.Utils.Http` 为 655 警告、0 错误。HTTP 构建输出含核心与 HTTP 其他源文件的既有 XML 文档诊断；本批修改的两个字段文件没有对应诊断。`output/debug/net8.0/Bing.Utils.Security.xml` 与 `Bing.Utils.Http.xml` 均可解析，分别包含 255 和 1,025 个成员项。首次在线构建因 NuGet 配置锁访问失败，随后使用现有还原资产完成构建。
- 复扫后字段缺失摘要候选为 0；summary 均简短，仅将旧目标框架分支的线程实例差异放入 remarks；重载一致性不适用于本批字段；private 字段无遗漏。新增 12 个 summary，新增 inheritdoc 为 0。差异审计确认新增源码行为均为 XML 注释；IPv6 文件中 2 行示例空注释删除属于本轮前已存在的工作区差异。`git diff --check` 通过。待确认项：无。

## 批次 7 历史执行记录

- 完成对 774 个纳入 C# 文件及生成/迁移/第三方排除项的最终范围复核；当前受注释任务影响的 285 个 C# 文件中，没有触及 `*.g.cs`、`*.generated.cs`、`*.Designer.cs`、Migration、ModelSnapshot 或非 C# 文件。
- 当时复核类型、构造函数、属性、方法、字段、重载及继承注释的前序 Roslyn 扫描记录：非分部类型摘要、构造函数模板、属性/索引器访问语义、字段缺失、override/显式实现 inheritdoc 和重复 inheritdoc 候选均为 0。前序记录还覆盖 774 个文件的五组条件符号。后续提交复核仍发现机械拼接语病及前缀复制方向错误，说明这些扫描结果没有覆盖全部文案语义；详见末尾补遗。
- 对当前全部 C# 源差异执行逐行审计，非注释代码行变更数为 0；本批未修改 C# 文件，新增注释数为 0，新增 inheritdoc 数为 0。批次 6 的 12 个新增 summary 和 0 个 inheritdoc 保持不变。排除文件差异数为 0。尝试在 PowerShell 中加载 SDK Roslyn 以重跑最终 token 比较时，宿主已加载程序集与 SDK 程序集版本冲突；未伪报本轮新 Roslyn 结果。此前各批次记录的 Roslyn 非注释 token 比较均为 0 差异，当前源差异的逐行检查亦确认仅为注释。
- 为最后涉及字段注释的 `Bing.Utils.Http` 和 `Bing.Utils.Security` 执行 Debug 文档构建，`net8.0`、`net7.0`、`net6.0`、`netstandard2.0` 共 8 组均成功、0 错误。Security 四组均为 0 警告；HTTP 四组分别有 466、655、654、645 条既有 XML/分析器警告。8 份生成 XML 文档均可解析；HTTP 各有 1,000、1,000、1,025、1,000 个成员项，Security 各有 242、242、255、81 个成员项。前序批次已记录受影响项目的四框架构建和共享绘图源码两个消费项目构建；本轮重新验证最后涉及字段注释的两个项目。
- `git diff --check` 通过。C# 差异仅含注释和必要注释格式调整；计划文件记录批次状态及验证证据。最终发现：HTTP 项目仍存在非本轮修订文件上的 XML/分析器警告，具体警告基线沿用批次 3–6 记录，当前未将其扩大为无关修复项。最终待确认项：无。

## 2026-10-13 定向复核补遗

- 基准：`3a1ef0c` 与本次未提交工作树；只处理剩余 8 个源码文件的注释及未提交文档。没有重新执行 774 文件扫描或历史四框架矩阵。
- Completed：修正 Env、CaseFormatter、TypesVal 等机械拼接语病；明确属性、索引器和数组副本语义；Item.Disabled 的可空状态移入 value；字典索引器改用有效继承契约。
- Completed：纠正 ReadPrefix 的复制方向、目标缓冲区和返回值说明；补充 CountRead 的大小限制异常；修正 Web.IsLocal 的 value 和实际异常契约，以及 BashInfo 返回标签缩进。
- Change Impact：源码涉及 Core、Collections、Text、HTTP 四项目；仅注释与 Markdown，公开签名、运行路径、目标框架和打包均未改变，风险 LOW。
- L0：四项目均以 Debug/net8.0、现有还原资产构建成功；警告数依次为 189、0、1、466，与前次记录一致。四份生成 XML 均可解析；源码差异中的非注释代码行变更为 0。
- L1–L5：未运行；仅文档和注释修订，不需要功能测试或性能基准。历史功能测试记录没有提升为本次证据。
- Open Actionable：本次定向范围无剩余项；Blocked Approval、Blocked External、Not Applicable、Accepted Limitations、Verified Boundaries：本次无。
- Deferred：其他成员既有 XML/分析器警告及历史 HTTP 计划中的未完成能力不在本次修复范围；未实施这些功能。
- No-Progress Check：CHANGED；Next Action：STOP。修订保留为未提交内容，历史批次数字仅作为历史记录保留。

---

## tests/** 中文 XML 注释补全计划（2026-10-13）

> 2026-10-14 计划复核：本轮只核查计划、原始扫描数据与当前目录清单，不修改 C#，不执行构建、测试或实施批次。当前仍为 522 个原始 C# 文件、521 个路径过滤后文件；以下问题数量是 2026-10-13 实施前基线，不是当前剩余数量。批次 1 及另行授权的冲突修复结果保留在执行记录中，批次 2–7 未开始。

### 目标与阶段边界

为 `tests/**` 中手写 C# 类型及成员补全准确的简体中文 XML 注释，统一摘要、继承注释和签名标签。本节是独立的测试目录计划，前文源码计划及历史验证记录不作为本次测试目录的验证证据。

本阶段仅扫描、分析并保存计划；不修改 C#、项目配置、测试行为或 Git 暂存区，不执行提交。后续实施也只允许注释及必要的注释格式调整，不改变访问级别、成员签名、特性、条件编译、测试数据及断言。

实施采用“先上游契约、后实现注释”的顺序，以项目和共享辅助文件为小批次。技术依据为 C# XML 文档、SDK 8.0.424 Roslyn 语法扫描、条件编译分支和人工语义复核。使用 `chinese-comments` 的文案与继承规则组织检查，并按 `writing-plans` 组织可核验的实施任务。

### 扫描范围与方法

- 根目录：`H:/Bing_Framework/Bing.Utils/tests`。覆盖测试类、测试方法、嵌套辅助类型、测试基类、集合定义、数据提供器、样例模型及支持项目；不因 private、internal、protected、static、readonly 而跳过成员。
- 原始 C# 文件 522 个，排除生成资源文件 `tests/Bing.Tests.Samples/Bing/Tests/TestResource.Designer.cs` 后，扫描 521 个物理文件，分布于 19 个含源码目录。`tests/**` 下另有 21 个项目文件，项目文件仅用于确认编译边界。
- 排除 `bin/**`、`obj/**`、`*.g.cs`、`*.generated.cs`、`*.Designer.cs`、EF Core Migration、ModelSnapshot、自动生成客户端、代理代码及第三方源码。路径过滤后还检查生成文件头；未发现其他带自动生成文件头的候选文件。实施前仍须复核新增文件和生成标记。
- 按物理文件及声明位置去重，避免 HTTP 集成测试链接单元测试源码时重复计数、重复修改。`Bing.Tests.Samples` 虽不是测试运行项目，仍属于用户指定范围。
- Roslyn 分别解析默认、DEBUG、netcoreapp3.1、net5.0、net6.0、net7.0、net8.0、netstandard2.0 及相关框架符号，再合并声明。配置符号不能替代真实项目编译，后续须按受影响项目复核条件分支。
- 共识别 9,900 个声明；`UnixTimeStampTest.cs` 有语法冲突，暂不纳入注释问题统计，其余 520 个文件包含 9,849 个可解析声明。目录清单保留该文件，避免遗漏阻断项。
- 自动统计属于候选清单：缺摘要不一定等于完全无注释，长摘要、继承关系和机械文案需要人工确认；同一成员可命中多项。参数及泛型参数缺口按标签次数计数，不是文件数。访问修饰符分类以语法为主，默认访问级别和隐式接口实现仍需语义模型复核。
- 本次只执行静态扫描与代表文件审阅，没有运行构建、单元测试或集成测试。

### 问题统计（实施前基线）

下表排除上述不可安全解析文件；“缺摘要”包含没有有效摘要且没有可用继承注释的候选声明。

| 声明类别 | 声明数 | 缺摘要候选数 |
| --- | ---: | ---: |
| 类 | 812 | 201 |
| 接口 | 36 | 25 |
| 结构体 | 19 | 6 |
| record | 1 | 1 |
| 枚举 | 20 | 14 |
| 枚举成员 | 54 | 51 |
| 构造函数 | 182 | 47 |
| 方法 | 7,809 | 2,557 |
| 属性 | 602 | 374 |
| 索引器 | 5 | 5 |
| 事件 | 1 | 1 |
| 字段及常量 | 308 | 183 |
| 合计 | 9,849 | 3,465 |

| 文案或 XML 检查 | 候选数 | 统计解释 |
| --- | ---: | --- |
| 构造函数模板不统一 | 47 | 针对已有构造函数摘要；不与缺摘要数相加 |
| 属性访问前缀不规范 | 191 | 需要结合 get/set/init 核对 |
| 非三行 summary | 129 | 排除有效 inheritdoc 后的格式候选 |
| 冗长 summary | 391 | 长度启发式，人工判断是否下沉 remarks |
| 机械化文案 | 6 | 关键词候选，未覆盖全部语义问题 |
| 缺 param | 2,569 | 缺失的参数标签次数 |
| 缺 typeparam | 19 | 缺失的泛型参数标签次数 |
| 缺 returns | 226 | 需再核对实际返回类型与继承契约 |
| 不适用的 returns | 3 | 普通 Task/ValueTask 等候选 |
| XML 解析异常 | 20 | 提取与 XML 解析候选，须核实原始注释 |
| override/显式实现未用 inheritdoc | 21 | 未包含全部隐式接口实现，须查上游契约 |

补充字段与辅助方法统计：字段中显式 private 字段 172 个、缺摘要 52 个，34 个常量有 28 个缺摘要，15 个 static readonly 字段有 6 个缺摘要，142 个 readonly 字段有 38 个缺摘要；这些集合相互重叠。方法中显式 private 方法 134 个、缺摘要 55 个，protected 方法 69 个、缺摘要 2 个，static 方法 183 个、缺摘要 74 个。键名启发式识别 5 个字段候选，其中 1 个缺摘要；该数字不能代表缓存键、配置键的完整语义审计。

基线原始数据位于本机临时目录 `bing-tests-comments-d3597ec9f32b4106b5187ee8ca44ccae/scan.json`。2026-10-14 从该数据重新汇总确认：排除当时冲突文件后有 9,849 个有效声明、3,465 个缺摘要候选、2,569 个缺 param、19 个缺 typeparam、226 个缺 returns、391 个长摘要及 20 个 XML 候选；将原始 Doc 中的字面量换行还原后，非三行 summary 为 129 个。临时数据不作为永久仓库依赖；后续实施须按成员名与当前文件重新定位，不直接套用旧行号或将这些数量当作实时缺口。

扫描发现 275 处 inheritdoc 和 16 个 partial 类型声明。这是现有用法数量，不是已验证正确的数量；分部类型需按完整类型检查，避免各部分重复补摘要。

### 按目录的工作量清单

以下为物理文件与合并声明的原始目录统计，包含冲突文件；因此声明合计为 9,900，不能与上表的有效声明合计直接比较。

| tests 下目录 | 文件数 | 声明数 | 缺摘要候选数 |
| --- | ---: | ---: | ---: |
| Bing.Tests.Samples | 11 | 102 | 25 |
| Bing.Utils.Collections.Tests | 24 | 305 | 6 |
| Bing.Utils.Comments.Tests | 5 | 30 | 2 |
| Bing.Utils.DateTime.Tests | 32 | 318 | 50 |
| Bing.Utils.Drawing.ImageSharp.Tests | 17 | 245 | 173 |
| Bing.Utils.Drawing.SkiaSharp.Tests | 9 | 161 | 87 |
| Bing.Utils.Drawing.Tests | 7 | 70 | 4 |
| Bing.Utils.Guard.Tests | 3 | 12 | 0 |
| Bing.Utils.Http.Tests | 41 | 1,397 | 103 |
| Bing.Utils.Http.Tests.Integration | 25 | 262 | 22 |
| Bing.Utils.IdUtils.Tests | 22 | 270 | 6 |
| Bing.Utils.Net.Tests | 2 | 22 | 0 |
| Bing.Utils.Net.Tests.Integration | 3 | 44 | 23 |
| Bing.Utils.Reflection.Tests | 10 | 218 | 68 |
| Bing.Utils.Security.Gm.Tests | 3 | 41 | 25 |
| Bing.Utils.Security.Tests | 17 | 113 | 12 |
| Bing.Utils.Tests | 218 | 5,353 | 2,579 |
| Bing.Utils.Text.Tests | 10 | 131 | 0 |
| BingUtilsUT | 62 | 755 | 280 |

缺摘要为 0 不表示文案审计通过，例如 Text 测试仍有冗长摘要及 XML 候选问题。`Bing.Utils.Extra.Tests`、`Bing.Utils.DependencyInjection.Tests` 本轮未发现符合范围的手写 C# 文件，不虚构注释工作量。

### 文案规范与已确认问题

- summary 使用三行结构，正文只说明成员核心用途。构造函数统一为 `初始化一个 <see cref="当前类型"/> 类型的实例。`，实际 cref 必须指向当前类型；参数差异由 param 或必要的 remarks 说明。
- 属性、索引器结合访问方式使用“获取……”“设置……”“获取或设置……”“获取或初始化……”。返回副本、可空含义、状态条件等放入 value 或 remarks，不能仅根据属性名称猜测。
- 方法不使用“无参方法”“带参方法”“提供测试支撑逻辑”等无意义模板。测试方法写清验证行为或边界，辅助成员说明其在测试中的真实用途，不把测试探针描述为生产功能。
- 同语义重载保持一致或高度一致的摘要；不同语义明确差异。不要为了摘要一致而掩盖同步/异步委托或参数语义差异。
- 算法细节、场景列表、长篇步骤和限制说明经确认后下沉 remarks；391 个长摘要只是待审阅清单，不能机械截断。
- param、typeparam 名称、数量和含义必须匹配实际签名；返回值标签不得为空。非泛型 Task/ValueTask 不添加 returns，Task<T>/ValueTask<T> 说明异步结果；bool、可空值、集合和枚举按实际契约说明。
- 注释中的 XML 标签、cref、paramref、typeparamref 必须可解析。保留真实兼容性测试说明，不能因出现“过时 API”就自动删除。

已人工确认的代表项：

| 文件及成员定位 | 问题 | 后续处理 |
| --- | --- | --- |
| `tests/Bing.Utils.Reflection.Tests/Infrastructure/TestTypes.cs:10` 起，SampleEntity、Greet、Echo<T> | 单行摘要；“无参/带参”文案；缺参数、泛型参数、返回标签 | 按真实问候和泛型回显行为补全 |
| 同文件 `:62` 起，SampleGeneric<T>、AbstractEntity.GetName、VirtualPropEntity | 泛型、抽象契约和属性注释缺失 | 先补上游契约和数据语义 |
| `tests/Bing.Utils.Tests/Bing/Reflection/TypeVisitMethodsAndPropertiesBoundaryTest.cs:215` 起 | Execute 重载使用机械模板；基类虚成员、抽象属性无上游注释 | 先补基类，再统一实现注释 |
| `tests/BingUtilsUT/Samples/JsonTestSample.cs:29` 起 | 属性访问语义不规范，Create 缺 returns，CreateToInterface 的 returns 为空 | 规范接口、属性与创建结果 |
| `tests/Bing.Utils.Http.Tests.Integration/Samples/CustomerDto.cs:3` 起 | DTO 属性摘要没有访问语义，XML 闭合标签空格不统一 | 统一属性前缀和格式 |
| `tests/Bing.Tests.Samples/Bing/Tests/Samples/EnumSample.cs:8` 起、`tests/BingUtilsUT/Samples/NormalEnums.cs:3` 起 | 枚举成员或类型无注释 | 说明测试值；不编造业务含义 |
| `tests/Bing.Utils.Http.Tests/Bing/Http/HttpJsonArrayStreamingTest.cs:238` 起 | Item 属性用“保存”；State 成员缺注释 | 规范属性和枚举成员 |
| `tests/Bing.Utils.Tests/UnitTest1.cs:340` FileCombine、`tests/Bing.Utils.Tests/Bing/CharExtensionsTest.cs:8` 起 | 空 param；机械测试摘要 | 补实际参数含义和验证目的 |
| `tests/Bing.Utils.Tests/Bing/Reflection/TypeReflectionsIsTaskAndTypeConverterTests.cs:22`、`:303` | 注释正文含替换字符 `�` | 根据上下文恢复文本，禁止猜测批量替换 |

### inheritdoc 问题与处理顺序

1. 对接口实现、显式接口实现、抽象成员实现及 override，先定位有效上游注释；上游完整时使用 `/// <inheritdoc />`，实现差异追加 remarks，避免复制摘要及签名标签。
2. 上游缺注释时，优先在本范围内补接口、抽象或虚成员；若上游位于 src，仅只读核对并记录依赖，不能擅自扩大修改范围。不得把没有文档来源的 inheritdoc 当作补全结果。
3. 隐式接口实现必须通过语义关系复核，不能只靠方法名称判断。16 个 partial 声明需合并检查。

确认的重复/缺失例子：

- `tests/Bing.Tests.Samples/Bing/Tests/Samples/ParameterFormatterSample.cs:13`、`:18` 的 override 属性复制了上游已有摘要，应使用 inheritdoc。
- `tests/Bing.Utils.Tests/Bing/Tests/Samples/TestSample.cs:45` 起：先补 `IA.Value`，再让 `A.Value` 继承；泛型样例的 typeparam 同步补全。
- `tests/BingUtilsUT/Samples/JsonTestSample.cs` 的 `JsonTestSample.Name` 复制接口摘要，应先规范 `IJsonTestSample.Name` 再继承。
- `tests/Bing.Utils.Tests/Bing/Reflection/TypeVisitMethodsAndPropertiesBoundaryTest.cs:243`、`:278` 等 override 的上游契约尚缺失，必须按依赖顺序处理。
- `WebRegressionTest`、`IdTest`、`DirectoryHelperTest`、`TimeTest` 中 Dispose 的复制摘要，以及绘图集成测试、`IOExtensionsTests` 中无注释的 Dispose，核对 IDisposable 后使用 inheritdoc；清理目录、恢复静态状态等特有行为放入 remarks。
- `tests/Bing.Utils.Security.Gm.Tests/Bing/Security/Gm/GmStreamApiTests.cs:69` 起的 Stream 重写成员，以及 Collections `ArraysTest.cs:577`、`:587` 的 Equals/GetHashCode，列入继承复核清单。

保留正确用法作为对照：`TypeVisitMethodsTest.cs` 的 `MethodTestDerived.VirtualMethod`、HTTP `HttpCapabilityExtensionsTest.cs` 的 LegacyHttpClient/LegacyHttpRequest、`WebTest.cs:83` 的 Dispose。21 个语法候选之外还需检查隐式实现，不把现有 275 处 inheritdoc 一律重写。

### 分批实施清单

规划时所有任务均未实施。当前批次 1 已完成：注释、审计和 8/8 目标项目构建均通过；原有冲突经用户另行授权修复后，构建阻断已解除，详见下方收尾记录。批次 2–7 未开始。每批先审阅候选、再按小模块补注释、最后验证并记录数量。

批次范围与清单规则：批次 2 按下列模型/枚举路径及扫描中识别的数据类型组织；批次 3–6 均覆盖“按目录的工作量清单”中的 19 个目录，仅按成员类别划分，所列重点文件不构成范围上限。每批获准实施后，先在本计划相应批次小节列出完整的当前物理文件路径、目标类型/成员、所属项目及目标框架，再开始编辑；链接源码只列一次，记录全部消费项目。无法分类的成员写入待确认清单，不根据名称机械归入模型或键值。批次 7 消费前六批累计清单，并与全范围文件清单交叉核对，防止只审计已经修改的文件。本轮仅制定这项流程，不生成实施修改或开始下一批。

#### 批次 1：接口、抽象类和公共契约

- [x] 审阅 `tests/BingUtilsUT/Samples/NormalInterfaceClass.cs`、`JsonTestSample.cs`、`tests/Bing.Utils.Tests/Bing/Tests/Samples/TestSample.cs` 的接口、泛型参数和成员契约。
- [x] 审阅 `tests/Bing.Utils.Reflection.Tests/Infrastructure/TestTypes.cs`、`Bing/Reflection/TypeVisitAdvancedTests.cs` 及 `tests/Bing.Utils.Tests/Bing/Reflection/TypeVisit*` 的抽象类、接口和虚成员。
- [x] 审阅各目录的 TestBase、fixture、collection 定义和公开测试辅助 API，包括 `tests/Bing.Utils.Http.Tests/Bing/Helpers/WebHttpContextCollection.cs`。
- [x] 建立上游成员与实现成员对应清单；先补契约，不改变反射探针形状、接口继承或访问级别。
- 完成条件：36 个接口、抽象/虚契约及公共辅助契约全部分类；每个继承来源明确，缺失来源有具体记录。

##### 批次 1 执行记录（2026-10-13）

- Candidate：基于 `6cc9daf` 的当前工作树；起始仅计划文件有未暂存修改，C# 和暂存区为空。本轮保留计划原有历史及扫描记录。
- 状态：注释实施、范围审计和构建尝试完成；7 个目标项目构建通过，`Bing.Utils.Tests` 因未改动文件中的既有冲突标记未通过。停止在本批，不启动批次 2，不自动修复冲突逻辑。
- 范围：36 个接口、11 个抽象类型、8 个抽象/虚成员、5 个有效 TestBase、3 个 CollectionDefinition，共 101 个批次契约声明。补齐/修订摘要及签名标签，统一测试基类构造函数模板、属性访问语义和路径辅助方法文案；将环境变量集合隔离说明放入 remarks。
- fixture 分类：`Bing.Utils.Comments.Tests/SampleFixtures.cs` 实际只包含枚举，留批次 2；`Bing.Tests.Samples/Bing/Tests/TestBase.cs` 全文为注释代码，不是有效声明，未修改。普通测试方法与非契约辅助类型依计划留后续批次，不把全部 public 测试方法误列为批次 1。
- 末尾复核：已修改文件中唯一显式 private 字段为 `ReflectionsTest._sample`，根据使用点修订为类型判定测试共用的样例；批次契约类型本身没有 private 字段。此项落实用户的私有字段检查要求，未启动全目录字段批次。
- 数量：22 个 C# 文件；新增 summary 55 个、修订 summary 47 个，共 102 个文档块；新增 inheritdoc 0 个。上游契约已补齐，实现成员统一替换仍按计划留批次 4；没有声称本批已完成实现层继承注释。

修改文件（以下路径均相对 `tests/`）：

| 项目 | 文件 |
| --- | --- |
| Bing.Tests.Samples | `Bing/Tests/Samples/Sample.cs` |
| Bing.Utils.Comments.Tests | `TestBase.cs` |
| Bing.Utils.DateTime.Tests | `Bing/Tests/TestBase.cs` |
| Bing.Utils.IdUtils.Tests | `Bing/Tests/TestBase.cs` |
| Bing.Utils.Http.Tests | `Bing/Tests/TestBase.cs`、`Bing/Helpers/WebHttpContextCollection.cs` |
| Bing.Utils.Reflection.Tests | `Infrastructure/TestTypes.cs`、`Bing/Reflection/TypeVisitMethodsTest.cs`、`Bing/Reflection/TypeVisitAdvancedTests.cs` |
| BingUtilsUT | `Samples/NormalInterfaceClass.cs`、`Samples/JsonTestSample.cs` |
| Bing.Utils.Tests | `Bing/Tests/TestBase.cs`、`Bing/Tests/Samples/TestSample.cs`、`Bing/Extensions/BingExtensionsTypeTests.cs`、`Bing/Helpers/JsonSystemTextJsonTest.cs`、`Bing/Helpers/EnvTestCollection.cs`、`Bing/Reflection/ReflectionsTest.cs`、`Bing/Reflection/TypeMetaVisitExtensionsAdditionalContractTest.cs`、`Bing/Reflection/TypeReflectionsPropertiesTests.cs`、`Bing/Reflection/TypeVisitCreateInstanceEdgeContractTest.cs`、`Bing/Reflection/TypeVisitMethodsAndPropertiesBoundaryTest.cs`、`Bing/Text/RegularExpressions/RegexPoolTest.cs` |

继承契约对应清单：

| 上游契约 | 实现或继承关系 | 本批结论 |
| --- | --- | --- |
| IModelOne、IModelTwo、IModelThree<T> | NormalInterfaceClass<T>；IModelFour 为未实现接口对照 | 标记契约完整，没有成员继承标签需求 |
| ITestSample3、ITestSample2、ITestSample | 三层接口继承；TestSample 实现顶层接口 | 标记契约完整 |
| ITestSample4、ITestSample5 | TestSample 实现派生接口 | 标记契约完整 |
| IB、IC、IG<T> | IA、A/B/C、D<T>/F<T> 等查找样例 | 基础与泛型标记契约完整 |
| IA.Value | A.Value | 上游已补全；实现留批次 4 |
| IJsonTestSample.Name | JsonTestSample.Name | 上游已规范；实现留批次 4 |
| IJsonContract.Name | JsonDerived.Name | 上游已补全；实现留批次 4 |
| ICreated.Value | FirstCreated.Value、SecondCreated.Value | 上游已补全；实现留批次 4 |
| MethodTestBase.VirtualMethod | MethodTestDerived.VirtualMethod | 上游已规范；实现已有 inheritdoc，保留 |
| BaseMetaSample.Execute | DerivedMetaSample.Execute | 上游已补全；实现留批次 4 |
| BaseMethodSample.VirtualMethod | DerivedMethodSample.VirtualMethod | 上游已补全；实现留批次 4 |
| AbstractPropertySample.Value | DerivedAbstractPropertySample.Value | 上游已补全；实现留批次 4 |
| IParentContract、IStringGet、IIntGet | IChildContract、IAmbiguousContract | 继承/返回类型冲突探针，无具体实现 |
| IBaseInterface、ILeft、IRight、IDiamond | 菱形属性继承 | 各声明完整，继承属性不重复声明 |
| IStringValue、IIntValue、IConflictingProperties | 同名不同类型属性组合 | 冲突形状契约完整 |
| IParentInterface、IChildInterface | 单链属性继承 | 各声明完整 |
| IGetOnlyValue、IGetSetValue、IAccessorShapeConflict | 同名不同访问器属性组合 | 访问语义完整 |
| ISample、IFoo、IInterfaceSample | 标记或不可实例化接口样例 | 没有具体成员，摘要完整 |
| AbstractEntity.GetName、VirtualPropEntity.VirtualProp、MethodTestBase.ProtectedVirtual、WriteOnlyVirtualPropertySample.WriteOnly | 抽象或虚成员元数据探针 | 独立上游契约完整；未发现具体重写成员 |
| TestBase、AbstractCreated、AbstractSample、MyBaseClass1、C | 测试支持、实例创建及继承形状样例 | 抽象类型已分类；本批没有缺失的上游来源 |

验证证据：

- Change Impact：ChangedFiles 为上述 22 个 C# 与本计划；ChangedProjects 为 8 个测试/支持项目；ChangedDocsOnly=true（指仅 XML 注释及文档变化，并非只修改 Markdown；非注释 token 无变化）。公共契约形状、运行路径、Provider、TFM、打包、依赖及测试数据均不变，RiskLevel=LOW；源码依赖仅为构建消费关系，未编辑 src。
- L0 静态审计：按本批声明范围重新扫描全部 521 个物理文件及 8 组符号配置；101 个契约声明的摘要、XML、param/typeparam/returns、属性前缀、长摘要候选均无遗留。人工核对 IStringGet.Get/IIntGet.Get 的同语义摘要一致，差异由 returns 说明；四份路径辅助方法文案一致，5 个实例构造函数模板统一。
- L0 差异审计：22 个变更文件 × 8 组符号，共 176 次 Roslyn 非注释 token 比较，差异为 0；UTF-8 严格读取通过，换行风格改变数为 0，`git diff --check` 通过。生成、迁移和第三方文件未修改；暂存区未改变。
- 构建命令：逐个执行 `dotnet build tests/<项目>/<项目>.csproj -c Debug -f <目标框架> --no-restore --nologo -m:1 -p:GenerateDocumentationFile=true -p:DocumentationFile=obj/comments-audit/<目标框架>/<项目>.xml`。使用已有还原资产，正常构建项目引用。初次沙盒写 obj 失败；随后只构建测试项目时缺依赖 DLL；启用正常引用构建后得到以下结果。没有更改项目配置或依赖，也没有用这些环境失败触发业务修复。

| 项目 | 框架 | 构建结果 | 警告/错误 |
| --- | --- | --- | --- |
| Bing.Tests.Samples | netstandard2.0 | 通过 | 669 / 0 |
| Bing.Utils.Reflection.Tests | net8.0 | 通过 | 193 / 0 |
| Bing.Utils.Comments.Tests | net8.0 | 通过 | 0 / 0 |
| Bing.Utils.DateTime.Tests | net8.0 | 通过 | 190 / 0 |
| Bing.Utils.IdUtils.Tests | net8.0 | 通过 | 189 / 0 |
| Bing.Utils.Http.Tests | net8.0 | 通过；末尾 cref 修改后复建通过 | 初次 1466 / 0，复建 811 / 0 |
| BingUtilsUT | net8.0 | 通过 | 990 / 0 |
| Bing.Utils.Tests | net8.0 | 被既有源码错误阻断 | 1012 / 7 |

- 警告数为实际构建日志总数，包含依赖构建，增量复建的数字不能当作警告消除数。Reflection 修改文件中的 4 条 CS0419 位于未修改的测试方法摘要，HTTP 和其他项目还存在批次之外的既有 XML/分析器警告；本批新增/修订契约没有对应新增文档诊断。
- `Bing.Utils.Tests` 的 7 个错误均来自 `Bing/Date/UnixTimeStampTest.cs`：当前编译器位置 124/127/129、564/567/569 的 CS8300，以及 1006 的 CS1022。`git diff --quiet HEAD -- tests/Bing.Utils.Tests/Bing/Date/UnixTimeStampTest.cs` 确认该文件与 HEAD 无差异；它不属于本批注释修订，未修复、未删除或排除编译。
- 7 份成功项目生成的 XML 均可解析；失败项目未生成目标 XML，不冒充验证成功。生成 XML 位于各项目的上述 obj 路径。静态原始注释/XML 检查仍覆盖失败项目中的本批契约。
- 原始证据位于本机临时目录：`bing-comments-batch1-audit.txt`、`bing-comments-batch1-build-<项目>.log`、`bing-comments-batch1-verified-Bing.Utils.Http.Tests.log`；这些日志仅对应本批，不复用前文 src 历史记录。
- L1–L5：未运行；没有代码行为变化，不运行功能测试、全量测试或性能基准。6 个成功项目的最后一次构建后相关源码未变化；HTTP 在最终 cref 修改后重新构建。
- Completed：本批 4/4 实施任务及 8/8 目标构建尝试、静态审计、差异检查和计划更新；构建成功 7/8，XML 成功解析 7/7。
- Open Actionable：本批范围无；Blocked Approval、Blocked External、Accepted Limitations、Verified Boundaries：本批无。Not Applicable：纯注释功能回归及性能验证。
- Deferred：批次 2–7；实现 inheritdoc 调整留批次 4。既有 UnixTimeStampTest 冲突需独立任务确认逻辑后修复，影响 Bing.Utils.Tests 的编译但不属于当前注释批次；其他既有文档警告留最终审计。
- 待确认项：本批注释语义无；源码冲突的正确保留逻辑本批未确认。No-Progress Check=CHANGED；Next Action=STOP。

##### 批次 1 验证收尾（2026-10-13）

- 本次继续闭合批次 1 的构建验证，不执行批次 2。上方首次执行记录及失败日志保留为历史证据；本节为当前状态。
- 用户另行授权的冲突修复仅涉及 `tests/Bing.Utils.Tests/Bing/Date/UnixTimeStampTest.cs`：清除两处冲突文本、重复括号与注释，将值类型空值断言改为对应 UTC 时间转换后的本地时间断言。此文件不计入 22 个注释文件，不能把它的非注释差异声称为纯注释修改。
- Completed：重新核对本批 101 个契约、5 个构造函数模板、属性访问前缀、参数/泛型/返回标签、同语义重载、冗长摘要与 private 字段；无遗漏。数量保持为新增 summary 55 个、修订 summary 47 个，共 102 个文档块，新增 inheritdoc 0 个。本次不新增 C# 注释修改；具体实现的继承注释仍按批次 4 执行。
- Change Impact：本轮仅更新此计划，22 个注释文件保持不变；先前另行修复的测试文件只影响 Bing.Utils.Tests。公开 API、生产运行路径、Provider、TFM、打包及项目依赖均不变，风险 LOW。
- L0：确认 22 个注释文件相对 HEAD 去除 XML 文档行及空白后的文本序列差异为 0，严格 UTF-8 读取通过；此前 176 次 Roslyn token 对比证据保持有效。8 个项目目标 XML 均重新解析成功，`git diff --check` 通过。UnixTimeStampTest 使用 UTF-8/LF，tests 手写 C# 中未发现冲突标记。
- 构建：复用前述 7 个未变化项目的成功日志；冲突修复后实际执行 `dotnet build tests/Bing.Utils.Tests/Bing.Utils.Tests.csproj -c Debug -f net8.0 --no-restore --nologo -m:1 -p:GenerateDocumentationFile=true -p:DocumentationFile=obj/comments-audit/net8.0/Bing.Utils.Tests.xml`，结果 98 个警告、0 错误，生成 XML 可解析。警告含批次之外的既有文档问题，增量数字不表示消除了历史警告。当前本批目标构建成功 8/8，XML 解析成功 8/8；未执行完整目标框架矩阵。
- L1：另行冲突修复任务执行 `dotnet test tests/Bing.Utils.Tests/Bing.Utils.Tests.csproj -c Debug -f net8.0 --no-build --no-restore --filter FullyQualifiedName~Bing.Date.UnixTimeStampTest --nologo`，81/81 通过；这项证据仅支持冲突修复，不代替其他测试。L2–L5 不适用于本次计划收尾。
- 原始新增证据：本机临时目录中的 `bing-unix-conflict-build.log`、`bing-unix-conflict-tests.log`；复用日志路径见首次执行记录。未重新运行已通过且范围未变化的构建。
- Implementation：4/4；Required Gates：契约审计、差异/编码检查、目标构建、XML 解析、计划更新全部通过。Open Actionable、Blocked Approval、Blocked External、Accepted Limitations、Verified Boundaries：无；Not Applicable：纯注释功能测试及性能基准；Deferred：批次 2–7 与其他既有文档警告。
- 待确认项：本批无。No-Progress Check=CHANGED（补齐实际验证与当前状态）；Next Action=STOP；Goal Status=COMPLETED。全部修改保留未暂存、未提交。

#### 批次 2：DTO、Entity、Options 和枚举

- [ ] 处理 `tests/Bing.Utils.Http.Tests.Integration/Samples/CustomerDto.cs`、`tests/Bing.Utils.Reflection.Tests/Infrastructure/TestTypes.cs` 及 `tests/BingUtilsUT/Samples/**` 的 DTO/Entity/样例数据类型。
- [ ] 处理 `tests/Bing.Tests.Samples/Bing/Tests/Samples/EnumSample.cs`、`tests/BingUtilsUT/Samples/NormalEnums.cs`、HTTP 流式测试的 Item/State/TransferState 等嵌套模型。
- [ ] 检查全范围 20 个枚举及 54 个成员，说明测试含义、数值边界和标志语义；Description 特性不能替代 XML。
- [ ] 核查 Options 命名、配置对象和匿名测试数据；本次未确认自定义 Options 类型，不预设数量、不新增类型。
- 完成条件：模型类型、属性、泛型参数和枚举成员完整；无捏造业务语义。

#### 批次 3：构造函数和属性文案统一

- [ ] 对 182 个构造函数复核缺摘要与 47 个模板候选，统一实例初始化摘要及参数标签。
- [ ] 对 602 个属性、5 个索引器和 1 个事件按访问方式检查；优先 SampleEntity、CustomerDto、JsonTestSample、HTTP Item 等代表类型。
- [ ] 处理 `tests/Bing.Utils.Tests/Bing/Helpers/MapperHelperTest.cs:170`、`MapperHelperAdvancedTests.cs:509`、`:515`、`Bing/Reflection/ReflectionsMemberPathTests.cs:350`、`TypeReflectionsPropertiesTests.cs:145` 的索引器，以及 `TypeMetaVisitExtensionsAdditionalContractTest.cs:151` 的事件。
- [ ] 核对 191 个属性前缀候选，区分 init、只读、只写及返回副本；较长条件说明下沉 value/remarks。
- 完成条件：模板与访问语义一致，参数及索引参数匹配；基类/接口实现留给批次 4 使用继承注释。

#### 批次 4：实现类、重写成员和重载方法

- [ ] 处理前述 ParameterFormatterSample、TestSample、JsonTestSample、Dispose、Stream 重写及 21 个语法候选。
- [ ] 通过语义关系扫描隐式/显式接口实现、抽象实现和 override，比较上游文档；完整契约使用 inheritdoc，特有行为只加 remarks。
- [ ] 复核 TestTypes.Greet、集成测试 `Controllers/Test1Controller.cs` 的 Get 重载，以及 `HttpTransferCapabilitiesTest.cs:1506` 起同步/异步委托构造函数。
- [ ] 同语义重载统一核心摘要，不同语义保留必要差别；清除机械复制或过时描述，不复制整段上游文档。
- 完成条件：所有继承候选与重载组有审阅结论；不存在无来源继承注释或遗漏实现差异。

#### 批次 5：private、internal、protected、static 辅助方法

- [ ] 按模块审阅所有方法，包括 public 测试方法与非公开辅助方法，优先 `Bing.Utils.Tests/Bing/Reflection`、Helpers、Threading、Http、Drawing 和 `BingUtilsUT`。
- [ ] 处理显式 private 缺摘要 55 个、protected 缺摘要 2 个、static 缺摘要 74 个的候选，补充测试数据提供器、流/处理器替身和反射探针用途；复核默认访问级别的 internal 成员。
- [ ] 核对 param/typeparam/returns，清理空标签；普通 Task/ValueTask 与泛型异步结果分别处理。
- [ ] 审阅 391 个长摘要、6 个机械关键词候选及人工发现的机械模板；只将确有必要的细节移到 remarks。
- 完成条件：所有方法候选经分类，注释说明核心行为；无仅复述名称的模板，异步结果和参数语义准确。

#### 批次 6：private 字段、其他字段、常量、缓存键和配置键

- [ ] 审阅 308 个字段及常量，处理 183 个缺摘要候选；private、readonly、static readonly、公开测试字段均在范围内。
- [ ] 优先处理 `tests/Bing.Utils.Tests/Bing/Logging/LogHelperTest.cs:9` 起的模拟对象字段、Reflection 的 PrivateField/ReadOnlyField/ConstantField，以及 `PropertyInfoExtensionsTests.cs:25` 的只写属性后备字段。
- [ ] 处理 `tests/Bing.Utils.Security.Tests/Bing/Security/Cryptography/AesGcmStreamEncryptionTests.cs:352` 的 HeaderLength、`tests/Bing.Utils.Tests/Bing/Reflection/ReflectionsTest.cs:524` 起的权限常量，以及 WebHttpContextCollection.Name。
- [ ] 依据使用点核查键、配置名、测试标识值和静态数据；说明用途及作用域，不将测试密钥或固定字符串误称为生产配置。
- 完成条件：常量含义、单位、后备字段关联和共享状态用途清楚；键名候选已完成语义复核。

#### 批次 7：最终审计和构建验证

- [ ] 重新列举文件及条件分支，复核排除项、链接源码和 partial 类型；冲突文件解决后重新纳入扫描。
- [ ] 复核三行摘要、标签空值/重复/错名、返回值适用性、cref、inheritdoc 来源和重复复制；确认 20 个 XML 候选的实际状态。
- [ ] 核查替换字符、UTF-8、中文语病和错误/过时描述，尤其 TypeReflectionsIsTaskAndTypeConverterTests 的两处已确认字符损坏。
- [ ] 比较修改前后 Roslyn 非注释 token、成员签名、特性及测试数据，确保只修改文档；不运行 dotnet format 扩大格式差异。
- [ ] 按受影响项目生成 XML 文档并构建，解析生成 XML；记录既有警告与新增问题，不能仅依赖被抑制的 CS1591。
- 完成条件：无未审阅候选、无新增 XML/编译错误；非注释 token 差异为 0；已执行与未执行验证分别记录。

### 风险、阻断和待确认项

| 状态 | 事项 | 处理边界 |
| --- | --- | --- |
| 已解除（历史阻断） | `tests/Bing.Utils.Tests/Bing/Date/UnixTimeStampTest.cs` 曾含两处冲突及重复括号，导致 7 条编译错误 | 用户另行授权修复；net8.0 构建及 81 个对应测试通过。该逻辑差异单列，不归为注释修改；全范围重扫仍留批次 7 |
| 初始扫描基线 | 3,465 个缺摘要候选，另有可能交叉的文案/XML候选 | 不代表当前剩余数量；批次 1 已完成，其他批次开始时按当前成员复核。不能直接视为全部已确认错误，也不能用机械模板清零 |
| 待语义复核 | 隐式接口实现、默认 internal 访问及跨项目继承 | 使用实际编译符号和语义模型，不根据命名猜测；src 只读作为契约来源 |
| 已确认文案问题 | 两处替换字符、空 param/returns、单行摘要与重复上游摘要 | 实施时逐项恢复真实语义，无法确定原意则保留并记录 |
| 边界风险 | 反射测试依赖私有成员、字段名称、构造函数形状和特性 | 只加注释，禁止“顺手重构”测试样例 |
| 构建限制 | 多目标框架、HTTP 链接源码、Samples 支持项目、历史框架 | 先选择实际受影响项目和已有还原资产；缺 SDK/资产记为未验证，不改框架或包版本 |
| 验证限制 | 测试 props 抑制 CS1591，构建可能有既有 XML/分析器警告 | 以静态审计和文档解析补充构建证据；仅记录本次新增问题 |

计划阶段交付已完成：范围、基线统计、七批清单、每批目录/文件、文案与继承问题、风险、验证命令和验收标准均已记录。2026-10-14 的计划复核只更新 Markdown；保留批次 1 已完成的实施状态，批次 2–7 继续等待后续授权。本轮不修复源码问题，也不运行集成环境。

### 后续验证命令

以下命令是实施阶段的验证方案，本次未执行构建或测试。PowerShell 先设置 UTF-8 控制台编码；读取和写入扫描报告时显式指定 UTF-8。

```powershell
[Console]::InputEncoding = [System.Text.UTF8Encoding]::new($false)
[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)
$OutputEncoding = [System.Text.UTF8Encoding]::new($false)

# 复核范围；生成文件头、EF 迁移及第三方路径另做内容审阅
rg --files --hidden tests -g '*.cs' -g '!**/bin/**' -g '!**/obj/**' -g '!*.g.cs' -g '!*.generated.cs' -g '!*.Designer.cs' -g '!**/Migrations/**' -g '!**/Migration/**' -g '!*ModelSnapshot.cs'
rg -n '^(<<<<<<<|=======|>>>>>>>)|�' tests -g '*.cs' -g '!**/bin/**' -g '!**/obj/**' -g '!*.Designer.cs'
git diff --check

# 每批仅选择实际受影响项目；以下是代表命令，不是全矩阵通过声明
dotnet build tests/Bing.Utils.Reflection.Tests/Bing.Utils.Reflection.Tests.csproj -c Debug -f net8.0 --no-restore --nologo -p:GenerateDocumentationFile=true
dotnet build tests/Bing.Utils.Http.Tests/Bing.Utils.Http.Tests.csproj -c Debug -f net8.0 --no-restore --nologo -p:GenerateDocumentationFile=true
dotnet build tests/Bing.Utils.Http.Tests.Integration/Bing.Utils.Http.Tests.Integration.csproj -c Debug -f net8.0 --no-restore --nologo -p:GenerateDocumentationFile=true

# 按实际输出路径解析生成 XML，读取时显式使用 UTF-8
$xmlFiles = Get-ChildItem -LiteralPath 'tests/Bing.Utils.Reflection.Tests/bin/Debug/net8.0' -Filter '*.xml'
foreach ($xmlFile in $xmlFiles) {
    [xml](Get-Content -LiteralPath $xmlFile.FullName -Encoding UTF8 -Raw) | Out-Null
}
```

每批记录：实际扫描文件数、修改成员数、summary/inheritdoc 增量、剩余问题及其原因、构建命令与结果、XML 解析结果和非注释 token 比较。跨框架成员按项目实际 TargetFrameworks 构建受影响分支；Samples 使用其实际 netstandard2.0 目标。纯注释不强制运行功能测试，若出现非注释差异先停止并核查，不以增加测试掩盖范围漂移。

### 验收标准

- [ ] 521 个候选文件均有范围分类，冲突文件有单独复核结论；排除文件未修改。
- [ ] 类型、构造函数、方法、属性、索引器、事件、字段、常量和枚举均审阅，所有访问级别都覆盖。
- [ ] summary、构造函数模板、属性前缀、重载语义和 remarks 符合规范；没有空标签、机械文案或凭名称编造的能力。
- [ ] param/typeparam/returns 与实际签名及返回语义一致；XML 与 cref 可解析，inheritdoc 有有效上游来源。
- [ ] 缺失及候选问题全部处理或形成逐项、有原因的保留清单，不以统计清零替代人工语义验收。
- [ ] C# 非注释 token、签名、特性、断言和测试数据无差异；项目及依赖配置未变。
- [ ] 受影响项目和条件分支的构建/XML证据可追溯，既有问题与新增问题分开记录，不声称未执行验证通过。
- [ ] 本计划阶段仅改变此 Markdown 文件；C# 修改数为 0，构建及测试运行次数为 0。

## tests/** 实施执行记录（2026-10-14）

本节记录本次按用户授权执行的 tests 注释补全；上文“等待后续授权”“本轮不修改 C#”等文字属于计划阶段历史状态，以本节为当前执行结果。src 计划正文及既有批次 1 历史均保留。

### 范围与基线

- 纳入 `tests/**` 下 521 个物理 C# 文件、19 个测试/样例目录，按物理路径去重链接源码；排除 `bin`、`obj`、生成文件、EF Migration/ModelSnapshot、自动生成客户端/代理代码及第三方源码。
- 基线为 9,900 个声明、3,430 个缺 summary 候选；另有缺 `<param>` 2,587、缺 `<typeparam>` 17、缺 `<returns>` 214、不适用但存在 `<returns>` 3、空标签 9、XML 注释解析候选 20、无 `<inheritdoc />` 的 override/显式实现候选 21。各候选按实际声明和继承关系复核，不将扫描候选直接视为错误。
- 382 个本轮注释变更 C# 文件完成审阅；Roslyn 对这些文件比较 `HEAD` 与工作树非注释 token，差异 0。`UnixTimeStampTest.cs` 中另有先前单独授权的逻辑修复，不计入注释变更及 token 比较；除此之外未发现非注释 token 变化。

### 分批实施

| 批次 | 状态 | 实施范围与结论 |
| --- | --- | --- |
| 1. 接口、抽象类和公共契约 | 已完成并复核 | 保留先前 22 个文件、102 个文档块和 8 个项目验证的批次 1 历史；复核样例接口/抽象类型、TestBase、fixture、collection、HTTP 上游契约。实现文档使用有效上游继承注释，未复制接口/基类整段注释。 |
| 2. DTO、Entity、Options 和枚举 | 已完成 | 覆盖 `Bing.Tests.Samples`、`Bing.Utils.Http.Tests.Integration`、`Bing.Utils.Reflection.Tests`、`BingUtilsUT/Samples`、HTTP 流式测试及其他目录的样例模型和枚举。按代码核对属性用途、默认值、范围、可空语义和枚举用途；未用 `Description` 替代 XML 注释或按名称臆造业务语义。 |
| 3. 构造函数和属性文案统一 | 已完成 | 覆盖全部 19 个目录的构造函数、属性和索引器候选。实例构造函数按 `初始化一个 <see cref="当前类型"/> 类型的实例。` 统一；属性按 `get`/`set`/`init` 语义使用“获取”“设置”“获取或设置”“获取或初始化”。只读字段按字段用途描述，不使用属性访问措辞。 |
| 4. 实现类、重写成员和重载方法 | 已完成 | 覆盖 HTTP、Reflection、Security.Gm、Collections、样例和 `BingUtilsUT` 等实现。确认显式/隐式接口实现、override、Dispose、异步重载和同义方法组；有效上游契约使用 `/// <inheritdoc />`，差异行为单独说明。修正了测试摘要中的错误语义、重载差异描述和无效/歧义 `cref`。 |
| 5. private、internal、protected、static 辅助方法 | 已完成 | 覆盖图像、HTTP、Helpers、Reflection、IdUtils、Security.Gm、Text、`BingUtilsUT` 及主测试程序集的辅助方法和数据提供器。缺失摘要、参数标签及异步结果说明均按签名与实现补齐；复杂的缓存、并发、重试和资源清理语义保留在必要说明中。 |
| 6. 字段、常量、缓存键和配置键 | 已完成 | 覆盖全部目录中的字段与常量候选，包括私有/只读/static 字段、测试固定值及嵌套模型后备数据。按使用点描述字段用途、共享边界、默认值和状态含义；只有代码能确认时才称为缓存键或配置键。 |
| 7. 最终审计和构建验证 | 已完成 | 对目标文件复扫类型、成员、标签、继承关系、XML、编码、冲突标记和条件分支；382 个 C# 注释变更文件 Roslyn 非注释 token 差异为 0。19 份目标项目 XML 文档可解析，未发现不含有效 inheritdoc 的空 summary；`git diff --check` 通过。 |

### 文案、继承与标签审计

- 构造函数模板、属性访问前缀、方法核心用途摘要、同语义重载一致性及冗长说明移入 remarks 均纳入逐目录复核；测试用例摘要描述被验证的行为，不复述机械化“测试 - …”标题。
- 所有新增/修订 `<param>`、`<typeparam>` 与实际签名核对；返回标签按 void、普通 Task/ValueTask、泛型异步结果和同步返回区分。各分区扫描未留参数名不匹配、无结果方法误用 returns 或异步结果漏说明候选。
- 实现、显式实现、抽象成员实现及 override 的继承注释均核对上游来源。主测试 XML 中 15 个空文本 summary 节点均为 `<inheritdoc />`，不是缺失注释；继承关系有效。
- 修复了 `ExpressionCopy` 泛型链接和主测试类摘要中的扩展类型歧义，并将无法稳定解析的类型名链接改为明确职责文本。最终主项目构建无 `CS1570`/`CS1574`；Reflection.Tests 复核构建 0 警告、0 错误。
- 全范围替换字符检查未发现 XML 文档中的 `�`。`TypeReflectionsIsTaskAndTypeConverterTests.cs` 的 4 个普通 `//` 注释仍含替换字符，因不属于 XML 文档注释，本轮未改动，列为范围外遗留。

### 验证结果与边界

- `dotnet build tests/Bing.Utils.Tests/Bing.Utils.Tests.csproj -c Debug -f net8.0 --no-restore --nologo -m:1 -p:GenerateDocumentationFile=true -p:BuildProjectReferences=false -v:q`：通过，0 错误、67 条警告；最终构建无 XML cref/格式诊断。警告集中在弃用 API、nullable 上下文、测试夹具字段和既有异步调用等。
- `dotnet build tests/Bing.Utils.Reflection.Tests/Bing.Utils.Reflection.Tests.csproj -c Debug -f net8.0 --no-restore --nologo -m:1 -p:GenerateDocumentationFile=true -p:BuildProjectReferences=false -v:q`：通过，0 警告、0 错误。
- `dotnet build tests/Bing.Utils.Text.Tests/Bing.Utils.Text.Tests.csproj -c Debug -f net8.0 --no-restore --nologo -m:1 -p:GenerateDocumentationFile=true -p:BuildProjectReferences=false -v:q`：通过，0 警告、0 错误。
- 其余受影响项目的代表性 Debug 文档构建由分区记录完成：多目标测试项目以 `net8.0` 为主；`Bing.Tests.Samples` 使用 `netstandard2.0`；`Bing.Utils.Net.Tests.Integration` 验证 `net8.0`、`net7.0`、`net6.0`；Security/Security.Gm 项目覆盖其可用的 `net8.0`、`net7.0`、`net6.0`。全项目所有旧目标框架矩阵未逐一重跑。
- 功能测试未运行；注释不改变运行行为。完整目标目录清单和执行统计见本计划前述基线及批次记录，最终验证使用：

```powershell
rg --files --hidden tests -g '*.cs' -g '!**/bin/**' -g '!**/obj/**' -g '!*.g.cs' -g '!*.generated.cs' -g '!*.Designer.cs' -g '!**/Migrations/**' -g '!**/Migration/**' -g '!**/*ModelSnapshot.cs'
rg -n '^(<<<<<<<|=======|>>>>>>>)|///.*�' tests -g '*.cs' -g '!**/bin/**' -g '!**/obj/**'
git diff --check
```

待确认项：无。未覆盖的全旧框架构建矩阵、功能测试及普通行注释中的替换字符均按上述边界记录；所有本次注释候选已审阅，无待修复 XML 文档项。C# 注释批次与 Unix 时间戳测试修复已分别提交；本次计划记录随文档提交。
