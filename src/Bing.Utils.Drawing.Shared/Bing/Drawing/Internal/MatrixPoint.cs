namespace Bing.Drawing.Internal;

/// <summary>
/// 二维矩阵坐标点（纯值类型，用于 Shared 纯算法层，不依赖 System.Drawing）
/// </summary>
internal struct MatrixPoint : IEquatable<MatrixPoint>
{
    /// <summary>
    /// X 坐标（列）
    /// </summary>
    public int X { get; }

    /// <summary>
    /// Y 坐标（行）
    /// </summary>
    public int Y { get; }

    /// <summary>
    /// 初始化一个<see cref="MatrixPoint"/>类型的实例
    /// </summary>
    /// <param name="x">X 坐标</param>
    /// <param name="y">Y 坐标</param>
    public MatrixPoint(int x, int y)
    {
        X = x;
        Y = y;
    }

    /// <inheritdoc />
    public bool Equals(MatrixPoint other) => X == other.X && Y == other.Y;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is MatrixPoint other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => X ^ (Y << 16);

    /// <inheritdoc />
    public override string ToString() => $"({X}, {Y})";

    /// <summary>
    /// 等于运算符
    /// </summary>
    public static bool operator ==(MatrixPoint left, MatrixPoint right) => left.Equals(right);

    /// <summary>
    /// 不等于运算符
    /// </summary>
    public static bool operator !=(MatrixPoint left, MatrixPoint right) => !left.Equals(right);
}
