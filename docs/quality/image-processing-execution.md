# 图像处理重构执行记录

## 范围与基线

- 批准范围：三个后端的静态 JPEG/PNG 上传处理、方向纠正、元数据、压缩、水印、拼图、边框圆角及标注。
- 分支：`codex/image-processing-refactor`；原 `feature/security` 未提交的 Http 修改保留，不属于本次图像变更。
- 影响：Shared 新增程序集及公共类型迁移；三个图像后端、对应测试和消费者；风险 HIGH。引擎版本、原有目标框架及发行版本保持不变。
- 新增依赖仅限批准的 ImageSharp.Drawing 1.0.0、Fonts 1.0.0。
- 基线产物位于 `TestResults/image-processing/`：`baseline-drawing-net8.trx` 79/79、`baseline-imagesharp-net8.trx` 238/238、`baseline-skia-net8.trx` 140/140，均无跳过。

## 实现及兼容性

- Shared 已成为独立程序集，公共选项、几何、限额、编码元数据及保存策略由三个后端共用。原可迁移类型保留命名空间并增加类型转发；GDI 颜色兼容类型移到 `Bing.Drawing.Gdi`。
- 三套入口统一为 `Identify`、`Process`、`Compose`、`Save`，支持静态 JPEG/PNG、方向 1–8、裁剪缩放、旋转翻转、水印、圆角边框、五类标注和三种拼图布局。
- PNG 为默认输出；JPEG 明确合成背景。压缩结果报告实际大小、质量和是否达标；严格失败不写目标文件。
- 输入限额在解码前检查，流从当前位置有界读取；PNG ICC 解压校验和 16 MiB 上限也在解码前执行。文件采用同目录临时文件替换。
- 保留旧公开入口，加载失败改为异常并提供 `TryLoad`。中文字体由调用方提供，不进行系统字体回退。
- 默认 Base64/Data URL 输出统一为 PNG；`SupportedNativeOutputFormats` 暴露 ImageSharp/SkiaSharp 的原生 WebP 编码能力，统一 `Process` 仍明确拒绝 WebP。
- `RemoveMetadata=false` 时，同格式 JPEG/PNG 保留未明确删除的应用元数据和辅助 chunk；MPF、动画控制及图像数据不迁移，ICC 继续独立控制。
- PNG 识别到 `acTL`、`fcTL` 或 `fdAT` 均拒绝统一静态流程；`sBIT`、色彩 chunk 和 ICC 按 `PLTE`/`IDAT` 顺序插入，避免生成顺序不兼容的 PNG。
- 原 Http 变更涉及 `src/Bing.Utils.Http`、两套 Http 测试、`docs/modules/http.md` 和 Http 修复报告，本次不修改、不暂存、不提交这些既有改动。
- 迁移及能力矩阵见 `docs/modules/image-processing.md`；四组可编译示例见 `tests/Bing.Utils.Drawing.Tests.Integration/ImageProcessingExamples.cs`。

## 已关闭问题与回归证据

| 问题 | 修复及验证 |
|---|---|
| JPEG 删除 EXIF 丢失 SOI/扫描数据 | 重写段遍历；断言扫描尾部字节一致，真实 JPEG 清理后逐像素解码一致 |
| PNG EXIF 修改后 CRC 不一致 | 重写 chunk CRC；真实经纬度/方向清理后可解码，损坏 CRC 被拒绝 |
| GPS、全部 EXIF、ICC 开关混用 | 独立断言 GPS 移除、方向与描述保留、全部 EXIF 移除、ICC 单独移除及跨格式保留 |
| 流所有权和输出资源异常泄漏 | 输入/输出流异常路径关闭、不可定位流、读取异常、取消和源图所有权回归 |
| 保存失败破坏已有文件 | 同路径覆盖成功；严格压缩失败、取消、锁定目标失败时原字节不变，临时文件清理 |
| 尺寸溢出及旋转边界 | 长整数中间计算；九宫格裁剪像素、90 度方向、45 度向上取整回归 |
| Skia JPEG 重复方向纠正 | 后端原始加载，公共流程只纠正一次；真实 Orientation=6 JPEG 及关闭自动纠正回归 |
| Skia 大边框恢复透明角 | 圆角路径填充；大边框及圆角透明像素断言 |
| 默认格式与水印合成差异 | JPEG 加载后默认 PNG；PNG 签名/MIME；JPEG 白底及图片水印 50% 透明度像素断言 |
| ICC 不受控解压及错误包装容忍 | zlib 头/Adler-32 校验、解压硬上限；损坏数据和膨胀数据在识别阶段拒绝 |
| 同格式未知元数据被编码器丢弃 | 迁移未明确删除的 JPEG APP/PNG 辅助 chunk；APP13、私有 PNG chunk 和真实 MPF 拒绝回归 |
| 后端能力查询缺少原生扩展边界 | 三后端统一/原生格式列表回归，WebP 只在 ImageSharp/Skia 原生列表出现 |
| APNG 控制 chunk 被误判为静态 PNG | `acTL` 单帧及缺失 `acTL` 的 `fcTL/fdAT` 均明确拒绝 |
| PNG 辅助 chunk 顺序不符合规范 | `sBIT`/`PLTE` 顺序回归，受约束 chunk 在 `PLTE` 前、其他辅助数据在图像数据前 |

