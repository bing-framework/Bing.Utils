# 本地 OCR 服务

本目录提供供 Bing.Utils 调用的私有 OCR HTTP 服务。服务使用独占子进程常驻加载模型，进程运行期间不下载模型。只有三个本地模型及其 manifest 校验通过、模型预热完成后，`/health/ready` 才返回就绪。

## 固定版本

- Python 3.11
- PaddleOCR 3.7.0
- PaddleX 3.7.0
- PaddlePaddle 3.3.1
- Pillow 13.0.0（安全修复版）
- 容器安装工具 uv 0.11.1，依赖按平台锁文件及 SHA-256 验证
- `PP-OCRv6_small_det`
- `PP-OCRv6_small_rec`
- `PP-LCNet_x1_0_textline_ori`

模型目录固定为 `models/PP-OCRv6_small_det`、`models/PP-OCRv6_small_rec`、`models/PP-LCNet_x1_0_textline_ori`。`model-manifest.json` 记录官方归档地址、归档 SHA-256 及所有模型文件的 SHA-256。服务会核对固定来源、文件清单和摘要，再根据实际运行时版本、模型清单、CPU 线程数及 OCR 配置生成 `modelProfileId`。

三个官方归档的批准 SHA-256 固定在 `manifest.py`。准备脚本在解包前核对下载归档摘要；运行时同时核对 manifest 中的批准归档摘要和每个模型文件摘要。升级模型资产时，应先从官方发布来源取得新版本并独立验证，再一起审查更新 `MODEL_SOURCES`、`MODEL_ARCHIVE_SHA256` 与运行时配置；不得只因下载内容变化就接受新摘要。准备与启动测试通过后，新的 manifest 会进入 profile 身份计算。

## Windows 准备与启动

在本目录打开 PowerShell，并确保已安装 `uv`：

```powershell
uv python install 3.11.13
uv venv --python 3.11 .venv
uv pip sync --require-hashes --python .venv\Scripts\python.exe requirements.lock
.venv\Scripts\python.exe prepare-models.py
$env:OCR_HOST = "127.0.0.1"
$env:OCR_PORT = "8080"
$env:OCR_MODELS_DIR = (Resolve-Path .\models).Path
.venv\Scripts\python.exe server.py
```

`prepare-models.py` 只使用 Python 标准库。模型准备阶段需要连接 Paddle 官方模型站；准备完成后，运行服务不需要网络。默认服务根地址为 `http://127.0.0.1:8080/`。

## Docker Compose

在联网机器准备模型后，将完整 `models` 目录传到本目录。然后构建并启动：

```powershell
docker compose build
docker compose up -d
```

容器使用 Python 3.11 Linux 锁文件，模型目录以只读方式挂载。服务在容器内监听 `0.0.0.0:8080`，Compose 仅将端口发布到宿主机 `127.0.0.1`。设置 `OCR_PUBLISHED_PORT` 可修改宿主机端口，容器端口保持 8080。

## HTTP 协议

`POST /ocr` 接受 `application/json`，且只接受以下字段：

```json
{
  "file": "<单张 PNG、JPEG 或 BMP 图片的 Base64>",
  "fileType": 1,
  "useDocOrientationClassify": false,
  "useDocUnwarping": false,
  "useTextlineOrientation": true,
  "textRecScoreThresh": 0,
  "visualize": false
}
```

不接受远端 URL、PDF、动画 PNG 或多帧图片。开图前检查 PNG/JPEG/BMP 文件头、尺寸及 PNG 动画标记，解码器仅允许对应格式。JSON 结构深度最多 8 层，整数文本最多 20 字符；协议本身仍是固定字段的扁平对象。成功响应包含 `errorCode: 0`、`errorMsg: "Success"`、实际 `modelProfileId`，以及 `result.ocrResults[0].prunedResult`。`prunedResult` 含 `rec_texts`、`rec_scores`、`rec_polys`、`input_img_shape`；多边形为图像坐标中的顺时针四点框，缺失或无效分数为 `null`。

