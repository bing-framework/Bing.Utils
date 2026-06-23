namespace Bing.Drawing;

/// <summary>
/// RGB 颜色值类型。分量范围：0-255。
/// </summary>
public readonly struct RgbColor : IEquatable<RgbColor>
{
    /// <summary>
    /// 红色分量
    /// </summary>
    public byte R { get; }

    /// <summary>
    /// 绿色分量
    /// </summary>
    public byte G { get; }

    /// <summary>
    /// 蓝色分量
    /// </summary>
    public byte B { get; }

    /// <summary>
    /// Alpha 分量
    /// </summary>
    public byte A { get; }

    /// <summary>
    /// 初始化一个<see cref="RgbColor"/>类型的实例（不透明）
    /// </summary>
    /// <param name="r">红色分量</param>
    /// <param name="g">绿色分量</param>
    /// <param name="b">蓝色分量</param>
    public RgbColor(byte r, byte g, byte b)
    {
        R = r;
        G = g;
        B = b;
        A = 255;
    }

    /// <summary>
    /// 初始化一个<see cref="RgbColor"/>类型的实例
    /// </summary>
    /// <param name="r">红色分量</param>
    /// <param name="g">绿色分量</param>
    /// <param name="b">蓝色分量</param>
    /// <param name="a">Alpha 分量</param>
    public RgbColor(byte r, byte g, byte b, byte a)
    {
        R = r;
        G = g;
        B = b;
        A = a;
    }

    /// <inheritdoc />
    public bool Equals(RgbColor other) => R == other.R && G == other.G && B == other.B && A == other.A;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is RgbColor other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => R | (G << 8) | (B << 16) | (A << 24);

    /// <inheritdoc />
    public override string ToString() => A == 255 ? $"RGB({R},{G},{B})" : $"RGBA({R},{G},{B},{A})";

    /// <summary>
    /// 等于运算符
    /// </summary>
    public static bool operator ==(RgbColor left, RgbColor right) => left.Equals(right);

    /// <summary>
    /// 不等于运算符
    /// </summary>
    public static bool operator !=(RgbColor left, RgbColor right) => !left.Equals(right);
}
