using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace Bing.Helpers;

/// <summary>
/// 测试结构体映射委托缓存的并发构建、键规范化和映射结果。
/// </summary>
[Trait("Bing.Helpers", "MapperHelper.Cache")]
[Collection("MapperHelperCacheSerial")]
public class MapperHelperCacheTests
{
    /// <summary>
    /// 冷启动并发访问同一结构体映射时，只允许编译一次委托。
    /// </summary>
    [Fact]
    public void Map_ConcurrentColdStart_CompilesDelegateOnce()
    {
        var source = new ConcurrentSource { Id = 7, Name = "concurrent" };
        var results = new ConcurrentBag<ConcurrentDestination>();
        var compileCountBefore = MapperHelper.StructMapCompileCount;

        Parallel.For(0, 32, _ =>
        {
            results.Add(MapperHelper.Map<ConcurrentSource, ConcurrentDestination>(source));
        });

        (MapperHelper.StructMapCompileCount - compileCountBefore).ShouldBe(1);
        results.Count.ShouldBe(32);
        results.All(result => result.Id == source.Id && result.Name == source.Name).ShouldBeTrue();
    }

    /// <summary>
    /// 指定属性的大小写、顺序和重复项变化应命中同一编译委托。
    /// </summary>
    [Fact]
    public void MapWith_PropertyNamesAreCanonicalized()
    {
        var source = new MapWithSource { Id = 11, Name = "canonical", Ignored = 99 };
        var compileCountBefore = MapperHelper.StructMapCompileCount;

        var first = MapperHelper.MapWith<MapWithSource, MapWithDestination>(source, "Name", "Id");
        var second = MapperHelper.MapWith<MapWithSource, MapWithDestination>(
            source,
            "id",
            null,
            "name",
            "ID");

        (MapperHelper.StructMapCompileCount - compileCountBefore).ShouldBe(1);
        first.Id.ShouldBe(source.Id);
        first.Name.ShouldBe(source.Name);
        first.Ignored.ShouldBe(0);
        second.Id.ShouldBe(source.Id);
        second.Name.ShouldBe(source.Name);
        second.Ignored.ShouldBe(0);
    }

    /// <summary>
    /// 排除属性的大小写、顺序和重复项变化应命中同一编译委托。
    /// </summary>
    [Fact]
    public void MapWithout_PropertyNamesAreCanonicalized()
    {
        var source = new MapWithoutSource { Id = 13, Name = "excluded", Kept = 17 };
        var compileCountBefore = MapperHelper.StructMapCompileCount;

        var first = MapperHelper.MapWithout<MapWithoutSource, MapWithoutDestination>(source, "Name", "Id");
        var second = MapperHelper.MapWithout<MapWithoutSource, MapWithoutDestination>(
            source,
            "id",
            null,
            "name",
            "ID");

        (MapperHelper.StructMapCompileCount - compileCountBefore).ShouldBe(1);
        first.Id.ShouldBe(0);
        first.Name.ShouldBeNull();
        first.Kept.ShouldBe(source.Kept);
        second.Id.ShouldBe(0);
        second.Name.ShouldBeNull();
        second.Kept.ShouldBe(source.Kept);
    }

    /// <summary>
    /// 可回收程序集中的结构体映射委托不得进入静态缓存并固定程序集生命周期。
    /// </summary>
    [Fact]
    public void Map_CollectibleTypes_DoNotEnterStaticDelegateCache()
    {
        var assembly = CreateCollectibleMap();
        for (var attempt = 0; attempt < 20 && assembly.IsAlive; attempt++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        assembly.IsAlive.ShouldBeFalse();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference CreateCollectibleMap()
    {
        var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(
            new AssemblyName($"MapperCacheProbe_{Guid.NewGuid():N}"),
            AssemblyBuilderAccess.RunAndCollect);
        var moduleBuilder = assemblyBuilder.DefineDynamicModule("Main");
        var sourceType = DefineValueType(moduleBuilder, "Source");
        var destinationType = DefineValueType(moduleBuilder, "Destination");
        var assembly = new WeakReference(sourceType.Assembly);

        var source = Activator.CreateInstance(sourceType);
        sourceType.GetProperty("Value").SetValue(source, 37);
        var mapDefinition = typeof(MapperHelper)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(method => method.Name == nameof(MapperHelper.Map)
                              && method.IsGenericMethodDefinition
                              && method.GetGenericArguments().Length == 2);
        var map = mapDefinition.MakeGenericMethod(sourceType, destinationType);
        var destination = map.Invoke(null, new[] { source });
        destinationType.GetProperty("Value").GetValue(destination).ShouldBe(37);

        destination = null;
        map = null;
        mapDefinition = null;
        source = null;
        sourceType = null;
        destinationType = null;
        moduleBuilder = null;
        assemblyBuilder = null;
        return assembly;
    }

    private static Type DefineValueType(ModuleBuilder moduleBuilder, string name)
    {
        var typeBuilder = moduleBuilder.DefineType(
            name,
            TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.SequentialLayout | TypeAttributes.BeforeFieldInit,
            typeof(ValueType));
        var field = typeBuilder.DefineField("_value", typeof(int), FieldAttributes.Private);
        var getter = typeBuilder.DefineMethod(
            "get_Value",
            MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig,
            typeof(int),
            Type.EmptyTypes);
        var getterIl = getter.GetILGenerator();
        getterIl.Emit(OpCodes.Ldarg_0);
        getterIl.Emit(OpCodes.Ldfld, field);
        getterIl.Emit(OpCodes.Ret);

        var setter = typeBuilder.DefineMethod(
            "set_Value",
            MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig,
            typeof(void),
            new[] { typeof(int) });
        var setterIl = setter.GetILGenerator();
        setterIl.Emit(OpCodes.Ldarg_0);
        setterIl.Emit(OpCodes.Ldarg_1);
        setterIl.Emit(OpCodes.Stfld, field);
        setterIl.Emit(OpCodes.Ret);

        var property = typeBuilder.DefineProperty("Value", PropertyAttributes.None, typeof(int), Type.EmptyTypes);
        property.SetGetMethod(getter);
        property.SetSetMethod(setter);
        return typeBuilder.CreateTypeInfo().AsType();
    }

    private struct ConcurrentSource
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    private struct ConcurrentDestination
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    private struct MapWithSource
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Ignored { get; set; }
    }

    private struct MapWithDestination
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Ignored { get; set; }
    }

    private struct MapWithoutSource
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Kept { get; set; }
    }

    private struct MapWithoutDestination
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Kept { get; set; }
    }
}
