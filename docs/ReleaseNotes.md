# Bing.Utils 发行说明

## [1.5.0](https://www.nuget.org/packages/Bing.Utils/1.5.0)

### 🚀新功能
- 🆕 组件新增
    - ✨ 新增 `ValueTask` 扩展方法
    - ✨ 新增 `LogHelper` 日志操作辅助类
    - ✨ 新增 `FileInfo` 扩展方法
    - ✨ 新增 `Converter` 类型转换构建器
    - ✨ 新增 `ThreadHelper` 线程操作工具类
    - ✨ 新增 `ReflectionHelper` 反射操作类
    - ✨ 新增 `StringHelper` 扩展
    - ✨ 新增 `DateTimeHelper` 计算星座、生肖等日期相关工具
- 📂 文件流操作优化
    - ✨ 新增 `MemoryStreamExtensions` 操作类
    - ✨ 新增 `Stream.CopyToAsync` 扩展
    - ✨ 优化 `FileHelper`，支持 `BufferedStream` 写入

### 🎨 代码重构
- ⚡ 重构 `Logging` 日志组件
    - 改进 `GenericLogger` 实现，减少重复代码，提高日志性能
    - `ILogger` 扩展方法支持更灵活的参数传递
- 📊 `Threading` 线程操作优化
    - 新增 `Task` 相关扩展，优化 `AsyncHelper`
    - 增加 `SemaphoreSlim` 扩展方法
- 📑 `Reflection` 反射增强
    - 增强 `TypeVisit`，支持 `CreateInstance` 更灵活的对象实例化
    - 调整 `GetAttribute` 获取属性的方式

### 🔨 修复 & 改进
- 🛠 修复 HttpRequest 上传文件问题
- 🛠 修复 Lambda 逻辑计算错误
- 🛠 修复 Base64 编解码兼容问题
- 🛠 优化 Try.Invoke 方法，增强错误捕获
- 🛠 修复 DateTime 星座、生肖计算异常

## [1.4.0](https://www.nuget.org/packages/Bing.Utils/1.4.0)

- 修复`RMB`大写金额转换空异常；
- 增加`CmdHeleper`命令行帮助类；
- 优化`FileHelper`文件帮助类读写操作；
- 增加`ExpandoObject`动态对象扩展方法；
- 优化`HttpRequest`请求支持证书设置；
- `decimal`增加数值截断方法`RoundTruncate`；
- 增加`DataTableHelper`数据表帮助类；
- 优化`DateTime`扩展方法；
- 重构并迁移出`Bing.Utils.Collections`类库；
- 增加`DictConv`字典转换操作类、`ReadOnlyDictConv`只读字典转换操作类；
- 增加`Arrays`数组操作类、`Colls`集合操作类、`ReadOnlyColls`只读集合操作类；
- 增加`Dicts`字典操作类、`ReadOnlyDicts`只读字典操作类；
- 增加`CollConv`集合转换操作类、`ReadOnlyCollConv`只读集合转换操作类；
- 增加`IdGenerator`ID生成器；
- 优化类型操作、类型反射操作；
- 优化数值操作；
- 优化时间操作；
- 增加`FastPathMatcher`快速路径匹配器；

## [1.3.0](https://www.nuget.org/packages/Bing.Utils/1.3.0)

- 修复`Locking`扩展方法问题
- `DirectoryHelper`增加文件夹相关操作
- 新增`Color`相关扩展
- 新增`Image`效果转换操作
- 优化`PathHelper`操作，不依赖于`HttpContext`

## [1.2.5](https://www.nuget.org/packages/Bing.Utils/1.2.5)

- 新增临时文件、临时目录、沙箱操作类
- 新增注释操作类
- 新增`TypeReflections`反射-派生(继承)相关操作
- 新增`Platform`平台操作类，用于获取物理路径相关
- 优化`FileHelper`文件操作类，调整异步调用方法
- `Conv`增加对象转换字典方法
- 优化`Time`时间操作多线程问题
- 支持枚举分组

## [1.2.4](https://www.nuget.org/packages/Bing.Utils/1.2.4)

- 新增`MapperHelper`映射器帮助类
- 优化`Check`操作方法
- 新增`TypeReflections`类型反射操作
- 新增`DateTimeOffset`扩展方法

## [1.2.3](https://www.nuget.org/packages/Bing.Utils/1.2.3)

- 支持 `net5`
- 调整异常获取方法
- 新增日期时间间隔相关方法
- 调整`Http`操作
- 重构数值相关操作
- 新增类型转换操作

## [1.2.2](https://www.nuget.org/packages/Bing.Utils/1.2.2)

- 分离后拆分库，并支持 `netcore3.1`