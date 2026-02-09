# 模块：Bing.Utils.Collections

## 1. 模块定位
- 目标：提供数组、集合、字典、只读集合等常用“集合工具/转换/判断/扩展”能力，面向日常业务代码减少样板。
    - 证据：src/Bing.Utils.Collections/Bing/Collections/Arrays.cs | Bing.Collections.Arrays
    - 证据：src/Bing.Utils.Collections/Bing/Collections/Colls.cs | Bing.Collections.Colls
    - 证据：src/Bing.Utils.Collections/Bing/Collections/Dicts.cs | Bing.Collections.Dicts
    - 证据：src/Bing.Utils.Collections/Bing/Collections/ReadOnlyColls.cs | Bing.Collections.ReadOnlyColls
- 非目标：不提供持久化集合/并发集合实现（仅对现有 BCL 集合做工具封装），不引入第三方集合库。
    - 证据：src/Bing.Utils.Collections/Bing.Utils.Collections.csproj | 未见 PackageReference
- 适用场景：
    - “返回空集合而非 null”的统一实践（如 `Arrays.Empty<T>()`、`Arrays.ToArraySafety(...)`）。
        - 证据：src/Bing.Utils.Collections/Bing/Collections/Arrays.cs | Arrays.Empty/ToArraySafety
    - 字典常用操作（AddOrUpdate / GetOrAdd / 级联 TryGet）。
        - 证据：src/Bing.Utils.Collections/Bing/Collections/Dicts.cs | Dicts.AddValueOrUpdate/GetValueOrAdd/TryGetValueCascading
    - 集合查询/排序/打乱/索引等通用逻辑。
        - 证据：src/Bing.Utils.Collections/Bing/Collections/Colls.cs | Colls.IndexOf/OrderByRandom/OrderByShuffle
    - 枚举器/集合转换（Enumerator -> IEnumerable、字典/集合排序、只读包装）。
        - 证据：src/Bing.Utils.Collections/Bing/Collections/CollConv.cs | CollConv.ToEnumerable/ToSortedArray/AsNullWhenEmpty
        - 证据：src/Bing.Utils.Collections/Bing/Collections/DictConv.cs | DictConv.ToDictionary/ToTuple/ToSortedArrayByKey
        - 证据：src/Bing.Utils.Collections/Bing/Collections/ReadOnlyColls.cs | ReadOnlyColls.Empty/Append/OfList
- 不适用场景：
    - 对随机性/可重复性有严格要求的“洗牌”（当前实现使用 `DateTime.Now.Ticks` + Random，每次调用不可复现且并发场景可预测性待确认）。
        - 证据：src/Bing.Utils.Collections/Bing/Collections/Colls.cs | Colls.OrderByShuffle

## 2. 目录结构
    src/Bing.Utils.Collections/
    tests/Bing.Utils.Collections.Tests/
        - 证据：src/Bing.Utils.Collections/Bing/Collections/ | Arrays/Colls/Dicts/ReadOnly* 等
        - 证据：tests/Bing.Utils.Collections.Tests/Bing/Collections/ArraysTest.cs | Arrays 单测