独立代理审查了公共流程/GDI、ImageSharp 和 Skia 适配器，以及元数据代码；发现的问题均已修复。复核确认旋转边界、圆角、JPEG 方向、默认 PNG 与 ICC 解压问题关闭。正常像素、异常路径和文件保护用实际数据验收，不以方法存在作为证据。

## 验证结果

2026-09-26，Windows 本地执行；所有下列测试无跳过。

| 范围 | 通过数 | 结果文件（均在 `TestResults/image-processing/`） |
|---|---:|---|
| System.Drawing 完整 net8.0 | 82/82 | `Bing.Utils.Drawing.Tests-final-net8.trx` |
| ImageSharp 完整 net8.0 | 255/255 | `Bing.Utils.Drawing.ImageSharp.Tests-final-net8.trx` |
| SkiaSharp 完整 net8.0 | 156/156 | `Bing.Utils.Drawing.SkiaSharp.Tests-final-net8.trx` |
| 三后端集成 net8.0 | 107/107 | `integration-final-fixed-net8.trx` |
| 三后端集成 net6.0 | 107/107 | `integration-final-net6.0.trx` |
| 三后端集成 net7.0 | 107/107 | `integration-final-net7.0.trx` |
| 受影响单元 net6.0 | 3 + 12 + 13 | 三个 `*-affected-net6.0.trx` |
| 受影响单元 net7.0 | 3 + 12 + 13 | 三个 `*-affected-net7.0.trx` |

测试包括真实 GPS/Orientation、系统 sRGB ICC、外部中文字体、压缩实际降尺寸、无法达标、三包共同消费者、图像及文字水印、标注像素、横向/竖向/网格布局、能力查询、同格式未知元数据保留、MPF/APNG 拒绝及 PNG chunk 顺序。ImageSharp 2.1 的 PNG ICC 往返能力有限，因此真实 PNG 夹具独立构造 iCCP 并检查解压后的配置字节，而不依赖该引擎的 ICC 属性。

Shared 多目标构建及最终解决方案 Release 构建均通过，包含三个后端与 Shared 的 netstandard2.0 编译。解决方案构建为 0 错误、1 个现有 `net6.0` 生命周期警告，不作为零警告验收；编译日志为 `solution-release-final.log`。`git diff --check` 已通过；64 个图像相关变更文本以 UTF-8 检查通过。

早期失败测试、构建日志与基线结果保留。最新有效证据以上表为准；文件名中较早出现 `final` 不表示该轮失败被忽略。公共 ICC 检查和 GDI 合成的最后调整由最终集成测试及 net6/net7 受影响测试覆盖，未无理由重跑不受影响的测试。

## 最终状态

- Completed：实现、回归测试、迁移文档、编译示例、独立审查、已发现问题修复及最终解决方案构建。
- Open Actionable：0；TODO：仅待外部环境补跑 .NET 5/3.1 与 Linux 运行验证。
- Blocked Approval：none。
- Blocked External：缺少 .NET 5/3.1 运行时及 Linux 实测环境，不声称运行验证通过。
- Accepted Limitations：统一流程仅静态 JPEG/PNG；WebP 仍属于旧原生接口的后端扩展能力，统一入口明确拒绝；System.Drawing 为 Windows 后端；同步编解码期间不能强制中断；压缩可能无法达到目标。
- Verified Boundaries：共同消费者无公共类型歧义；失败保存保护原文件；流所有权；输入、像素、帧数、ICC 解压及有界压缩尝试。
- Deferred：动画、多页、HEIC/AVIF、AI 修图与批处理调度。
- Not Applicable：未运行性能基准，不声明性能提升。
- Provider/model：主代理负责契约、集成和验证；Luna 负责窄范围后端/元数据及测试，交叉独立审查。
- No-Progress Check：COMPLETE；Next Action：交付。执行已完成，仅保留明确的外部运行验证阻塞，不自动开始新修复循环。
