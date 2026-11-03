# OCR 模块首版实施计划

2026-10-29。用户已明确授权按 T1–T7 实施。依据 [解决方案](../../../docs/superpowers/plans/2026-10-29-ocr-solution.md)；本任务处于执行阶段。

## 固定交付
四包 Ocr、Ocr.SkiaSharp、Ocr.PaddleServing、Ocr.DependencyInjection，命名空间 Bing.Ocr；沿用 net8/net7/net6/netstandard2.0。
识别 PNG/JPEG/BMP 单图及 ROI；流从当前位置读取并保持打开；原文/文字行/顺时针四点框/可空分数；EXIF 校正完整图坐标；保留引擎顺序，以 LF 拼接。
Standard 保留彩色，Scan 增加灰度。显式直角旋转与缩放，越界 ROI 拒绝。默认10 MiB输入、2000万像素、8192单边、20 MiB预处理、4 MiB响应、1活动/8等待、30秒总期限。
模型初始为 PaddleOCR/PaddleX 3.7.0、PaddlePaddle 3.3.1、Python3.11、PP-OCRv6 small及行方向模型。部署环境实证不通过时如实归类，不用替身替代。
PDF、多页、表格、业务字段、GPU、云及进程内Provider延后。完整真实标注集CER和正式P95另行验收，本次工程闭环与真实冒烟。

## TODO
- [x] T1 四包、公共契约、目标框架、测试项目、sln及DI打包入口。
- [x] T2 核心有界准入、读取、取消/超时、坐标和结果、替身测试。
- [x] T3 Skia图像检查、EXIF/ROI/旋转/缩放、资源释放、真实图像测试。
- [x] T4 Paddle HTTP协议及边界、低分保留、模型身份、本地协议测试。
- [x] T5 隔离运行环境、模型清单/离线准备、单子进程宿主、有界调度/超时重启、测试。
- [x] T6 DI、net8 Web示例、上传/区域/叠框/编辑/导出/取消、防伪及Web测试。
- [x] T7 分层验证脚本、CPU CI、四包消费、质量测量入口、文档与独立Review。

## 公共实现契约
IOcrService.RecognizeAsync(Stream,OcrOptions?,CancellationToken)及文件便捷方法。
IOcrImagePreprocessor.Prepare(byte[],OcrOptions,OcrServiceOptions,CancellationToken)返回PreparedOcrImage（有限编码字节、完整显示图/处理图尺寸、ToOriginal变换、警告）。
IOcrProvider.ValidateOptions及RecognizeAsync(PreparedOcrImage,OcrOptions,CancellationToken)；核心不暴露Skia/Paddle类型。
Paddle POST /ocr显式fileType=1、禁整页方向/展平/visualize、启行方向、textRecScoreThresh=0；校验数组、四点框与有限数值，保持响应上限。普通上游身份未验证时不可伪称已验证。
服务借用注入组件/HttpClient，每次请求释放自己创建的资源；DI拥有自己创建实例。取消不保证原生/远端推理中止，服务端容量保留至完成或子进程回收。

## 验证
先Change Impact Analysis，再定向L0/L1/L2；阶段收口L3；最终一次solution Release net8构建/测试、包消费及依赖审计。
单元：Ocr.Tests、Ocr.DependencyInjection.Tests、Ocr.PaddleServing.Tests（net8/7/6）。
集成：Ocr.SkiaSharp.Tests.Integration、Ocr.PaddleServing.Tests.Integration、Ocr.Tests.Integration（net8）。
build/Test-Ocr.ps1阶段：Unit、Integration、PackConsumer、Engine -RequireEngine、Final。
图像EXIF八方向与几何往返<=1px；协议错误/容量/取消/释放；真实OCR固定中英样本、空白图、ROI；无外部环境显式BLOCKED_EXTERNAL。
主代理负责核心、集成与验证；luna_worker负责明确独立模块；完成后独立Review。
仅OPEN_ACTIONABLE>0进入修复，最多三轮；同类实验一次诊断+一次正式确认。保留已有变更，不暂存/提交/推送/发布，不变更发行版本。

## 证据
当前状态与完成条目证据保存在 execution.md；必要原始日志保存在 artifacts/。

完成记录：工程实施与独立复核已完成。全仓审计既有问题、Linux 容器未验证及正式 CER/P95 延后详见 [execution.md](execution.md)。
