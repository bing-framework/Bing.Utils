namespace Bing.Utils.Tests.Bing;

/// <summary>
/// <see cref="Singleton"/>、<see cref="Singleton{T}"/>、<see cref="SingletonList{T}"/>、
/// <see cref="SingletonDictionary{TKey,TValue}"/> 单元测试
/// </summary>
public class SingletonTests
{
    // ─────────────────────────────────────────────────────────────────
    // Singleton base
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// 验证全部单例注册表非空。
    /// </summary>
    [Fact]
    public void Singleton_AllSingletons_IsNotNull()
    {
        Singleton.AllSingletons.ShouldNotBeNull();
    }

    /// <summary>
    /// 验证单例注册表实现类型到对象的字典接口。
    /// </summary>
    [Fact]
    public void Singleton_AllSingletons_IsIDictionary()
    {
        Singleton.AllSingletons.ShouldBeAssignableTo<IDictionary<Type, object>>();
    }

    // ─────────────────────────────────────────────────────────────────
    // Singleton<T>
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// 用于隔离单例测试的服务类型。
    /// </summary>
    private sealed class MyService
    {
        /// <summary>
        /// 获取或设置服务名称。
        /// </summary>
        public string Name { get; set; } = "default";
    }
    /// <summary>
    /// 用于验证不同类型单例隔离的服务类型。
    /// </summary>
    private sealed class AnotherService { }

    /// <summary>
    /// 验证泛型单例读回设置的同一实例。
    /// </summary>
    [Fact]
    public void SingletonT_Instance_SetAndGet_ReturnsSameValue()
    {
        var svc = new MyService { Name = "test" };
        Singleton<MyService>.Instance = svc;
        Singleton<MyService>.Instance.ShouldBeSameAs(svc);
    }

    /// <summary>
    /// 验证设置泛型单例时同步注册到全部单例表。
    /// </summary>
    [Fact]
    public void SingletonT_Instance_Set_RegisteredInAllSingletons()
    {
        var svc = new MyService { Name = "registered" };
        Singleton<MyService>.Instance = svc;
        Singleton.AllSingletons[typeof(MyService)].ShouldBeSameAs(svc);
    }

    /// <summary>
    /// 验证不同服务类型的单例互相独立。
    /// </summary>
    [Fact]
    public void SingletonT_DifferentTypes_AreIndependent()
    {
        var myService = new MyService { Name = "my" };
        var another = new AnotherService();

        Singleton<MyService>.Instance = myService;
        Singleton<AnotherService>.Instance = another;

        Singleton<MyService>.Instance.ShouldBeSameAs(myService);
        Singleton<AnotherService>.Instance.ShouldBeSameAs(another);
    }

    /// <summary>
    /// 验证覆盖泛型单例会更新保存的实例。
    /// </summary>
    [Fact]
    public void SingletonT_Instance_Overwrite_UpdatesValue()
    {
        var svc1 = new MyService { Name = "first" };
        var svc2 = new MyService { Name = "second" };

        Singleton<MyService>.Instance = svc1;
        Singleton<MyService>.Instance.ShouldBeSameAs(svc1);

        Singleton<MyService>.Instance = svc2;
        Singleton<MyService>.Instance.ShouldBeSameAs(svc2);
    }

    // ─────────────────────────────────────────────────────────────────
    // SingletonList<T>
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// 用于隔离单例列表测试的元素类型。
    /// </summary>
    private sealed class ListItem { }

    /// <summary>
    /// 验证单例列表非空。
    /// </summary>
    [Fact]
    public void SingletonList_Instance_IsNotNull()
    {
        SingletonList<ListItem>.Instance.ShouldNotBeNull();
    }

    /// <summary>
    /// 验证单例列表实现对应元素类型的列表接口。
    /// </summary>
    [Fact]
    public void SingletonList_Instance_IsIList()
    {
        SingletonList<ListItem>.Instance.ShouldBeAssignableTo<IList<ListItem>>();
    }

    /// <summary>
    /// 验证单例列表支持追加并查找到元素。
    /// </summary>
    [Fact]
    public void SingletonList_Instance_CanAddItems()
    {
        var item = new ListItem();
        SingletonList<ListItem>.Instance.Add(item);
        SingletonList<ListItem>.Instance.ShouldContain(item);
        // 清理：避免影响其他测试
        SingletonList<ListItem>.Instance.Remove(item);
    }

    // ─────────────────────────────────────────────────────────────────
    // SingletonDictionary<TKey, TValue>
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// 验证单例字典非空。
    /// </summary>
    [Fact]
    public void SingletonDictionary_Instance_IsNotNull()
    {
        SingletonDictionary<string, int>.Instance.ShouldNotBeNull();
    }

    /// <summary>
    /// 验证单例字典实现对应键值类型的字典接口。
    /// </summary>
    [Fact]
    public void SingletonDictionary_Instance_IsIDictionary()
    {
        SingletonDictionary<string, int>.Instance.ShouldBeAssignableTo<IDictionary<string, int>>();
    }

    /// <summary>
    /// 验证单例字典支持按键写入并读取值。
    /// </summary>
    [Fact]
    public void SingletonDictionary_Instance_CanAddAndRetrieve()
    {
        const string key = "__test_singleton_key__";
        SingletonDictionary<string, int>.Instance[key] = 123;
        SingletonDictionary<string, int>.Instance[key].ShouldBe(123);
        // 清理
        SingletonDictionary<string, int>.Instance.Remove(key);
    }
}
