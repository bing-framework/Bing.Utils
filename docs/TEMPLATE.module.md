# {模块名} 模块设计说明

## 1. 模块定位
- 目标：
- 非目标：
- 适用场景：
- 不适用场景：

## 2. 目录结构
    src/{ProjectName}/
    tests/{ProjectName}.Tests/

## 3. 对外 API
| API | 说明 | 参数 | 返回 | 异常 |
|---|---|---|---|---|
| `MethodA(...)` |  |  |  |  |

## 4. 核心类型
| 类型 | 职责 | 线程安全 | 备注 |
|---|---|---|---|
| `XxxService` |  | 是/否 |  |

## 5. 依赖关系
- 直接依赖：
- 可选依赖：
- 禁止依赖：

## 6. 关键实现说明

### 6.1 算法/流程
- 步骤1：
- 步骤2：

### 6.2 边界与异常处理
- 输入校验：
- 异常策略：
- 失败回退：

## 7. 性能与复杂度
- 时间复杂度：
- 空间复杂度：
- 大数据量表现：
- Benchmark 链接：

## 8. 测试策略
- 单测覆盖点：
- 边界用例：
- 随机/属性测试（如有）：
- 回归用例：

## 9. 版本与兼容性
- 当前版本：
- 破坏性变更：
- 升级建议：

## 10. 使用示例

```csharp
/// <summary>
/// 示例：请替换为模块真实代码。
/// </summary>
public static class Example
{
    /// <summary>
    /// 执行示例逻辑。
    /// </summary>
    /// <param name="input">输入参数。</param>
    /// <returns>处理结果。</returns>
    /// <exception cref="ArgumentNullException">当输入为空时抛出。</exception>
    public static string Run(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            throw new ArgumentNullException(nameof(input));
        return input.Trim();
    }
}
```

## 11. 待办与改进
- []
- []