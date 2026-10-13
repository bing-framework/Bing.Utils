# Bing.Utils.Guard

## 功能说明

`Bing.Utils.Guard` 当前只提供一项公开能力：`CharJudge.IsBetween` 按 UTF-16 `char` 码元判断字符是否位于闭区间。项目还包含供包内使用的验证异常构造辅助。

## 核心实现与关键文件

- `src/Bing.Utils.Guard/Bing/Text/CharJudge.cs`：执行 `value >= left && value <= right` 的闭区间比较。
- `src/Bing.Utils.Guard/Bing/Text/CharGuard.cs`：公开 `CharGuard` 目前为空；同文件的 `CharGuardHelper` 为内部类型。
- `src/Bing.Utils.Guard/Bing/Validation/ValidationExceptionHelper.cs`：内部按构造参数创建指定异常，并保留构造函数抛出的原始异常。
- 测试：`tests/Bing.Utils.Guard.Tests/CharJudgeTest.cs` 和 `ValidationExceptionHelperTest.cs`。

## 对外接口与用法

```csharp
using Bing.Text;

bool isDigit = CharJudge.IsBetween('5', '0', '9');
bool reversed = CharJudge.IsBetween('b', 'z', 'a'); // false
```

## 依赖与配置

- 通过 `project.dependency.props` 引用 `Bing.Utils`，目标框架继承 `common.props`。
- 无运行时配置项。

## 已知限制与注意事项

- `CharGuard` 是空公开类型，属于**待实现/待确认**，不能作为参数守护入口使用。
- 比较基于单个 UTF-16 `char`，不理解 Unicode 标量值、组合字符、区域或文化排序。
- 倒序边界不会自动交换，结果始终为 `false`。
- `ValidationExceptionHelper` 为内部实现，不属于对外 API。