输入错误返回 HTTP 400，容量已满返回 429，正文读取超过绝对期限返回 408，单次推理超过 30 秒返回 504，模型或 worker 不可用返回 503。正文读取期限默认 30 秒，持续滴流不会重置期限；上传超时或取消后释放准入位。单次推理不会重试。最多一个活动请求和八个等待请求；客户端在等待期间断开会释放等待位。请求正文和图片不会写入日志。

`GET /health/ready` 在模型清单校验并完成预热后返回 HTTP 200 和 `{ "ready": true, "modelProfileId": "..." }`，否则返回 503。`GET /health/live` 报告宿主进程是否存活。

## 配置

- `OCR_HOST`：监听地址，默认 `127.0.0.1`。
- `OCR_PORT`：监听端口，默认 `8080`。
- `OCR_MODELS_DIR`：本地模型根目录，默认 `models`。
- `OCR_MAX_BODY_BYTES`：JSON body 上限，默认 28 MiB。
- `OCR_MAX_IMAGE_BYTES`：Base64 解码后图片上限，默认 20 MiB。
- `OCR_MAX_PIXELS`：像素总数上限，默认 20,000,000。
- `OCR_MAX_DIMENSION`：单边尺寸上限，默认 8192。
- `OCR_MAX_WAITING`：等待请求数，默认 8，范围 0 到 8。
- `OCR_BODY_TIMEOUT_SECONDS`：正文读取绝对期限，默认 30 秒，必须为有限正数；独立于排队与推理期限。
- `OCR_STARTUP_TIMEOUT_SECONDS`：worker 启动及预热时限，默认 180 秒。
- `OCR_WORKER_JOIN_TIMEOUT_SECONDS`：worker 终止后的等待时限，默认 5 秒。
- `OCR_CPU_THREADS`：CPU 推理线程数，默认 2。

活动推理超时固定为 30 秒。Paddle 3.3.1 的 PP-OCRv6 PIR oneDNN 路径在 Windows 和 Linux 均出现未实现指令，因此两平台固定关闭 oneDNN，该配置参与 profile 身份计算。宿主默认绑定 loopback；只有容器环境才设置 `OCR_HOST=0.0.0.0`。

## 许可来源

