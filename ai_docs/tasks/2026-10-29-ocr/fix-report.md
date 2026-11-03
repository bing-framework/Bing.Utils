# OCR 审查修复记录

日期：2026-10-29。对应 [修复计划](fix-plan.md)，承接 [首版实施记录](execution.md)。本轮修复上轮列举的安全缺陷，并补齐真实进程、Linux 容器、断网与资源边界证据。

**IMPLEMENTATION_STATUS = PASS；TEST_STATUS = PASS；REVIEW_STATUS = PASS；OPEN_ACTIONABLE = 0。** 独立 reviewer 完成修复复核及新增 Linux 路径的定向复核。ImageSharp 兼容性迁移按用户决定延期，真实标注集和远端 CI 的状态单独列出；不宣称全仓发布门禁全部通过。

## 问题与修复

| ID / 检查项 | 最终状态 | 修改与结果 |
| --- | --- | --- |
| OCR-RV-005，MUST_FIX | CLOSED | Pillow 升至 13.0.0，同步 Windows/Linux 哈希锁和 CI；在 Image.open 前识别 PNG/JPEG/BMP、检查尺寸，扫描并拒绝 APNG；限制可调用的解码器。恶意/超限图片在完整像素分配前拒绝 |
| OCR-RV-006，MUST_FIX | CLOSED | 正文读取增加绝对期限，默认 30 秒；停滞或滴流均在期限到达后返回 408 并释放容量；错误 Content-Type 在读正文前拒绝 |
| OCR-RV-007，MUST_FIX | CLOSED | JSON 深度最多 8 层、整数文本最多 20 字符；非法编码、极长数字、过深结构统一返回 400，合法字符串转义不受误判 |
| OCR-RV-008，MUST_FIX | CLOSED | Windows 真实 Pipe 测试暴露关闭时仍有 Overlapped I/O；对 send/poll/recv 与关闭使用同一锁，先终止子进程解除读阻塞，再关闭 Pipe；复测无该异常 |
| OCR-RV-009，MUST_FIX | CLOSED | Linux 实跑确认 Paddle 3.3.1 的 PP-OCRv6 oneDNN PIR 路径报未实现指令；Windows/Linux 均固定关闭 oneDNN，配置继续进入模型身份计算；真实识别与恢复通过 |
| Captcha 集成测试的传递依赖 | CLOSED | 显式引用 System.Text.Json 8.0.6，消除原 8.0.0 公告；受影响项目 6 项测试通过，定向 NuGet 审计无漏洞条目 |
| 私有宿主验证不足 | CLOSED | 增加 5 项真实 spawn/Pipe 进程测试、2 项可选真实 Paddle 故障恢复测试、真实 HTTP 慢正文验证、Linux 构建/识别和 network=none 验证 |
| 资源证据缺失 | CLOSED / VERIFIED_BOUNDARY | 增加 Host / Resource 脚本入口；完成 1MP 满队列和 20MP 上限图片的受控实测，保留资源上限与失败分类 |

主要代码位于 [输入检查](../../../deploy/ocr/inputs.py)、[HTTP 宿主](../../../deploy/ocr/server.py)、[worker 生命周期](../../../deploy/ocr/worker.py)、[配置](../../../deploy/ocr/config.py)。新增测试位于 [安全测试](../../../deploy/ocr/tests/test_security.py)、[进程测试](../../../deploy/ocr/tests/test_process_worker.py)、[真实 Paddle 测试](../../../deploy/ocr/tests/test_real_worker.py)。

Pillow 的 XBM/APNG/GIF/EPS 等漏洞触发面通过安全版本和输入格式限制共同处理；2026-10-29 的 [Pillow 13.0.0 OSV 查询](../../../output/ocr-validation/fix-pillow-audit.json)无已登记漏洞条目。[System.Text.Json 项目审计](../../../output/ocr-validation/fix-captcha-audit.json)对应当前修复版本。ImageSharp 的既有公告仍保留，见后文决策。

## Change Impact Analysis 与验证范围

