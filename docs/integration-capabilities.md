# 集成能力示例

`samples/IntegrationCapabilities` 展示三个扩展包与核心 CSV/表格映射 API 的组合流程：通过固定主机指纹的 SFTP 客户端下载远端 TSV 到本地同目录暂存文件，使用有界编码检测和增量转码生成 UTF-8 文件，再由 `CsvStream` 和 `TabularMap<T>` 逐行解析，最后用受限 Scriban 渲染报告。

项目目标为 .NET 8，可在没有 SFTP 服务时编译：

```powershell
dotnet build samples/IntegrationCapabilities/IntegrationCapabilities.csproj -c Debug
```

样例提供无需 SFTP 服务的非空本地组合验证，使用同一 TSV 映射和报告模板：

```powershell
dotnet run --project samples/IntegrationCapabilities/IntegrationCapabilities.csproj -- --self-test
```

运行时需要一个受控 SFTP 服务，并设置以下环境变量：

| 环境变量 | 用途 |
| --- | --- |
| `BING_SFTP_HOST` | SFTP 主机 |
| `BING_SFTP_PORT` | 端口，可省略，默认 `22` |
| `BING_SFTP_USER` | 用户名 |
| `BING_SFTP_PASSWORD` | 密码认证；使用私钥时省略 |
| `BING_SFTP_PRIVATE_KEY` | 可选私钥文件路径 |
| `BING_SFTP_PRIVATE_KEY_PASSPHRASE` | 可选私钥口令 |
| `BING_SFTP_HOSTKEY_SHA256` | 必需 SHA-256 主机指纹，可带 `SHA256:` 前缀 |
| `BING_REPORT_CULTURE` | 可选数字解析/模板文化，默认 `zh-CN` |

TSV 必须包含 `姓名` 和 `金额` 表头。金额按 `BING_REPORT_CULTURE` 解析。ASCII 兼容输入按调用方显式配置的 UTF-8 回退处理；其他低置信度检测会失败。模板不加载外部 include，不调用宿主方法，并受字符、循环和递归限制。

```powershell
dotnet run --project samples/IntegrationCapabilities/IntegrationCapabilities.csproj -- /reports/month.tsv ./report.txt
```

`BING_SFTP_PRIVATE_KEY` 非空时使用私钥认证，否则需要密码。示例使用 `DownloadFileAsync`，远端内容完整下载后才发布本地目标；直接使用流下载时，失败可能留下部分输出。
