# Bing.Utils.Sftp

使用 SHA-256 主机密钥固定连接 SFTP，支持密码或私钥认证、目录项/文件信息以及流和文件传输。每次操作使用独立会话，不在同一 SSH/SFTP 会话上并行传输。上传以同目录唯一远端暂存路径写入，默认拒绝已有目标，只有 rename 成功才报告已提交；rename 请求发出后连接中断会标记结果不确定。流下载失败时目标流可能保留部分输出；文件下载先写本地同目录临时文件再发布。

可运行的 Docker/OpenSSH 环境驱动测试说明见 `tests/Bing.Utils.Sftp.Tests.Integration/README.md`。当前主机没有可用的 SSH 服务或 Docker daemon，因此真实服务器交互标记为 `BLOCKED_EXTERNAL`。
