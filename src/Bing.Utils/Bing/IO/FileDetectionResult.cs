namespace Bing.IO;

/// <summary>文件内容检测的结论；匹配不代表内容完整或安全。</summary>
public enum FileDetectionStatus
{
    /// <summary>没有匹配的已知签名。</summary>
    Unknown,
    /// <summary>唯一格式已识别。</summary>
    Matched,
    /// <summary>多个不同格式的签名已匹配。</summary>
    Ambiguous,
    /// <summary>输入起点之后没有数据。</summary>
    Empty,
    /// <summary>仍有可能匹配的规则需要更多字节。</summary>
    Incomplete
}

/// <summary>文件检测结论的原因。</summary>
public enum FileDetectionReason
{
    /// <summary>没有额外原因。</summary>
    None,
    /// <summary>已读取的内容不匹配任何内置规则。</summary>
    NoSignatureMatch,
    /// <summary>输入结束时仍缺少完成签名匹配所需的字节。</summary>
    Truncated,
    /// <summary>读取预算耗尽时仍缺少签名字节，尚未确认输入结束。</summary>
    HeaderLimitReached,
    /// <summary>请求的容器检查需要可定位的完整输入。</summary>
    SeekableSourceRequired,
    /// <summary>容器读取、目录、条目或元数据解压预算已耗尽。</summary>
    ContainerLimitReached,
    /// <summary>识别所需的 ZIP 或包元数据结构损坏、不一致或含歧义。</summary>
    InvalidContainer,
    /// <summary>容器使用本检测器未支持的 ZIP64、分卷、加密或识别所需部件压缩方式。</summary>
    UnsupportedContainer
}

/// <summary>导致检测停止的具体资源预算；None 表示没有命中预算。</summary>
public enum FileDetectionLimitKind
{
    /// <summary>未命中资源预算。</summary>
    None,
    /// <summary>文件头读取预算。</summary>
    HeaderBytes,
    /// <summary>容器阶段累计源读取预算。</summary>
    ContainerBytesRead,
    /// <summary>ZIP 中央目录字节预算。</summary>
    ZipDirectoryBytes,
    /// <summary>ZIP 条目数量预算。</summary>
    ZipEntries,
    /// <summary>识别元数据累计解压字节预算。</summary>
    MetadataBytes
}

/// <summary>检测结果所依据的证据类型。</summary>
public enum FileDetectionEvidence
{
    /// <summary>没有完整匹配的签名。</summary>
    None,
    /// <summary>字节签名匹配；未校验完整格式结构。</summary>
    Signature,
    /// <summary>已检查有界的容器目录及识别所需元数据；不代表完整文件验证。</summary>
    ContainerStructure
}

/// <summary>文件内容与声明扩展名的比较结论。</summary>
public enum FileExtensionMatch
{
    /// <summary>检测或声明不足以确定一致性。</summary>
    Indeterminate,
    /// <summary>声明扩展名属于检测出的格式。</summary>
    Match,
    /// <summary>声明扩展名属于另一种已知格式。</summary>
    Mismatch
}

/// <summary>不可变的文件格式信息。</summary>
public sealed class FileContentFormat
{
    /// <summary>获取稳定的小写标识，例如 jpeg、zip。</summary>
    public string Id { get; }
    /// <summary>获取不含参数的 MIME 类型。</summary>
    public string MimeType { get; }
    /// <summary>获取不带点号的小写扩展名及别名。</summary>
    public IReadOnlyList<string> Extensions { get; }
    /// <summary>获取此结果是否只描述通用归档或压缩容器。</summary>
    public bool IsContainer { get; }

    /// <summary>创建格式并复制扩展名集合，避免规则元数据被调用方修改。</summary>
    internal FileContentFormat(string id, string mimeType, bool isContainer, params string[] extensions)
    {
        Id = id;
        MimeType = mimeType;
        IsContainer = isContainer;
        Extensions = Array.AsReadOnly((string[])extensions.Clone());
    }
}

/// <summary>不可变的文件内容检测结果；不表示文件可解码或可信任。</summary>
public sealed class FileDetectionResult
{
    /// <summary>获取检测结论。</summary>
    public FileDetectionStatus Status { get; }
    /// <summary>获取唯一确定的格式；非 Matched 状态为 null。</summary>
    public FileContentFormat Format { get; }
    /// <summary>获取已确认并按格式合并的候选；不包含仅匹配部分前缀的规则。</summary>
    public IReadOnlyList<FileContentFormat> Candidates { get; }
    /// <summary>获取外层通用容器；ZIP 子类型或容器检查未完成时仍可为 zip。</summary>
    public FileContentFormat ContainerFormat { get; }
    /// <summary>获取完整候选的证据类型；无完整候选时为 None。</summary>
    public FileDetectionEvidence Evidence { get; }
    /// <summary>获取未知或未完成检测的原因。</summary>
    public FileDetectionReason ReasonCode { get; }
    /// <summary>获取命中的具体预算；与 ReasonCode 一起判断停止原因。</summary>
    public FileDetectionLimitKind LimitKind { get; }
    /// <summary>获取实际从流读取或从字节输入检查的前缀长度。</summary>
    public int BytesRead { get; }
    /// <summary>获取容器阶段额外读取或检查的累计字节数；不含文件头，重复范围重复计数。</summary>
    public long ContainerBytesRead { get; }

    /// <summary>固定本次检测结论与候选快照。</summary>
    internal FileDetectionResult(FileDetectionStatus status, FileDetectionReason reason,
        IEnumerable<FileContentFormat> candidates, int bytesRead, FileContentFormat container = null,
        FileDetectionEvidence? evidence = null, long containerBytesRead = 0, FileDetectionLimitKind limitKind = FileDetectionLimitKind.None)
    {
        Status = status;
        ReasonCode = reason;
        LimitKind = reason == FileDetectionReason.HeaderLimitReached ? FileDetectionLimitKind.HeaderBytes : limitKind;
        Candidates = Array.AsReadOnly(candidates.ToArray());
        Format = status == FileDetectionStatus.Matched ? Candidates[0] : null;
        BytesRead = bytesRead;
        ContainerFormat = container ?? (Format?.IsContainer == true ? Format : null);
        Evidence = evidence ?? (Candidates.Count == 0 ? FileDetectionEvidence.None : FileDetectionEvidence.Signature);
        ContainerBytesRead = containerBytesRead;
    }
}
