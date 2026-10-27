namespace Bing.IO;

/// <summary>内置格式的共享不可变实例；Office 子类型需要显式容器检查。</summary>
public static class FileContentFormats
{
    /// <summary>获取 JPEG 格式信息。</summary>
    public static FileContentFormat Jpeg { get; } = new FileContentFormat("jpeg", "image/jpeg", false, "jpg", "jpeg", "jpe");

    /// <summary>获取 PNG 格式信息。</summary>
    public static FileContentFormat Png { get; } = new FileContentFormat("png", "image/png", false, "png");

    /// <summary>获取 GIF 格式信息。</summary>
    public static FileContentFormat Gif { get; } = new FileContentFormat("gif", "image/gif", false, "gif");

    /// <summary>获取 BMP 格式信息。</summary>
    public static FileContentFormat Bmp { get; } = new FileContentFormat("bmp", "image/bmp", false, "bmp");

    /// <summary>获取 TIFF 格式信息。</summary>
    public static FileContentFormat Tiff { get; } = new FileContentFormat("tiff", "image/tiff", false, "tif", "tiff");

    /// <summary>获取 ICO 格式信息。</summary>
    public static FileContentFormat Ico { get; } = new FileContentFormat("ico", "image/x-icon", false, "ico");

    /// <summary>获取 PDF 格式信息。</summary>
    public static FileContentFormat Pdf { get; } = new FileContentFormat("pdf", "application/pdf", false, "pdf");

    /// <summary>获取 ZIP 格式信息。</summary>
    public static FileContentFormat Zip { get; } = new FileContentFormat("zip", "application/zip", true, "zip");

    /// <summary>获取 GZIP 格式信息。</summary>
    public static FileContentFormat Gzip { get; } = new FileContentFormat("gzip", "application/gzip", true, "gz", "gzip");

    /// <summary>获取 7Z 格式信息。</summary>
    public static FileContentFormat SevenZip { get; } = new FileContentFormat("7z", "application/x-7z-compressed", true, "7z");

    /// <summary>获取 RAR 格式信息。</summary>
    public static FileContentFormat Rar { get; } = new FileContentFormat("rar", "application/vnd.rar", true, "rar");

    /// <summary>获取 FLAC 格式信息。</summary>
    public static FileContentFormat Flac { get; } = new FileContentFormat("flac", "audio/flac", false, "flac");

    /// <summary>获取 WEBP 格式信息。</summary>
    public static FileContentFormat WebP { get; } = new FileContentFormat("webp", "image/webp", false, "webp");

    /// <summary>获取 WAV 格式信息。</summary>
    public static FileContentFormat Wav { get; } = new FileContentFormat("wav", "audio/wav", false, "wav");

    /// <summary>获取 AVI 格式信息。</summary>
    public static FileContentFormat Avi { get; } = new FileContentFormat("avi", "video/x-msvideo", false, "avi");

    /// <summary>获取 DOCX 格式信息。</summary>
    public static FileContentFormat Docx { get; } = new FileContentFormat("docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document", false, "docx");

    /// <summary>获取 XLSX 格式信息。</summary>
    public static FileContentFormat Xlsx { get; } = new FileContentFormat("xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", false, "xlsx");

    /// <summary>获取 PPTX 格式信息。</summary>
    public static FileContentFormat Pptx { get; } = new FileContentFormat("pptx", "application/vnd.openxmlformats-officedocument.presentationml.presentation", false, "pptx");

    /// <summary>获取 DOCM 格式信息。</summary>
    public static FileContentFormat Docm { get; } = new FileContentFormat("docm", "application/vnd.ms-word.document.macroEnabled.12", false, "docm");

    /// <summary>获取 XLSM 格式信息。</summary>
    public static FileContentFormat Xlsm { get; } = new FileContentFormat("xlsm", "application/vnd.ms-excel.sheet.macroEnabled.12", false, "xlsm");

    /// <summary>获取 PPTM 格式信息。</summary>
    public static FileContentFormat Pptm { get; } = new FileContentFormat("pptm", "application/vnd.ms-powerpoint.presentation.macroEnabled.12", false, "pptm");

    /// <summary>获取全部内置格式，包括需要容器结构证据的六种 Office 子类型。</summary>
    public static IReadOnlyList<FileContentFormat> SupportedFormats { get; } = Array.AsReadOnly(new[]
    {
        Jpeg, Png, Gif, Bmp, Tiff, Ico, Pdf, Zip, Gzip, SevenZip, Rar, Flac, WebP, Wav, Avi, Docx, Xlsx, Pptx, Docm, Xlsm, Pptm
    });
}
