using Bing.Dynamic;
using System.Collections;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Threading.Tasks;

namespace Bing.Reflection;

#if NET6_0_OR_GREATER

/// <summary>动态类型模式和实例边界测试。</summary>
[Trait("Reflection", "DynamicTypeFactory")]
public class DynamicTypeFactoryTests
{
    [Fact]
    public void CreateDynamicType_ConcurrentSameSchema_ReusesOneType()
    {
        var types = new Type[32];
        Parallel.For(0, types.Length, i =>
        {
            var schema = i % 2 == 0
                ? new Dictionary<string, Type> { ["Name"] = typeof(string), ["Count"] = typeof(int) }
                : new Dictionary<string, Type> { ["Count"] = typeof(int), ["Name"] = typeof(string) };
            types[i] = TypeVisit.CreateDynamicType(schema);
        });

        foreach (var type in types)
            type.ShouldBeSameAs(types[0]);
    }

    [Fact]
    public void CreateDynamicType_ConcurrentFirstAccess_EmitsOnce()
    {
        var cache = new TypeFactory.SchemaCache(2);
        var types = new Type[32];
        Parallel.For(0, types.Length, index =>
            types[index] = TypeFactory.CreateType(
                new Dictionary<string, Type> { ["Value"] = typeof(int) }, cache));

        types.ShouldAllBe(type => ReferenceEquals(type, types[0]));
        cache.BuildCount.ShouldBe(1);
    }

    [Fact]
    public void CreateDynamicType_ConcurrentDistinctSchemas_EmitsEachOnce()
    {
        var cache = new TypeFactory.SchemaCache(8);
        var types = new Type[32];
        Parallel.For(0, types.Length, index =>
            types[index] = TypeFactory.CreateType(
                new Dictionary<string, Type> { [$"Value{index % 4}"] = typeof(int) }, cache));

        cache.BuildCount.ShouldBe(4);
        for (var index = 0; index < types.Length; index++)
            types[index].ShouldBeSameAs(types[index % 4]);
    }

    [Fact]
    public void CreateDynamicType_MutatedDictionary_RevalidatesSchema()
    {
        var schema = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase) { ["Value"] = typeof(int) };
        var first = TypeVisit.CreateDynamicType(schema);
        TypeVisit.CreateDynamicType(schema).ShouldBeSameAs(first);

        schema["Value"] = typeof(long);
        var changedType = TypeVisit.CreateDynamicType(schema);
        changedType.ShouldNotBeSameAs(first);
        changedType.GetProperty("Value")!.PropertyType.ShouldBe(typeof(long));

