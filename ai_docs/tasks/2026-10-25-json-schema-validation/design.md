# JSON Schema 校验方案

日期：2026-10-25。任务来源：用户已接受前一轮方案分析，并明确授权依次完成方案、计划、代码与验证。

## 目标与范围

新增独立可选包 Bing.Utils.JsonSchema，首版目标 net8.0，命名空间 Bing.Json.Schema。使用 JsonSchema.Net 9.4.1 完成标准求值，提供可复用校验器、string/JsonElement 输入、结构化结果、显式方言、format 开关和预注册 Schema 引用。新包不引用核心 Bing.Utils 项目，不变更现有项目的 JSON API、依赖和目标框架。

当前工作区基于 9258eab，含其他任务的大量未提交变更。仅增加本任务项目、文档，以及解决方案/模块目录的必要入口。

## 方案比较与决策

| 方案 | 标准与工程适配 | 成本与速度 | 决策 |
|---|---|---|---|
| JsonSchema.Net | System.Text.Json 原生输入，支持 Draft 6/7、2019-09、2020-12，分离构建和求值 | 接入较快，依赖独立；9.x 二进制维护费条款需随文档披露 | 首版采用 9.4.1 |
| NJsonSchema | 适合 DTO/代码生成及 NSwag | 本任务不需要生成工具链 | 暂不引入 |
| Corvus.JsonSchema | 支持现代标准，Apache-2.0 | 动态校验涉及 Roslyn，接入范围更大 | 可作为后续替代引擎评估 |
| 自研引擎 | 需要自行承担组合规则、引用、版本兼容 | 开发和长期维护成本高 | 不采用 |

JsonSchema.Net 9.0 起的官方二进制包带 OSMF EULA，源码仍为 MIT；当前条款对用于营收活动且年总营收达到 10,000 美元的使用者规定维护费条件。本任务仅进行已授权的本地实现和验证，不购买服务或发布包。包内 README 保留官方许可入口，不把第三方依赖声称为仅 MIT。旧目标框架会涉及较新 System.Text.Json 传递依赖，首版只提供 net8.0。

来源：https://www.nuget.org/packages/JsonSchema.Net/9.4.1 、https://github.com/json-everything/json-everything/blob/master/OSMFEULA.txt 。

## 公开契约

- JsonSchemaValidator.Create(string schemaJson, JsonSchemaValidatorOptions? options = null)：准备 Schema，返回可并发复用实例；可提供 JsonElement Schema 重载，输入在创建时复制。
- Validate(string json) / Validate(JsonElement instance)：只读校验，保留原始 JSON 的缺失、null、数字及类型语义。
- JsonSchemaValidatorOptions：DefaultDialect（默认 Draft202012）、BaseUri（可选绝对 URI）、ReferencedSchemas（URI 到 Schema JSON 文本）、ValidateFormats（默认 false）、Culture（默认 InvariantCulture）。创建时快照配置与引用集合。
- JsonSchemaDialect：Draft6、Draft7、Draft201909、Draft202012；显式 $schema 优先，未知方言作为配置错误。
- JsonSchemaValidationResult：Status（Valid/InvalidJson/SchemaMismatch）、IsValid、Errors、SyntaxError。
- JsonSchemaValidationError：InstancePath（JSON Pointer）、SchemaLocation（URI/片段）、Keyword、Message、Children。集合只读；不暴露第三方类型。
- JsonSchemaSyntaxError：Message、LineNumber、BytePositionInLine；行/字节位置沿用 System.Text.Json 的零基语义。
- JsonSchemaConfigurationException：Schema 非法、方言不支持、引用无法解析时明确失败。空引用参数、无效 options 属于调用参数错误；不吞掉编程错误。

## 构建、引用与生命周期

1. 校验参数并复制 Schema 输入、选项和引用文本，校验器持有独立的配置与输入快照。
2. 显式选择默认方言，不修改第三方全局 Default/Registry/Culture。
3. 分两阶段装入根 Schema 与预注册引用。通过公开文档接口和 BuildNode 先构建私有结构、登记内嵌资源，并复制实际引用到的宿主全局内存资源；随后才用准备图解析全部静态引用，避免引擎把共享节点重绑定到实例资源。相对引用相对 BaseUri/$id 解析，支持内部 $defs、锚点和合法递归引用。
4. 在实例交付前完成可静态解析的引用准备；无法解析的引用明确作为配置错误，不静默通过。默认无网络和文件加载。
5. 重复 Validate 使用已构建 Schema，每次求值的结果相互独立。无需额外无界全局缓存。

## 结果与语义

