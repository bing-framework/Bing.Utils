# Bing.Utils.DependencyInjection

## 状态

**待实现/待确认。**

## 核心实现与关键文件

- `src/Bing.Utils.DependencyInjection/Bing.Utils.DependencyInjection.csproj` 仅声明包名、描述和 `Bing/DependencyInjection` 目录。
- 当前没有可说明的核心实现文件；`Bing/DependencyInjection` 只是空目录声明。
- 当前未发现 `.cs` 源文件、服务注册扩展、`Microsoft.Extensions.DependencyInjection` 包引用或对应有效测试。
- 因此该项目目前不能视为已提供依赖注入功能，也没有可用的对外接口或用法示例。

## 依赖与配置

- 目标框架继承 `common.props`：`net8.0;net7.0;net6.0;netstandard2.0`。
- 项目描述提到 `Microsoft.Extensions.DependencyInjection`，但项目文件当前未配置该依赖。

## 对外接口与用法

当前没有公开类型或可调用接口，因此没有有效用法示例。任何服务注册扩展、生命周期辅助或自动扫描行为均属于**待实现/待确认**。

## 待实现/待确认

- 确认该包是否继续保留及其边界：服务注册扩展、生命周期辅助或仅用于聚合注册。
- 明确公共 API 后补充对应测试项目、最小注册示例和目标框架兼容性验证。
- 在实现落地前，不应发布或在其他模块文档中宣称该包可用。
