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

    [Fact]
    public void Singleton_AllSingletons_IsNotNull()
    {
        Singleton.AllSingletons.ShouldNotBeNull();
    }

    [Fact]
    public void Singleton_AllSingletons_IsIDictionary()
    {
        Singleton.AllSingletons.ShouldBeAssignableTo<IDictionary<Type, object>>();
    }

    // ─────────────────────────────────────────────────────────────────
    // Singleton<T>
    // ─────────────────────────────────────────────────────────────────

    /// <summary>用于隔离测试的专属标记类型</summary>
    private sealed class MyService { public string Name { get; set; } = "default"; }
    private sealed class AnotherService { }

    [Fact]
    public void SingletonT_Instance_SetAndGet_ReturnsSameValue()
    {
        var svc = new MyService { Name = "test" };
        Singleton<MyService>.Instance = svc;
        Singleton<MyService>.Instance.ShouldBeSameAs(svc);
    }

    [Fact]
    public void SingletonT_Instance_Set_RegisteredInAllSingletons()
    {
        var svc = new MyService { Name = "registered" };
        Singleton<MyService>.Instance = svc;
        Singleton.AllSingletons[typeof(MyService)].ShouldBeSameAs(svc);
    }

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

    private sealed class ListItem { }

    [Fact]
    public void SingletonList_Instance_IsNotNull()
    {
        SingletonList<ListItem>.Instance.ShouldNotBeNull();
    }

    [Fact]
    public void SingletonList_Instance_IsIList()
    {
        SingletonList<ListItem>.Instance.ShouldBeAssignableTo<IList<ListItem>>();
    }

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

    [Fact]
    public void SingletonDictionary_Instance_IsNotNull()
    {
        SingletonDictionary<string, int>.Instance.ShouldNotBeNull();
    }

    [Fact]
    public void SingletonDictionary_Instance_IsIDictionary()
    {
        SingletonDictionary<string, int>.Instance.ShouldBeAssignableTo<IDictionary<string, int>>();
    }

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
