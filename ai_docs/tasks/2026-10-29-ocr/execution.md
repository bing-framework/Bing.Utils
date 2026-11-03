# OCR 首版实施记录

> 本文为首版历史记录。后续安全修复、Linux 容器/断网验证、资源实测及用户延期决策，以 [当前修复记录](fix-report.md) 为准；下文保留当时的结果与失败归属。

日期：2026-10-29。依据 [计划](plan.md) 完成 T1–T7 工程交付。未进行 Git 暂存、提交、推送、NuGet 发布或发行版本修改。

**工程状态：PASS；独立 Review：PASS；OPEN_ACTIONABLE = 0。** 全仓发布审计仍被既有依赖问题阻断，Linux 容器执行与正式质量门槛分别记录，不以本次冒烟替代。

## 交付与验证

| 阶段 | 状态 | 证据 |
| --- | --- | --- |
| T1 四包与契约 | PASS | Ocr、SkiaSharp、PaddleServing、DependencyInjection 均构建/打包 net8/net7/net6/netstandard2.0；六个测试项目、Web 和测量入口已加入 sln |
| T2 编排 | PASS | Core 14 用例 × 3 TFM = 42 次通过；覆盖排队满、取消、总期限、输入读取、快照、资源借用、空文字和坐标 |
| T3 图像 | PASS | 12 项 net8 图像集成：EXIF 八方向、ROI/旋转/缩放、BMP、透明白底、灰度、限额、损坏输入和取消 |
| T4 Provider | PASS | 9 项纯解析 × 3 TFM = 27 次通过；8 项本地 HTTP 集成；真实引擎单独通过 |
| T5 私有部署 | PASS（Windows CPU） | Python 3.11.13 / PaddleOCR 3.7.0 / PaddleX 3.7.0 / PaddlePaddle 3.3.1；74 包 pip check；22 项宿主与资产测试 |
| T6 DI / Web | PASS | DI 3 用例 × 3 TFM = 9 次通过；WebApplicationFactory 6 项；页面坐标 Node 测试 2 项；浏览器真实识别、EXIF、ROI、编辑和 JSON 下载 |
| T7 打包 / 文档 / 审查 | PASS | 正式 Flubu 打包集合包含四包；独立 NuGet 消费端 net6/net7/net8 运行及 netstandard2.0 编译通过；两轮独立审查后无未解决项 |

普通 .NET 单测共 78 次通过，图像/HTTP/Web 集成 26 项通过。真实 Engine 测试 1 项包括多次真实请求；部署 Python 测试 22 项；页面纯函数测试 2 项。计数不把跳过的外部测试算作通过。

## 真实引擎与离线证据

使用固定三个模型：PP-OCRv6_small_det、PP-OCRv6_small_rec、PP-LCNet_x1_0_textline_ori。11 个模型文件合计 38,340,062 字节；三份归档批准 SHA-256 已固化在 `deploy/ocr/manifest.py`，准备阶段解包前校验，运行时再次核对清单及文件。

隔离解释器：`C:/Users/jianx/AppData/Local/Temp/bingutils-ocr-venv-311/Scripts/python.exe`。模型位于 `deploy/ocr/models`，已忽略，不提交权重。

Windows 实际模型标识：

```text
ppocrv6-small:b45f39f9e5acc5436f0f17516157b6dd178a7859be4cf4164ab7e9c6f95b874b
```

- .NET Engine：中英样图识别出 `BING OCR 12345` 和 `中文识别测试`；空白图无文字；ROI 只保留英文；身份与就绪探针匹配。
- 修复尺寸解析后，HTTP + 真实 Engine 共 9 项再次通过，证据 `output/ocr-validation/review-fix/*.trx`。
- 拒绝 Python socket 连接的独立进程重新加载、预热并识别成功，拒绝连接尝试数为 0。日志 `output/ocr-validation/20261029-083332-Engine/offline.log`；探测脚本 `output/ocr-ui/offline_probe.py`。这是进程内 Python 网络边界验证，没有切断用户机器网络；Linux 容器网络隔离尚未实跑。
- Windows Paddle 3.3.1 的 oneDNN 路径出现 PP-OCRv6 PIR 操作未实现，宿主在 Windows 显式关闭 oneDNN 后成功。模型身份包含该配置，Linux 保留默认配置。

## Web 实证

通过本机浏览器上传生成的 PNG 及 EXIF=6 JPEG。JPEG 原始存储尺寸 180×700，显示和返回坐标均为 700×180；完整图片的两个文字框与内容一致。ROI 高度 90 时只返回英文。人工修改增加“（人工核对）”，下载 JSON 的 recognition.text 仍保留引擎原文，editedText 单独保存修改。

- [页面截图](../../../output/ocr-ui/verified-web.jpg)
- [导出 JSON](../../../output/ocr-ui/verified-export.json)

测量入口实际跑过两张合成图；第二条标注故意增加一个 emoji，以验证 Unicode 标量编辑距离为 1、标注字符数为 22。日志 `output/ocr-ui/measurement-check.json`。这些合成用例只验证测量程序，不是识别质量验收集，不声明 CER 或正式 P95 达标。

