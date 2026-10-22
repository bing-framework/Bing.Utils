# Bing.Utils.DependencyInjection

## 功能与状态

DI-01～DI-03 已实现：基于 Microsoft 原生 DI 的显式接口批量注册、受控程序集扫描、标准开放泛型，以及有限服务集合的复杂泛型提前闭合。生命周期必须显式指定，所有入口返回原 `IServiceCollection`。验证结果见 [DI 执行记录](../quality/dependency-injection-execution.md)。

## 核心逻辑与关键文件

生产目录：`src/Bing.Utils.DependencyInjection/Bing/DependencyInjection/`。

| 文件 | 职责 |
| --- | --- |
| `BingServiceCollectionExtensions.cs` | 六个公开入口，包含普通注册及扫描的泛型/Type 重载 |
| `ServiceRegistrationStrategy.cs` | 跳过、追加、拒绝重复组合三种策略 |
| `ServiceRegistrationBuilder.cs` | 参数及兼容性校验、确定性扫描、临时集合冲突检查 |
| `GenericImplementationMatcher.cs` | 递归结构匹配、泛型约束校验和歧义报告 |

所有输入先枚举、校验并生成描述符，再修改目标集合；参数、类型加载、谓词、推导或重复冲突失败时不留下本批部分注册。集合自身的自定义 `Add` 故障不在此保证内。注册阶段不执行工厂、构造函数或 `BuildServiceProvider`，对象创建、依赖解析和释放由容器负责。

## 接口与示例

扩展入口位于 `Microsoft.Extensions.DependencyInjection`，策略枚举位于 `Bing.DependencyInjection`。示例类型由应用定义，调用代码放在启动方法中。

### 普通注册

```csharp
using Bing.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

services.AddImplementations<IHandler>(
    new[] { typeof(FirstHandler), typeof(SecondHandler) },
    ServiceLifetime.Scoped, ServiceRegistrationStrategy.SkipDuplicates);
// FirstHandler、SecondHandler 都实现应用定义的 IHandler。
```

同样支持 `services.AddImplementations(typeof(IHandler), types, lifetime, strategy)`。

| 策略 | 行为 |
| --- | --- |
| `SkipDuplicates`（默认） | 原生 `TryAddEnumerable` 去重，保留不同实现及重复组合原有生命周期 |
| `Append` | 全部追加，包括本批或已有的重复组合 |
| `ThrowOnDuplicate` | 本批或已有重复组合使整批抛 `InvalidOperationException` |

重复识别沿用原生容器的服务类型、实现类型及服务键规则。已有实例按其运行时类型识别；工厂不执行，按原生描述符可识别的返回类型处理，不能据此猜测其实际创建类型。生成的注册均无服务键，已有 keyed 注册按原生规则与其区分。

### 可控扫描

```csharp
services.ScanImplementations<IHandler>(
    new[] { typeof(FirstHandler).Assembly },
    ServiceLifetime.Transient,
    predicate: type => type.Name.EndsWith("Handler", StringComparison.Ordinal));
```

`Type serviceType` 重载支持运行时服务类型。只扫描显式传入的程序集；按输入顺序去重，各程序集内按 `FullName` 序数排序。默认只选择公开可见、非抽象的实现类。谓词应用于可见非抽象类，再筛选服务兼容性；类型加载和谓词异常直接传播。

### 标准开放泛型

```csharp
// 应用类型：public interface IRepository<T> { }
// public sealed class Repository<T> : IRepository<T> { }
services.AddImplementations(typeof(IRepository<>),
    new[] { typeof(Repository<>) }, ServiceLifetime.Scoped);
```

服务、实现必须都是泛型定义，参数数量相同，接口投影按实现参数原序一一对应。构造函数依赖和具体泛型约束由容器在解析时检查。不满足标准映射的候选会提示使用提前闭合入口。

### 复杂泛型提前闭合

```csharp
// 应用类型：public interface IPair<TLeft, TRight> { }
// public sealed class Reversed<TLeft, TRight> : IPair<TRight, TLeft> { }
// 为 IPair<string,int> 推导出 Reversed<int,string>：
services.AddClosedGenericImplementations(
    new[] { typeof(IPair<string, int>) },
    new[] { typeof(Reversed<,>) }, ServiceLifetime.Singleton);

services.ScanClosedGenericImplementations(
    new[] { typeof(IPair<string, int>) },
    new[] { typeof(Reversed<,>).Assembly }, ServiceLifetime.Singleton,
    predicate: type => type == typeof(Reversed<,>));
```

服务集合必须是闭合泛型接口，实现可为闭合类或泛型类定义。读取包括继承接口在内的投影，支持重排、嵌套泛型、固定参数及数组（区分维度及向量形状）；重复变量须绑定一致，全部实现参数须可推导。`MakeGenericType` 检查 CLR 约束，随后检查可赋值性。

结构/约束不匹配的候选跳过；任一请求服务无有效实现时报告服务、候选及原因，整批失败。同一实现定义得到多个不同有效闭合结果时报告歧义，不自动挑选。

## 依赖与配置

- 保留生产目标 `net8.0;net7.0;net6.0;netstandard2.0`。
- 运行库仅直接引用 `Microsoft.Extensions.DependencyInjection.Abstractions 8.0.2`；应用需提供原生容器或兼容 `IServiceCollection` 的容器。
- 现有模块测试使用官方容器 `Microsoft.Extensions.DependencyInjection 8.0.1`，不引入生产容器依赖。
- 继承的 SourceLink 工具在本项目标为私有构建依赖，不传递给包消费者。
- 无配置文件、全局类型缓存、程序集自动加载或环境变量配置。

## 限制与注意事项

- 仅面向应用启动阶段，不支持调用期间并发修改同一服务集合；不捕获用户枚举器、谓词或类型加载异常。
- null 参数/元素、非法枚举、非接口服务、抽象或不兼容实现抛参数异常；普通注册/扫描空候选无操作，复杂闭合仍要求每个已列服务找到实现。空服务集合无注册，但其他参数仍校验。
- 复杂闭合不做协变/逆变推导，不猜测未绑定参数，不在解析时补充未列服务；不会自动注册实现依赖。
- 不扫描目录或进程全部程序集，不自动暴露实现的所有接口，不提供 keyed 注册入口或装饰器功能。
- 反射需要保留接口、泛型约束及构造函数元数据；net6+ 入口声明 `RequiresUnreferencedCode`，复杂闭合在 net7+ 同时声明 `RequiresDynamicCode`。不承诺任意程序集裁剪/NativeAOT 兼容；netstandard2.0 资产仍有相同运行时限制。
- 未实施额外 AOT/裁剪发布矩阵及旧测试运行时执行，不把四生产 TFM 编译等同于所有部署场景已验证。
