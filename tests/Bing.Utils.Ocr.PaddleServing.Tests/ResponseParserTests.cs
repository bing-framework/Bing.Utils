using Bing.Ocr;
using Bing.Ocr.PaddleServing;
using System.Text;
using Xunit;

namespace Bing.Utils.Ocr.PaddleServing.Tests;

public class ResponseParserTests
{
    private static byte[] Response(string texts = "[\"中文 A\"]", string scores = "[0.2]", string polys = "[[[0,0],[10,0],[10,5],[0,5]]]", string profile = "")
        => Encoding.UTF8.GetBytes("{\"errorCode\":0" + profile + ",\"result\":{\"ocrResults\":[{\"prunedResult\":{\"input_img_shape\":[10,20],\"rec_texts\":" + texts +
            ",\"rec_scores\":" + scores + ",\"rec_polys\":" + polys + "}}]}}");

    [Fact]
    public void Parse_LowScoresAndMissingScores_ArePreserved()
    {
        var result = PaddleResponseParser.Parse(Response(),20,10);
        Assert.Equal("中文 A", result.Lines[0].Text); Assert.Equal(.2, result.Lines[0].Confidence);
        Assert.False(result.ModelIdentityVerified);
        Assert.Null(PaddleResponseParser.Parse(Response(scores:"null"),20,10).Lines[0].Confidence);
    }
    [Fact]
    public void Parse_EmptyPage_IsSuccess()
    {
        Assert.Empty(PaddleResponseParser.Parse(Response("[]","[]","[]"),20,10).Lines);
    }
    [Fact]
    public void Parse_DimensionsAreRequired_AndOfficialDataInfoIsSupported()
    {
        var withoutShape = Encoding.UTF8.GetString(Response()).Replace("\"input_img_shape\":[10,20],", "");
        Assert.Throws<OcrProviderException>(() => PaddleResponseParser.Parse(Encoding.UTF8.GetBytes(withoutShape),20,10));
        var official = withoutShape.Replace("\"ocrResults\":", "\"dataInfo\":{\"type\":\"image\",\"width\":20,\"height\":10},\"ocrResults\":");
        Assert.Single(PaddleResponseParser.Parse(Encoding.UTF8.GetBytes(official),20,10).Lines);
        Assert.Throws<OcrProviderException>(() => PaddleResponseParser.Parse(Encoding.UTF8.GetBytes(official),21,10));
        var conflicting = official.Replace("\"prunedResult\":{", "\"prunedResult\":{\"input_img_shape\":[11,20],");
        Assert.Throws<OcrProviderException>(() => PaddleResponseParser.Parse(Encoding.UTF8.GetBytes(conflicting),20,10));
        var wrongType = official.Replace("\"type\":\"image\"", "\"type\":\"pdf\"");
        Assert.Throws<OcrProviderException>(() => PaddleResponseParser.Parse(Encoding.UTF8.GetBytes(wrongType),20,10));
    }

    [Theory]
    [InlineData("[]","[0.1]","[]")]
    [InlineData("[\"a\"]","[2]","[[[0,0],[10,0],[10,5],[0,5]]]")]
    [InlineData("[\"a\"]","[0.9]","[[[0,0],[30,0],[30,5],[0,5]]]")]
    [InlineData("[null]","[0.9]","[[[0,0],[10,0],[10,5],[0,5]]]")]
    [InlineData("[\"a\"]","[0.9]","[[[0,0],[10,5],[10,0],[0,5]]]")]
    public void Parse_InvalidProtocol_Throws(string texts,string scores,string polys)
    {
        Assert.Throws<OcrProviderException>(() => PaddleResponseParser.Parse(Response(texts,scores,polys),20,10));
    }
    [Fact]
    public void Parse_InvalidJsonAndShapeAndModelMismatch_Throw()
    {
        Assert.Throws<OcrProviderException>(() => PaddleResponseParser.Parse(Encoding.UTF8.GetBytes("{}"),20,10));
        Assert.Throws<OcrProviderException>(() => PaddleResponseParser.Parse(Response(),21,10));
        Assert.Throws<OcrProviderException>(() => PaddleResponseParser.Parse(Response(),20,10,"profile"));
        var bytes = Response(profile:",\"modelProfileId\":\"profile\"");
        Assert.True(PaddleResponseParser.Parse(bytes,20,10,"profile").ModelIdentityVerified);
        Assert.False(PaddleResponseParser.Parse(bytes,20,10).ModelIdentityVerified);
    }
}
