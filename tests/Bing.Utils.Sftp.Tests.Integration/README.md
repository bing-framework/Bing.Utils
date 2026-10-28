# SFTP 集成测试

测试连接到用户明确指定的受控 OpenSSH SFTP 服务，不访问外网服务，也不修改本机 SSH 服务配置。未配置环境变量时，测试以 `BLOCKED_EXTERNAL` 原因动态跳过；设置后会执行完整测试。

## 环境变量

| 变量 | 用途 |
| --- | --- |
| `BING_SFTP_HOST` | 测试服务地址 |
| `BING_SFTP_PORT` | 端口，省略时为 `22` |
| `BING_SFTP_USER` | 专用测试用户 |
| `BING_SFTP_PASSWORD` | 密码认证；或改用私钥变量 |
| `BING_SFTP_PRIVATE_KEY_PATH` | 私钥认证，可选口令变量 `BING_SFTP_PRIVATE_KEY_PASSPHRASE` |
| `BING_SFTP_HOST_KEY_SHA256` | `ssh-keygen -E sha256` 输出的 SHA-256 指纹 |
| `BING_SFTP_REMOTE_DIRECTORY` | 测试用户可写的远端 POSIX 目录 |

## Docker 启用示例

以下命令只启动并移除一次性容器。请从 `ssh-keygen` 输出中复制 `SHA256:...` 指纹到环境变量；测试目录使用容器账户的 `upload` 目录。

```powershell
docker run --rm --name bing-sftp-fixture -p 2222:22 -d atmoz/sftp integration:integration-pass:1001:100:upload
docker exec bing-sftp-fixture ssh-keygen -lf /etc/ssh/ssh_host_ed25519_key.pub -E sha256
$env:BING_SFTP_HOST = '127.0.0.1'
$env:BING_SFTP_PORT = '2222'
$env:BING_SFTP_USER = 'integration'
$env:BING_SFTP_PASSWORD = 'integration-pass'
$env:BING_SFTP_HOST_KEY_SHA256 = 'SHA256:替换为上一步指纹'
$env:BING_SFTP_REMOTE_DIRECTORY = '/upload'
dotnet test tests/Bing.Utils.Sftp.Tests.Integration/Bing.Utils.Sftp.Tests.Integration.csproj -c Debug
docker stop bing-sftp-fixture
```

夹具覆盖错主机指纹、错认证、上传/下载、目录枚举与属性、远端和本地目标冲突保护、上传/文件下载中取消及暂存清理、竞争目标导致的 rename 拒绝，并断言读取/写入最终事件先于提交事件。

## 本地 Docker 一键验证

执行器要求 PowerShell 7.3 或更高版本。在仓库根目录运行：

```powershell
pwsh -NoLogo -NoProfile -File .\tests\Bing.Utils.Sftp.Tests.Integration\scripts\Invoke-SftpDockerValidation.ps1
```

执行器固定使用 PowerShell `Standard` 原生命令参数模式，并显式捕获 stderr 与退出码。修改执行器后可用 Python 原生进程做本地语义回归：

```powershell
pwsh -NoLogo -NoProfile -File .\tests\Bing.Utils.Sftp.Tests.Integration\scripts\Test-SftpDockerValidationSemantics.ps1
```

脚本在 Docker daemon 未就绪时会启动已安装的 Docker Desktop 并有界等待，然后拉取 `atmoz/sftp:latest`，记录本次解析出的镜像 ID/digest 和运行环境。它创建带唯一名称和标签的临时容器，SFTP 端口只绑定 `127.0.0.1`，不挂载宿主目录、不使用特权或 Docker socket；客户端密钥和随机密码只在本次任务临时目录/进程中生成。脚本分别以密码和私钥运行集成测试，再通过真实 SFTP 下载非空中文 TSV 并执行组合样例。

TRX、脱敏运行日志、镜像身份及样例报告保存在 `ai_docs/tasks/2026-10-28-sftp-docker-validation/artifacts/<RunId>`。结束时脚本只移除带本次唯一标签且身份匹配的容器，并只删除本任务目录下本次生成的临时目录；环境变量恢复原值。Docker Desktop 若由脚本启动会保持运行。rename 请求后服务端提交但丢弃响应、以及远端暂存文件清理失败是独立故障窗口；除非执行结果明确证明对应注入已发生，否则仍标为未验证。
