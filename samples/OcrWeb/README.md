# OCR Web 示例

ASP.NET Core .NET 8 + 原生 HTML/JavaScript。上传图片、拖动/填写 ROI、直角旋转、Standard/Scan 预处理、原图文字框叠加、低分提示、取消、复制和 TXT/JSON 下载均在本示例中演示。人工修订与识别原文分别保存，下载 JSON 同时包含两者；没有数据库。

先按 [部署说明](../../deploy/ocr/README.md) 准备本地引擎，再从仓库根目录运行：

```powershell
$env:Ocr__Endpoint = "http://127.0.0.1:8080/"
$env:Ocr__ModelProfileId = (Invoke-RestMethod "http://127.0.0.1:8080/health/ready").modelProfileId
dotnet run --project samples/OcrWeb -c Release
```

浏览器打开 `http://127.0.0.1:5086`。应用部署时应固定准备阶段核验的模型 profile；上面的探针赋值是本机演示快捷方式。

浏览器使用 `createImageBitmap(..., {imageOrientation:'from-image'})` 显示 EXIF 校正完整图片。叠框与 ROI 使用其像素坐标；服务响应尺寸不匹配时拒绝叠框。UI 中取消会终止 fetch，远端已开始的推理仍按宿主容量规则结束。

默认只监听 loopback。防伪 cookie + `X-CSRF-TOKEN` 保护上传接口，响应设置 no-store；图片字段有界读取至内存、不落盘，未知/重复字段拒绝。Kestrel 请求 11 MiB，单图 10 MiB，选项 4 KiB，上传入口最多 9 个活动请求，核心另有 1 活动 / 8 等待限制。样例不包含账号认证，不直接暴露到公网。

测试：

```powershell
dotnet test tests/Bing.Utils.Ocr.Tests.Integration -c Release
node --test samples/OcrWeb/tests/geometry.test.mjs
```

WebApplicationFactory 测试使用真实图像解码和编排、替身 Provider，断言上传/坐标/防伪/错误响应；真实 OCR 使用独立 Engine 阶段。原始图片、输出文字、模型权重均不写入仓库。