        schema.Remove("Value");
        schema["value"] = typeof(long);
        var renamedType = TypeVisit.CreateDynamicType(schema);
        renamedType.ShouldNotBeSameAs(changedType);
        renamedType.GetProperty("value")!.PropertyType.ShouldBe(typeof(long));
    }

    [Fact]
    public void CreateDynamicType_FastSchemaSnapshot_StoresTypeIdentityWithoutStrongTypeValue()
    {
        var snapshotType = typeof(TypeFactory).GetNestedType("SchemaFastEntry", BindingFlags.NonPublic)!;
        var constructor = snapshotType.GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            new[] { typeof(KeyValuePair<string, Type>[]), typeof(Type) },
            modifiers: null)!;
        var snapshot = constructor.Invoke(new object[]
        {
            new[] { new KeyValuePair<string, Type>("Value", typeof(int)) },
            typeof(object)
        });

        var properties = snapshot!
            .GetType()
            .GetField("_properties", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(snapshot);
        properties.ShouldBeOfType<Dictionary<string, TypeFactory.SchemaTypeIdentity>>();
    }

    [Fact]
    public void CreateInstance_NullValue_AllowsRoundTrip()
    {
        var instance = (DynamicBase)TypeVisit.CreateInstance(new Dictionary<string, object> { ["Optional"] = null! });

        instance.GetType().GetProperty("Optional")!.PropertyType.ShouldBe(typeof(object));
        instance.GetPropertyValue("Optional").ShouldBeNull();
        instance.SetPropertyValue("Optional", "later");
        instance.GetPropertyValue("Optional").ShouldBe("later");
        instance.SetPropertyValue("Optional", null);
        instance.GetPropertyValue("Optional").ShouldBeNull();
    }

    [Fact]
    public void CreateInstance_ValueDictionary_UsesSingleSnapshot()
    {
        var values = new SingleEnumerationDictionary { ["Value"] = 12 };

        var instance = (DynamicBase)TypeFactory.CreateObject(values);

        instance.GetPropertyValue("Value").ShouldBe(12);
        values.EnumerationCount.ShouldBeLessThanOrEqualTo(1);
    }

    [Theory]
    [InlineData("")]
    [InlineData("1Invalid")]
    [InlineData("Has Space")]
    public void CreateDynamicType_InvalidName_ThrowsArgumentException(string name)
    {
        var error = Should.Throw<ArgumentException>(() =>
            TypeVisit.CreateDynamicType(new Dictionary<string, Type> { [name] = typeof(string) }));
        error.ParamName.ShouldBe("properties");
    }

    [Fact]
    public void CreateDynamicType_TooManyProperties_ThrowsArgumentException()
    {
        var schema = Enumerable.Range(0, 256)
            .ToDictionary(i => $"P{i}", _ => typeof(int));

        var error = Should.Throw<ArgumentException>(() => TypeVisit.CreateDynamicType(schema));
        error.ParamName.ShouldBe("properties");
    }

    [Fact]
    public unsafe void CreateDynamicType_UnsupportedPropertyTypes_ThrowsArgumentException()
    {
        var invalidTypes = new[]
        {
            typeof(void),
            typeof(int).MakeByRefType(),
            typeof(int).MakePointerType(),
            typeof(Span<int>),
            typeof(List<>),
            typeof(PrivatePropertyType),
#if NET8_0_OR_GREATER
            typeof(delegate*<int, int>)
#endif
        };

        foreach (var invalidType in invalidTypes)
        {
            var error = Should.Throw<ArgumentException>(() => TypeVisit.CreateDynamicType(
                new Dictionary<string, Type> { ["Value"] = invalidType }));
            error.ParamName.ShouldBe("properties");
        }
    }

#if NET5_0_OR_GREATER && !NET8_0_OR_GREATER
    [Fact]
    public unsafe void CreateDynamicType_FunctionPointerReflectionBeforeNet8_IsRepresentedAsIntPtr()
    {
        var functionPointerType = typeof(delegate*<int, int>);
        functionPointerType.ShouldBe(typeof(IntPtr));

        var generatedType = TypeVisit.CreateDynamicType(
            new Dictionary<string, Type> { ["Value"] = functionPointerType });
        generatedType.GetProperty("Value")!.PropertyType.ShouldBe(typeof(IntPtr));
    }
