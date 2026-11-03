# OCR 质量测量入口

准备独立标注集后运行；图片路径相对于 UTF-8 数据集 JSON。示例结构：

```json
[
  {"image":"screenshots/001.png","expected":"原文，保留空格。\n第二行","group":"screenshot"},
  {"image":"scans/001.jpg","expected":"扫描件原文","group":"scan"}
]
```

```powershell
$env:OCR_ENDPOINT = "http://127.0.0.1:8080/"
$env:OCR_MODEL_PROFILE = (Invoke-RestMethod "$env:OCR_ENDPOINT/health/ready").modelProfileId
dotnet run --project benchmarks/OcrProbe -c Release -- dataset.json results.json
```

工具记录第一次调用耗时，随后以并发 1 测量每张图片，按 group 输出微平均 CER（总编辑距离 / 总标注 Unicode 字符数）和 nearest-rank P95。空格、标点、大小写和换行均保留；无标注字符的组 CER 为 null，编辑数仍保留。报告包含原文和预测文本，应存储在本地受控目录。

首次调用不混入 P95；它是已启动服务的第一次客户端调用，不代表容器冷启动。失败立即终止，不用缺失样本得出质量结论。正式验收应固定机器、模型清单、图像尺寸分布、预热次数、至少 300 张真实样本及独立验收集。本次工程冒烟不宣称 CER 或正式 P95 已达标。