1. 输入解析、HTTP 读取期限、配置：影响 Python 私有宿主的拒绝路径及容量释放，执行安全测试、完整宿主测试和真实 HTTP 停滞上传。
2. Pillow 与锁文件：影响真实图像解码及部署，执行 Windows/Linux 真实模型识别、进程恢复、容器离线验证。
3. Pipe 关闭顺序与 oneDNN 固定配置：影响进程超时回收、崩溃重建、模型身份，执行实际子进程测试与固定模型测试。Linux 初次预热失败是定位证据，不覆盖其日志。
4. Captcha 仅改集成测试项目的 System.Text.Json 引用，执行该项目 6 项集成测试及定向审计；未改变共享生产依赖。
5. OCR .NET 公共契约、Core/Skia/Provider/DI 实现、Web 页面和打包配置本轮未改变，复用首版四目标框架、78 次单测、26 项集成、包消费者和全仓验证证据。本轮再跑 Windows/Linux .NET Engine 以验证服务联通与真实识别。
6. 最后仅整理文档、CI 挂载路径与测试进程回收时序断言，按影响检查语法/链接与 Linux 40 项测试，不重复模型下载、全仓测试或资源矩阵。

本轮未建立自动目标/任务运行记录；现有工作流中无该任务的注册项，未生成虚构 StageRun 或基线。

## 新增验证证据

原始日志位于 gitignored 的 output/ocr-validation；以下链接指向本工作区实际文件。

| 验证 | 结果 | 证据 |
| --- | --- | --- |
| Windows Host 入口 | 38 通过，2 项显式真实模型测试默认跳过 | [结果](../../../output/ocr-validation/fix-host-final/results.json)、[完整日志](../../../output/ocr-validation/fix-host-final/commands.log) |
| Windows 真实 Paddle 进程 | 2/2 通过；主动终止/挂死后回收并成功重建 | [日志](../../../output/ocr-validation/fix-real-worker.log) |
| Windows .NET Engine | PASS；中英文、空白、ROI、身份 | [结果](../../../output/ocr-validation/fix-windows-engine/results.json) |
| Linux 镜像构建 | PASS；固定 uv 0.11.1，依赖按哈希安装 | [最终构建日志](../../../output/ocr-validation/fix-docker-build-final.log) |
| Linux .NET Engine | PASS；中英文、空白、ROI、身份 | [结果](../../../output/ocr-validation/fix-linux-engine/results.json) |
| Linux 宿主 + 真实 Paddle | 40/40 通过，包含 2 项真实进程恢复 | [最终日志](../../../output/ocr-validation/fix-linux-host-tests-final.log) |
| 容器 network=none | PASS；仅 lo 接口，真实 HTTP 预热、中文/英文和空白识别 | [离线日志](../../../output/ocr-validation/fix-linux-offline.log) |
| 真实慢正文 HTTP | 9 个停滞请求约 30.06 秒全部 408，第 10 个请求 429，之后 ready=true | [结果](../../../output/ocr-validation/fix-upload-http.json) |
| Captcha 定向回归 | 6/6 通过 | [TRX](../../../output/ocr-validation/fix-captcha/jianx_LAPTOP-2V5T48RH_2026-10-29_10_11_43.trx) |

离线与故障恢复日志末尾的 Paddle SIGTERM 来自测试主动终止独占子进程和 finally 清理；命令退出码与测试结果为成功。该信号不表示识别失败。

Windows 使用隔离 Python 3.11.13；Linux 镜像实际为 Python 3.11.18。两端均为 PaddleOCR/PaddleX 3.7.0、PaddlePaddle 3.3.1、Pillow 13.0.0，三个本地模型不变。实际模型身份：

```text
ppocrv6-small:b45f39f9e5acc5436f0f17516157b6dd178a7859be4cf4164ab7e9c6f95b874b
```

原始失败记录保留：[Linux oneDNN 定位](../../../output/ocr-validation/fix-linux-warmup-diagnostic.log)、[初次 Linux 测试](../../../output/ocr-validation/fix-linux-host-tests.log)。后者暴露了测试辅助脚本挂载目录和已回收进程的断言时序问题，均已修正，最终 40 项通过。

首版全仓构建的最初失败由 TFM 强制覆盖引起，修正后的 [post-build.json](../../../output/ocr-validation/20261029-083040-Final/post-build.json)为 PASS、0 错误；首版全仓测试 14,030 通过、37 跳过。包消费者的最终 [结果](../../../output/ocr-validation/20261029-084834-PackConsumer/results.json)为 PASS。本轮没有把这些历史结果标作重新执行。

