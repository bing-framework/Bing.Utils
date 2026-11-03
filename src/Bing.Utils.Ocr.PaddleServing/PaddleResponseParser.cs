using System.Text.Json;

namespace Bing.Ocr.PaddleServing;

/// <summary>官方单图协议解析；内部使用，纯解析测试不需要网络。</summary>
internal static class PaddleResponseParser
{
    internal static OcrProviderResult Parse(byte[] utf8, int width, int height, string? expectedProfile = null)
    {
        try
        {
            using var document = JsonDocument.Parse(utf8, new JsonDocumentOptions { MaxDepth = 32 });
            var root = document.RootElement;
            var errorCode = root.GetProperty("errorCode").GetInt32();
            if (errorCode == 429) throw new OcrBusyException();
            if (errorCode == 408 || errorCode == 504) throw new OcrTimeoutException();
            if (errorCode != 0) throw new OcrProviderException("OCR 服务业务错误码：" + errorCode);
            var response = root.GetProperty("result");
            var items = response.GetProperty("ocrResults");
            if (items.GetArrayLength() != 1) throw new OcrProviderException("单图请求必须返回一个页面结果。");
            var result = items[0].GetProperty("prunedResult");
            // 官方服务在 dataInfo 给出尺寸；私有宿主在 prunedResult 提供 input_img_shape。
            // 至少存在一种，两者均存在时必须都与当前处理图一致。
            var hasShape = result.TryGetProperty("input_img_shape", out var shape);
            var hasDataInfo = response.TryGetProperty("dataInfo", out var dataInfo);
            if (!hasShape && !hasDataInfo) throw new OcrProviderException("OCR 响应缺少图片尺寸。");
            if (hasShape && (shape.GetArrayLength() < 2 || shape.GetArrayLength() > 3 ||
                shape[0].GetInt32() != height || shape[1].GetInt32() != width))
                throw new OcrProviderException("OCR 返回的图片尺寸与输入不一致。");
            if (hasDataInfo && (dataInfo.GetProperty("type").GetString() != "image" ||
                dataInfo.GetProperty("width").GetInt32() != width || dataInfo.GetProperty("height").GetInt32() != height))
                throw new OcrProviderException("OCR dataInfo 与单图请求不一致。");
            var texts = result.GetProperty("rec_texts");
            var polygons = result.GetProperty("rec_polys");
            var scoresPresent = result.TryGetProperty("rec_scores", out var scores) && scores.ValueKind != JsonValueKind.Null;
            var count = texts.GetArrayLength();
            if (count != polygons.GetArrayLength() || (scoresPresent && count != scores.GetArrayLength()))
                throw new OcrProviderException("OCR 文字、文字框与分数数量不一致。");
            var lines = new List<OcrLine>(count);
            for (var i = 0; i < count; i++)
            {
                var text = texts[i].GetString() ?? throw new OcrProviderException("文字不能为 null。");
                var quad = polygons[i];
                if (quad.GetArrayLength() != 4) throw new OcrProviderException("文字框必须包含四点。");
                var points = new OcrPoint[4];
                for (var p = 0; p < 4; p++)
                {
                    if (quad[p].GetArrayLength() != 2) throw new OcrProviderException("文字框坐标格式错误。");
                    var x = quad[p][0].GetDouble(); var y = quad[p][1].GetDouble();
                    if (x < 0 || y < 0 || x > width || y > height) throw new OcrProviderException("文字框超出输入图范围。");
                    points[p] = new OcrPoint(x, y);
                }
                double? score = scoresPresent && scores[i].ValueKind != JsonValueKind.Null ? scores[i].GetDouble() : null;
                lines.Add(new OcrLine(text, points, score));
            }
            var profile = root.TryGetProperty("modelProfileId", out var profileValue) && profileValue.ValueKind != JsonValueKind.Null
                ? profileValue.GetString() : null;
            if (expectedProfile != null && profile != expectedProfile)
                throw new OcrProviderException("识别响应与预期模型身份不一致。");
            return new OcrProviderResult(lines, profile, expectedProfile != null);
        }
        catch (Exception ex) when (ex is JsonException || ex is InvalidOperationException || ex is KeyNotFoundException ||
            ex is ArgumentException || ex is FormatException || ex is OverflowException)
        { throw new OcrProviderException("OCR 响应协议无效。", ex); }
    }
}
