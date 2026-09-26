# 统一图像处理

`ImageHelper`（Windows GDI+）、`ImageSharpHelper`、`SkiaSharpHelper` 提供相同的 `Identify`、`Process`、`Compose`、`Save` 入口，共享 `Bing.Utils.Drawing.Shared` 中的选项与结果。图像引擎仍在各自后端，不通过其他后端转发图像处理。

## 使用约定

统一流程支持静态 JPEG/PNG，输出默认 PNG；`SupportedOutputFormats` 是此流程的能力列表。三个 Helper 另提供 `SupportedNativeOutputFormats` 显示后端原生编码器能力，ImageSharp/SkiaSharp 可列出 WebP，但 WebP 不进入统一流程。请求不支持的格式抛出异常，不静默转码。动画、多页图片及其他格式不在统一流程范围内，旧原生格式转换入口仍由后端自身能力决定。

处理顺序为：识别限额 → EXIF 方向纠正 → 裁剪 → 缩放 → 旋转/翻转 → 圆角/内边框 → 水印/标注 → 元数据策略 → 编码压缩。坐标以纠正方向后的图像为准，标注坐标位于编辑完成后的画布。旋转正角度为顺时针。裁剪越界取交集，无交集报错。

默认不放大；需要固定尺寸头像时显式设置 `AllowEnlarge=true` 和 `Cover`。`Contain` 不补边，返回实际等比尺寸；拼图单元格内剩余位置使用背景色。边框在画布内侧，不增加宽高；JPEG 将透明像素合成到默认白色背景。

输入数组不修改。输入流从当前位置读取，默认保持打开，支持不可定位流；指定 `leaveOpen=false` 时成功、失败和取消均关闭该流。输出字节归调用方持有。图片原生编辑函数的返回图像需要调用方释放。

默认限制为 20 MiB 编码字节、4000 万像素、16384 单边、单帧、拼图最多 16 张。拼图与图片水印计入累计输入预算；输出尺寸和编码大小也受限制。`ImageProcessingLimits` 可调整限制，不将限制解释为整个进程的内存上限。

这些限额属于统一流程，不自动保护旧的原生 `FromStream` / `FromBytes` / Base64 加载入口。外部输入应使用受限入口，并在接收和 Base64 解码前限制请求体。转换异常及历史 32 进制编码迁移参见 [转换迁移说明](conversion-migration.md)。

取消在读取、处理阶段之间、编码尝试之间及文件提交前检查；同步第三方编解码器执行中不能强制中断。保存先写同目录临时文件，关闭后替换；失败保留已有目标，支持源目标同路径。

## 压缩与元数据

JPEG 默认质量 85，最低 40，以 15 为步长；不达标时以 0.8 比例缩小，最多六轮，编码总次数不超过 28。PNG 不使用 JPEG 质量参数，仅尝试缩尺寸。最低尺寸默认 64，原图更小则不放大。`TargetSizeReached=false` 表示无法在约束内满足体积；严格模式抛异常，不能覆盖目标文件。

上传流程默认先纠正方向，然后去除描述性元数据，保留 ICC。专项 `DeleteCoordinate` 不重新压缩图像；仅 GPS 模式保留非目标 EXIF，全部 EXIF 模式不等于删除所有格式中的任意描述内容。GPS 专项清理不承诺清除 XMP 或自由文本中可能存在的位置。

将 `RemoveMetadata` 设为 `false` 时，同格式 JPEG 会保留源 EXIF、XMP、注释及未明确删除的 APP 元数据，同格式 PNG 会保留源 EXIF、文本及辅助 chunk；编码器生成的同类 chunk 会由源数据替换。ICC 仍由 `PreserveIccProfile` 独立控制。MPF、多帧控制和图像数据 chunk 不迁移，避免复制已经失效的帧偏移或动画结构。

PNG ICC 在识别及迁移时校验 zlib 头部及 Adler-32，解压后的配置文件另有 16 MiB 硬上限，超过上限会在图像解码前明确失败。

## 字体与编辑

文字水印、文字标注必须提供字体文件路径及像素字号；支持普通字体和 TTC 集合首字体，检查 Unicode 字形，不依赖系统字体回退。字体不打包到 NuGet 中。调用方负责部署包含所需中文字符的字体。

水印可使用图片字节或文字，支持九宫格锚点、边距、透明度。拼图支持横排、竖排和固定列数网格；标注按列表顺序覆盖绘制，提供矩形、椭圆、直线、箭头及文字。

四组可编译示例位于 `tests/Bing.Utils.Drawing.Tests.Integration/ImageProcessingExamples.cs`：上传压缩、头像裁剪、中文水印、拼图标注。替换 Helper 名称即可切换后端。

## 后端能力矩阵

| 能力 | System.Drawing | ImageSharp | SkiaSharp |
|---|---|---|---|
| 统一静态 JPEG/PNG 输入输出 | 支持 | 支持 | 支持 |
| 方向 1–8、裁剪、缩放、旋转、翻转 | 支持 | 支持 | 支持 |
| 图片/外部字体水印、五类标注 | 支持 | 支持 | 支持 |
| 圆角、内边框、三种拼图布局 | 支持 | 支持 | 支持 |
| 有界压缩、元数据清理、原子保存 | 支持 | 支持 | 支持 |
| 统一流程 WebP/动画/多页 | 不支持 | 不支持 | 不支持 |
| 原生 WebP 编码查询 | 不支持 | `SupportedNativeOutputFormats` | `SupportedNativeOutputFormats` |
| 平台定位 | Windows | 跨平台 | 跨平台，需要原生运行库 |

原生转换方法支持的其他格式不计入统一流程能力；统一入口以 `SupportedOutputFormats` 为准。集成测试默认使用 Windows 的 `msyh.ttc` 和 sRGB ICC 文件，其他环境可分别设置 `BING_TEST_FONT`、`BING_TEST_ICC` 指定真实测试资源。

## 迁移

| 原调用或类型 | 当前契约 |
|---|---|
| `From*` 失败返回 null | 失败抛异常；需要探测时使用 `TryLoad` |
| 未指定输出格式时沿用来源格式 | 默认 PNG；需要其他格式必须明确指定 |
| 多包各自定义的公共选项/枚举 | 由 Shared 提供统一类型身份 |
| System.Drawing 的 `Bing.Conversions.ColorConv` | `Bing.Drawing.Gdi.ColorConv`；可使用 using 别名迁移 |
| System.Drawing 的 `Bing.Drawing.ColorMatrices` | `Bing.Drawing.Gdi.ColorMatrices`；可使用 using 别名迁移 |
| Metadata 的 `leaveOpen=false` 曾被忽略 | 现在关闭输入及输出流，包括异常路径 |

System.Drawing 的返回图像滤镜现在编辑副本；`DeleteCoordinate(Image)` 作为 void 原生接口仍明确执行就地清理。统一 `Process` 不修改调用方输入。System.Drawing 在 Windows 验证，不能据此宣称 Linux 支持。无损像素值、几何和状态可跨后端比较；有损编码、抗锯齿和字体栅格结果不保证逐字节一致。