JSON 语法错误返回 InvalidJson，Schema 违反返回 SchemaMismatch；Schema 配置错误抛异常。成功结果 Errors 为空。分层诊断只沿失败的求值分支提取，避免把成功 anyOf/not/if 的内部失败误报。保留组合失败的 Children；oneOf 多分支同时成功仍须返回组合错误。required 缺失字段的位置遵循父对象位置，不拼造不存在节点。

format 默认遵循标准注解语义，通过 ValidateFormats 显式开启断言。校验不会填充 default、转换字符串数字或修改输入。

## 测试与交付

按 test-project-boundary/refs/test-project-boundary.md 的模块归属规则，所有新增纯逻辑测试放 tests/Bing.Utils.JsonSchema.Tests，不迁移既有测试。覆盖正常、边界、异常、参数化输入、JSON Pointer、组合规则、引用顺序、缺失引用、方言、format、输入生命周期、配置快照与并发复用；固定若干官方 Draft 2020-12 测试文件，离线运行并记录提交及许可证。

验证依次为新项目 restore/build、模块完整测试、pack、独立 NuGet 消费者冒烟及静态差异检查。新项目无现有生产依赖者；原核心包和其他模块代码没有运行时变动，故验证边界是新包及消费者，不运行无关全量资源矩阵。

## 后续范围

Stream 异步读取、NDJSON/数组逐项校验、DTO 生成、ASP.NET Core 接入和旧 TFM 支持留到后续独立需求。普通 DOM 校验不承诺恒定内存、实时取消或对任意不可信 Schema 的硬资源隔离。

## 实现核实后的边界

依赖精确锁定为 JsonSchema.Net [9.4.1]，其构建注册顺序经对应源码提交 de5ef333009b20db9a7437b9e6433dfa9bb88d44 核实。实例同步进入引擎，避免动态引用延迟解析的并发状态问题。

引擎仍可能读取宿主直接注册到其全局注册表的内存 Schema；本封装禁止全局 Fetch 回退且不修改全局配置，但不承诺隔离宿主对全局注册表的直接操作。宿主同时使用 JsonSchema.Net 时应避免冲突标识。该边界已写入使用文档，不影响默认独立实例及各自预注册引用的隔离测试。


## F1—F4 兼容修正

保持 JsonSchema.Net [9.4.1] 和公开 API 不变。编译完成后建立按规范资源 URI 和名称索引的动态锚点表，先通过注册表把检索别名还原到规范 URI。动态求值采用最外层同名锚点；动态作用域没有覆盖时使用初始引用目标。

局部关键字适配通过 BuildContext.From、Dialect.With、JsonSchema.BuildNode 生成，只替换当前校验器构建的引用关键字，保留引擎的已解析目标及引用类型语义，不修改全局方言或共享布尔节点。外部布尔引用按求值结果对象记录实际 URI，根 false 使用配置 BaseUri；记录在实例锁内使用，并在每次调用的 finally 中清空。嵌套布尔 Schema 的 JSON Pointer 仍沿用原有位置。

方言声明先拒绝非空 URI 片段，再与四种受支持方言比较；无片段及空片段继续兼容。定向回归、模块完整测试、打包消费和独立复核证据见 execution.md。


## F5—F6 兼容修正（2026-10-26）

按每个节点构建时的实际方言决定 $ref 同级规则是否参与求值。旧方言节点将同级规则适配为忽略求值，同时保留结构子节点供指针寻址。私有求值入口统一允许引用同级关键字，各资源不再受入口默认方言的全局开关影响；不改调用方 Schema 文本或公开 API。

SchemaDocumentStore 使用 BuildContext.From、JsonSchema.BuildNode 和 IBaseDocument 公开接口，先登记显式文档及其内嵌资源，再按实际构建方言复制引用到的宿主内存资源，保留检索别名和规范 URI。副本根带相对路径的 $id 转成规范 URI；Draft 6/7 的 #anchor 形式保留，以维持锚点登记。复制阶段只构建结构，不解析引用。全部私有资源准备完成后才调用引擎 Build，避免共享 A→B 引用被实例内同 URI 的 B 重绑定。指针既支持结构子节点，也支持指向未知关键字或整个 Schema 容器的目标。

资源位置按原始文档的结构边遍历，引用边不参与路径递推；进入不同 BaseUri 时重置资源内 JSON Pointer。使用 9.4.1 公开高级 PathFromResourceRoot 属性修正路径，嵌套 false 额外保存位置元数据供引用诊断；顶层共享布尔节点不变。后续引擎版本升级须重新执行相关兼容回归。
