# 文件内容签名识别

`Bing.IO.FileContentDetector` 默认根据文件头识别 15 类常用二进制格式；显式启用容器检查后可进一步识别 ZIP 中的六种 Office 格式。它不依赖扩展名、不添加识别库依赖。检测依据分为 Signature 和 ContainerStructure，两者都不保证正文完整、可解码或安全。

## 支持范围与规则来源

| Id | MIME | 扩展名 | 识别规则 |
|---|---|---|---|
| jpeg | image/jpeg | jpg/jpeg/jpe | FF D8 FF |
| png | image/png | png | 89 50 4E 47 0D 0A 1A 0A |
| gif | image/gif | gif | GIF87a / GIF89a |
| bmp | image/bmp | bmp | BM |
| tiff | image/tiff | tif/tiff | classic TIFF 大小端；BigTIFF 大小端且偏移长度为 8、保留位为 0 |
| ico | image/x-icon | ico | 00 00 01 00；不包括 CUR |
| webp | image/webp | webp | RIFF + 偏移 8 的 WEBP |
| pdf | application/pdf | pdf | 起点的 %PDF- |
| zip | application/zip | zip | PK 03 04 / PK 05 06（空归档） |
| gzip | application/gzip | gz/gzip | 1F 8B 08 |
| 7z | application/x-7z-compressed | 7z | 37 7A BC AF 27 1C |
| rar | application/vnd.rar | rar | RAR 4 / RAR 5 起始签名 |
| wav | audio/wav | wav | RIFF + 偏移 8 的 WAVE |
| avi | video/x-msvideo | avi | RIFF + 偏移 8 的 AVI 空格 |
| flac | audio/flac | flac | fLaC |

规则来自公开格式规范中的字节常量，未复制第三方识别器或规则数据库：

