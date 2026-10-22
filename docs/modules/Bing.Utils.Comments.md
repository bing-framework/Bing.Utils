# Bing.Utils.Comments

## 功能说明

`Bing.Utils.Comments` 读取程序集同目录的 C# XML 文档文件，并把类型、构造函数、方法、属性、字段和事件注释转换为结构化模型。它还支持枚举注释映射、`inheritdoc` 继承解析和按程序集缓存。

## 核心实现与关键文件

源码根目录为 `src/Bing.Utils.Comments/Bing/Comments`。

- `CsCommentReader.cs`：按 `MemberInfo` 类型构造 XML 文档 ID，读取注释并提供枚举值到摘要的映射。
- `CsCommentId.cs`：生成类型和成员的标准 XML 文档标识，处理泛型、数组、指针、引用和转换运算符签名。
- `CsComments.cs`、`CsCommentElement.cs`、`CsCommentNode.cs`：承载 `summary`、参数、返回值、异常及其他 XML 节点。
- `CsCommentsInheritanceResolver.cs`：解析显式/隐式 `inheritdoc`，合并基类、接口和成员注释并防止循环继承。
- `CsCommentsXmlCache.cs`：使用弱程序集键缓存 XML；文件变化时重载，解析时禁用 DTD 和外部实体，并限制文档大小为 64 MiB。
- 测试：`tests/Bing.Utils.Comments.Tests`；缓存文件行为在 `tests/Bing.Utils.Comments.Tests.Integration`。

## 对外接口与用法

```csharp
using Bing.Comments;

var method = typeof(MyService).GetMethod(nameof(MyService.Execute));
var comments = CsCommentReader.Create(method!);

Console.WriteLine(comments?.Summary);
if (comments is not null)
{
    foreach (var parameter in comments.Parameters.Values)
        Console.WriteLine($"{parameter.Name}: {parameter.Text}");
}
```

枚举可使用 `CsCommentReader.CreateEnumComment<TEnum>()`；没有 XML 摘要时会回退到 `DescriptionAttribute`，再回退到成员名。

## 依赖与配置

- 无第三方包依赖，目标框架继承 `common.props`。
- 被读取程序集必须在构建时生成 XML 文档，并让 `.xml` 与程序集同目录；另有兼容查找路径 `AppContext.BaseDirectory/bin`。
- XML 缺失或成员无注释时，`Create` 返回 `null`。

## 已知限制与注意事项

- 只读取编译器生成的 XML 文档，不解析 C# 源文件。
- 缓存刷新依赖文件长度和最后写入时间；调用方不能通过公共 API 指定任意 XML 路径。
- 已有有效缓存时，文件刷新遇到 XML、IO 或权限错误会继续返回上一份有效快照。
- `Get(MemberInfo)` 返回防御性副本，修改返回的 `XmlDocument` 不会修改缓存。
- `inheritdoc` 的复杂跨程序集解析依赖目标程序集及其 XML 文件可定位；缺失文档时只能保留当前可解析内容。