## 3. 对外 API
| API | 说明 | 参数 | 返回 | 异常 |
|---|---|---|---|---|
| `Arrays.Empty<T>()` | 获取缓存的空数组实例（避免返回 null） |  | `T[]` |  |
| `Arrays.ToArraySafety<T>(IEnumerable<T>)` / `ToArraySafety<T>(IEnumerable<T>, int)` | 安全转数组：处理 null；按 count 截取不足时用 default 填充 | `src`/`count` | `T[]` |  |
| `Arrays.ToArraySafety<T>(Array)` / `ToArraySafety<T>(Array, int)` | 非泛型数组安全转类型化数组（逐个强制转换） | `src`/`count` | `T[]` | `InvalidCastException`（元素不可转换） |
| `Arrays.GetLength(Array)` | 获取数组长度（null 直接抛） | `array` | `int` | `ArgumentNullException` |
| `Arrays.AreEqual(Array, Array)` | 比较两个数组（允许双方均为 null） | `array1`/`array2` | `bool` |  |
| `Arrays.Copy<T>(T[]/T[,]...T[,,,,,,])` | 克隆多维数组（1~7 维） | `bytes` | 新数组 |  |
| `Colls.BeContainedIn<T>(...)` | 判断元素是否包含在集合中（支持 comparer/条件表达式） | `item`/`items`/... | `bool` | `ArgumentNullException` |
| `Colls.ContainsAtLeast<T>(...)` | 判断集合/Queryable 至少包含 N 个元素 | `source`/`count` | `bool` |  |
| `Colls.IndexOf<T>(IEnumerable<T>, T, ...)` | 获取元素索引；找不到返回 -1 | `source`/`item` | `int` | `ArgumentNullException` |
| `Colls.OrderByRandom<T>(IEnumerable<T>)` / `OrderByShuffle<T>(IList<T>, int)` | 随机排序/原地洗牌 | `source`/`items`/`times` | `IEnumerable<T>` / `void` | `ArgumentNullException`（OrderByRandom） |
| `Dicts.AddValueOrUpdate/AddValueIfNotExist/GetValueOrAdd/...` | 字典常用写入与 GetOrAdd；支持 insert/update 委托 | `dictionary`/`key`/... | `void` / `TValue` | `ArgumentNullException`（委托为 null 等） |
| `Dicts.TryGetValueCascading/GetValueOrDefaultCascading` | 在多个字典中级联查找 key | `dictionaryColl`/`key` | `bool` / `TValue` | `ArgumentNullException`（dictionaryColl 为 null） |
| `CollConv.*` / `DictConv.*` | 集合/字典转换：Enumerator->Enumerable、排序数组、Nullable cast、只读字典 cast、Hashtable->Dictionary 等 | 见方法 | 多种 | `ArgumentNullException`（多处 guard） |
| `ReadOnlyColls.Empty/Append/OfList` + `ReadOnlyCollsExtensions.Append` | 只读集合/只读列表创建与追加 | `source`/`item`/`params` | `IReadOnlyCollection<T>` / `ReadOnlyCollection<T>` | `ArgumentNullException` |

> API 证据：
> - src/Bing.Utils.Collections/Bing/Collections/Arrays.cs | Arrays.Empty/ToArraySafety/GetLength/AreEqual
> - src/Bing.Utils.Collections/Bing/Collections/Arrays.Copy.cs | Arrays.Copy
> - src/Bing.Utils.Collections/Bing/Collections/Colls.cs | Colls.BeContainedIn/ContainsAtLeast/IndexOf/OrderByRandom/OrderByShuffle
> - src/Bing.Utils.Collections/Bing/Collections/Dicts.cs | Dicts.AddValueOrUpdate/GetValueOrAdd/TryGetValueCascading
> - src/Bing.Utils.Collections/Bing/Collections/CollConv.cs | CollConv.ToEnumerable/ToSortedArray/AsNullWhenEmpty
> - src/Bing.Utils.Collections/Bing/Collections/DictConv.cs | DictConv.ToDictionary/ToTuple/ToSortedArrayByKey
> - src/Bing.Utils.Collections/Bing/Collections/ReadOnlyColls.cs | ReadOnlyColls.Empty/Append/OfList

## 4. 核心类型
| 类型 | 职责 | 线程安全 | 备注 |
|---|---|---|---|
| `Bing.Collections.Arrays` | 数组工具：空数组、ToArraySafety、长度/比较、克隆多维数组等 | 是（纯静态方法，无共享状态） | `ToArraySafety(Array)` 依赖强制转换，可能抛 `InvalidCastException` |
| `Bing.Collections.Dicts` | 字典工具：AddOrUpdate、GetOrAdd、级联查找等 | 是（纯静态方法；但会修改传入的 Dictionary） | 多处对委托做 guard clause |
| `Bing.Collections.Colls` | 集合工具：包含判断、索引、随机排序、洗牌等 | 是（纯静态方法；会修改传入 List/IList） | `OrderByShuffle` 原地修改列表 |
| `Bing.Collections.CollJudge` / `ArrayJudge` | 判空/范围判断 | 是（纯静态） | `ArrayJudge.IsIndexInRange(Array,int,int)` 对 dimension 做范围检查 |
| `Bing.Collections.CollConv` / `DictConv` | 集合/字典转换与包装（包括只读字典 cast、Enumerator->Enumerable 等） | 是（纯静态） | `AsNullWhenEmpty` 使用 internal enumerator wrapper（single-use） |
| `Bing.Collections.ReadOnlyColls` / `ReadOnlyDicts` | 只读集合/字典相关工厂与操作 | 是（纯静态） | 返回 wrapper 类型（Internals 下）实现只读语义 |
| `Bing.Collections.EnumerableProxy<T>` | 将 IEnumerable 包装为可传递的代理（实现 IEnumerable<T>） | 取决于被包装 enumerable | 构造参数 null 会抛 `ArgumentNullException` |

