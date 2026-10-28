using System.Globalization;
using System.Text;
using Bing.Serialization.Csv;
using Bing.Sftp;
using Bing.Tabular;
using Bing.Text.EncodingDetection;
using Bing.Text.Templating.Scriban;

if (args.Length == 1 && args[0] == "--self-test")
{
    await RunLocalValidationAsync();
    return 0;
}

if (args.Length != 2)
{
    Console.Error.WriteLine("用法: IntegrationCapabilities <远端 TSV 路径> <报告输出路径>");
    return 2;
}

var token = CancellationToken.None;
var privateKeyPath = Environment.GetEnvironmentVariable("BING_SFTP_PRIVATE_KEY");
var sftpOptions = new SftpConnectionOptions
{
    Host = Required("BING_SFTP_HOST"),
    Port = int.TryParse(Environment.GetEnvironmentVariable("BING_SFTP_PORT"), out var port) ? port : 22,
    UserName = Required("BING_SFTP_USER"),
    Password = string.IsNullOrWhiteSpace(privateKeyPath) ? Required("BING_SFTP_PASSWORD") : null,
    PrivateKeyPath = string.IsNullOrWhiteSpace(privateKeyPath) ? null : privateKeyPath,
    PrivateKeyPassPhrase = Environment.GetEnvironmentVariable("BING_SFTP_PRIVATE_KEY_PASSPHRASE"),
    HostKeySha256 = Required("BING_SFTP_HOSTKEY_SHA256")
};
var culture = CultureInfo.GetCultureInfo(Environment.GetEnvironmentVariable("BING_REPORT_CULTURE") ?? "zh-CN");
var downloadedPath = Path.Combine(Path.GetTempPath(), $"bing-integration-{Guid.NewGuid():N}.source");
var utf8Path = Path.Combine(Path.GetTempPath(), $"bing-integration-{Guid.NewGuid():N}.utf8");
try
{
    var sftp = new SftpTransferClient(sftpOptions);
    await sftp.DownloadFileAsync(args[0], downloadedPath, cancellationToken: token);
    await using (var downloaded = new FileStream(downloadedPath, FileMode.Open, FileAccess.Read, FileShare.Read,
                     81920, FileOptions.Asynchronous | FileOptions.SequentialScan))
    await using (var utf8Output = new FileStream(utf8Path, FileMode.CreateNew, FileAccess.Write, FileShare.None,
                     81920, FileOptions.Asynchronous | FileOptions.SequentialScan))
    {
        await TextEncodingDetector.TranscodeToUtf8Async(downloaded, utf8Output,
            new EncodingDetectionOptions
            {
                FallbackEncoding = Encoding.UTF8,
                MinimumConfidence = 0.8,
                ReplaceInvalidSequences = false
            }, cancellationToken: token);
    }

    List<ReportRow> rows;
    await using (var utf8 = new FileStream(utf8Path, FileMode.Open, FileAccess.Read, FileShare.Read,
                     81920, FileOptions.Asynchronous | FileOptions.SequentialScan))
    {
        rows = await ReadRowsAsync(utf8, culture, token);
    }

    var template = CreateReportTemplate(culture);
    var report = await template.RenderAsync(new { rows }, token);
    await File.WriteAllTextAsync(args[1], report, new UTF8Encoding(false), token);
    Console.WriteLine($"已写入 {args[1]}，共 {rows.Count} 条。");
    return 0;
}
finally
{
    if (File.Exists(downloadedPath)) File.Delete(downloadedPath);
    if (File.Exists(utf8Path)) File.Delete(utf8Path);
}

static string Required(string name)
    => Environment.GetEnvironmentVariable(name) is { Length: > 0 } value
        ? value
        : throw new InvalidOperationException($"必须设置环境变量 {name}。");

static TabularMap<ReportRow> CreateReportMap(CultureInfo culture)
    => TabularMap<ReportRow>.Create(configuration =>
    {
        configuration.Field(row => row.Name).From("姓名").RequiredColumn().RequiredValue();
        configuration.Field(row => row.Amount).From("金额").RequiredColumn().RequiredValue();
    }, new TabularMapOptions { Culture = culture });

static async Task<List<ReportRow>> ReadRowsAsync(Stream utf8, CultureInfo culture, CancellationToken cancellationToken)
{
    var map = CreateReportMap(culture);
    var rows = new List<ReportRow>();
    await foreach (var csvRow in CsvStream.ReadAsync(utf8,
                       new CsvOptions { Delimiter = '\t', HasHeader = true, MaxRows = 10_000 },
                       leaveOpen: true, cancellationToken: cancellationToken))
    {
        var result = map.Map(csvRow, cancellationToken);
        if (!result.IsSuccess)
            throw new InvalidDataException($"第 {csvRow.RowNumber} 行映射失败：{string.Join("；", result.Issues.Select(issue => issue.Message))}");
        rows.Add(result.Value);
    }

    return rows;
}

static CompiledTemplate CreateReportTemplate(CultureInfo culture)
    => new ScribanTemplateEngine().Compile(
        "导入报告（{{ rows.size }} 条）\n{{ for row in rows }}- {{ row.Name }}：{{ row.Amount }}\n{{ end }}",
        new ScribanTemplateOptions { TemplateName = "TSV 导入报告", Culture = culture });

static async Task RunLocalValidationAsync()
{
    var culture = CultureInfo.InvariantCulture;
    using var tsv = new MemoryStream(new UTF8Encoding(false).GetBytes("姓名\t金额\r\nAlice\t12.5\r\n"));
    var rows = await ReadRowsAsync(tsv, culture, CancellationToken.None);
    var report = await CreateReportTemplate(culture).RenderAsync(new { rows });
    if (!report.Contains("Alice", StringComparison.Ordinal) || !report.Contains("12.5", StringComparison.Ordinal))
        throw new InvalidOperationException("本地组合验证未在报告中找到映射的姓名和金额。");
    Console.WriteLine("本地组合验证通过：TSV 行已映射并渲染到报告。");
}

public sealed class ReportRow
{
    public string Name { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}
