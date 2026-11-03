using Bing.Ocr;
using Bing.Ocr.SkiaSharp;
using SkiaSharp;
using System.Text;
using Xunit;

namespace Bing.Utils.Ocr.SkiaSharp.Tests.Integration;

public class ImagePreprocessorTests
{
    private readonly SkiaOcrImagePreprocessor _preprocessor = new();

    [Theory]
    [InlineData(1,0,1,2,3)] [InlineData(2,1,0,3,2)] [InlineData(3,3,2,1,0)] [InlineData(4,2,3,0,1)]
    [InlineData(5,0,2,1,3)] [InlineData(6,2,0,3,1)] [InlineData(7,3,1,2,0)] [InlineData(8,1,3,0,2)]
    public void ExifOrientation_ProducesExpectedPixelsAndDisplaySize(int orientation,int tl,int tr,int bl,int br)
    {
        var png = PngWithExif(orientation);
        var result = _preprocessor.Prepare(png,new OcrOptions(),new OcrServiceOptions());
        Assert.Equal(orientation >= 5 ? 10 : 16, result.OriginalWidth);
        Assert.Equal(orientation >= 5 ? 16 : 10, result.OriginalHeight);
        using var decoded = SKBitmap.Decode(result.CopyBytes());
        var colors = new[] { SKColors.Red, SKColors.Green, SKColors.Blue, SKColors.Yellow };
        Assert.Equal(colors[tl], decoded.GetPixel(1,1));
        Assert.Equal(colors[tr], decoded.GetPixel(decoded.Width-2,1));
        Assert.Equal(colors[bl], decoded.GetPixel(1,decoded.Height-2));
        Assert.Equal(colors[br], decoded.GetPixel(decoded.Width-2,decoded.Height-2));
    }

    [Fact]
    public void RegionRotationAndResize_MapToKnownOriginalCoordinates()
    {
        var result = _preprocessor.Prepare(PngWithExif(1),
            new OcrOptions { Region = new OcrRectangle(4,2,8,6), RotationDegrees=90, Scale=.5 }, new OcrServiceOptions());
        Assert.Equal(3, result.Width); Assert.Equal(4, result.Height);
        var origin = result.ToOriginal.Apply(new OcrPoint(0,0));
        var opposite = result.ToOriginal.Apply(new OcrPoint(3,4));
        Assert.Equal(4,origin.X,8); Assert.Equal(8,origin.Y,8);
        Assert.Equal(12,opposite.X,8); Assert.Equal(2,opposite.Y,8);
        Assert.Contains("image_scaled",result.Warnings);
    }

    [Fact]
    public void ScanAndTransparency_AreRenderedAsGrayOnWhite()
    {
        using var bitmap = new SKBitmap(8,8); bitmap.Erase(SKColors.Transparent);
        bitmap.SetPixel(2,2,SKColors.Red);
        using var image = SKImage.FromBitmap(bitmap); using var png = image.Encode(SKEncodedImageFormat.Png,100);
        var result = _preprocessor.Prepare(png.ToArray(),new OcrOptions { Preprocessing=OcrPreprocessingPreset.Scan },new OcrServiceOptions());
        using var output=SKBitmap.Decode(result.CopyBytes());
        Assert.Equal(SKColors.White,output.GetPixel(0,0));
        var gray=output.GetPixel(2,2); Assert.Equal(gray.Red,gray.Green); Assert.Equal(gray.Red,gray.Blue);
    }

    [Fact]
    public void Bmp_IsSupportedWithoutChangingSharedDrawingParser()
    {
        var result = _preprocessor.Prepare(Bmp(),new OcrOptions(),new OcrServiceOptions());
        Assert.Equal(2,result.Width); Assert.Equal(2,result.Height);
        using var image = SKBitmap.Decode(result.CopyBytes());
        Assert.Equal(SKColors.Red,image.GetPixel(0,0));
    }

    [Fact]
    public void InvalidInputAndLimitsAndCancellation_AreExplicit()
    {
        Assert.Throws<OcrInputException>(()=>_preprocessor.Prepare(Encoding.UTF8.GetBytes("%PDF-1.7"),new OcrOptions(),new OcrServiceOptions()));
        Assert.Throws<OcrInputException>(()=>_preprocessor.Prepare(PngWithExif(1),new OcrOptions(),new OcrServiceOptions{MaxPixels=10}));
        Assert.Throws<OcrInputException>(()=>_preprocessor.Prepare(PngWithExif(1),new OcrOptions{Region=new OcrRectangle(15,0,2,2)},new OcrServiceOptions()));
        Assert.Throws<OcrInputException>(()=>_preprocessor.Prepare(PngWithExif(1),new OcrOptions(),new OcrServiceOptions{MaxPreparedBytes=10}));
        using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
        Assert.ThrowsAny<OperationCanceledException>(()=>_preprocessor.Prepare(PngWithExif(1),new OcrOptions(),new OcrServiceOptions(),cancellation.Token));
        var animation = AddChunk(PngWithExif(1), "acTL", new byte[]{0,0,0,2,0,0,0,0});
        Assert.Throws<OcrInputException>(()=>_preprocessor.Prepare(animation,new OcrOptions(),new OcrServiceOptions()));
    }

    private static byte[] PngWithExif(int orientation)
    {
        using var bitmap=new SKBitmap(16,10);
        for(var y=0;y<10;y++) for(var x=0;x<16;x++)
            bitmap.SetPixel(x,y,y<5 ? (x<8?SKColors.Red:SKColors.Green):(x<8?SKColors.Blue:SKColors.Yellow));
        using var image=SKImage.FromBitmap(bitmap); using var png=image.Encode(SKEncodedImageFormat.Png,100);
        byte[] tiff={73,73,42,0,8,0,0,0,1,0,18,1,3,0,1,0,0,0,(byte)orientation,0,0,0,0,0,0,0};
        return AddChunk(png.ToArray(),"eXIf",tiff);
    }
    private static byte[] AddChunk(byte[] png,string type,byte[] data)
    {
        using var output=new MemoryStream();
        output.Write(png,0,33);
        var size=BitConverter.GetBytes(data.Length); if(BitConverter.IsLittleEndian) Array.Reverse(size);
        output.Write(size,0,4); var name=Encoding.ASCII.GetBytes(type); output.Write(name,0,4); output.Write(data,0,data.Length);
        uint crc=0xffffffff;
        foreach(var b in name.Concat(data)) { crc ^= b; for(var i=0;i<8;i++) crc=(crc>>1)^((crc&1)!=0?0xedb88320u:0u); }
        var checksum=BitConverter.GetBytes(~crc); if(BitConverter.IsLittleEndian) Array.Reverse(checksum);
        output.Write(checksum,0,4); output.Write(png,33,png.Length-33); return output.ToArray();
    }
    private static byte[] Bmp()
    {
        using var stream=new MemoryStream(); using var writer=new BinaryWriter(stream,Encoding.UTF8,true);
        writer.Write((byte)'B'); writer.Write((byte)'M'); writer.Write(70); writer.Write(0); writer.Write(54);
        writer.Write(40); writer.Write(2); writer.Write(2); writer.Write((short)1); writer.Write((short)24);
        writer.Write(0); writer.Write(16); writer.Write(0); writer.Write(0); writer.Write(0); writer.Write(0);
        writer.Write(new byte[]{255,0,0,0,255,255,0,0, 0,0,255,0,128,0,0,0});
        return stream.ToArray();
    }
}