> 核心类型证据：
> - src/Bing.Utils.Collections/Bing/Collections/Arrays.cs | Bing.Collections.Arrays
> - src/Bing.Utils.Collections/Bing/Collections/Dicts.cs | Bing.Collections.Dicts
> - src/Bing.Utils.Collections/Bing/Collections/Colls.cs | Bing.Collections.Colls
> - src/Bing.Utils.Collections/Bing/Collections/CollJudge.cs | Bing.Collections.CollJudge
> - src/Bing.Utils.Collections/Bing/Collections/ArrayJudge.cs | Bing.Collections.ArrayJudge
> - src/Bing.Utils.Collections/Bing/Collections/CollConv.cs | Bing.Collections.CollConv
> - src/Bing.Utils.Collections/Bing/Collections/DictConv.cs | Bing.Collections.DictConv
> - src/Bing.Utils.Collections/Bing/Collections/ReadOnlyColls.cs | Bing.Collections.ReadOnlyColls
> - src/Bing.Utils.Collections/Bing/Collections/EnumerableProxy.cs | Bing.Collections.EnumerableProxy<T>

## 5. 依赖关系
- 直接依赖：
    - `Bing.Utils`（项目引用）
        - 证据：src/Bing.Utils.Collections/references.props | ProjectReference=..\Bing.Utils\Bing.Utils.csproj
    - BCL：System.Linq / Expressions / Collections.ObjectModel 等
        - 证据：src/Bing.Utils.Collections/Bing/Collections/Colls.cs | using System.Linq.Expressions
        - 证据：src/Bing.Utils.Collections/Bing/Collections/ReadOnlyColls.cs | using System.Collections.ObjectModel
- 可选依赖：无（未发现外部第三方包引用）。
    - 证据：src/Bing.Utils.Collections/Bing.Utils.Collections.csproj | 未见 PackageReference
- 禁止依赖：待确认（仓库层面未见该模块显式“禁止依赖”约束声明）。

## 6. 关键实现说明
### 6.1 算法/流程
- Arrays：
    - `Empty<T>()` 返回缓存空数组（通过 InternalArray.ForEmpty<T>()），可用于高频路径避免分配。
        - 证据：src/Bing.Utils.Collections/Bing/Collections/Arrays.cs | Arrays.Empty
    - `ToArraySafety` 系列对 null 进行兜底，并按 count 截取/填充。
        - 证据：src/Bing.Utils.Collections/Bing/Collections/Arrays.cs | Arrays.ToArraySafety
- Colls：
    - `IndexOf` 通过 `Select((item,index)=>...)` + `FirstOrDefault` 查找并返回索引，找不到返回 -1。
        - 证据：src/Bing.Utils.Collections/Bing/Collections/Colls.cs | Colls.IndexOf
    - `OrderByShuffle` 通过多次 Random 交换实现原地打乱。
        - 证据：src/Bing.Utils.Collections/Bing/Collections/Colls.cs | Colls.OrderByShuffle
- Dicts：
    - `AddValueOrUpdate` 根据 `ContainsKey` 选择 insert/update 委托计算新值，再覆盖写入。
        - 证据：src/Bing.Utils.Collections/Bing/Collections/Dicts.cs | Dicts.AddValueOrUpdate
    - `TryGetValueCascading` 依次遍历字典集合，首个命中即返回。
        - 证据：src/Bing.Utils.Collections/Bing/Collections/Dicts.cs | Dicts.TryGetValueCascading
- CollConv/DictConv：
    - `ToEnumerable(IEnumerator<T>)` 通过 iterator/yield 将枚举器转换为可枚举序列。
        - 证据：src/Bing.Utils.Collections/Bing/Collections/CollConv.cs | CollConv.ToEnumerable
    - `AsNullWhenEmpty` 先探测 MoveNext；为空返回 null，否则用 internal wrapper 返回 single-use enumerable。
        - 证据：src/Bing.Utils.Collections/Bing/Collections/CollConv.cs | CollConv.AsNullWhenEmpty
    - `DictConv.Cast` 返回只读字典 wrapper，以实现键值类型向上转型。
        - 证据：src/Bing.Utils.Collections/Bing/Collections/DictConv.cs | DictConv.Cast

