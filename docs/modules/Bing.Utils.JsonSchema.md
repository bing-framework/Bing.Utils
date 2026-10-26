# Bing.Utils.JsonSchema

提供 .NET 8 JSON Schema 校验，命名空间为 Bing.Json.Schema。独立于 Bing.Utils 核心包，使用 JsonSchema.Net 9.4.1。

## 基本使用

```csharp
using Bing.Json.Schema;

// 在应用启动或配置加载时创建，后续复用。
var validator = JsonSchemaValidator.Create("""
    {
      "$schema": "https://json-schema.org/draft/2020-12/schema",
      "type": "object",
      "required": ["name"],
      "properties": {
        "name": { "type": "string", "minLength": 1 },
        "age": { "type": "integer", "minimum": 0 }
      },
      "additionalProperties": false
    }
    """);

var result = validator.Validate("""{"name":"张三","age":-1}""");
// result.Status == JsonSchemaValidationStatus.SchemaMismatch
// result.Errors 包含 /age 的 minimum 错误。
foreach (var error in result.Errors)
    Console.WriteLine($"{error.InstancePath}: {error.Keyword} - {error.Message}");
```

Create 和 Validate 均提供 string、JsonElement 重载。Create 复制 Schema；原始 JsonDocument 可在创建后释放。Validate(JsonElement) 调用期间输入文档必须存活，返回结果不依赖输入文档。一个实例可由多个线程调用，同一实例内部同步进入引擎。

## 方言、引用与 format

支持 Draft 6、Draft 7、2019-09、2020-12；无 $schema 声明时默认 2020-12。声明优先于 DefaultDialect，未知方言在创建时失败。format 默认作为注解，设置 ValidateFormats = true 可要求格式断言。

```csharp
var options = new JsonSchemaValidatorOptions
{
    BaseUri = new Uri("https://schemas.example.test/order.json"),
    ValidateFormats = true
};
options.ReferencedSchemas.Add(
    new Uri("https://schemas.example.test/email.json"),
    """{"type":"string","format":"email"}""");

var validator = JsonSchemaValidator.Create("""
    { "properties": { "email": { "$ref": "email.json" } } }
    """, options);
var result = validator.Validate("""{"email":"not-an-email"}""");
```

支持内部引用、预注册文档之间的引用、锚点和递归引用；引用集合的添加顺序不限。URI 是标识符，校验器不自动读取网络或文件。无法解析的引用作为配置错误处理。BaseUri 与引用集合的键必须是无非空片段的绝对 URI。创建后修改选项或引用集合不会改变已有实例。

封装不修改 JsonSchema.Net 的全局配置，也不调用全局下载器。引擎仍可能读取宿主提前放入全局注册表的内存 Schema；若宿主直接使用该引擎，应避免全局 Schema 与本实例的预注册文档使用冲突标识。这里不承诺隔离宿主对引擎全局注册表的直接操作。

## 错误处理

| 情况 | 行为 |
|---|---|
| JSON 符合 Schema | Status = Valid，IsValid = true，Errors 为空 |
| JSON 文本语法错误 | Status = InvalidJson，SyntaxError 提供消息、零基行号和字节位置 |
| JSON 不符合 Schema | Status = SchemaMismatch，Errors 提供规则诊断 |
| Schema 非法、不支持的方言、引用配置错误 | Create 抛出 JsonSchemaConfigurationException；求值时发现的无效引用循环也抛该异常 |
| null、未初始化 JsonElement、无效配置参数 | 参数异常；已释放的 JsonElement 保持 ObjectDisposedException |

每个规则错误包括 InstancePath（JSON Pointer，根为空字符串）、SchemaLocation、Keyword、Message 和只读 Children。组合规则失败时 Children 保留分支诊断，展示完整错误需要递归遍历。required 的路径指向缺失字段所在对象；布尔 false Schema 的 Keyword 为空字符串。错误消息用于展示，不作为稳定业务标识。

校验只读，不填充 default、不做类型转换。当前入口基于 DOM，暂不提供 Stream、NDJSON、DTO Schema 生成或 ASP.NET Core 集成。

## 第三方依赖许可

本包许可证由 NuGet 元数据中的 LICENSE 提供。JsonSchema.Net 源码采用 MIT，其 9.x 官方 NuGet 二进制包另附维护费协议；当前条款对用于营收活动且年总营收达到 10,000 美元的使用者规定维护费条件。消费本包时应同时查阅依赖自身的条款：

- [JsonSchema.Net 9.4.1](https://www.nuget.org/packages/JsonSchema.Net/9.4.1)
- [OSMF EULA](https://github.com/json-everything/json-everything/blob/master/OSMFEULA.txt)

## 源码与测试

- 项目：src/Bing.Utils.JsonSchema/Bing.Utils.JsonSchema.csproj
- 公开 API：src/Bing.Utils.JsonSchema/Bing/Json/Schema/JsonSchemaValidator.cs
- 测试：tests/Bing.Utils.JsonSchema.Tests；包含固定提交的官方 Draft 2020-12 离线样例，来源与许可证见 Fixtures/SOURCES.md。
- 方案与执行记录：ai_docs/tasks/2026-10-25-json-schema-validation。
