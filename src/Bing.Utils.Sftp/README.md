# Bing.Utils.Sftp

基于 SSH.NET 的 SFTP 上传、下载和目录查询。必须固定 SHA-256 主机密钥；上传使用同目录唯一暂存文件，默认拒绝已有目标，rename 成功后才报告提交。流下载失败时可能留下部分输出；文件下载使用本地同目录暂存发布。组合示例见 `docs/integration-capabilities.md`。

目标框架：`netstandard2.0;net8.0`。
