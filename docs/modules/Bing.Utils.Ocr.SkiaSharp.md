# Bing.Utils.Ocr.SkiaSharp

`Bing.Ocr.SkiaSharp.SkiaOcrImagePreprocessor` 实现 `IOcrImagePreprocessor`。依赖仓库 Drawing.SkiaSharp 和 SkiaSharp 2.88.9，核心 OCR 契约不暴露 Skia 类型。Linux 宿主另装同版本 `SkiaSharp.NativeAssets.Linux.NoDependencies`，示例已引用。

预处理顺序：格式和头部限额 → 解码 → EXIF 八方向校正 → ROI → 显式直角旋转 → 显式比例缩放 → 背景/灰度 → PNG 编码。完整像素分配前检查尺寸、像素、格式和帧数。

PNG/JPEG 复用 Drawing.Shared 头部检查；BMP 使用 SKCodec 检查，未扩展全部 Drawing 管线。多帧、PDF、解码失败或越界 ROI 都拒绝。透明区域合成白底；Standard 保留色彩，Scan 增加灰度。默认保持尺寸，缩放时记录 `image_scaled` 警告。强二值化、腐蚀、自动裁边及逐字切分不在首版。

独立仿射矩阵记录裁切、旋转和实际整数尺寸缩放，结果逆变换回到 EXIF 校正完整图片。Skia 对象在调用内部创建和释放，失败与取消不会转移其所有权。

图像集成测试覆盖八种 EXIF 的尺寸与像素方向、ROI/旋转/缩放坐标、BMP、透明背景、灰度、帧数与各限额。参见 [主模块](Bing.Utils.Ocr.md)。