## 资源实测与边界

运行硬件：Windows x64，16 个物理核心、22 个逻辑处理器，推理线程数 2。采用 100 ms RSS 采样，范围是 Python 调度宿主及其实际子进程；不含 HTTP 请求层、.NET 和浏览器。Windows peak_wset 为各进程历史峰值之和，不能当作同时峰值，以下采用采样 RSS 合计。

| 场景 | 实际结果 | RSS 合计峰值 | 证据 |
| --- | --- | --- | --- |
| 1000×1000 PNG、受控填满队列 | 1 个活动、8 个等待；9 次真实推理成功，第 10 个调用 BusyError；队列阶段 24.984 秒 | 701,788,160 字节，约 669 MiB | [完整结果](../../../output/ocr-validation/fix-windows-queue/resource.json) |
| 5000×4000 PNG、20MP 上限 | 30.031 秒触发 InferenceTimeout；原 worker 已退出，退出码 -15 | 3,223,531,520 字节，约 3.00 GiB | [完整结果](../../../output/ocr-validation/fix-windows-maximum/resource.json) |

队列用例是受控容量测量，不是自然并发吞吐或 P95。20MP 结果分类为 VERIFIED_BOUNDARY / timeout_boundary；输入可接收不代表该硬件在 30 秒内可以完成任意 20MP 识别。测试没有放宽 30 秒期限、像素或队列限制；需要更小处理图时，调用方可按业务显式设置 ROI 或缩放。

最大图片探测的首次诊断把超时错误归为失败，[原始结果](../../../output/ocr-validation/fix-windows-resource.json)保留。确认超时类型与旧 PID 退出后，正式复测按既定 GOAL_RULES 记录有效边界；没有增加性能阈值或冒充成功识别。每个场景的诊断/正式证据均保留，未进行反复试跑选优。

## 用户决策与剩余状态

| 项目 | 状态 | 原因 / 处理 |
| --- | --- | --- |
| Drawing.ImageSharp 已知漏洞 | DEFERRED_BY_USER | 用户明确选择“保留 netstandard2.0，此项另行迁移”。未升级至缺少该资产的 3.2.0，也未改漏洞基线；[兼容性决策](imagesharp-migration.md)记录后续方案 |
| 300 张独立真实标注集、CER、正式 P95 | DEFERRED_BY_USER / BLOCKED_EXTERNAL | 用户暂时没有真实验收集，要求先完成其他修复；保留 OcrProbe 测量入口。本轮合成图及资源测试不替代质量验收 |
| GitHub Actions 远端执行 | NOT_RUN / BLOCKED_EXTERNAL | workflow 已更新，本次不包含提交/推送；本地 Windows/Linux 验证不记作远端 CI 成功 |
| 全仓发布审计 | NOT_PASS | 已修复 Captcha 测试 STJ 项；ImageSharp 既有漏洞仍存在，保留原始审计失败记录，不增加豁免基线 |
| 当前 OCR 修复 | COMPLETE | 两轮独立复核后 OCR-RV-005 至 OCR-RV-009 全部 CLOSED，OPEN_ACTIONABLE = 0；不因上述延期/外部门禁启动新一轮 Fix |

## 交付与清理

复用 [Test-Ocr.ps1](../../../build/Test-Ocr.ps1)，新增 Host 和 Resource 阶段；[部署说明](../../../deploy/ocr/README.md)含离线镜像/模型准备、正文期限、跨平台 oneDNN 配置及资源口径。[CI](../../../.github/workflows/ocr.yml)增加真实进程恢复和容器断网步骤；普通测试不下载模型。

本次启动的 Windows HTTP 宿主及其子进程均已停止，[清理证据](../../../output/ocr-validation/fix-cleanup-windows.json)无遗留 PID；bing-ocr-fix Compose 容器及网络已移除。隔离解释器、已下载模型和镜像保留，便于再次启动。未改动其他运行中的容器。

未进行 Git 暂存、提交、推送、发布或仓库发行版本修改；既有工作区变更保留。