#endif

    [Fact]
    public void CreateDynamicType_DuplicateEnumeratedPropertyNames_ThrowsArgumentException()
    {
        TypeVisit.CreateDynamicType(new Dictionary<string, Type> { ["Value"] = typeof(int) });

        var error = Should.Throw<ArgumentException>(() =>
            TypeVisit.CreateDynamicType(new DuplicateKeySchemaDictionary()));

        error.ParamName.ShouldBe("properties");
    }

    [Fact]
    public void CreateDynamicType_MaximumProperties_CreatesUsableType()
    {
        var schema = Enumerable.Range(0, 255).ToDictionary(i => $"P{i}", _ => typeof(int));

        var type = TypeVisit.CreateDynamicType(schema);
        var instance = (DynamicBase)Activator.CreateInstance(type)!;
        instance.SetPropertyValue("P254", 42);

        type.GetProperties().Length.ShouldBe(255);
        instance.GetPropertyValue("P254").ShouldBe(42);

        var orderedNames = schema.Keys.OrderBy(name => name, StringComparer.Ordinal).ToArray();
        var values = orderedNames.Select((_, index) => (object)index).ToArray();
        var constructed = (DynamicBase)Activator.CreateInstance(type, values)!;
        constructed.GetPropertyValue(orderedNames[254]).ShouldBe(254);
    }

    [Fact]
    public void CreateDynamicType_LiveSchemaCapacity_RejectsNewAndReusesExisting()
    {
        var cache = new TypeFactory.SchemaCache(2);
        var firstSchema = new Dictionary<string, Type> { ["First"] = typeof(int) };
        var secondSchema = new Dictionary<string, Type> { ["Second"] = typeof(int) };
        var thirdSchema = new Dictionary<string, Type> { ["Third"] = typeof(int) };
        var first = TypeFactory.CreateType(firstSchema, cache);
        var second = TypeFactory.CreateType(secondSchema, cache);

        Should.Throw<InvalidOperationException>(() => TypeFactory.CreateType(thirdSchema, cache));
        TypeFactory.CreateType(firstSchema, cache).ShouldBeSameAs(first);
        GC.KeepAlive(second);
    }

    [Fact]
    public void CreateDynamicType_ConcurrentDistinctSchemas_RespectsLiveCapacity()
    {
        const int requestCount = 32;
        const int capacity = 2;
        var cache = new TypeFactory.SchemaCache(capacity);
        var createdTypes = new Type[requestCount];
        var failures = new Exception[requestCount];

        Parallel.For(0, requestCount, index =>
        {
            try
            {
                createdTypes[index] = TypeFactory.CreateType(
                    new Dictionary<string, Type> { [$"Value{index}"] = typeof(int) }, cache);
            }
            catch (Exception exception)
            {
                failures[index] = exception;
            }
        });

        createdTypes.Count(type => type != null).ShouldBe(capacity);
        failures.OfType<InvalidOperationException>().Count().ShouldBe(requestCount - capacity);
        cache.BuildCount.ShouldBe(capacity);
    }

    [Fact]
    public void CreateDynamicType_UnreferencedSchema_AllowsAssemblyCollection()
    {
        var assembly = CreateUnreferencedAssembly();
        for (var attempt = 0; attempt < 20 && assembly.IsAlive; attempt++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        assembly.IsAlive.ShouldBeFalse();
    }

    [Fact]
    public void CreateDynamicType_CollectiblePropertyType_DoesNotPinPropertyAssembly()
    {
        var assembly = CreateUnreferencedPropertyTypeAssembly();
        for (var attempt = 0; attempt < 20 && assembly.IsAlive; attempt++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        assembly.IsAlive.ShouldBeFalse();
    }

    [Fact]
    public void CreateDynamicType_CrossContextTypeCollision_RejectsWithoutPinningLoadContext()
    {
        var context = CreateUnreferencedLoadContext();
        for (var attempt = 0; attempt < 20 && context.IsAlive; attempt++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        context.IsAlive.ShouldBeFalse();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference CreateUnreferencedAssembly()
    {
        var propertyName = $"Unique_{Guid.NewGuid():N}";
        var schema = new Dictionary<string, Type> { [propertyName] = typeof(int) };
        var type = TypeVisit.CreateDynamicType(schema);
        type.Assembly.IsCollectible.ShouldBeTrue();
        var instance = (DynamicBase)Activator.CreateInstance(type)!;
        instance.SetPropertyValue(propertyName, 42);
        instance.GetPropertyValue(propertyName).ShouldBe(42);
        return new WeakReference(type.Assembly);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference CreateUnreferencedPropertyTypeAssembly()
    {
        var assembly = AssemblyBuilder.DefineDynamicAssembly(
            new AssemblyName($"DynamicPropertyType_{Guid.NewGuid():N}"),
            AssemblyBuilderAccess.RunAndCollect);
        var propertyType = assembly.DefineDynamicModule("Main")
            .DefineType($"PropertyType_{Guid.NewGuid():N}", TypeAttributes.Public | TypeAttributes.Class)
            .CreateTypeInfo()!
            .AsType();
        propertyType.IsVisible.ShouldBeTrue();

        var schema = new Dictionary<string, Type> { ["Value"] = propertyType };
        var generatedType = TypeVisit.CreateDynamicType(schema);
        var instance = (DynamicBase)Activator.CreateInstance(generatedType)!;
        instance.SetPropertyValue("Value", null);
        instance.GetPropertyValue("Value").ShouldBeNull();
        return new WeakReference(assembly);
    }

    [Fact]
    public void CreateDynamicType_CollectedSchema_ReleasesCapacityForNewSchema()
    {
        var cache = new TypeFactory.SchemaCache(1);
        var oldAssembly = FillUnreferencedCache(cache);
        for (var attempt = 0; attempt < 20 && oldAssembly.IsAlive; attempt++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
        oldAssembly.IsAlive.ShouldBeFalse();
        var replacement = TypeFactory.CreateType(new Dictionary<string, Type> { ["Replacement"] = typeof(string) }, cache);
        replacement.GetProperty("Replacement").ShouldNotBeNull();
        cache.BuildCount.ShouldBe(2);
        GC.KeepAlive(cache);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference FillUnreferencedCache(TypeFactory.SchemaCache cache)
    {
        var type = TypeFactory.CreateType(new Dictionary<string, Type> { ["Old"] = typeof(int) }, cache);
        return new WeakReference(type.Assembly);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference CreateUnreferencedLoadContext()
    {
        var context = new AssemblyLoadContext($"DynamicProperty_{Guid.NewGuid():N}", isCollectible: true);
        var assembly = context.LoadFromAssemblyPath(typeof(TypeVisit).Assembly.Location);
        var propertyType = assembly.GetType(typeof(TypeVisit).FullName!, throwOnError: true)!;
        var schema = new Dictionary<string, Type> { ["Value"] = propertyType };
        Should.Throw<ArgumentException>(() => TypeVisit.CreateDynamicType(schema));
        var weak = new WeakReference(context);
        context.Unload();
        return weak;
    }

    private sealed class SingleEnumerationDictionary : Dictionary<string, object>, IDictionary<string, object>
    {
        internal int EnumerationCount { get; private set; }

        IEnumerator<KeyValuePair<string, object>> IEnumerable<KeyValuePair<string, object>>.GetEnumerator()
        {
            EnumerationCount++;
            if (EnumerationCount > 1)
                throw new InvalidOperationException("Dictionary was enumerated more than once.");
            return base.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() =>
            ((IEnumerable<KeyValuePair<string, object>>)this).GetEnumerator();
    }

    private sealed class DuplicateKeySchemaDictionary : Dictionary<string, Type>, IDictionary<string, Type>
    {
        internal DuplicateKeySchemaDictionary()
        {
            Add("Value", typeof(int));
        }

        IEnumerator<KeyValuePair<string, Type>> IEnumerable<KeyValuePair<string, Type>>.GetEnumerator()
        {
            yield return new KeyValuePair<string, Type>("Value", typeof(int));
            yield return new KeyValuePair<string, Type>("Value", typeof(int));
        }
    }

    private sealed class PrivatePropertyType
    {
    }

    [Fact]
    public void CreateDynamicType_NullPropertyType_InfersObjectAndAllowsNull()
    {
        var schema = new Dictionary<string, Type> { ["Value"] = null! };
        var type = TypeVisit.CreateDynamicType(schema);
        TypeVisit.CreateDynamicType(schema).ShouldBeSameAs(type);
        type.GetProperty("Value")!.PropertyType.ShouldBe(typeof(object));

        var instance = (DynamicBase)Activator.CreateInstance(type)!;
        instance.SetPropertyValue("Value", null);
        instance.GetPropertyValue("Value").ShouldBeNull();
        instance.SetPropertyValue("Value", "text");
        instance.GetPropertyValue("Value").ShouldBe("text");
    }

    [Fact]
    public void CreateDynamicType_EmptySchema_HasOnlyDefaultConstructor()
    {
        var type = TypeVisit.CreateDynamicType(new Dictionary<string, Type>());
        var instance = (DynamicBase)Activator.CreateInstance(type)!;

        type.GetConstructors().Length.ShouldBe(1);
        Should.Throw<MissingMemberException>(() => instance.GetPropertyValue("Missing"));
        Should.Throw<MissingMemberException>(() => instance.SetPropertyValue("Missing", 1));
    }
}

#endif
