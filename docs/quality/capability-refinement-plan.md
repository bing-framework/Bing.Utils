# 工具能力收敛实施计划（2026-10-23）

用户已授权继续实现四项设计，并明确同意 LargeMemoryStream 默认页改为 64 KiB。保留当前未提交成果、公开旧入口、目标框架及依赖版本；不提交、推送、发布。

## 工作包与验收

| ID | 内容 | 验收 |
|---|---|---|
| R01 | 默认小页与几何页表 | 空实例不租页、默认首写、跨页、缩容增长、租用失败及归还 |
| R02 | C02 固定工作循环与单次读取合并 | 取消、重试、回调失败、摘要和长度校验、旧目标保护；同环境性能证据 |
| R03 | FTP 显式完整配置模式 | TLS、代理、传输类型、目录、连接重试、最外层操作释放；不支持的超时明确诊断 |
| R04 | 表格 v2 逐行流处理 | 结构先行、单次枚举、短读、预算、取消、独立行和资源归属 |
| R05 | 表格编辑状态恢复 | Original/Current/Proposed、四种在表行状态、错误、约束和后续接受/拒绝/取消编辑行为 |
| R06 | 消费者、跨框架、文档和审查 | 模块完整测试、实际 netstandard、最终构建；局部和外部证据分别记录 |

## 技术边界

- C02 保留公开 checkpoint 的独立快照和逐片持久化时点。增量日志仅在本轮证据证明状态保存是主要瓶颈时推进，不静默降低持久化频率。
- FTP 旧入口保持兼容，完整配置显式启用。FluentFTP 42.0.1 无独立发送及同步断开超时契约，不用后台任务等待超时伪装为底层已中止。
- 表格 Legacy/v1 默认保持。v2 支持当前已允许的列类型与结构；编辑状态指表内行版本与错误，不包括事件、DataView、脱离行、内部自增游标或任意 CLR 对象。
- 流式消费内存随结构和最大记录增长；返回完整 DataTable/DataSet 仍随行数增长。提前停止或失败不回滚已交付记录。

## 影响与验证

生产项目：Bing.Utils、Bing.Utils.Http、Bing.Utils.Net。主包测试承载内存流和表格，HTTP/Net 使用各自测试项目；不迁移无关测试。

公共契约新增 v2 与完整 FTP 配置；默认页布局改变已批准。运行路径涉及池资源、并发、磁盘提交、网络配置和序列化，风险 HIGH。不改 TFM/依赖/打包配置。

先定向测试，再受影响项目全测，最后消费者、实际 netstandard 和解决方案检查。性能运行不与构建/测试并行；每候选/负载最多一次诊断和一次确认。旧图像、反射及其他未变范围复用已有证据。

固定基线：`artifacts/capability-refinement-2026-10-23/baseline`，包含实施开始时的工作区源码、产物与身份。既有异常调度基准复用原工作负载；新增成功/合并/流式能力明确可比边界。

```powershell
dotnet test tests/Bing.Utils.Tests/Bing.Utils.Tests.csproj -c Release -f net8.0
dotnet test tests/Bing.Utils.Http.Tests/Bing.Utils.Http.Tests.csproj -c Release -f net8.0
dotnet test tests/Bing.Utils.Http.Tests.Integration/Bing.Utils.Http.Tests.Integration.csproj -c Release -f net8.0
dotnet test tests/Bing.Utils.Net.Tests/Bing.Utils.Net.Tests.csproj -c Release -f net8.0
pwsh ./build/Test-ReviewNetStandard.ps1
dotnet build Bing.Utils.sln -c Release
dotnet test Bing.Utils.sln -c Release -f net8.0 --no-build
```

仅 OPEN_ACTIONABLE 驱动进一步修复；真实 FTP/FTPS/代理和远程 CI 未执行时分别记录，不以重复本地测试替代。