### 6.2 边界与异常处理
- Arrays：
    - `GetLength(null)` 抛 `ArgumentNullException`。
        - 证据：src/Bing.Utils.Collections/Bing/Collections/Arrays.cs | Arrays.GetLength
    - `ToArraySafety<T>(Array)` 对元素做强制转换，类型不匹配会抛 `InvalidCastException`（测试覆盖）。
        - 证据：src/Bing.Utils.Collections/Bing/Collections/Arrays.cs | Arrays.ToArraySafety<TElement>(Array,int)
        - 证据：tests/Bing.Utils.Collections.Tests/Bing/Collections/ArraysTest.cs | ToArraySafety_InvalidCastFromArrayWithCount_ThrowInvalidCastException
- Dicts：
    - 多处对 insert/update/doAct 委托做 null guard，并抛 `ArgumentNullException`。
        - 证据：src/Bing.Utils.Collections/Bing/Collections/Dicts.cs | Dicts.AddValueOrUpdate/AddValueOrDo
- CollConv/ReadOnlyColls：
    - 多数入口对 source/enumerator 做 null guard。
        - 证据：src/Bing.Utils.Collections/Bing/Collections/CollConv.cs | CollConv.ToEnumerable/AsEnumerableProxy
        - 证据：src/Bing.Utils.Collections/Bing/Collections/ReadOnlyColls.cs | ReadOnlyColls.Append
- ArrayJudge：
    - 多维索引检查中 dimension <=0 抛 `ArgumentOutOfRangeException`。
        - 证据：src/Bing.Utils.Collections/Bing/Collections/ArrayJudge.cs | ArrayJudge.IsIndexInRange(Array,int,int)

## 7. 性能与复杂度
- 时间复杂度（基于实现推断，未做 benchmark）：
    - `Arrays.ToArraySafety`：$O(n)$（最多遍历 count 或源长度）。
        - 证据：src/Bing.Utils.Collections/Bing/Collections/Arrays.cs | Arrays.ToArraySafety
    - `Dicts.TryGetValueCascading`：$O(k)$（k 为字典数量，命中即返回）。
        - 证据：src/Bing.Utils.Collections/Bing/Collections/Dicts.cs | Dicts.TryGetValueCascading
    - `Colls.OrderByShuffle`：$O(n \cdot times)$。
        - 证据：src/Bing.Utils.Collections/Bing/Collections/Colls.cs | Colls.OrderByShuffle
- 空间复杂度：多数方法为 $O(n)$（返回新数组/列表/中间序列）或 $O(1)$（就地修改）。
- 大数据量表现：待补 benchmark。
- Benchmark 链接：待补（benchmarks/Bing.Utils.Benchmark 目前未见 Collections 专项基准用例，待确认）。

## 8. 测试策略
- 单测覆盖点（以仓库现有事实为准）：
    - Arrays：`Empty`、`ToArraySafety` 多重重载、异常路径（InvalidCast）、以及大量边界组合。
        - 证据：tests/Bing.Utils.Collections.Tests/Bing/Collections/ArraysTest.cs | ArraysTest
- 边界用例（建议补充）：
    - Dicts：AddValueOrUpdate/GetValueOrAdd/级联查找等行为。
        - 证据：src/Bing.Utils.Collections/Bing/Collections/Dicts.cs | Dicts
    - Colls：OrderByShuffle 的稳定性/随机性与并发场景。
        - 证据：src/Bing.Utils.Collections/Bing/Collections/Colls.cs | Colls.OrderByShuffle
- 回归用例：
    - `AsNullWhenEmpty` 返回的 enumerable 为 single-use（内部 wrapper），建议补充“重复枚举行为”测试。
        - 证据：src/Bing.Utils.Collections/Bing/Collections/CollConv.cs | CollConv.AsNullWhenEmpty

## 9. 版本与兼容性
- 当前版本：1.5.0（全仓库版本号）。
    - 证据：version.props | VersionMajor/Minor/Patch
