# Bing.Utils.Ocr.PaddleServing

`PaddleServingOcrProvider` 借用宿主 HttpClient，调用官方结构的 `POST /ocr`。配置对象在构造时复制，HttpClient 应复用并设 `Timeout.InfiniteTimeSpan`，由核心总期限取消请求。建议关闭自动重定向，避免把图片转发到配置外的服务。

请求明确设置 `fileType=1`、`useDocOrientationClassify=false`、`useDocUnwarping=false`、`useTextlineOrientation=true`、`textRecScoreThresh=0`、`visualize=false`。服务必须已部署文本行方向模型。默认语言提示为中英、中文或英文；这是能力检查，不会临时切换模型。

解析 `result.ocrResults[0].prunedResult` 的 `rec_texts`、`rec_scores`、`rec_polys`；要求单图结果、数组一一对应、四点凸框、有限且合法的坐标和分数。允许分数缺失，并保留低分行；必须通过官方 `result.dataInfo` 或私有 `input_img_shape` 核对处理图尺寸；同时提供时均须匹配。默认有界读取响应 4 MiB，上传处理图上限 20 MiB。HTTP 429 对应繁忙，408/504 对应超时，其他失败或损坏结构对应 Provider 异常。

`ExpectedModelProfileId` 设置后，每次识别先读取 `GET /health/ready`，核对就绪状态与 profile，再核对识别响应的身份；全部匹配才设置 `ModelIdentityVerified=true`。profile 来自部署模型清单，应用应固定经核验的值。普通官方服务没有此元数据时，不设置该选项，结果明确标记未验证。没有重试或云回退。

[私有宿主](../../deploy/ocr/README.md) 提供身份和探针；客户端断开不等于远端推理停止。HTTP 链路测试使用本地 TestServer；真实引擎测试独立标记 OcrEngine，普通测试不会下载模型。
