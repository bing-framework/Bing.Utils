# API 文档

本目录由 DocFX 根据 `src` 下的 .NET 项目和 XML 文档注释生成 API 页面。

当前仓库仅保留入口文件，尚未提交生成后的 API 页面。需要本地生成时，在 `docs` 目录运行 DocFX，并以 [API 参考说明](../reference/api-reference.md) 和 [模块文档索引](../modules/README.md) 作为人工维护的补充入口。

新增扩展包的 API XML 文档来自 `Bing.Utils.Sftp`、`Bing.Utils.Templates.Scriban` 和 `Bing.Utils.Text.EncodingDetection`。

端到端的 SFTP 下载、编码转码、TSV 映射和模板报告示例见 [集成能力示例](../integration-capabilities.md)。

**待确认**：生成站点是否作为发布产物提交仓库，或仅由 CI 构建和发布。