## 最终检查与证据复用

1. `dotnet test Bing.Utils.sln -c Release -f net8.0 --no-build --filter Category!=OcrEngine`：42 份 TRX，14,030 通过、37 跳过、0 失败。跳过项属于已有平台/外部服务条件。
2. 最初对 solution 强制 `-f net8.0` 会误覆盖既有 netstandard-only 的 Text 和测试夹具项目；新增消费模板也曾被 BuildScript 默认 Compile glob 收入。已排除模板，并将 Final 构建入口调整为保留各项目 TFM。正式执行 `dotnet build Bing.Utils.sln -c Release -v minimal`：0 错误、37 个已有依赖/目标框架警告。
3. 最终打包消费阶段：`output/ocr-validation/20261029-084834-PackConsumer/results.json` 为 PASS；其 feed 中包含四个 OCR nupkg/snupkg 和匹配版本的 Drawing 依赖包。隔离 NuGet 缓存与精确源映射确保使用本次 feed。
4. 独立审查修复后只复测受影响的 Provider、HTTP/Engine、Python 资产校验和打包集合，复用未受影响 Core、Skia、DI、Web 与全仓测试证据。没有重复全量测试循环。
5. `git diff --check` 通过。

原始日志位于 `output/ocr-validation/`（gitignored）。保留失败尝试原始记录；修正后的构建另记为 `20261029-083040-Final/post-build.json` 和 `solution-build-corrected.log`，不覆盖历史失败。

## 剩余状态与原因

| 项目 | 状态 | 归属与处理 |
| --- | --- | --- |
| 全仓 NuGet 审计 | BLOCKED_BASELINE | 现有 Drawing.ImageSharp 的 SixLabors.ImageSharp 2.1.11 高危公告及 Captcha Web 测试传递 System.Text.Json 8.0.0 未登记；审计输出无 OCR 项目。未修改已有基线、未扩展本任务升级其他模块 |
| Linux x64 容器构建/推理 | NOT_VERIFIED | Docker 守护进程未运行；已提供 Linux 锁、Dockerfile/Compose 与手动 Engine CI，Compose 配置解析通过。Windows 私有引擎已真实验证 |
| CI 远端运行 | NOT_RUN | 已写入 workflow，未提交/推送触发 |
| 300 张独立验收集、CER、正式 P95 | DEFERRED_BY_PLAN | 用户确认后续质量验收；本次提供 OcrProbe 测量入口 |
| OS/容器级断网验证 | NOT_VERIFIED | 已完成本机禁 Python socket 的加载/识别检查；未改机器网络配置 |

根据 GOAL_RULES，以上非 actionable OCR 缺陷不触发新的 Fix 循环。全仓 `Final` 入口仍会如实因既有审计问题返回非零退出码。

## 独立 Review

原始实现由主代理设计/集成，luna_worker 负责私有宿主。独立 reviewer（Codex，gpt-5.6-sol）完成首轮和定向复核。

| ID | 首轮要求 | 最终状态 | 处理 |
| --- | --- | --- | --- |
| OCR-RV-001 | MUST_FIX | CLOSED | 必须有 input_img_shape 或官方 dataInfo 尺寸；均存在时分别校验，新增兼容和拒绝测试 |
| OCR-RV-002 | MUST_FIX | CLOSED | 固定三份模型归档摘要，下载和运行时均校验，新增篡改/正确摘要测试 |
| OCR-RV-003 | SHOULD_FIX | CLOSED / NOT_AN_ISSUE | 独立复核撤销：NuGet 精确 ID 优先于通配，current 是六个仓库包的唯一源；隔离缓存避免跳过查找 |
| OCR-RV-004 | SHOULD_FIX | CLOSED | 增加 BuildScript 正式 GetPackProjects 的 ocr.pack.check，并纳入 PackConsumer/CI 及相关文件触发路径 |

RV-003 的依据：[NuGet 源映射优先级](https://github.com/NuGet/docs.microsoft.com-nuget/blob/main/docs/consume-packages/Package-Source-Mapping.md)、[官方 SearchTree 实现](https://source.dot.net/NuGet.Configuration/PackageSourceMapping/SearchTree.cs.html)。复核结论 PASS，停止自动修复。

## 使用

主入口见 [OCR 模块](../../../docs/modules/Bing.Utils.Ocr.md)、[部署](../../../deploy/ocr/README.md)、[Web 示例](../../../samples/OcrWeb/README.md)。

```powershell
pwsh -File build/Test-Ocr.ps1 -Stage Unit
pwsh -File build/Test-Ocr.ps1 -Stage Integration
pwsh -File build/Test-Ocr.ps1 -Stage PackConsumer
# 先设置 OCR_ENDPOINT、OCR_MODEL_PROFILE、OCR_FONT_PATH
pwsh -File build/Test-Ocr.ps1 -Stage Engine -RequireEngine
pwsh -File build/Test-Ocr.ps1 -Stage Final
```

本次临时 Web/推理进程在验证后关闭；环境与模型保留。正常启动按部署 README 创建项目级 .venv，或使用上述已准备的隔离解释器。
