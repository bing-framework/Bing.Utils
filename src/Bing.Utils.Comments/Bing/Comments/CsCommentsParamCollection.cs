using System.Collections.Generic;

namespace Bing.Comments;

/// <summary>
/// 表示按参数名索引的参数和泛型参数说明集合。
/// </summary>
public sealed class CsCommentsParamCollection : Dictionary<string, CsCommentsParam>
{
    /// <summary>
    /// 获取或设置根据参数名获取说明；查找键会忽略开头的 <c>@</c>，未找到时返回 <see langword="null"/>。
    /// </summary>
    /// <param name="name">XML 文档注释中的参数名；可带编译器使用的开头 <c>@</c>。</param>
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
