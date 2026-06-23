# Bing.Utils.Drawing
## 1. 包职责（Scope）
- 解决的问题
- 基于 `System.Drawing` 提供验证码生成与图像处理辅助（旧版 GDI+ 实现）。
- 同时提供 `Bing.Utils.Drawing.Shared` 源码共享层，包含跨平台纯算法和公共类型，不依赖任何图像引擎。
- 不解决的问题（Out of Scope）
- 不提供前端渲染组件。
- ImageSharp / SkiaSharp 跨平台实现由独立子包承担。

## 2. 核心类型

### 2.1 旧版（System.Drawing 依赖）
- `CaptchaBuilder`：验证码生成器（属性驱动配置，支持中文系统字体）
- `ImageHelper`：图像操作（MakeThumbnail、DeleteCoordinate、BrightnessHandle、LeftRightTurn 等）
- `ColorConv`：颜色转换（RGB/HSB/Hex）

### 2.2 Shared 公共类型（无图像引擎依赖）
- `RgbColor`：RGB 颜色值类型（R/G/B/A，0-255）
- `HslColor`：HSL 颜色值类型（H: 0-360, S/L: 0-1）
- `ColorConversion`：核心颜色转换（RgbToHsl、HslToRgb、SRgbToLinearRgb、LinearRgbToSRgb、ToHex、TryParseHex）
- `ColorConv`：兼容层（旧 `RgbToHsb`/`HsbToRgb` 委托到 HSL 核心，保持旧命名）
- `BinaryMatrixHelper`：二维矩阵公共兼容层（ClearBorder、AddBorder、Clone、DrawTo、FloodFill、ToCodeString、FromFlatArray、ToFlatArray）
- `CaptchaOptions`：验证码配置（含 Width/Height/FontSize/FontWidth/HasBorder/NoiseLineCount/NoisePointCount/RandomPosition/RandomColor/RandomItalic/RandomRotation/MaxRotationDegrees/RandomSeed/BackgroundA/R/G/B）
- `CaptchaType`：验证码类型枚举（Number/NumberAndLetter/ChineseChar）
- `ThumbnailMode`：缩略图模式枚举（Cut/FixedW/FixedH/FixedBoth）
- `ColorMatrices`：共享颜色矩阵工厂（Brightness/Contrast/Saturation/GrayScale Filter）
- `ImageMetadataOptions`：元数据清理选项（RemoveGps/RemoveEntireExif/PreserveIccProfile/UnsupportedFormatBehavior）

### 2.3 Shared Internal 纯算法
- `DrawingCompatibilityHelper`：netstandard2.0 兼容辅助
- `GrayImageBuffer`：灰度缓冲区工具
- `BinaryImageProcessor`：二值化图像处理
- `NoiseReductionProcessor`：噪声消除
- `ConnectedComponentProcessor`：连通域处理
- `ProjectionProcessor`：投影/切分/调试字符串
- `CaptchaCodeGenerator`：验证码文本生成（支持中文白名单字符集）
- `ChineseCaptchaGlyphSet`：中文 16x16 点阵字形库
- `ImageGeometryHelper`：图像几何/颜色数学
- `IcoContainerWriter`：ICO 容器写入
- `TwistGeometryProcessor`：扭曲效果
- `ErosionEffectHelper`：冲蚀效果
- `JpegMetadataSanitizer`：JPEG EXIF GPS 清除
- `PngMetadataSanitizer`：PNG eXIf GPS 清除
- `EncodedImageSanitizer`：格式检测与调度

## 3. 注意事项
- 旧版 `ColorConv.RgbToHsb` 实际语义是 HSL（GDI+ 行为），新版 `ColorConversion` 正确命名为 `RgbToHsl`。
- 旧版 `DeleteCoordinate(Image)` 基于已解码对象，跨平台新版使用编码字节级清理。
- Shared 层禁止依赖 `System.Drawing`、`SixLabors.ImageSharp`、`SkiaSharp`。
- `ConnectedComponentProcessor` 已使用 `MatrixPoint` 替代 `System.Drawing.Point`。
- 是否分配敏感
- 生成图片时会创建 `Bitmap`、`Graphics` 与多种绘图对象，属于分配敏感路径。[证据] `src/Bing.Utils.Drawing/Bing/Drawing/CaptchaBuilder.cs:224` [证据] `src/Bing.Utils.Drawing/Bing/Drawing/CaptchaBuilder.cs:225` [证据] `src/Bing.Utils.Drawing/Bing/Drawing/CaptchaBuilder.cs:359`
- 是否线程安全
- `CaptchaBuilder` 是有状态实例（属性可变），并包含静态 `Random` 字段，不建议多线程共享同一实例。[证据] `src/Bing.Utils.Drawing/Bing/Drawing/CaptchaBuilder.cs:18` [证据] `src/Bing.Utils.Drawing/Bing/Drawing/CaptchaBuilder.cs:35`
- 是否可并发调用
- 建议“每线程/每请求独立实例”并发调用。

## 5. 异常与日志策略
- 抛出哪些异常
- 参数非法抛 `ArgumentOutOfRangeException` / `ArgumentNullException`。[证据] `src/Bing.Utils.Drawing/Bing/Drawing/CaptchaBuilder.cs:121` [证据] `src/Bing.Utils.Drawing/Bing/Drawing/CaptchaBuilder.cs:219`
- 什么时候返回默认值而不是抛异常
- 当前核心 API 对非法输入倾向抛异常，不返回默认值。

## 6. 与其他子包的关系
- 依赖的包
- 依赖 `Bing.Utils` 与 `System.Drawing.Common`。[证据] `src/Bing.Utils.Drawing/Bing.Utils.Drawing.csproj:15` [证据] `src/Bing.Utils.Drawing/Bing.Utils.Drawing.csproj:19`
- 被哪些包复用
- 当前 `src` 层未见其他子包直接引用 `Bing.Utils.Drawing`。

## 7. 测试映射
- 对应测试项目/测试类/关键用例
- 项目：`tests/Bing.Utils.Tests` 引用本包。[证据] `tests/Bing.Utils.Tests/Bing.Utils.Tests.csproj:13`
- 类：`CaptchaBuilderTest`（验证码生成与图片生成冒烟）。[证据] `tests/Bing.Utils.Tests/Drawing/CaptchaBuilderTest.cs:5`
- 未覆盖风险点
- 当前测试以“执行通过+输出”为主，断言强度有限（如像素正确性、并发一致性未覆盖）。[证据] `tests/Bing.Utils.Tests/Drawing/CaptchaBuilderTest.cs:19` [证据] `tests/Bing.Utils.Tests/Drawing/CaptchaBuilderTest.cs:47`

## 8. 版本与兼容性注意事项
- 跟随公共多目标框架策略。[证据] `common.props:3`
- Linux/Docker 需安装 `libgdiplus` 等系统依赖，否则运行时可能失败。[证据] `src/Bing.Utils.Drawing/README.md:2` [证据] `src/Bing.Utils.Drawing/README.md:6`

