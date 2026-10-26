using Bing.Utils.Json;
using Newtonsoft.Json.Linq;

namespace Bing.Utils.Tests.Json;

/// <summary>
/// 验证 JSON 文件覆盖写入会清除旧内容尾部。
/// </summary>
[Trait("Category", "Regression")]
public sealed class JsonFileOverwriteRegressionTest
{
    /// <summary>
    /// 验证较短 JSON 覆盖较长内容后仍是完整有效的 JSON 文本。
    /// </summary>
    [Fact]
    public void SerializableToFile_OverwriteWithShorterJson_TruncatesOldTail()
    {
        var path = Path.Combine(Path.GetTempPath(), $"bing-json-overwrite-{Guid.NewGuid():N}.json");
        try
        {
            JsonHelper.SerializableToFile(path, new { Value = new string('x', 64) });
            JsonHelper.SerializableToFile(path, new { Value = "x" });

            var json = File.ReadAllText(path, Encoding.UTF8);
            JObject.Parse(json).Value<string>("Value").ShouldBe("x");
            json.ShouldBe(JsonHelper.ToJson(new { Value = "x" }, false, false, true));
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }
}
