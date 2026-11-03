# OCR 审查修复计划

2026-10-29。用户授权修复上轮全部列举问题；依赖修复包含必要安全版本升级，不变更仓库发行版本，不暂存/提交/推送/发布。保留已有 Captcha、OTP 等工作区修改。

当前：本地修复与验证完成，独立复核 PASS，OPEN_ACTIONABLE = 0。详见 [修复记录](fix-report.md)。

## 本地实施项

- [x] OCR-RV-005：Pillow 13.0.0 安全升级、Windows/Linux 哈希锁及 CI 同步；开图前校验 PNG/JPEG/BMP 格式与尺寸，拒绝动画 PNG，限制解码器；新增恶意格式/提前分配回归。
- [x] OCR-RV-006：正文读取绝对期限，超时返回 408，容量及时释放；测试停滞、滴流、取消和容量复用。
- [x] OCR-RV-007：限制 JSON 深度与数字大小，非法 JSON 统一 400；保留合法字符串转义与严格协议。
- [x] OCR-RV-008：修复真实 Windows Pipe 关闭时仍存在 Overlapped I/O 的生命周期竞争，真实进程复测通过。
- [x] OCR-RV-009：Linux 实跑定位 oneDNN PIR 不支持，固定关闭该路径并纳入模型身份；Linux 真实识别通过。
- [x] V1：真实 spawn/Pipe 子进程挂死、崩溃、取消、启动中关闭与重建测试；Windows/Linux 真实 Paddle 恢复验证。
- [x] V2：Linux 容器构建/识别；network=none 下真实 HTTP、本地模型加载与识别。远端 CI workflow 已准备，执行状态单列。
- [x] V3：最大允许图片、满队列容量与进程内存峰值测量入口及受控实测；20MP 超时且进程回收归类 VERIFIED_BOUNDARY；未自定正式性能阈值。
- [x] V4a：Captcha 集成测试显式 System.Text.Json 8.0.6，6 项回归及定向审计通过。
- [x] V4b：ImageSharp 兼容性核查并形成迁移决策；用户要求保留 netstandard2.0，实际迁移 DEFERRED_BY_USER。
- [x] 两轮独立只读复核、最终报告、临时宿主/容器清理。

## 验证与外部依赖

按 Change Impact Analysis 执行 Python 安全回归、完整宿主、真实进程及实际引擎、Linux 和离线验证。未改变共享生产依赖，OCR 核心、几何、Web、四目标框架和打包契约复用首版证据；仅对变更的 Captcha 测试项目定向验证。

- 真实 300 张图片/人工标注、CER/P95 正式阈值：用户明确暂缺，DEFERRED_BY_USER / BLOCKED_EXTERNAL。
- ImageSharp：用户明确“保留 netstandard2.0，此项另行迁移”，DEFERRED_BY_USER；不修改漏洞豁免基线。
- GitHub Actions 远端运行：NOT_RUN / BLOCKED_EXTERNAL，本次不包含提交/推送；本地结果不替代远端记录。
- 全仓发布审计：ImageSharp 既有漏洞尚在，不能宣称通过。

每轮先 CIA；独立只读复核最多三轮，只在 OPEN_ACTIONABLE > 0 时继续修复。实际两轮复核后为 0，停止自动修复。主代理负责架构、集成和最终验证，luna_worker 负责独立进程测试及资源测量工具。
