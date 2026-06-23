namespace Bing.Drawing;

/// <summary>
/// HSL（色相-饱和度-亮度）颜色值类型。
/// <para>
/// 色相 (H) 范围：[0, 360)；饱和度 (S) 范围：0-1；亮度 (L) 范围：0-1。
/// </para>
/// <para>
/// 注意：旧版 <c>ColorConv.RgbToHsb</c> / <c>HsbToRgb</c> 使用的 GDI+ HSB
/// 实际语义即 HSL。本类型正确反映该语义。
/// </para>
/// </summary>
public readonly struct HslColor : IEquatable<HslColor>
{
    /// <summary>
    /// 色相。范围：[0, 360)
    /// </summary>
    public double H { get; }

    /// <summary>
    /// 饱和度。范围：0-1
    /// </summary>
    public double S { get; }

    /// <summary>
    /// 亮度。范围：0-1
    /// </summary>
    public double L { get; }

    /// <summary>
    /// 初始化一个<see cref="HslColor"/>类型的实例
    /// </summary>
    /// <param name="h">色相</param>
    /// <param name="s">饱和度</param>
    /// <param name="l">亮度</param>
    public HslColor(double h, double s, double l)
    {
        H = h;
        S = s;
        L = l;
    }

    /// <inheritdoc />
    public bool Equals(HslColor other) =>
        Math.Abs(H - other.H) < 1e-6 &&
        Math.Abs(S - other.S) < 1e-6 &&
        Math.Abs(L - other.L) < 1e-6;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is HslColor other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => H.GetHashCode() ^ S.GetHashCode() ^ L.GetHashCode();

    /// <inheritdoc />
    public override string ToString() => $"HSL({H:F1},{S:F3},{L:F3})";

    /// <summary>
    /// 等于运算符
    /// </summary>
    public static bool operator ==(HslColor left, HslColor right) => left.Equals(right);

    /// <summary>
    /// 不等于运算符
    /// </summary>
    public static bool operator !=(HslColor left, HslColor right) => !left.Equals(right);
}
