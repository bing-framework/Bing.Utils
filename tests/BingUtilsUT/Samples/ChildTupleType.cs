namespace BingUtilsUT.Samples;

/// <summary>
/// 表示由两个整数元素组成的元组样例类型。
/// </summary>
public class ChildTupleType : Tuple<int, int>
{
    /// <summary>
    /// 初始化一个 <see cref="ChildTupleType"/> 类型的实例。
    /// </summary>
    /// <param name="item1">元组的第一个整数元素。</param>
    /// <param name="item2">元组的第二个整数元素。</param>
    public ChildTupleType(int item1, int item2) : base(item1, item2) { }
}