- [WHATWG MIME Sniffing](https://mimesniff.spec.whatwg.org/)：常见图像、RIFF 音视频、PDF、ZIP/GZip 签名；本库并非浏览器 sniffing 算法的完整实现。
- [LibTIFF BigTIFF 设计](https://libtiff.gitlab.io/libtiff/specification/bigtiff.html)：TIFF 42/43 标识和 BigTIFF 头字段。
- [RAR 官方格式说明](https://www.rarlab.com/technote.htm)、[7z 官方头结构](https://www.7-zip.org/recover.html)。
- [RFC 9639](https://www.rfc-editor.org/rfc/rfc9639.html)：原生 FLAC 标记。

所有偏移相对于本次输入起点。首版不搜索任意位置的嵌入签名，不识别带可执行前导的自解压归档、分卷标记、前置垃圾数据或 RF64/RIFX。默认浅检测将 ZIP 内的 Office、EPUB/APK 等返回 ZIP；启用下文的容器检查可识别六种 Office 格式。文本及其他格式返回 Unknown 或未完成的前缀结果。

## 基本使用

```csharp
using Bing.IO;

var result = FileContentDetector.DetectFile("upload.bin");
if (result.Status == FileDetectionStatus.Matched)
{
    Console.WriteLine($"{result.Format.Id} / {result.Format.MimeType}");
    var comparison = FileContentDetector.CompareFileName(result, "photo.png");
    Console.WriteLine(comparison);
}

// 也支持 byte[]、ReadOnlySpan<byte>；不保存调用方字节内存。
var headerResult = FileContentDetector.Detect(new byte[] { 0xff, 0xd8, 0xff });
```

`CompareExtension` 接受扩展名（`jpg` 或 `.jpg`），忽略大小写及两端空白，不接受文件路径。只有唯一识别且声明为内置已知扩展名时返回 Match/Mismatch；未知扩展名、空声明、未知/歧义/未完成结果返回 Indeterminate。例：JPEG + .png 为 Mismatch；JPEG + .JPEG 为 Match；仅有 ZIP 签名时 ZIP + .docx 为 Indeterminate；完成容器检查后，DOCX + .docx 为 Match、DOCM + .docx 或普通 ZIP + .docx 为 Mismatch。

`CompareFileName(result, "report.docx")` 从最后一个路径段提取扩展名，同时识别 `/` 与 `\`，不访问文件系统。无扩展名、单个隐藏文件名（如 `.docx`）和尾点返回 Indeterminate。

`FileContentDetector.SupportedFormats` 是 21 种内置格式的只读目录。可直接使用共享不可变实例，例如 `result.Format == FileContentFormats.Docx`、`FileContentFormats.Jpeg.Extensions`，无需手写格式 Id。

文件路径入口以只读方式自行打开/关闭文件。扩展名不参与检测。文件不存在、访问拒绝等正常抛出 IO 异常。

## 流入口与资源归属

```csharp
// 可定位流：以当前位置为内容起点，成功、异常或取消后恢复位置。
using var file = System.IO.File.OpenRead("upload.bin");
var result = await FileContentDetector.DetectAsync(file, cancellationToken: token);

// 任意可读流：成功后改用 probe.Content，完整正文不会丢失文件头。
using var probe = await FileContentDetector.ProbeAsync(
    uploadStream, leaveOpen: true, cancellationToken: token);
await StreamTransfer.CopyAsync(probe.Content, destination, cancellationToken: token);
```

- `Detect/DetectAsync(Stream)` 要求 `CanSeek`，不满足时在读取之前抛出 NotSupportedException。始终保留调用方源流。位置恢复依赖源仍支持定位且未被并发关闭。
- `Probe/ProbeAsync` 读取并消费源前缀；返回的 Content 先重放前缀，再读取源剩余正文。可定位输入也遵循此消费契约，不恢复位置。
- Content 只读且不可定位；不提供 Length/Position，不支持并发消费。零长度读取不消费前缀、不访问源；同步与异步均支持短读。
- `leaveOpen: true` 为默认，释放租约或 Content 清理缓冲但保留源；false 则同时关闭源，重复释放不会重复关闭源。
- 参数校验和初始取消检查发生在资源接管之前，即使 leaveOpen=false 也不关闭源。接管后的失败/取消按 leaveOpen 清理；Probe 中途失败不保证源位置恢复，应结束本次消费流程。
- 取消令牌传到底层异步读取；同步读取在调用前后检查取消，不能打断底层阻塞的同步 IO。重放流的后续异步读取使用该次 ReadAsync 的令牌，不绑定已完成探测的令牌。

## 文件头预算与结果

`MaxHeaderBytes` 默认为 65536，必须大于零，所有选项均在操作开始时快照。检测器自身的前缀缓冲及向源流请求的读取上限为 `min(MaxHeaderBytes, 内置规则最大所需长度)`，当前最大 12 字节。该预算不包含底层流或文件系统的内部缓冲、预读。不会为了确认 EOF 越过预算额外读取，也不会等待整个正文；所有候选已确定时可提前结束。默认不解压；启用容器检查后另受下文预算约束，Detect / Probe 没有隐式落盘；下文 ProbeBuffered 是显式启用完整有界缓存的独立入口。

BytesRead 保持文件头阶段的计数：流入口为实际取得的前缀字节数，字节入口为检查的前缀长度。两者可能因流短读后的提前停止而不同。ContainerBytesRead 记录容器阶段额外实际读取或检查的字节数；包含重复范围，不含解压后的字节或底层流预读。两项相加为检测阶段取得的累计字节数。ProbeBuffered 会额外完整读取源并写入缓存，这部分不计入结果计数；其总量由必填 maxContentBytes 限制，实际缓存长度可从 Content.Length 查询。

| Status | 含义 |
|---|---|
| Matched | 唯一格式已识别，Format 非空 |
| Ambiguous | 不同格式完整命中，Format 为空，保留 Candidates |
| Unknown | 已读字节排除了所有内置规则 |
| Empty | 输入没有内容 |
| Incomplete | 签名字节不足，或请求的容器检查未完成；保留已确认候选 |

同格式多变体会合并，规则顺序不决定结果。Candidates 只包含已确认的候选；Evidence 为 Signature 只说明文件头依据，ContainerStructure 表示已检查识别所需的目录和包元数据，不代表完整格式验证。当前内置签名规则互斥，Ambiguous 由内部规则组合测试覆盖。

Incomplete 的原因：Truncated 表示已到输入末尾或提供的字节片段末尾；HeaderLimitReached 表示读预算已耗尽且尚未确认 EOF。流恰好在预算处结束时也可能返回后者，因为库不进行预算之外的探测。所有签名均已排除时即使读取长度达到上限仍是 Unknown。

ContainerFormat 在唯一匹配 ZIP/GZip/7z/RAR 时返回对应通用容器；Office 子类型识别成功以及 ZIP 容器检查未完成时仍保留 zip。BMP 的 BM、PDF 的 %PDF- 等短特征可以人工构造，上传放行需要再调用相应格式解析/解码器。

## ZIP / Office 容器识别

```csharp
var result = await FileContentDetector.DetectFileAsync("upload.bin", new FileDetectionOptions
{
    InspectContainers = true,
    MaxContainerBytesRead = 4 * 1024 * 1024,
    MaxZipDirectoryBytes = 1024 * 1024,
    MaxZipEntries = 4096,
    MaxMetadataBytes = 1024 * 1024
}, token);

// 成功示例：Format.Id == "docx"，ContainerFormat.Id == "zip"，
// Evidence == ContainerStructure。
// 未完成时先检查 Status / ReasonCode，不使用 ZIP 候选作为 Office 放行依据。
```

| Id | MIME |
|---|---|
| docx | application/vnd.openxmlformats-officedocument.wordprocessingml.document |
| xlsx | application/vnd.openxmlformats-officedocument.spreadsheetml.sheet |
| pptx | application/vnd.openxmlformats-officedocument.presentationml.presentation |
| docm | application/vnd.ms-word.document.macroEnabled.12 |
| xlsm | application/vnd.ms-excel.sheet.macroEnabled.12 |
| pptm | application/vnd.ms-powerpoint.presentation.macroEnabled.12 |

检查步骤为：确认 ZIP 签名，先读取末尾 22 字节。若未找到有效目录，再补读最大 65557 字节注释窗口。候选按距离文件尾由近到远检查中央目录，使用第一个完整有效的目录；注释中的无效候选不阻止继续搜索。若存在多个有效嵌套目录，最近者优先，不承诺识别全部嵌套歧义；选定目录后的 OPC 错误不会通过回退到其他候选隐藏。候选共享累计读取预算。

随后读取 `[Content_Types].xml` 与 `_rels/.rels`。通过包级 officeDocument 关系定位唯一普通主部件，再核对其本地头签名、标志、压缩方式、名称、长度声明及数据范围，最后依据内容类型区分格式。包清单和关系部件不能冒充主部件。该检查只读取主部件头，不读取或解压主部件正文。

支持 Strict/Transitional 关系、自定义主部件路径及 Override/Default 映射。Override 的 PartName 必须是规范绝对路径，不接受点段、编码后的未保留字符或编码后的斜杠；关系 Target 可解析合法相对点段，但不能越过包根或以目录路径结尾。允许 `custom/folder:part/document.xml`，拒绝 scheme、authority、查询和片段。百分号转义大小写统一，转义后的保留字符与原字符仍为不同名称；部件比较仅忽略 ASCII 字母大小写，不合并不同的 Unicode 名称。编码后的空格 `%20` 可以出现在部件路径中，原始空白及控制字符仍被拒绝。Default 的 Extension 必须是单个扩展名，只从文件名提取后缀；允许合法 URI 字符和转义，例如 `a:b`、`a%2Bb`，仍拒绝点号、路径分隔符及非法转义。重复清单、重复主部件及多条主关系返回 InvalidContainer。

宏文档依据主部件类型识别，即使没有 VBA 部件也可以返回 DOCM/XLSM/PPTM。检测器不解压正文，不验证正文 XML、数据 CRC、全部部件或宏行为。命中结构证据仍不是文件完整性或安全判定。没有 OPC 元数据或未命中受支持主类型时保留普通 ZIP；只缺少一项识别元数据时返回未完成。

预算均须为正整数：额外源读取 4 MiB、中央目录 1 MiB、4096 条目录项、两个 XML 累计解压 1 MiB 为默认值。每段读取与分配前检查预算，实际解压量也受限，不能通过伪造 ZIP 未压缩长度绕过。Deflate 输出恰好耗尽预算且尚未确认结束时保守返回 ContainerLimitReached，不额外解压一个字节探测。XML 禁用 DTD 和外部实体；主部件只请求本地头及名称。注释回退窗口可能覆盖正文，重复读取仍重复计数。预算约束检测器的读取与保留数据，非 CLR 总内存或底层流预读上限。

| ReasonCode（Status 为 Incomplete） | 说明 |
|---|---|
| SeekableSourceRequired | Probe 输入是不可定位 ZIP，无法检查末尾目录 |
| ContainerLimitReached | 读取、目录、条目或元数据解压预算不足 |
| InvalidContainer | ZIP 目录/识别元数据损坏、缺失或相互矛盾 |
| UnsupportedContainer | ZIP64、分卷、所需部件加密或非 Store/Deflate 压缩 |

`LimitKind` 提供预算细分：HeaderBytes 对应 HeaderLimitReached；ContainerBytesRead、ZipDirectoryBytes、ZipEntries、MetadataBytes 对应 ContainerLimitReached；其他情况为 None。例如：

```csharp
if (result.ReasonCode == FileDetectionReason.ContainerLimitReached)
    Console.WriteLine($"检测预算不足：{result.LimitKind}，已额外读取 {result.ContainerBytesRead} 字节");
```

以上容器未完成结果保持 ContainerFormat=zip、Candidates 中的 ZIP 签名和 Evidence=Signature。底层 IO 异常及取消仍抛出；Detect 恢复原位置，Probe 按已有所有权约定清理。字节数组/Span 入口须提供完整归档；按有限片段读取，不复制整个输入。可定位输入从当前位置到 Length 视为完整归档，不支持尾随无关数据。

本批支持 ZIP32 单卷，识别所需的元数据及主部件采用 Store/Deflate；ZIP64、分卷、所需部件加密明确返回 UnsupportedContainer。非 UTF-8 标记且包含非 ASCII 字节的 ZIP 条目名不猜测代码页，不能作为主部件匹配。旧 DOC/XLS/PPT、采用 OLE 包装的加密 Office、模板类型、EPUB/APK 和文本识别留待后续。

## 不可定位流的一步缓存识别

`ProbeBuffered / ProbeBufferedAsync` 适用于需要对不可定位上传流进行 Office 深检测，同时继续使用完整正文的场景。必须提供 TemporaryFileStore 和总字节上限；省略 options 默认启用容器检查，显式传入 options 则尊重其 InspectContainers 值。

```csharp
// store 由调用方配置并拥有；缓存超过内存阈值后使用其临时文件配额。
await using var buffered = await FileContentDetector.ProbeBufferedAsync(
    uploadStream, store,
    maxContentBytes: 64L * 1024 * 1024,
    memoryThresholdBytes: 1024 * 1024,
    leaveOpen: true,
    cancellationToken: token);

if (buffered.Result.Format == FileContentFormats.Docx)
{
    // Content 从零开始，只读、可定位，可多次读取。
    await StreamTransfer.CopyAsync(buffered.Content, destination, cancellationToken: token);
}
```

完整缓存从源当前位置读到 EOF，成功后不恢复源位置。maxContentBytes 可为零，允许空输入；超限最多额外读取一个判定字节，抛出 InvalidDataException。内存门限默认 1 MiB，可设为零强制落盘。SpilloverStream 在溢写时按 maxContentBytes 预留配额，因此 TemporaryFileStore 的可用配额必须容纳这一上限。

返回的 FileContentBuffer 拥有缓存，Dispose/DisposeAsync 关闭 Content 并释放内存或临时文件租约；store 始终由调用方管理。只关闭 Content 不释放缓存租约，应始终释放 FileContentBuffer。leaveOpen 默认为 true；false 在参数验证和初始取消检查通过后接管源，并在操作成功、失败或取消时关闭源。失败或取消会清理已创建缓存；源位置可能已经前进。这个入口需要完整复制一次内容，已有可定位流可直接 DetectAsync 以避免复制。

原有 Probe 继续提供轻量前缀重放；可定位 Probe 在容器检查后恢复到前缀末尾，返回的 Content 仍重放完整内容。临时存储配置与配额详见[流式平台指南](stream-platform-guide.md)。

实现依据：[PKWARE ZIP 规范](https://pkware.cachefly.net/webdocs/casestudies/APPNOTE.TXT)、[Microsoft OPC/Office 包说明](https://learn.microsoft.com/en-us/archive/msdn-magazine/2007/february/office-space-building-office-open-xml)、[OPC 部件名等价规则](https://learn.microsoft.com/en-us/dotnet/core/compatibility/core-libraries/8.0/system-io-packaging-case-insensitive-uri)、[宏主部件类型说明](https://learn.microsoft.com/en-us/openspecs/office_standards/ms-offmacro2/ec97c690-7a7e-422f-8af0-d5baa7c9e385)。未引入 Office SDK 或第三方签名数据库。

## 验证样本

测试按规范手写各格式签名及反例；完整小 PNG 使用 Python struct/zlib 按块结构生成（包含有效 CRC32），GIF 按一像素调色板及清除/像素/结束 LZW 码手工生成，均以 Base64 内嵌，ZIP/GZip 在测试运行时由 .NET 生成。完整样本用于检测入口联通，签名样本用于截断/变体/偏移边界；这些测试不宣称完成所有格式解码验证。内部额外覆盖掩码、冲突、多变体合并与部分规则保留。测试位于 `Bing.Utils.Tests/Bing/IO/FileContentDetection*Tests.cs`。

第二批 Office 用例由 ZipArchive 生成有效 ZIP，包含 OPC 清单、根关系及最小主部件；这类样本验证类型识别，未声称是 Office 应用可完整呈现的文档。额外覆盖伪造长度、读取预算、非定位重放、异步短读、取消/异常恢复；大正文验证只读取有限尾部、目录与元数据。


OPC 兼容性额外使用 `Resources/FileContentDetection` 中由 System.IO.Packaging 8.0.1 独立生成的固定夹具，覆盖编码空格、Override / Default、合法特殊扩展名及无关附件。夹具在生成时由生产器重新打开验证，测试运行不依赖该包；来源与重生成方式见同目录 README。它们仍是最小识别样本，不代表 Office 应用的完整文档验收。

ZIP 数据描述符测试通过不可定位输出流实际生成 bit 3 条目，显式验证本地头零字段、中央目录和描述符，再检查同步/异步检测、非定位有界缓存回读、临时租约清理及不一致本地字段拒绝。