- 破坏性变更：待确认（建议结合 docs/ReleaseNotes.md 或 git history 验证）。
- 升级建议：
    - 业务代码可优先用 `Arrays.Empty<T>()`/`ToArraySafety(...)` 替换返回 null 的集合，减少空引用分支。
        - 证据：src/Bing.Utils.Collections/Bing/Collections/Arrays.cs | Arrays.Empty/ToArraySafety

## 10. 使用示例
```csharp
using System;
using System.Collections.Generic;
using Bing.Collections;

// 1) 返回空数组而非 null
var empty = Arrays.Empty<int>();

// 2) 安全转数组（null -> 空数组）
IEnumerable<string> maybeNull = null;
var arr = Arrays.ToArraySafety(maybeNull);

// 3) 字典 GetOrAdd
var dict = new Dictionary<string, int>();
var value = Dicts.GetValueOrAdd(dict, "k", 1);

// 4) 级联查找
var d1 = new Dictionary<string, int> { ["a"] = 1 };
var d2 = new Dictionary<string, int> { ["b"] = 2 };
var found = Dicts.TryGetValueCascading(new[] { d1, d2 }, "b", out var v);
```

> 示例证据：
> - src/Bing.Utils.Collections/Bing/Collections/Arrays.cs | Arrays.Empty/ToArraySafety
> - src/Bing.Utils.Collections/Bing/Collections/Dicts.cs | Dicts.GetValueOrAdd/TryGetValueCascading

## 11. 待办与改进
- [ ] 补齐 Dicts/Colls/Conv/ReadOnly* 的单测覆盖（当前仅看到 ArraysTest）。
    - 证据：tests/Bing.Utils.Collections.Tests/Bing/Collections/ArraysTest.cs | 仅 Arrays 覆盖
- [ ] 为 `OrderByShuffle` 的随机性与可重复性制定策略（例如支持注入 Random/种子，或明确不保证可重复）。
    - 证据：src/Bing.Utils.Collections/Bing/Collections/Colls.cs | Colls.OrderByShuffle
- [ ] 增加 benchmark 覆盖常用路径（ToArraySafety、Cascading lookup、Shuffle）。
    - 证据：benchmarks/Bing.Utils.Benchmark/Program.cs | 基准工程入口（待确认是否已覆盖 Collections）

## 12. 证据定位（汇总）
- src/Bing.Utils.Collections/Bing.Utils.Collections.csproj | PackageId=Bing.Utils.Collections
- src/Bing.Utils.Collections/references.props | ProjectReference=..\\Bing.Utils\\Bing.Utils.csproj
- src/Bing.Utils.Collections/Bing/Collections/Arrays.cs | Arrays.Empty/ToArraySafety/GetLength/AreEqual
- src/Bing.Utils.Collections/Bing/Collections/Arrays.Copy.cs | Arrays.Copy
- src/Bing.Utils.Collections/Bing/Collections/Dicts.cs | Dicts.AddValueOrUpdate/GetValueOrAdd/TryGetValueCascading
- src/Bing.Utils.Collections/Bing/Collections/Colls.cs | Colls.IndexOf/OrderByRandom/OrderByShuffle
- src/Bing.Utils.Collections/Bing/Collections/CollJudge.cs | CollJudge.IsNullOrEmpty/IsSameCount
- src/Bing.Utils.Collections/Bing/Collections/ArrayJudge.cs | ArrayJudge.IsIndexInRange
- src/Bing.Utils.Collections/Bing/Collections/CollConv.cs | CollConv.ToEnumerable/AsNullWhenEmpty
- src/Bing.Utils.Collections/Bing/Collections/DictConv.cs | DictConv.Cast/ToDictionary/ToTuple
- src/Bing.Utils.Collections/Bing/Collections/ReadOnlyColls.cs | ReadOnlyColls.Empty/Append/OfList
- src/Bing.Utils.Collections/Bing/Collections/EnumerableProxy.cs | EnumerableProxy<T>
- tests/Bing.Utils.Collections.Tests/Bing/Collections/ArraysTest.cs | Arrays 单测覆盖
- version.props | VersionMajor/VersionMinor/VersionPatch

## 13. 待确认
- `Colls.OrderByShuffle` 是否需要在库层面提供“可重复”的洗牌能力（当前实现每次种子取自 ticks）。
- `CollConv.AsNullWhenEmpty` 返回的 single-use enumerable 的预期行为是否需在文档中明确（重复枚举是否应抛异常/返回空/重复返回）。
