# 工具能力扩展实施计划

用户已批准全部能力分阶段实施，并批准 DataTable/DataSet 本轮实现双向格式。

## 边界

- 保留当前工作区全部已有修复、公开成员、四个目标框架和默认配置。
- 不升级依赖、不修改版本、不提交、不推送、不发布。
- 基线为本轮开始时的实际工作区；原始源码、构建日志、产物及清单保存在 `artifacts/capability-expansion-2026-10-23/baseline`。
- ImageSharp 迁移、既有图像/反射性能门禁单列。仅 OPEN_ACTIONABLE 触发修复，不因外部检查未运行重复修复。

## 工作项

| ID | 实施内容 | 主要验收 |
|---|---|---|
| A01 | SaveFile 重载递归修复 | 所有重载正常写入 |
| A02 | 同步字节写入句柄和截断修复 | 新建、覆盖、追加、释放 |
| A03 | JSON 文件覆盖截断 | 长内容改短后完整解析 |
| A04 | FTPS 默认验证证书 | 有效/无效证书和显式策略 |
| A05 | PowerShell 双路输出消费 | 大输出不阻塞 |
| A06 | 表格 JSON 双向格式 | 类型、空值、结构、约束和关系往返 |
| B01 | 原子文件写入 | 提交前失败保留原文件 |
| B02 | 有界流传输 | 限额、取消、进度、SHA256 |
| B03 | 严格文件读取 | 异常/取消传播、流位置 |
| B04 | ZIP 多维预算和取消 | 条目/单文件/总量/深度 |
| B05 | Regexs/ZipHelper 便捷安全入口 | 超时和限额到达底层 |
| B06 | 大流有效数据分段 | 不暴露无效容量，生命周期说明 |
| B07 | 流写文件消除完整数组 | 保留原位置/关闭约定 |
| C01 | 分段下载直接存储 | 响应存活期间写入 |
| C02 | 分段下载固定工作任务 | 在途工作有界 |
| C03 | 最终下载总量预算 | 重试、未知长度、续传 |
| C04 | 异步序列有界遍历/转换 | 取消、异常、提前停止、顺序 |
| C05 | JSON 数组流式写入 | 增量、刷新、取消 |
| C06 | NDJSON 读写 | 单记录字节限额、UTF8、错误位置 |
| C07 | JSON 内部默认选项复用 | 兼容输出/区域，保留调用方选项 |
| D01 | 结构化异步命令入口 | 参数、退出码、捕获上限、取消/超时 |
| D02 | 原生异步 FTP 与配置能力 | 不扩充旧接口必需成员、明确支持矩阵 |
| E01 | 显式时区/时钟 | 夏令时、跨日 |
| E02 | 工作日历 | 节假日、补班、正负偏移 |
| E03 | 自定义字符集安全 ID | 无偏、分块、边界 |
| E04 | HTTP 诊断适配 | 现代框架 Activity/Meter，旧框架回调 |

## 表格 JSON v1

新入口显式使用 `$format=bing.tabular`、`$version=1`；旧转换器默认写出 Rows/Tables 保持不变。旧格式读取必须提供 schema。
包含表名、命名空间、区域和大小写规则、列顺序/类型/空值/默认值/长度/只读/自增/日期模式、主键、唯一约束、外键、关系和数据集约束状态。
行值按列顺序编码。类型采用白名单；null 对应 DBNull，长整数/decimal/浮点采用精确字符串编码，日期采用往返格式，二进制 Base64。
只保存当前数据快照；删除行、表达式、自定义对象列、非空扩展属性明确拒绝。行历史与自增内部计数不保证恢复。
在新建临时对象中恢复，成功才返回；失败不修改调用方 schema。读取提供字节数、表数、列数、行数和深度限制。

## 验证与交付

定向回归 → 受影响项目完整测试 → API 消费者与实际 netstandard2.0 验证 → 最终解决方案验证。
SDK 固定 8.0.425，保留实际 .NET Core 3.1 验证。FTP 真实服务和外部 Windows/Linux CI 独立记录。
每项新 API 提供中文契约；测试按生产模块归属；不做无关迁移。
性能记录同环境耗时、全线程分配和操作次数，最多一次诊断/一次正式确认；新功能 before=NOT_APPLICABLE。
完成后独立审查取消、生命周期、提交失败、限额、TLS 与结构恢复。

## 主要命令

```powershell
dotnet build Bing.Utils.sln -c Release
dotnet test tests/Bing.Utils.Tests/Bing.Utils.Tests.csproj -c Release -f net8.0
dotnet test tests/Bing.Utils.Http.Tests/Bing.Utils.Http.Tests.csproj -c Release -f net8.0
dotnet test tests/Bing.Utils.Http.Tests.Integration/Bing.Utils.Http.Tests.Integration.csproj -c Release -f net8.0
dotnet test tests/Bing.Utils.Net.Tests/Bing.Utils.Net.Tests.csproj -c Release -f net8.0
dotnet test tests/Bing.Utils.DateTime.Tests/Bing.Utils.DateTime.Tests.csproj -c Release -f net8.0
dotnet test tests/Bing.Utils.IdUtils.Tests/Bing.Utils.IdUtils.Tests.csproj -c Release -f net8.0
pwsh ./build/Test-ReviewNetStandard.ps1
dotnet test Bing.Utils.sln -c Release -f net8.0 --no-build
pwsh ./build/Test-NuGetAudit.ps1
```