部署或再分发前，请分别核对运行库与模型权重的适用条款。[PaddleOCR 许可文件](https://github.com/PaddlePaddle/PaddleOCR/blob/main/LICENSE)、[PaddleX 许可文件](https://github.com/PaddlePaddle/PaddleX/blob/release/3.7/LICENSE)和 [PaddlePaddle 许可文件](https://github.com/PaddlePaddle/Paddle/blob/develop/LICENSE)可在各自官方仓库查看。模型归档来自 [PaddleX 官方推理模型目录](https://paddle-model-ecology.bj.bcebos.com/paddlex/official_inference_model/paddle3.0.0/)；保留归档中随附的许可声明，并按具体模型发布页核对权重条款。

## 测试

```powershell
.venv\Scripts\python.exe -m unittest discover -s tests -v
.venv\Scripts\python.exe smoke.py --endpoint http://127.0.0.1:8080/ --model-profile $env:OCR_MODEL_PROFILE --font C:\Windows\Fonts\msyh.ttc
```

单元测试使用可替换假 worker 覆盖协议、输入边界、排队取消、超时恢复、崩溃与关闭路径。`smoke.py` 只访问本机服务；容器内可运行 `docker compose exec -T ocr python smoke.py --model-profile <实际模型标识>`。可用 `--font` 指定支持中文的字体，让样图同时覆盖中英文；不指定时使用内置字体识别英文。

## 首版验证记录（历史）

- 隔离 Python 环境：`C:\Users\jianx\AppData\Local\Temp\bingutils-ocr-venv-311\Scripts\python.exe`，Python 3.11.13；没有改动系统 Python。
- Windows x64 已安装并核对 PaddleOCR 3.7.0、PaddleX 3.7.0、PaddlePaddle 3.3.1；`uv pip check` 的 74 个包均兼容。
- `python -m unittest discover -s tests -v`：19 项通过。`docker compose config --quiet` 通过；Linux/Python 3.11 锁文件已解析，容器构建与 Linux 推理由部署环境另行验证。
- 已从固定 PaddleX 官方 HTTPS 地址准备三个模型，manifest 校验通过；11 个模型文件共 38,340,062 字节。三个真实模型均已加载并预热。
- Windows 真实宿主的 `/health/ready` 与 `smoke.py` 通过。本机 smoke 验证了空白图及 `OCR 测试 123 ABC`，识别为 `OCR \\u6d4b\\u8bd5 123 ABC`，模型 profile 为 `ppocrv6-small:b45f39f9e5acc5436f0f17516157b6dd178a7859be4cf4164ab7e9c6f95b874b`。
- Windows PaddlePaddle 3.3.1 默认 oneDNN 在 PP-OCRv6 small 检测模型上曾报 PIR `ArrayAttribute<DoubleAttribute>` 未实现；Windows worker 关闭 oneDNN 后预热和识别通过。首版 Linux 尚未实跑；本轮复验后两平台均关闭 oneDNN。

另已在拒绝 Python socket 连接的独立进程中验证本地模型加载、预热和中英文识别，期间没有连接尝试；该证据不等同于 OS/容器网络隔离。该条是首版证据。当前修复、容器断网验证及资源边界见 [修复记录](../../ai_docs/tasks/2026-10-29-ocr/fix-report.md)。

## 安全回归与资源验证

在仓库根目录执行（Windows 隔离环境路径可替换）：

```powershell
$ocrPython = './deploy/ocr/.venv/Scripts/python.exe'
pwsh -File build/Test-Ocr.ps1 -Stage Host -PythonExecutable $ocrPython
# 真实 Paddle 进程的挂死、崩溃与重建测试；缺少本地模型时失败。
$env:OCR_MODELS_DIR = (Resolve-Path './deploy/ocr/models').Path
$env:OCR_REAL_PROCESS_TESTS = '1'
pwsh -File build/Test-Ocr.ps1 -Stage Host -PythonExecutable $ocrPython
Remove-Item Env:/OCR_REAL_PROCESS_TESTS
pwsh -File build/Test-Ocr.ps1 -Stage Resource -ResourceScenario queue -PythonExecutable $ocrPython
pwsh -File build/Test-Ocr.ps1 -Stage Resource -ResourceScenario maximum-image -PythonExecutable $ocrPython
```

queue 使用 1000×1000 PNG，受控地保持 1 个活动和 8 个等待，确认第 10 个调用被拒绝后执行 9 次真实推理。它测量队列容量，不代表自然并发吞吐或 P95。maximum-image 使用 5000×4000 PNG；返回 success 或核验旧进程退出后的 timeout_boundary，其他错误仍失败。两者最多运行 120 秒，保留 30 秒推理期限和实际宿主 limits，不为测量放宽限制。

JSON 记录实际并行度、等待数、模型/平台、输入、结果和 100 ms 采样的宿主及子进程 RSS 峰值。Windows peak_wset 是各进程历史峰值之和，可能不同时发生。范围不包括 HTTP、.NET 或浏览器内存；实际 HTTP 包装与图片内容会增加占用。

Windows 本机记录：1MP 满队列完成 9 次推理、1 次拒绝，采样 RSS 合计峰值约 669 MiB；20MP 在约 30 秒触发受控超时并回收 worker，采样峰值约 3.00 GiB。20MP 是可接收上限，不保证任意图片在期限内完成；可按业务内容显式指定 ROI 或缩放。正式 CER/P95 仍需要独立真实标注集及目标硬件。

## 容器级断网验证

先完成镜像构建和模型准备，再在本目录执行：

```powershell
$ocrImage = docker compose images -q ocr
$ocrModels = (Resolve-Path './models').Path
docker run --rm --network none --env OCR_MODELS_DIR=/models --mount "type=bind,source=$ocrModels,target=/models,readonly" --entrypoint python $ocrImage -B verify_offline.py
```

验证脚本要求网络接口只有 lo，在容器内启动 HTTP 宿主并验证本地模型预热、空白图与文本识别；正常退出和失败均回收宿主。增加只读字体挂载和 --font 可验证中文。此验证不修改用户机器网络设置。离线机器需要事先导入已经构建的镜像（docker save/load）并复制完整模型目录；离线时不执行构建或依赖安装。
