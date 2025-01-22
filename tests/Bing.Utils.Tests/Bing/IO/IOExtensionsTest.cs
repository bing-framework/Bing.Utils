namespace Bing.IO;

/// <summary>
/// IO扩展测试
/// </summary>
public class IOExtensionsTest
{
    #region MemoryStream

    [Fact]
    public void AsString_Should_Return_String_From_MemoryStream()
    {
        // Arrange
        var encoding = Encoding.UTF8;
        var input = "Test String";
        var ms = new MemoryStream(encoding.GetBytes(input));

        // Act
        var result = ms.AsString(encoding);

        // Assert
        Assert.Equal(input, result);
    }

    [Fact]
    public void AsString_Should_Use_Default_Encoding_When_None_Provided()
    {
        // Arrange
        var input = "Test String";
        var ms = new MemoryStream(Encoding.UTF8.GetBytes(input));

        // Act
        var result = ms.AsString();

        // Assert
        Assert.Equal(input, result);
    }

    [Fact]
    public void AsString_Should_Throw_Exception_When_Stream_Is_Null()
    {
        // Arrange
        MemoryStream ms = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ms.AsString());
    }

    [Fact]
    public void FromString_Should_Write_String_To_MemoryStream()
    {
        // Arrange
        var encoding = Encoding.UTF8;
        var input = "Test String";
        var ms = new MemoryStream();

        // Act
        ms.FromString(input, encoding);

        // Assert
        var result = encoding.GetString(ms.ToArray());
        Assert.Equal(input, result);
    }

    [Fact]
    public void FromString_Should_Clear_Stream_When_Append_Is_False()
    {
        // Arrange
        var encoding = Encoding.UTF8;
        var input = "Test String";
        var ms = new MemoryStream();
        ms.Write(encoding.GetBytes("Old Content"), 0, "Old Content".Length);

        // Act
        ms.FromString(input, encoding, append: false);

        // Assert
        var result = encoding.GetString(ms.ToArray());
        Assert.Equal(input, result);
    }

    [Fact]
    public void FromString_Should_Append_To_Existing_Content()
    {
        // Arrange
        var encoding = Encoding.UTF8;
        var initialContent = "Initial Content";
        var input = "Appended Content";
        var ms = new MemoryStream();
        ms.FromString(initialContent, encoding);

        // Act
        ms.FromString(input, encoding, append: true);

        // Assert
        var result = encoding.GetString(ms.ToArray());
        Assert.Equal(initialContent + input, result);
    }

    [Fact]
    public void FromString_Should_Throw_Exception_When_Stream_Is_Null()
    {
        // Arrange
        MemoryStream ms = null;
        var input = "Test String";

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ms.FromString(input));
    }

    [Fact]
    public void FromString_Should_Throw_Exception_When_Input_Is_Null()
    {
        // Arrange
        var ms = new MemoryStream();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ms.FromString(null));
    }

    [Fact]
    public void FromString_Should_Throw_Exception_When_Stream_Is_Not_Writable()
    {
        // Arrange
        var ms = new MemoryStream(new byte[0], writable: false);
        var input = "Test String";

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => ms.FromString(input));
    }

    #endregion

}