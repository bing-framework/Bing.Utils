using System.Collections.Generic;

namespace Bing.Comments;

/// <summary>
/// 表示注释中的 param 或 typeparam 节点集合。
/// </summary>
public sealed class CsCommentsParamCollection : Dictionary<string, CsCommentsParam>
{
    /// <summary>
    /// 根据节点的 name 属性获取注释；没有匹配项时返回 null。
    /// </summary>
    /// <param name="name">name 属性</param>
    public new CsCommentsParam this[string name]
    {
        get
        {
            if (string.IsNullOrEmpty(name))
                return null;
            if (name[0] == '@')
                name = name.Remove(0, 1);

            return base.TryGetValue(name, out var value) ? value : null;
        }
        set
        {
            if (string.IsNullOrEmpty(name))
                return;
            if (!string.IsNullOrEmpty(name) && name[0] == '@')
                name = name.Remove(0, 1);
            base[name] = value;
        }
    }
}