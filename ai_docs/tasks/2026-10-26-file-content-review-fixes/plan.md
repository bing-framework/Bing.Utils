# 文件内容检测审查修复计划

范围：用户批准的结构识别修复、资源诊断及便捷 API。保留现有浅检测行为和枚举数值，不调整依赖、项目、版本或其他模块。

1. 主部件必须是普通 OPC 部件，唯一映射到目录条目；验证本地头、名称、标志、压缩方式、长度和数据范围，不解压正文。
2. 分离严格 PartName 与关系 Target 的根相对解析；拒绝非法转义、点段、伪造扩展名，允许合法路径段中的冒号。
3. 先检查尾部 22 字节；无有效目录时才读取剩余注释窗口。按距离文件尾由近到远验证候选中央目录，选择第一个完整有效目录；无效候选继续搜索，真实读取累计计费。该明确优先级不承诺检测所有嵌套 ZIP 歧义，也不回退掩盖已选目录内的 OPC 错误。
4. 增加 FileDetectionLimitKind / LimitKind，区分头、容器读取、目录、条目数与元数据预算。
5. 新增 ProbeBuffered / ProbeBufferedAsync：调用方提供 TemporaryFileStore 与必填 maxContentBytes；默认深检测，显式 options 受尊重。SpilloverStream + StreamTransfer 有界缓存，返回 FileContentBuffer（Result + 从零开始的只读可定位 Content）。失败清理缓存；leaveOpen=false 在验证及初始取消通过后接管源，并在操作结束时关闭。超限最多读取一个额外判定字节。
6. 增加 CompareFileName、FileContentFormats 的共享格式常量与 SupportedFormats 只读目录；补文档和示例。

验证：新回归与全部 FileContentDetection 测试先跑 net8.0；检查 net6.0/net7.0/netcoreapp3.1（覆盖 netstandard2.0）兼容；四个生产 TFM 构建；最后 Bing.Utils.Tests net8.0 全量。源读取量用计数流验证，不进行无关基准。

Change Impact Analysis：生产范围仅 Bing.Utils/Bing/IO/FileContent*、FileDetection*；测试归属主包 Bing.Utils.Tests/Bing/IO。公共契约增量；运行路径为 ZIP32/OPC 检测、显式流缓存；无 Provider、打包、依赖或构建图变化；风险 MEDIUM。不修改既有计时测试修复。仅 OPEN_ACTIONABLE 驱动修复，最多三轮，已通过且相关源码未变的证据复用。

任务分工：主代理负责解析器、诊断、目录、集成验证与文档；luna_worker 负责已冻结契约的缓存便捷入口及其生命周期测试，不并行运行 dotnet。

执行状态：6 项全部完成，最终证据与状态分类见 execution.md；独立复核见 review.md。
