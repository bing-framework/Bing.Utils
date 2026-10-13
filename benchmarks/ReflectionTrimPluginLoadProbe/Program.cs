using Bing.Reflection;
using Bing.Conversions;
using BingTypeConverter = Bing.Conversions.TypeConverter;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.Loader;

namespace ReflectionTrimPluginLoadProbe;

public static class Program
{
    private const string AssemblyName = "ReflectionTrimProbe.UnrootedModels";
    private const string TypeName = "ReflectionTrimProbe.UnrootedModels.UnrootedBoundaryModel";
    private const string BaseTypeName = "ReflectionTrimProbe.UnrootedModels.UnrootedBoundaryBase";
    private const string InheritedTypeName = "ReflectionTrimProbe.UnrootedModels.UnrootedInheritanceModel";
    private const string GenericTypeName = "ReflectionTrimProbe.UnrootedModels.UnrootedGenericModel";
    private const string GenericBaseTypeName = "ReflectionTrimProbe.UnrootedModels.UnrootedGenericBase`1";
    private const string ContractName = "ReflectionTrimProbe.UnrootedModels.IUnrootedBoundaryContract";
    private const string BaseContractName = "ReflectionTrimProbe.UnrootedModels.IUnrootedBoundaryBaseContract";

    public static int Main(string[] args)
    {
        if ((args.Length != 1 && args.Length != 2 && args.Length != 3)
            || string.IsNullOrWhiteSpace(args[0])
            || (args.Length == 3 && !string.Equals(args[1], "--expect", StringComparison.Ordinal)))
        {
            Console.Error.WriteLine("Usage: ReflectionTrimPluginLoadProbe <published-directory> [<metadata-preserved|assembly-trimmed|member-trimmed|trimmed>] or [--expect <...>]");
            return 2;
        }

        var expectedBoundary = args.Length switch
        {
            1 => null,
            2 => args[1],
            _ => args[2]
        };
        if (expectedBoundary is not null
            && expectedBoundary is not ("metadata-preserved" or "assembly-trimmed" or "member-trimmed" or "trimmed"))
        {
            Console.Error.WriteLine($"Unsupported expected plugin boundary: {expectedBoundary}");
            return 2;
        }

        var targetDirectory = Path.GetFullPath(args[0]);
        if (!Directory.Exists(targetDirectory))
        {
            Console.Error.WriteLine($"Plugin target directory does not exist: {targetDirectory}");
            return 2;
        }

        var assemblyPath = Path.Combine(targetDirectory, AssemblyName + ".dll");
        var pathResult = LoadFromPath(assemblyPath);
        var resolverResult = LoadWithResolver(targetDirectory, assemblyPath);
        var bytesResult = LoadFromBytes(assemblyPath);

        Console.WriteLine($"reflection-plugin-load-boundary:{pathResult}");
        Console.WriteLine($"reflection-plugin-load-boundary:resolver:{resolverResult}");
        Console.WriteLine($"reflection-plugin-load-boundary:bytes:{bytesResult}");

        return IsUnexpectedBoundaryResult(pathResult, expectedBoundary)
            || IsUnexpectedBoundaryResult(resolverResult, expectedBoundary)
            || IsUnexpectedBoundaryResult(bytesResult, expectedBoundary)
            ? 1
            : 0;
    }

    private static bool IsUnexpectedBoundaryResult(string result, string? expectedBoundary) =>
        expectedBoundary switch
        {
            null => result is not ("metadata-preserved" or "assembly-trimmed"),
            "metadata-preserved" => result != "metadata-preserved",
            "assembly-trimmed" => result != "assembly-trimmed",
            "member-trimmed" => result != "member-trimmed",
            "trimmed" => result is not ("assembly-trimmed" or "member-trimmed"),
            _ => true
        };

    private static string LoadFromPath(string assemblyPath)
    {
        if (!File.Exists(assemblyPath))
            return "assembly-trimmed";

        AssemblyLoadContext? loadContext = null;
        try
        {
            loadContext = new AssemblyLoadContext("ReflectionTrimProbe.UnrootedPlugin.Path", isCollectible: true);
            return ReadBoundary(loadContext.LoadFromAssemblyPath(assemblyPath), assemblyPath);
        }
        catch (FileLoadException)
        {
            return "assembly-load-failed";
        }
        catch (FileNotFoundException)
        {
            return "assembly-load-failed";
        }
        catch (BadImageFormatException)
        {
            return "assembly-load-failed";
        }
        finally
        {
            loadContext?.Unload();
        }
    }

    private static string LoadWithResolver(string targetDirectory, string assemblyPath)
    {
        if (!File.Exists(assemblyPath))
            return "assembly-trimmed";

        AssemblyLoadContext? loadContext = null;
        try
        {
            var hostPath = FindHostPath(targetDirectory);
            if (hostPath is null)
                return "assembly-load-failed";
            loadContext = new ResolverLoadContext(hostPath, assemblyPath);
            var assembly = loadContext.LoadFromAssemblyName(new AssemblyName(AssemblyName));
            return ReadBoundary(assembly, assemblyPath);
        }
        catch (FileLoadException)
        {
            return "assembly-load-failed";
        }
        catch (FileNotFoundException)
        {
            return "assembly-load-failed";
        }
        catch (BadImageFormatException)
        {
            return "assembly-load-failed";
        }
        catch (ArgumentException)
        {
            return "assembly-load-failed";
        }
        finally
        {
            loadContext?.Unload();
        }
    }

    // NativeAOT 目录通常只有原生 EXE，managed 发布目录则保留 host DLL。
    private static string? FindHostPath(string targetDirectory) =>
        new[] { "ReflectionTrimProbe.dll", "ReflectionTrimProbe.exe" }
            .Select(name => Path.Combine(targetDirectory, name))
            .FirstOrDefault(File.Exists);

    private static string LoadFromBytes(string assemblyPath)
    {
        if (!File.Exists(assemblyPath))
            return "assembly-trimmed";

        try
        {
            return ReadBoundary(Assembly.Load(File.ReadAllBytes(assemblyPath)), assemblyPath);
        }
        catch (FileLoadException)
        {
            return "assembly-load-failed";
        }
        catch (FileNotFoundException)
        {
            return "assembly-load-failed";
        }
        catch (BadImageFormatException)
        {
            return "assembly-load-failed";
        }
    }

    private static string ReadBoundary(Assembly assembly, string sourceAssemblyPath)
    {
        try
        {
            var type = assembly.GetType(TypeName, throwOnError: false);
            if (type is null)
                return "member-trimmed";
            var baseType = assembly.GetType(BaseTypeName, throwOnError: false);
            var inheritedType = assembly.GetType(InheritedTypeName, throwOnError: false);
            var genericType = assembly.GetType(GenericTypeName, throwOnError: false);
            var genericBaseType = assembly.GetType(GenericBaseTypeName, throwOnError: false);
            var contract = assembly.GetType(ContractName, throwOnError: false);
            if (contract is null)
                return "member-trimmed";
            var baseContract = assembly.GetType(BaseContractName, throwOnError: false);
            if (baseType is null || inheritedType is null || genericType is null || genericBaseType is null || baseContract is null)
                return "member-trimmed";

            var inheritedDefaultDescription = TypeReflections.GetDescription(inheritedType);
            var inheritedDescription = TypeReflections.GetDescription(inheritedType, ReflectionOptions.Inherit);
            var inheritedAttribute = TypeReflections.GetAttribute<DescriptionAttribute>(
                inheritedType, ReflectionOptions.Inherit);
            var inheritedAttributes = TypeReflections.GetAttributes<DescriptionAttribute>(
                inheritedType, ReflectionOptions.Inherit).ToArray();
            var inheritedDefined = TypeReflections.IsAttributeDefined<DescriptionAttribute>(
                inheritedType, ReflectionOptions.Inherit);
            var runtimeDescriptionAttributeType = typeof(DescriptionAttribute);
            var inheritedRuntimeAttribute = TypeReflections.GetAttribute(
                inheritedType, runtimeDescriptionAttributeType, ReflectionOptions.Inherit);
            var inheritedRuntimeAttributeIgnoringAmbiguity = TypeReflections.GetAttribute(
                inheritedType, runtimeDescriptionAttributeType, ReflectionOptions.Inherit,
                ReflectionAmbiguousOptions.IgnoreAmbiguous);
            var inheritedRuntimeAttributes = TypeReflections.GetAttributes(
                inheritedType, runtimeDescriptionAttributeType, ReflectionOptions.Inherit).ToArray();
            var inheritedRuntimeRequiredAttribute = TypeReflections.GetAttributeRequired(
                inheritedType, runtimeDescriptionAttributeType, ReflectionOptions.Inherit);
            var inheritedRuntimeRequiredAttributes = TypeReflections.GetAttributesRequired(
                inheritedType, runtimeDescriptionAttributeType, ReflectionOptions.Inherit).ToArray();
            var inheritedRuntimeDefined = TypeReflections.IsAttributeDefined(
                inheritedType, runtimeDescriptionAttributeType, ReflectionOptions.Inherit);

            var instance = Activator.CreateInstance(type);
            if (instance is null)
                return "member-trimmed";

            var assemblyFileVersion = AssemblyVisit.GetFileVersion(assembly);
            var assemblyProductVersion = AssemblyVisit.GetProductVersion(assembly);
            var scannedAssemblies = ScanAssemblyDirectory(sourceAssemblyPath, assembly);
            var nullableUnderlyingType = Reflections.GetUnderlyingType(typeof(int?));
            var nullableUnderlyingTypeInfo = Reflections.GetUnderlyingType(typeof(int?).GetTypeInfo());
            var normalizedNullableArray = TypeConv.GetNonNullableType(typeof(int?[]));
            var normalizedNullableList = TypeConv.GetNonNullableType(typeof(List<int?>));
            var baseTypes = Reflections.GetBaseClasses(type, includeObject: false);
            var baseTypesBeforeObject = Reflections.GetBaseClasses(type, typeof(object), includeObject: false);
            var topBaseType = Reflections.GetTopBaseType(type);
            var typeBasedOn = TypeReflections.IsTypeBasedOn(type, baseType);
            var typeDerived = TypeReflections.IsTypeDerivedFrom(type, baseType);
            var isAssignableToBase = Reflections.IsAssignableTo(type, baseType);
            var isAssignableToContract = Reflections.IsAssignableTo(type, contract);
            var isBaseOn = Reflections.IsBaseOn(type, baseType);
            var isDerived = Reflections.IsDeriveClassFrom(type, baseType);
            var isGenericAssignable = Reflections.IsGenericAssignableFrom(
                typeof(IEnumerable<>), typeof(List<int>));
            var listElementType = Reflections.GetElementType(typeof(List<int>));
            var fullNameWithAssemblyName = Reflections.GetFullNameWithAssemblyName(type);
            var genericImplementation = TypeReflections.IsImplementationOfGenericType(
                typeof(List<int>), typeof(IEnumerable<>), out var genericImplementationType, out var genericArguments);
            var rawGenericType = TypeReflections.GetRawTypeFromGenericType(typeof(List<int>), typeof(IEnumerable<>));
            var rawGenericArguments = TypeReflections.GetRawTypeArgsFromGenericType(
                typeof(List<int>), typeof(IEnumerable<>));
            var rawUnrootedGenericType = Reflections.GetRawTypeFromGenericClass(genericType, genericBaseType);
            var closedUnrootedGenericBase = genericBaseType.MakeGenericType(typeof(int));
            var rawClosedUnrootedGenericType = Reflections.GetRawTypeFromGenericClass(
                genericType, closedUnrootedGenericBase);
            var enumUnderlyingType = TypeReflections.GetEnumUnderlyingType(typeof(DayOfWeek));
            if (!TypeVisit.HasParameterlessConstructor(type)
                || TypeVisit.GetParameterlessConstructor(type).GetParameters().Length != 0
                || !type.HasParameterlessConstructor()
                || type.GetParameterlessConstructor().GetParameters().Length != 0)
                return "member-trimmed";
            if (TypeVisit.HasParameterlessConstructor(baseType)
                || TypeVisit.HasParameterlessConstructor(contract)
                || baseType.HasParameterlessConstructor()
                || contract.HasParameterlessConstructor())
                return "member-trimmed";
            var stringConstructor = TypeVisit.GetMatchingConstructor(type, new[] { typeof(string) });
            var stringConstructorParameter = stringConstructor.GetParameters().Single();
            var stringInstance = stringConstructor.Invoke(new object[] { "unrooted-boundary" });
            var typeVisitStringInstance = TypeVisit.CreateInstance(type, "typevisit-boundary");
            var typeVisitBatchInstances = TypeVisit.CreateInstances(new[] { type }, "typevisit-batch-boundary");
            var fields = TypeVisit.GetFields(type);
            var publicField = fields.FirstOrDefault(field => field.Name == "PublicField");
            var boolField = fields.FirstOrDefault(field => field.Name == "BoolField");
            var enumField = fields.FirstOrDefault(field => field.Name == "EnumField");
            var nullableEnumField = fields.FirstOrDefault(field => field.Name == "NullableEnumField");
            var numberField = fields.FirstOrDefault(field => field.Name == "NumberField");
            var tupleField = fields.FirstOrDefault(field => field.Name == "TupleField");
            var collectionField = fields.FirstOrDefault(field => field.Name == "CollectionField");
            var structField = fields.FirstOrDefault(field => field.Name == "StructField");
            var getterProperties = TypeVisit.GetProperties(type, PropertyAccessOptions.Getters).ToArray();
            var setterProperties = TypeVisit.GetProperties(type, PropertyAccessOptions.Setters).ToArray();
            var properties = TypeVisit.GetProperties(type, PropertyAccessOptions.Both).ToArray();
            var nullPropertyTypeRejected = false;
            try
            {
                TypeVisit.GetProperties(null!, PropertyAccessOptions.Getters).ToArray();
            }
            catch (ArgumentNullException)
            {
                nullPropertyTypeRejected = true;
            }
            var invalidPropertyAccessOptionRejected = false;
            try
            {
                TypeVisit.GetProperties(type, (PropertyAccessOptions)999).ToArray();
            }
            catch (InvalidOperationException)
            {
                invalidPropertyAccessOptionRejected = true;
            }
            var publicProperty = properties.FirstOrDefault(property => property.Name == "PublicProperty");
            var initOnlyProperty = type.GetProperty("InitOnlyProperty");
            var inheritedProperty = properties.FirstOrDefault(property => property.Name == "InheritedProperty");
            var abstractProperty = baseType.GetProperty("AbstractBoundaryProperty");
            var implementedAbstractProperty = type.GetProperty("AbstractBoundaryProperty");
            var abstractPropertyIsAbstract = abstractProperty is not null && abstractProperty.IsAbstract();
            var abstractPropertyIsVisibleAndVirtual = abstractProperty is not null
                && TypeVisit.IsVisibleAndVirtual(abstractProperty);
            var implementedAbstractPropertyIsAbstract = implementedAbstractProperty is not null
                && implementedAbstractProperty.IsAbstract();
            var inheritedPropertyIsVirtual = inheritedProperty is not null && inheritedProperty.IsVirtual();
            var inheritedPropertyIsAbstract = inheritedProperty is not null && inheritedProperty.IsAbstract();
            var inheritedPropertyIsVisibleAndVirtual = inheritedProperty is not null
                && TypeVisit.IsVisibleAndVirtual(inheritedProperty);
            var getterOnlyProperty = getterProperties.FirstOrDefault(property => property.Name == "GetterOnlyProperty");
            var setterOnlyProperty = setterProperties.FirstOrDefault(property => property.Name == "SetterOnlyProperty");
            var indexer = type.GetProperties().FirstOrDefault(property => property.GetIndexParameters().Length != 0);
            var staticProperty = type.GetProperties().FirstOrDefault(property => property.Name == "StaticProperty");
            var cachedProperties = TypeReflections.GetPublicInstanceProperties(type);
            var readableProperties = TypeReflections.GetPublicReadableInstanceProperties(type);
            var writableProperties = TypeReflections.GetPublicWritableInstanceProperties(type);
            var reflectedProperties = Reflections.GetProperties(type);
            var reflectedInstanceProperties = Reflections.GetProperties(type, BindingFlags.Public | BindingFlags.Instance);
            var genericPublicProperties = InvokeGenericArray<PropertyInfo>(nameof(Reflections.GetProperties), type,
                BindingFlags.Public | BindingFlags.Instance);
            var genericNonPublicProperties = InvokeGenericArray<PropertyInfo>(nameof(Reflections.GetProperties), type,
                BindingFlags.NonPublic | BindingFlags.Instance);
            var reflectedFields = Reflections.GetFields(instance);
            var reflectedInstanceFields = Reflections.GetFields(instance, BindingFlags.Public | BindingFlags.Instance);
            var publicPropertyItems = Reflections.GetPublicProperties(instance);
            var reflectedMethods = InvokeGenericArray<MethodInfo>(nameof(Reflections.GetMethods), type);
            var reflectedInstanceMethods = InvokeGenericArray<MethodInfo>(nameof(Reflections.GetMethods), type,
                BindingFlags.Public | BindingFlags.Instance);
            var interfaceTypes = Reflections.GetInterfaceTypes(type);
            var directInterfaceTypes = Reflections.GetDirectInterfaceTypes(type);
            var implementationTypes = Reflections.FindImplementTypes(contract, assembly);
            var publicConstants = Reflections.GetPublicConstantsRecursively(type);
            var getInstancesByInterface = typeof(Reflections).GetMethods()
                .Single(methodInfo => methodInfo.Name == nameof(Reflections.GetInstancesByInterface)
                    && methodInfo.IsGenericMethodDefinition
                    && methodInfo.GetParameters().Length == 1);
            var instances = (System.Collections.IList?)getInstancesByInterface
                .MakeGenericMethod(contract)
                .Invoke(null, new object[] { assembly });
            var method = TypeVisit.FindMethod(type, "Execute", typeof(int));
            var overriddenMethod = TypeVisit.FindMethod(type, "ComputeBaseValue", typeof(int));
            var contractMethod = contract.GetMethod("Execute");
            if (contractMethod is null)
                return "member-trimmed";
            var signatureMethod = TypeVisit.GetMethodBySignature(type, contractMethod);
            var baseMethod = TypeVisit.GetBaseMethod(method);
            var overriddenBaseMethod = TypeVisit.GetBaseMethod(overriddenMethod);
            var overriddenMethodIsVisible = overriddenMethod is not null && TypeVisit.IsVisible(overriddenMethod);
            var overriddenMethodIsOverridden = overriddenMethod?.IsOverridden() == true;
            var overriddenBaseMethodIsVisibleAndVirtual = overriddenBaseMethod is not null
                && TypeVisit.IsVisibleAndVirtual(overriddenBaseMethod);
            var overriddenBaseMethodIsOverridden = overriddenBaseMethod?.IsOverridden() == true;
            var genericMethodDefinition = type.GetMethods()
                .Single(methodInfo => methodInfo.Name == "Echo" && methodInfo.IsGenericMethodDefinition);
            var taskMethod = type.GetMethod("GetTask", BindingFlags.Public | BindingFlags.Instance);
            var taskResultMethod = type.GetMethod("GetTaskResult", BindingFlags.Public | BindingFlags.Instance);
            var valueTaskMethod = type.GetMethod("GetValueTask", BindingFlags.Public | BindingFlags.Instance);
            var valueTaskResultMethod = type.GetMethod("GetValueTaskResult", BindingFlags.Public | BindingFlags.Instance);
            if (taskMethod is null || taskResultMethod is null || valueTaskMethod is null || valueTaskResultMethod is null)
                return "member-trimmed";
            var taskInfo = taskMethod.ReturnType.GetTypeInfo();
            var taskResultInfo = taskResultMethod.ReturnType.GetTypeInfo();
            var voidTaskResultType = Type.GetType("System.Threading.Tasks.VoidTaskResult", throwOnError: true)!;
            var voidTaskResultInfo = typeof(Task<>).MakeGenericType(voidTaskResultType).GetTypeInfo();
            var valueTaskInfo = valueTaskMethod.ReturnType.GetTypeInfo();
            var valueTaskResultInfo = valueTaskResultMethod.ReturnType.GetTypeInfo();
            var nonPublicProperty = Reflections.GetProperty(
                type, "ReflectedValue", BindingFlags.Instance | BindingFlags.NonPublic);
            var genericPublicPropertyWithFlags = InvokeGenericMemberWithBindingFlags<PropertyInfo>(
                nameof(Reflections.GetProperty),
                type,
                "PublicProperty",
                BindingFlags.Public | BindingFlags.Instance);
            var genericPrivatePropertyWithFlags = InvokeGenericMemberWithBindingFlags<PropertyInfo>(
                nameof(Reflections.GetProperty),
                type,
                "ReflectedValue",
                BindingFlags.NonPublic | BindingFlags.Instance);
            var genericPrivatePropertyIgnoreCase = InvokeGenericMemberWithBindingFlags<PropertyInfo>(
                nameof(Reflections.GetProperty),
                type,
                "reflectedvalue",
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase);
            var genericPrivatePropertyWithoutNonPublicRejected = false;
            try
            {
                InvokeGenericMemberWithBindingFlags<PropertyInfo>(
                    nameof(Reflections.GetProperty),
                    type,
                    "ReflectedValue",
                    BindingFlags.Public | BindingFlags.Instance);
            }
            catch (TargetInvocationException exception) when (exception.InnerException is MissingMemberException)
            {
                genericPrivatePropertyWithoutNonPublicRejected = true;
            }
            var genericMissingPropertyNameRejected = false;
            try
            {
                InvokeGenericMemberWithBindingFlags<PropertyInfo>(
                    nameof(Reflections.GetProperty),
                    type,
                    "MissingProperty",
                    BindingFlags.Public | BindingFlags.Instance);
            }
            catch (TargetInvocationException exception) when (exception.InnerException is MissingMemberException)
            {
                genericMissingPropertyNameRejected = true;
            }
            var genericEmptyPropertyNameRejected = false;
            try
            {
                InvokeGenericMemberWithBindingFlags<PropertyInfo>(
                    nameof(Reflections.GetProperty),
                    type,
                    string.Empty,
                    BindingFlags.Public | BindingFlags.Instance);
            }
            catch (TargetInvocationException exception) when (exception.InnerException is ArgumentException)
            {
                genericEmptyPropertyNameRejected = true;
            }
            var genericPublicMethodWithFlags = InvokeGenericMemberWithBindingFlags<MethodInfo>(
                nameof(Reflections.GetMethod),
                type,
                "Execute",
                BindingFlags.Public | BindingFlags.Instance);
            var genericPrivateMethodWithFlags = InvokeGenericMemberWithBindingFlags<MethodInfo>(
                nameof(Reflections.GetMethod),
                type,
                "get_ReflectedValue",
                BindingFlags.NonPublic | BindingFlags.Instance);
            var genericPrivateMethodIgnoreCase = InvokeGenericMemberWithBindingFlags<MethodInfo>(
                nameof(Reflections.GetMethod),
                type,
                "GET_REFLECTEDVALUE",
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase);
            var genericPrivateMethodWithoutNonPublicRejected = false;
            try
            {
                InvokeGenericMemberWithBindingFlags<MethodInfo>(
                    nameof(Reflections.GetMethod),
                    type,
                    "get_ReflectedValue",
                    BindingFlags.Public | BindingFlags.Instance);
            }
            catch (TargetInvocationException exception) when (exception.InnerException is MissingMethodException)
            {
                genericPrivateMethodWithoutNonPublicRejected = true;
            }
            var genericMissingMethodNameRejected = false;
            try
            {
                InvokeGenericMemberWithBindingFlags<MethodInfo>(
                    nameof(Reflections.GetMethod),
                    type,
                    "MissingMethod",
                    BindingFlags.Public | BindingFlags.Instance);
            }
            catch (TargetInvocationException exception) when (exception.InnerException is MissingMethodException)
            {
                genericMissingMethodNameRejected = true;
            }
            var genericEmptyMethodNameRejected = false;
            try
            {
                InvokeGenericMemberWithBindingFlags<MethodInfo>(
                    nameof(Reflections.GetMethod),
                    type,
                    string.Empty,
                    BindingFlags.Public | BindingFlags.Instance);
            }
            catch (TargetInvocationException exception) when (exception.InnerException is ArgumentException)
            {
                genericEmptyMethodNameRejected = true;
            }
            var bindingMethod = TypeVisit.FindMethod(
                type, "Execute", BindingFlags.Public | BindingFlags.Instance, typeof(int));
            var genericSignatureMethod = TypeVisit.FindMethod(
                type, "Echo", 1,
                new MethodParameterSignature(genericMethodDefinition.GetGenericArguments()[0]));
            var genericReturnMethod = TypeVisit.FindMethodByReturnType(
                type,
                "Echo",
                typeof(int),
                BindingFlags.Public | BindingFlags.Instance,
                1,
                new MethodParameterSignature(typeof(int)));
            var genericReturnMismatchRejected = false;
            try
            {
                TypeVisit.FindMethodByReturnType(
                    type,
                    "Echo",
                    typeof(string),
                    BindingFlags.Public | BindingFlags.Instance,
                    1,
                    new MethodParameterSignature(typeof(int)));
            }
            catch (MissingMethodException)
            {
                genericReturnMismatchRejected = true;
            }
            var genericMethod = TypeVisit.FindMethod(type, genericMethodDefinition);
            var genericResult = genericMethod.MakeGenericMethod(typeof(int))
                .Invoke(instance, new object[] { 127 });
            var reflectedField = InvokeGenericMember<FieldInfo>(nameof(Reflections.GetField), type, "PublicField");
            var genericPublicFieldWithFlags = InvokeGenericMemberWithBindingFlags<FieldInfo>(
                nameof(Reflections.GetField),
                type,
                "PublicField",
                BindingFlags.Public | BindingFlags.Instance);
            var genericPrivateFieldWithFlags = InvokeGenericMemberWithBindingFlags<FieldInfo>(
                nameof(Reflections.GetField),
                type,
                "_reflectedValue",
                BindingFlags.NonPublic | BindingFlags.Instance);
            var genericPrivateFieldIgnoreCase = InvokeGenericMemberWithBindingFlags<FieldInfo>(
                nameof(Reflections.GetField),
                type,
                "_REFLECTEDVALUE",
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase);
            var genericPrivateFieldWithoutNonPublicRejected = false;
            try
            {
                InvokeGenericMemberWithBindingFlags<FieldInfo>(
                    nameof(Reflections.GetField),
                    type,
                    "_reflectedValue",
                    BindingFlags.Public | BindingFlags.Instance);
            }
            catch (TargetInvocationException exception) when (exception.InnerException is MissingFieldException)
            {
                genericPrivateFieldWithoutNonPublicRejected = true;
            }
            var genericMissingFieldNameRejected = false;
            try
            {
                InvokeGenericMemberWithBindingFlags<FieldInfo>(
                    nameof(Reflections.GetField),
                    type,
                    "MissingField",
                    BindingFlags.Public | BindingFlags.Instance);
            }
            catch (TargetInvocationException exception) when (exception.InnerException is MissingFieldException)
            {
                genericMissingFieldNameRejected = true;
            }
            var genericEmptyFieldNameRejected = false;
            try
            {
                InvokeGenericMemberWithBindingFlags<FieldInfo>(
                    nameof(Reflections.GetField),
                    type,
                    string.Empty,
                    BindingFlags.Public | BindingFlags.Instance);
            }
            catch (TargetInvocationException exception) when (exception.InnerException is ArgumentException)
            {
                genericEmptyFieldNameRejected = true;
            }
            var reflectedProperty = InvokeGenericMember<PropertyInfo>(nameof(Reflections.GetProperty), type, "PublicProperty");
            var reflectedMethod = InvokeGenericMember<MethodInfo>(nameof(Reflections.GetMethod), type, "Execute");
            var reflectedPropertyOrField = InvokeGenericMember<MemberInfo>(nameof(Reflections.GetPropertyOrField), type, "PublicProperty");
            var typeFullName = TypeVisit.GetFullName(type);
            var methodFullName = TypeVisit.GetFullName(method);
            var methodQualifiedName = TypeVisit.GetFullyQualifiedName(method);
            var typeDescription = Reflections.GetDescription(type);
            var typeReflectionsDescription = TypeReflections.GetDescription(type);
            var fieldDescription = publicField is null ? null : Reflections.GetDescription(publicField);
            var fieldReflectionsDescription = publicField is null ? null : TypeReflections.GetDescription(publicField);
            var propertyDisplayName = publicProperty is null ? null : TypeReflections.GetDisplayName(publicProperty);
            var propertyDisplayNameOr = publicProperty is null ? null : TypeReflections.GetDisplayNameOr(publicProperty, "property-display-fallback");
            var propertyDescriptionOrDisplayName = publicProperty is null ? null : TypeReflections.GetDescriptionOrDisplayName(publicProperty);
            var reflectedPropertyDisplayNameOrDescription = publicProperty is null ? null : Reflections.GetDisplayNameOrDescription(publicProperty);
            var propertyDescription = publicProperty is null ? null : Reflections.GetDescription(publicProperty);
            var propertyReflectionsDescription = publicProperty is null ? null : TypeReflections.GetDescription(publicProperty);
            var methodDescription = Reflections.GetDescription(method);
            var executeParameter = method.GetParameters().Single();
            var parameterDescription = TypeReflections.GetDescription(executeParameter);
            var parameterDisplayName = TypeReflections.GetDisplayName(executeParameter);
            var parameterDescriptionOr = TypeReflections.GetDescriptionOr(executeParameter, "parameter-description-fallback");
            var parameterDisplayNameOr = TypeReflections.GetDisplayNameOr(executeParameter, "parameter-display-fallback");
            var parameterDescriptionOrDisplayName = TypeReflections.GetDescriptionOrDisplayName(executeParameter);
            var typeDescriptionAttributes = TypeReflections.GetAttributes<DescriptionAttribute>(type).ToArray();
            var reflectedTypeDescriptionAttributes = Reflections.GetAttributes<DescriptionAttribute>(type).ToArray();
            var fieldDescriptionAttributes = publicField is null
                ? Array.Empty<DescriptionAttribute>()
                : TypeReflections.GetAttributes<DescriptionAttribute>(publicField).ToArray();
            var reflectedFieldDescriptionAttributes = publicField is null
                ? Array.Empty<DescriptionAttribute>()
                : Reflections.GetAttributes<DescriptionAttribute>(publicField).ToArray();
            var propertyDescriptionAttributes = publicProperty is null
                ? Array.Empty<DescriptionAttribute>()
                : TypeReflections.GetAttributes<DescriptionAttribute>(publicProperty).ToArray();
            var reflectedPropertyDescriptionAttributes = publicProperty is null
                ? Array.Empty<DescriptionAttribute>()
                : Reflections.GetAttributes<DescriptionAttribute>(publicProperty).ToArray();
            var methodDescriptionAttributes = TypeReflections.GetAttributes<DescriptionAttribute>(method).ToArray();
            var reflectedMethodDescriptionAttributes = Reflections.GetAttributes<DescriptionAttribute>(method).ToArray();
            var typeDescriptionAttribute = TypeReflections.GetAttribute<DescriptionAttribute>(type);
            var reflectedTypeDescriptionAttribute = Reflections.GetAttribute<DescriptionAttribute>(type);
            var fieldDescriptionAttribute = publicField is null
                ? null
                : TypeReflections.GetAttribute<DescriptionAttribute>(publicField);
            var reflectedFieldDescriptionAttribute = publicField is null
                ? null
                : Reflections.GetAttribute<DescriptionAttribute>(publicField);
            var propertyDescriptionAttribute = publicProperty is null
                ? null
                : TypeReflections.GetAttribute<DescriptionAttribute>(publicProperty);
            var reflectedPropertyDescriptionAttribute = publicProperty is null
                ? null
                : Reflections.GetAttribute<DescriptionAttribute>(publicProperty);
            var methodDescriptionAttribute = TypeReflections.GetAttribute<DescriptionAttribute>(method);
            var reflectedMethodDescriptionAttribute = Reflections.GetAttribute<DescriptionAttribute>(method);
            var parameterDescriptionAttribute = TypeReflections.GetAttribute<DescriptionAttribute>(executeParameter);
            var constructorRuntimeAttribute = TypeReflections.GetAttribute(
                stringConstructor, runtimeDescriptionAttributeType, ReflectionOptions.Default);
            var constructorRuntimeAttributeIgnoringAmbiguity = TypeReflections.GetAttribute(
                stringConstructor, runtimeDescriptionAttributeType, ReflectionOptions.Inherit,
                ReflectionAmbiguousOptions.IgnoreAmbiguous);
            var constructorRuntimeAttributes = TypeReflections.GetAttributes(
                stringConstructor, runtimeDescriptionAttributeType, ReflectionOptions.Default).ToArray();
            var constructorRuntimeRequiredAttribute = TypeReflections.GetAttributeRequired(
                stringConstructor, runtimeDescriptionAttributeType, ReflectionOptions.Default);
            var constructorRuntimeRequiredAttributes = TypeReflections.GetAttributesRequired(
                stringConstructor, runtimeDescriptionAttributeType, ReflectionOptions.Default).ToArray();
            var constructorRuntimeDefined = TypeReflections.IsAttributeDefined(
                stringConstructor, runtimeDescriptionAttributeType, ReflectionOptions.Default);
            var constructorParameterRuntimeAttribute = TypeReflections.GetAttribute(
                stringConstructorParameter, runtimeDescriptionAttributeType);
            var constructorParameterRuntimeAttributes = TypeReflections.GetAttributes(
                stringConstructorParameter, runtimeDescriptionAttributeType).ToArray();
            var constructorParameterRuntimeRequiredAttribute = TypeReflections.GetAttributeRequired(
                stringConstructorParameter, runtimeDescriptionAttributeType);
            var constructorParameterRuntimeRequiredAttributes = TypeReflections.GetAttributesRequired(
                stringConstructorParameter, runtimeDescriptionAttributeType).ToArray();
            var constructorParameterRuntimeDefined = TypeReflections.IsAttributeDefined(
                stringConstructorParameter, runtimeDescriptionAttributeType);
            var reflectedConstructorRuntimeAttribute = Reflections.GetAttribute(
                (MemberInfo)stringConstructor, runtimeDescriptionAttributeType);
            var reflectedConstructorRuntimeAttributes = Reflections.GetAttributes(
                (MemberInfo)stringConstructor, runtimeDescriptionAttributeType).ToArray();
            var reflectedConstructorRuntimeRequiredAttribute = Reflections.GetRequiredAttribute(
                (MemberInfo)stringConstructor, runtimeDescriptionAttributeType);
            var reflectedConstructorRuntimeRequiredAttributes = Reflections.GetRequiredAttributes(
                (MemberInfo)stringConstructor, runtimeDescriptionAttributeType).ToArray();
            var parameterRuntimeAttribute = TypeReflections.GetAttribute(
                executeParameter, runtimeDescriptionAttributeType);
            var parameterRuntimeAttributes = TypeReflections.GetAttributes(
                executeParameter, runtimeDescriptionAttributeType).ToArray();
            var parameterRuntimeRequiredAttribute = TypeReflections.GetAttributeRequired(
                executeParameter, runtimeDescriptionAttributeType);
            var parameterRuntimeRequiredAttributes = TypeReflections.GetAttributesRequired(
                executeParameter, runtimeDescriptionAttributeType).ToArray();
            var parameterRuntimeDefined = TypeReflections.IsAttributeDefined(
                executeParameter, runtimeDescriptionAttributeType);
            var runtimeTypeDescriptionAttribute = Reflections.GetAttribute(type, runtimeDescriptionAttributeType);
            var runtimeTypeInfoDescriptionAttribute = Reflections.GetAttribute(type.GetTypeInfo(), runtimeDescriptionAttributeType);
            var runtimeFieldDescriptionAttribute = publicField is null
                ? null
                : Reflections.GetAttribute(publicField, runtimeDescriptionAttributeType);
            var runtimeMemberDescriptionAttribute = Reflections.GetAttribute((MemberInfo)method, runtimeDescriptionAttributeType);
            var runtimeTypeDescriptionAttributes = Reflections.GetAttributes(type, runtimeDescriptionAttributeType).ToArray();
            var runtimeTypeInfoDescriptionAttributes = Reflections.GetAttributes(type.GetTypeInfo(), runtimeDescriptionAttributeType).ToArray();
            var runtimeFieldDescriptionAttributes = publicField is null
                ? Array.Empty<Attribute>()
                : Reflections.GetAttributes(publicField, runtimeDescriptionAttributeType).ToArray();
            var runtimeMemberDescriptionAttributes = Reflections.GetAttributes((MemberInfo)method, runtimeDescriptionAttributeType).ToArray();
            var typeRequiredDescriptionAttribute = TypeReflections.GetAttributeRequired<DescriptionAttribute>(type);
            var reflectedTypeRequiredDescriptionAttribute = Reflections.GetRequiredAttribute<DescriptionAttribute>(type);
            var fieldRequiredDescriptionAttribute = publicField is null
                ? null
                : TypeReflections.GetAttributeRequired<DescriptionAttribute>(publicField);
            var reflectedFieldRequiredDescriptionAttribute = publicField is null
                ? null
                : Reflections.GetRequiredAttribute<DescriptionAttribute>(publicField);
            var propertyRequiredDescriptionAttribute = publicProperty is null
                ? null
                : TypeReflections.GetAttributeRequired<DescriptionAttribute>(publicProperty);
            var reflectedPropertyRequiredDescriptionAttribute = publicProperty is null
                ? null
                : Reflections.GetRequiredAttribute<DescriptionAttribute>(publicProperty);
            var methodRequiredDescriptionAttribute = TypeReflections.GetAttributeRequired<DescriptionAttribute>(method);
            var reflectedMethodRequiredDescriptionAttribute = Reflections.GetRequiredAttribute<DescriptionAttribute>(method);
            var parameterRequiredDescriptionAttribute = TypeReflections.GetAttributeRequired<DescriptionAttribute>(executeParameter);
            var runtimeTypeRequiredDescriptionAttribute = Reflections.GetRequiredAttribute(type, runtimeDescriptionAttributeType);
            var runtimeTypeInfoRequiredDescriptionAttribute = Reflections.GetRequiredAttribute(type.GetTypeInfo(), runtimeDescriptionAttributeType);
            var runtimeFieldRequiredDescriptionAttribute = publicField is null
                ? null
                : Reflections.GetRequiredAttribute(publicField, runtimeDescriptionAttributeType);
            var runtimeMemberRequiredDescriptionAttribute = Reflections.GetRequiredAttribute((MemberInfo)method, runtimeDescriptionAttributeType);
            var typeRequiredDescriptionAttributes = TypeReflections.GetAttributesRequired<DescriptionAttribute>(type).ToArray();
            var reflectedTypeRequiredDescriptionAttributes = Reflections.GetRequiredAttributes<DescriptionAttribute>(type).ToArray();
            var fieldRequiredDescriptionAttributes = publicField is null
                ? Array.Empty<DescriptionAttribute>()
                : TypeReflections.GetAttributesRequired<DescriptionAttribute>(publicField).ToArray();
            var reflectedFieldRequiredDescriptionAttributes = publicField is null
                ? Array.Empty<DescriptionAttribute>()
                : Reflections.GetRequiredAttributes<DescriptionAttribute>(publicField).ToArray();
            var propertyRequiredDescriptionAttributes = publicProperty is null
                ? Array.Empty<DescriptionAttribute>()
                : TypeReflections.GetAttributesRequired<DescriptionAttribute>(publicProperty).ToArray();
            var reflectedPropertyRequiredDescriptionAttributes = publicProperty is null
                ? Array.Empty<DescriptionAttribute>()
                : Reflections.GetRequiredAttributes<DescriptionAttribute>(publicProperty).ToArray();
            var methodRequiredDescriptionAttributes = TypeReflections.GetAttributesRequired<DescriptionAttribute>(method).ToArray();
            var reflectedMethodRequiredDescriptionAttributes = Reflections.GetRequiredAttributes<DescriptionAttribute>(method).ToArray();
            var parameterRequiredDescriptionAttributes = TypeReflections.GetAttributesRequired<DescriptionAttribute>(executeParameter).ToArray();
            var runtimeTypeRequiredDescriptionAttributes = Reflections.GetRequiredAttributes(type, runtimeDescriptionAttributeType).ToArray();
            var runtimeTypeInfoRequiredDescriptionAttributes = Reflections.GetRequiredAttributes(type.GetTypeInfo(), runtimeDescriptionAttributeType).ToArray();
            var runtimeFieldRequiredDescriptionAttributes = publicField is null
                ? Array.Empty<Attribute>()
                : Reflections.GetRequiredAttributes(publicField, runtimeDescriptionAttributeType).ToArray();
            var runtimeMemberRequiredDescriptionAttributes = Reflections.GetRequiredAttributes((MemberInfo)method, runtimeDescriptionAttributeType).ToArray();
            var typeDescriptionDefined = TypeReflections.IsAttributeDefined<DescriptionAttribute>(type)
                && TypeReflections.IsAttributeDefined(type, typeof(DescriptionAttribute))
                && Types.IsAttributeDefined<DescriptionAttribute>(type)
                && Types.IsAttributeDefined(type, typeof(DescriptionAttribute));
            var propertyDescriptionDefined = publicProperty is not null
                && TypeReflections.IsAttributeDefined<DescriptionAttribute>(publicProperty)
                && TypeReflections.IsAttributeDefined(publicProperty, typeof(DescriptionAttribute));
            var fieldDescriptionDefined = publicField is not null
                && TypeReflections.IsAttributeDefined<DescriptionAttribute>(publicField)
                && TypeReflections.IsAttributeDefined(publicField, typeof(DescriptionAttribute));
            var methodDescriptionDefined = TypeReflections.IsAttributeDefined<DescriptionAttribute>(method)
                && TypeReflections.IsAttributeDefined(method, typeof(DescriptionAttribute));
            var parameterDescriptionDefined = TypeReflections.IsAttributeDefined<DescriptionAttribute>(executeParameter)
                && TypeReflections.IsAttributeDefined(executeParameter, typeof(DescriptionAttribute));
            if (publicField is null
                 || boolField is null
                 || enumField is null
                 || nullableEnumField is null
                 || numberField is null
                 || string.IsNullOrWhiteSpace(assemblyFileVersion)
                || string.IsNullOrWhiteSpace(assemblyProductVersion)
                || !scannedAssemblies
                || nullableUnderlyingType != typeof(int)
                || nullableUnderlyingTypeInfo != typeof(int)
                || normalizedNullableArray != typeof(int[])
                || normalizedNullableList != typeof(List<int>)
                || inheritedDefaultDescription != inheritedType.Name
                || inheritedDescription != "Unrooted boundary base type"
                || inheritedAttribute?.Description != "Unrooted boundary base type"
                || inheritedAttributes.Length != 1
                || inheritedAttributes[0].Description != "Unrooted boundary base type"
                || !inheritedDefined
                || (inheritedRuntimeAttribute as DescriptionAttribute)?.Description != "Unrooted boundary base type"
                || (inheritedRuntimeAttributeIgnoringAmbiguity as DescriptionAttribute)?.Description
                    != "Unrooted boundary base type"
                || inheritedRuntimeAttributes.Length != 1
                || (inheritedRuntimeAttributes[0] as DescriptionAttribute)?.Description
                    != "Unrooted boundary base type"
                || (inheritedRuntimeRequiredAttribute as DescriptionAttribute)?.Description
                    != "Unrooted boundary base type"
                || inheritedRuntimeRequiredAttributes.Length != 1
                || (inheritedRuntimeRequiredAttributes[0] as DescriptionAttribute)?.Description
                    != "Unrooted boundary base type"
                || !inheritedRuntimeDefined
                || fieldDescription != "Unrooted boundary field"
                || fieldReflectionsDescription != "Unrooted boundary field"
                || propertyDisplayName != "Unrooted boundary display property"
                || propertyDisplayNameOr != "Unrooted boundary display property"
                || propertyDescriptionOrDisplayName != "Unrooted boundary property"
                || reflectedPropertyDisplayNameOrDescription != "Unrooted boundary display property"
                || fieldDescriptionAttributes.Length != 1
                || fieldDescriptionAttributes[0].Description != "Unrooted boundary field"
                || reflectedFieldDescriptionAttributes.Length != 1
                || reflectedFieldDescriptionAttributes[0].Description != "Unrooted boundary field"
                || fieldDescriptionAttribute?.Description != "Unrooted boundary field"
                || reflectedFieldDescriptionAttribute?.Description != "Unrooted boundary field"
                || fieldRequiredDescriptionAttribute?.Description != "Unrooted boundary field"
                || reflectedFieldRequiredDescriptionAttribute?.Description != "Unrooted boundary field"
                || fieldRequiredDescriptionAttributes.Length != 1
                || fieldRequiredDescriptionAttributes[0].Description != "Unrooted boundary field"
                || reflectedFieldRequiredDescriptionAttributes.Length != 1
                || reflectedFieldRequiredDescriptionAttributes[0].Description != "Unrooted boundary field"
                || !fieldDescriptionDefined
                 || tupleField is null
                 || collectionField is null
                 || structField is null
                 || publicProperty is null
                 || inheritedProperty is null
                 || !abstractPropertyIsAbstract
                 || !abstractPropertyIsVisibleAndVirtual
                 || implementedAbstractProperty is null
                 || implementedAbstractPropertyIsAbstract
                 || !inheritedPropertyIsVirtual
                 || inheritedPropertyIsAbstract
                 || !inheritedPropertyIsVisibleAndVirtual
                 || getterOnlyProperty is null
                || getterOnlyProperty.GetMethod is null
                || getterOnlyProperty.SetMethod is not null
                || !getterProperties.Any(property => property.Name == "GetterOnlyProperty")
                || getterProperties.Any(property => property.Name == "SetterOnlyProperty")
                || setterOnlyProperty is null
                || setterOnlyProperty.GetMethod is not null
                || setterOnlyProperty.SetMethod?.IsPublic != true
                || !setterProperties.Any(property => property.Name == "SetterOnlyProperty")
                || setterProperties.Any(property => property.Name == "GetterOnlyProperty")
                || properties.Any(property => property.Name is "GetterOnlyProperty" or "SetterOnlyProperty")
                || indexer is null
                || indexer.GetIndexParameters().Length != 1
                || staticProperty is null
                || !staticProperty.GetMethod!.IsStatic
                || getterProperties.Any(property => property.Name == "StaticProperty")
                || setterProperties.Any(property => property.Name == "StaticProperty")
                || properties.Any(property => property.Name == "StaticProperty")
                || getterProperties.Any(property => property.GetIndexParameters().Length != 0)
                || setterProperties.Any(property => property.GetIndexParameters().Length != 0)
                || properties.Any(property => property.GetIndexParameters().Length != 0)
                || !nullPropertyTypeRejected
                || !invalidPropertyAccessOptionRejected
                || baseTypes.Length != 1
                || baseTypes[0] != baseType
                || baseTypesBeforeObject.Length != 1
                || baseTypesBeforeObject[0] != baseType
                || topBaseType != baseType
                || !typeBasedOn
                || !typeDerived
                || !isAssignableToBase
                || !isAssignableToContract
                || !isBaseOn
                || !isDerived
                || !isGenericAssignable
                || listElementType != typeof(int)
                || !string.Equals(fullNameWithAssemblyName,
                    $"{type.FullName}, {type.Assembly.GetName().Name}", StringComparison.Ordinal)
                || !genericImplementation
                || genericImplementationType != typeof(IEnumerable<int>)
                || genericArguments.Length != 1
                || genericArguments[0] != typeof(int)
                || rawGenericType != typeof(IEnumerable<int>)
                || rawGenericArguments.Count != 1
                || rawGenericArguments[0] != typeof(int)
                || rawUnrootedGenericType != typeof(int)
                || rawClosedUnrootedGenericType != typeof(int)
                || enumUnderlyingType != typeof(int)
                || !cachedProperties.Any(property => property.Name == "PublicProperty")
                || !cachedProperties.Any(property => property.Name == "InitOnlyProperty")
                || !cachedProperties.Any(property => property.Name == "InheritedProperty")
                || !readableProperties.Any(property => property.Name == "PublicProperty")
                || !readableProperties.Any(property => property.Name == "InitOnlyProperty")
                || !readableProperties.Any(property => property.Name == "InheritedProperty")
                || !readableProperties.Any(property => property.Name == "GetterOnlyProperty")
                || readableProperties.Any(property => property.Name is "SetterOnlyProperty" or "StaticProperty")
                || readableProperties.Any(property => property.GetIndexParameters().Length != 0)
                || initOnlyProperty is null
                || !Equals(initOnlyProperty.GetValue(instance), 91)
                || !writableProperties.Any(property => property.Name == "PublicProperty")
                || !writableProperties.Any(property => property.Name == "InheritedProperty")
                || !writableProperties.Any(property => property.Name == "SetterOnlyProperty")
                || writableProperties.Any(property => property.Name is "GetterOnlyProperty" or "StaticProperty" or "InitOnlyProperty")
                || writableProperties.Any(property => property.GetIndexParameters().Length != 0)
                || setterProperties.Any(property => property.Name == "InitOnlyProperty")
                || properties.Any(property => property.Name == "InitOnlyProperty")
                || !reflectedProperties.Any(property => property.Name == "PublicProperty")
                || !reflectedProperties.Any(property => property.Name == "InitOnlyProperty")
                || !reflectedProperties.Any(property => property.Name == "StaticProperty")
                || !reflectedProperties.Any(property => property.GetIndexParameters().Length != 0)
                || !reflectedInstanceProperties.Any(property => property.Name == "PublicProperty")
                || reflectedInstanceProperties.Any(property => property.Name == "StaticProperty")
                || !reflectedInstanceProperties.Any(property => property.GetIndexParameters().Length != 0)
                || !genericPublicProperties.Any(property => property.Name == "PublicProperty")
                || !genericPublicProperties.Any(property => property.Name == "InitOnlyProperty")
                || genericPublicProperties.Any(property => property.Name == "ReflectedValue")
                || !genericNonPublicProperties.Any(property => property.Name == "ReflectedValue")
                || genericNonPublicProperties.Any(property => property.Name == "PublicProperty")
                || !reflectedFields.Any(field => field.Name == "PublicField")
                || !reflectedFields.Any(field => field.Name == "PublicConstant")
                || !reflectedInstanceFields.Any(field => field.Name == "PublicField")
                || reflectedInstanceFields.Any(field => field.Name == "PublicConstant")
                 || publicPropertyItems.Count != 5
                 || publicPropertyItems.Any(item => item.Text is "StaticProperty" or "SetterOnlyProperty")
                 || publicPropertyItems.Any(item => item.Text == "Item")
                 || publicPropertyItems.SingleOrDefault(item => item.Text == "PublicProperty")?.Value is not 83
                 || publicPropertyItems.SingleOrDefault(item => item.Text == "InitOnlyProperty")?.Value is not 91
                || !reflectedMethods.Any(candidate => candidate.Name == "Execute")
                || !reflectedMethods.Any(candidate => candidate.Name == "Echo")
                || !reflectedInstanceMethods.Any(candidate => candidate.Name == "Execute")
                || reflectedPropertyOrField is not PropertyInfo
                || !interfaceTypes.Contains(contract)
                || !interfaceTypes.Contains(baseContract)
                || !directInterfaceTypes.Contains(contract)
                || directInterfaceTypes.Contains(baseContract)
                || !implementationTypes.Contains(type)
                || instances is null
                || instances.Count != 1
                || instances[0]?.GetType() != type
                || genericMethod.GetGenericArguments().Length != 1
                || genericMethod.GetParameters().Length != 1
                || genericMethod.ReturnType != genericMethod.GetParameters()[0].ParameterType
                 || !Equals(genericResult, 127)
                 || nonPublicProperty.DeclaringType != type
                 || !Equals(nonPublicProperty.GetValue(instance), 73)
                 || genericPublicPropertyWithFlags.DeclaringType != type
                 || genericPublicPropertyWithFlags.Name != "PublicProperty"
                 || genericPrivatePropertyWithFlags.DeclaringType != type
                 || genericPrivatePropertyWithFlags.Name != "ReflectedValue"
                 || genericPrivatePropertyIgnoreCase.Name != "ReflectedValue"
                 || !genericPrivatePropertyWithoutNonPublicRejected
                 || !genericMissingPropertyNameRejected
                 || !genericEmptyPropertyNameRejected
                 || genericPublicMethodWithFlags.DeclaringType != type
                 || genericPublicMethodWithFlags.Name != "Execute"
                 || genericPrivateMethodWithFlags.DeclaringType != type
                 || genericPrivateMethodWithFlags.Name != "get_ReflectedValue"
                 || genericPrivateMethodIgnoreCase.Name != "get_ReflectedValue"
                 || !genericPrivateMethodWithoutNonPublicRejected
                 || !genericMissingMethodNameRejected
                 || !genericEmptyMethodNameRejected
                 || bindingMethod.DeclaringType != type
                || bindingMethod.Name != "Execute"
                || bindingMethod.GetParameters().Length != 1
                || bindingMethod.GetParameters()[0].ParameterType != typeof(int)
                || genericSignatureMethod.DeclaringType != type
                || genericSignatureMethod.Name != "Echo"
                || !genericSignatureMethod.IsGenericMethodDefinition
                || genericSignatureMethod.GetGenericArguments().Length != 1
                || genericSignatureMethod.GetParameters().Length != 1
                || genericSignatureMethod.GetParameters()[0].ParameterType
                    != genericSignatureMethod.GetGenericArguments()[0]
                || genericReturnMethod != genericMethod
                || !genericReturnMismatchRejected
                 || signatureMethod.DeclaringType != type
                || !TypeVisit.IsSignatureCompatible(signatureMethod, contractMethod)
                || baseMethod is not null
                 || reflectedField.Name != "PublicField"
                 || genericPublicFieldWithFlags.DeclaringType != type
                 || genericPublicFieldWithFlags.Name != "PublicField"
                 || genericPrivateFieldWithFlags.DeclaringType != type
                 || genericPrivateFieldWithFlags.Name != "_reflectedValue"
                 || genericPrivateFieldIgnoreCase.Name != "_reflectedValue"
                 || !genericPrivateFieldWithoutNonPublicRejected
                 || !genericMissingFieldNameRejected
                 || !genericEmptyFieldNameRejected
                 || reflectedProperty.Name != "PublicProperty"
                || reflectedMethod.Name != "Execute"
                || typeFullName != TypeName
                || !methodFullName.EndsWith(".Execute", StringComparison.Ordinal)
                || !methodQualifiedName.Contains("Execute", StringComparison.Ordinal)
                || stringConstructor.GetParameters().Length != 1
                || stringConstructor.GetParameters()[0].ParameterType != typeof(string)
                || stringInstance.GetType() != type
                || typeVisitStringInstance.GetType() != type
                || typeVisitBatchInstances.Count != 1
                || typeVisitBatchInstances[0].GetType() != type
                || publicConstants.Length != 2
                || !publicConstants.Contains("Unrooted boundary base constant")
                || !publicConstants.Contains("Unrooted boundary constant")
                 || method.ReturnType != typeof(int)
                 || overriddenMethod is null
                 || overriddenMethod.DeclaringType != type
                 || overriddenMethod.Name != "ComputeBaseValue"
                 || overriddenBaseMethod is null
                 || overriddenBaseMethod.DeclaringType != baseType
                 || overriddenBaseMethod.Name != "ComputeBaseValue"
                 || !Equals(overriddenBaseMethod.Invoke(instance, new object[] { 7 }), 13)
                 || !overriddenMethodIsVisible
                 || !overriddenMethodIsOverridden
                 || !overriddenBaseMethodIsVisibleAndVirtual
                 || overriddenBaseMethodIsOverridden
                 || typeDescription != "Unrooted boundary type"
                || typeReflectionsDescription != "Unrooted boundary type"
                || propertyDescription != "Unrooted boundary property"
                || propertyReflectionsDescription != "Unrooted boundary property"
                 || methodDescription != "Unrooted boundary method"
                 || !TypeReflections.IsTask(taskInfo)
                 || !TypeReflections.IsTaskWithResult(taskResultInfo)
                 || !TypeReflections.IsTaskWithVoidTaskResult(voidTaskResultInfo)
                 || !taskInfo.IsTask()
                 || !taskResultInfo.IsTaskWithResult()
                 || !voidTaskResultInfo.IsTaskWithVoidTaskResult()
                 || !TypeReflections.IsValueTask(valueTaskInfo)
                 || !TypeReflections.IsValueTaskWithResult(valueTaskResultInfo)
                 || !valueTaskInfo.IsValueTask()
                 || !valueTaskResultInfo.IsValueTaskWithResult()
                || !typeDescriptionDefined
                || !propertyDescriptionDefined
                || !methodDescriptionDefined
                || !parameterDescriptionDefined
                || typeDescriptionAttribute?.Description != "Unrooted boundary type"
                || reflectedTypeDescriptionAttribute?.Description != "Unrooted boundary type"
                || (runtimeTypeDescriptionAttribute as DescriptionAttribute)?.Description != "Unrooted boundary type"
                || (runtimeTypeInfoDescriptionAttribute as DescriptionAttribute)?.Description != "Unrooted boundary type"
                || (runtimeFieldDescriptionAttribute as DescriptionAttribute)?.Description != "Unrooted boundary field"
                || (runtimeMemberDescriptionAttribute as DescriptionAttribute)?.Description != "Unrooted boundary method"
                || propertyDescriptionAttribute?.Description != "Unrooted boundary property"
                || reflectedPropertyDescriptionAttribute?.Description != "Unrooted boundary property"
                || methodDescriptionAttribute?.Description != "Unrooted boundary method"
                 || reflectedMethodDescriptionAttribute?.Description != "Unrooted boundary method"
                  || parameterDescription != "Unrooted boundary parameter"
                  || parameterDisplayName != "value"
                  || parameterDescriptionOr != "Unrooted boundary parameter"
                  || parameterDisplayNameOr != "parameter-display-fallback"
                  || parameterDescriptionOrDisplayName != "Unrooted boundary parameter"
                  || parameterDescriptionAttribute?.Description != "Unrooted boundary parameter"
                 || stringConstructorParameter.ParameterType != typeof(string)
                 || (constructorRuntimeAttribute as DescriptionAttribute)?.Description
                     != "Unrooted boundary constructor"
                 || (constructorRuntimeAttributeIgnoringAmbiguity as DescriptionAttribute)?.Description
                     != "Unrooted boundary constructor"
                 || constructorRuntimeAttributes.Length != 1
                 || (constructorRuntimeAttributes[0] as DescriptionAttribute)?.Description
                     != "Unrooted boundary constructor"
                 || (constructorRuntimeRequiredAttribute as DescriptionAttribute)?.Description
                     != "Unrooted boundary constructor"
                 || constructorRuntimeRequiredAttributes.Length != 1
                 || (constructorRuntimeRequiredAttributes[0] as DescriptionAttribute)?.Description
                     != "Unrooted boundary constructor"
                 || !constructorRuntimeDefined
                 || (constructorParameterRuntimeAttribute as DescriptionAttribute)?.Description
                     != "Unrooted boundary constructor parameter"
                 || constructorParameterRuntimeAttributes.Length != 1
                 || (constructorParameterRuntimeAttributes[0] as DescriptionAttribute)?.Description
                     != "Unrooted boundary constructor parameter"
                 || (constructorParameterRuntimeRequiredAttribute as DescriptionAttribute)?.Description
                     != "Unrooted boundary constructor parameter"
                 || constructorParameterRuntimeRequiredAttributes.Length != 1
                 || (constructorParameterRuntimeRequiredAttributes[0] as DescriptionAttribute)?.Description
                     != "Unrooted boundary constructor parameter"
                 || !constructorParameterRuntimeDefined
                 || (reflectedConstructorRuntimeAttribute as DescriptionAttribute)?.Description
                     != "Unrooted boundary constructor"
                 || reflectedConstructorRuntimeAttributes.Length != 1
                 || (reflectedConstructorRuntimeAttributes[0] as DescriptionAttribute)?.Description
                     != "Unrooted boundary constructor"
                 || (reflectedConstructorRuntimeRequiredAttribute as DescriptionAttribute)?.Description
                     != "Unrooted boundary constructor"
                 || reflectedConstructorRuntimeRequiredAttributes.Length != 1
                 || (reflectedConstructorRuntimeRequiredAttributes[0] as DescriptionAttribute)?.Description
                     != "Unrooted boundary constructor"
                 || (parameterRuntimeAttribute as DescriptionAttribute)?.Description != "Unrooted boundary parameter"
                || parameterRuntimeAttributes.Length != 1
                || (parameterRuntimeAttributes[0] as DescriptionAttribute)?.Description
                    != "Unrooted boundary parameter"
                || (parameterRuntimeRequiredAttribute as DescriptionAttribute)?.Description
                    != "Unrooted boundary parameter"
                || parameterRuntimeRequiredAttributes.Length != 1
                || (parameterRuntimeRequiredAttributes[0] as DescriptionAttribute)?.Description
                    != "Unrooted boundary parameter"
                || !parameterRuntimeDefined
                || runtimeTypeDescriptionAttributes.Length != 1
                || (runtimeTypeDescriptionAttributes[0] as DescriptionAttribute)?.Description != "Unrooted boundary type"
                || runtimeTypeInfoDescriptionAttributes.Length != 1
                || (runtimeTypeInfoDescriptionAttributes[0] as DescriptionAttribute)?.Description != "Unrooted boundary type"
                || runtimeFieldDescriptionAttributes.Length != 1
                || (runtimeFieldDescriptionAttributes[0] as DescriptionAttribute)?.Description != "Unrooted boundary field"
                || runtimeMemberDescriptionAttributes.Length != 1
                || (runtimeMemberDescriptionAttributes[0] as DescriptionAttribute)?.Description != "Unrooted boundary method"
                || typeRequiredDescriptionAttribute.Description != "Unrooted boundary type"
                || reflectedTypeRequiredDescriptionAttribute.Description != "Unrooted boundary type"
                || (runtimeTypeRequiredDescriptionAttribute as DescriptionAttribute)?.Description != "Unrooted boundary type"
                || (runtimeTypeInfoRequiredDescriptionAttribute as DescriptionAttribute)?.Description != "Unrooted boundary type"
                || (runtimeFieldRequiredDescriptionAttribute as DescriptionAttribute)?.Description != "Unrooted boundary field"
                || (runtimeMemberRequiredDescriptionAttribute as DescriptionAttribute)?.Description != "Unrooted boundary method"
                || propertyRequiredDescriptionAttribute?.Description != "Unrooted boundary property"
                || reflectedPropertyRequiredDescriptionAttribute?.Description != "Unrooted boundary property"
                || methodRequiredDescriptionAttribute.Description != "Unrooted boundary method"
                || reflectedMethodRequiredDescriptionAttribute.Description != "Unrooted boundary method"
                || parameterRequiredDescriptionAttribute.Description != "Unrooted boundary parameter"
                || runtimeTypeRequiredDescriptionAttributes.Length != 1
                || (runtimeTypeRequiredDescriptionAttributes[0] as DescriptionAttribute)?.Description != "Unrooted boundary type"
                || runtimeTypeInfoRequiredDescriptionAttributes.Length != 1
                || (runtimeTypeInfoRequiredDescriptionAttributes[0] as DescriptionAttribute)?.Description != "Unrooted boundary type"
                || runtimeFieldRequiredDescriptionAttributes.Length != 1
                || (runtimeFieldRequiredDescriptionAttributes[0] as DescriptionAttribute)?.Description != "Unrooted boundary field"
                || runtimeMemberRequiredDescriptionAttributes.Length != 1
                || (runtimeMemberRequiredDescriptionAttributes[0] as DescriptionAttribute)?.Description != "Unrooted boundary method"
                || typeDescriptionAttributes.Length != 1
                || typeDescriptionAttributes[0].Description != "Unrooted boundary type"
                || reflectedTypeDescriptionAttributes.Length != 1
                || reflectedTypeDescriptionAttributes[0].Description != "Unrooted boundary type"
                || propertyDescriptionAttributes.Length != 1
                || propertyDescriptionAttributes[0].Description != "Unrooted boundary property"
                || reflectedPropertyDescriptionAttributes.Length != 1
                || reflectedPropertyDescriptionAttributes[0].Description != "Unrooted boundary property"
                || methodDescriptionAttributes.Length != 1
                || methodDescriptionAttributes[0].Description != "Unrooted boundary method"
                || reflectedMethodDescriptionAttributes.Length != 1
                || reflectedMethodDescriptionAttributes[0].Description != "Unrooted boundary method"
                || typeRequiredDescriptionAttributes.Length != 1
                || typeRequiredDescriptionAttributes[0].Description != "Unrooted boundary type"
                || reflectedTypeRequiredDescriptionAttributes.Length != 1
                || reflectedTypeRequiredDescriptionAttributes[0].Description != "Unrooted boundary type"
                || propertyRequiredDescriptionAttributes.Length != 1
                || propertyRequiredDescriptionAttributes[0].Description != "Unrooted boundary property"
                || reflectedPropertyRequiredDescriptionAttributes.Length != 1
                || reflectedPropertyRequiredDescriptionAttributes[0].Description != "Unrooted boundary property"
                || methodRequiredDescriptionAttributes.Length != 1
                || methodRequiredDescriptionAttributes[0].Description != "Unrooted boundary method"
                || reflectedMethodRequiredDescriptionAttributes.Length != 1
                || reflectedMethodRequiredDescriptionAttributes[0].Description != "Unrooted boundary method"
                || parameterRequiredDescriptionAttributes.Length != 1
                || parameterRequiredDescriptionAttributes[0].Description != "Unrooted boundary parameter"
                 || !Equals(publicField.GetValue(instance), 79)
                 || !Equals(publicProperty.GetValue(instance), 83)
                 || !Equals(implementedAbstractProperty.GetValue(instance), 127)
                 || inheritedProperty.DeclaringType != baseType
                || !Equals(inheritedProperty.GetValue(instance), 89)
                || !Equals(getterOnlyProperty.GetValue(instance), 101)
                || !Equals(method.Invoke(instance, new object[] { 1 }), 80))
                return "member-trimmed";

            if (!publicField.IsAttributeDefined<DescriptionAttribute>()
                 || !publicField.IsAttributeDefined(typeof(DescriptionAttribute))
                 || !Reflections.IsBool(boolField)
                 || !Reflections.IsEnum(enumField)
                 || !Reflections.IsEnum(nullableEnumField)
                 || !Reflections.IsInt(publicField)
                 || !Reflections.IsNumber(numberField)
                 || !Reflections.IsDate(structField)
                 || !publicField.IsNumeric()
                || !publicProperty.IsNumeric()
                || !executeParameter.IsNumeric()
                || !tupleField.IsTupleType()
                || !tupleField.IsStructType()
                 || !structField.IsStructType()
                 || structField.IsTupleType()
                || !TypeReflections.IsCollection(collectionField)
                || Reflections.GetElementType(collectionField.FieldType) != typeof(int)
                || !Types.IsNumericType(typeof(int))
                || Types.IsNumericType(type)
                || Types.IsStructType((TypeInfo)null!)
                || !Types.IsPrimitiveType(typeof(int))
                || Types.IsPrimitiveType(type)
                 || !Types.IsStaticType(typeof(Types))
                 || Types.IsStaticType(type)
                 || !Types.IsAnonymousType(new { Value = 1 }.GetType())
                 || Types.IsAnonymousType(type))
                return "member-trimmed";

            if (!TypeJudge.IsNumericType<int>()
                || !TypeJudge.IsNumericType(typeof(int).GetTypeInfo())
                || !TypeJudge.IsNullableType(typeof(int?).GetTypeInfo())
                || TypeJudge.IsNullableType(typeof(int).GetTypeInfo())
                || !TypeJudge.IsGenericImplementation(typeof(List<int>), typeof(IEnumerable<>)))
                return "member-trimmed";

            var intTypeInfo = typeof(int).GetTypeInfo();
            var nullableIntTypeInfo = typeof(int?).GetTypeInfo();
            if (BingTypeConverter.ToNonNullableTypeInfo(intTypeInfo) is not null
                || BingTypeConverter.ToNonNullableTypeInfo(nullableIntTypeInfo) != intTypeInfo
                || BingTypeConverter.ToSafeNonNullableTypeInfo(intTypeInfo) != intTypeInfo
                || BingTypeConverter.ToSafeNonNullableTypeInfo(nullableIntTypeInfo) != intTypeInfo)
                return "member-trimmed";

             var genericFieldValue = InvokeGenericFieldValue(type, instance, "PublicField");
             InvokeGenericSetFieldValue(type, instance, "PublicField", 91);
             var genericFieldValueAfterSet = InvokeGenericFieldValue(type, instance, "PublicField");
             if (!Equals(genericFieldValue, 79)
                 || !Equals(genericFieldValueAfterSet, 91))
                 return "member-trimmed";

             var baseHiddenFieldValue = InvokeGenericFieldValue(baseType, instance, "HiddenField");
             InvokeGenericSetFieldValue(baseType, instance, "HiddenField", 113);
             var baseHiddenFieldValueAfterSet = InvokeGenericFieldValue(baseType, instance, "HiddenField");
             var derivedHiddenFieldValueAfterBaseSet = InvokeGenericFieldValue(type, instance, "HiddenField");
             if (!Equals(baseHiddenFieldValue, 41)
                 || !Equals(baseHiddenFieldValueAfterSet, 113)
                 || !Equals(derivedHiddenFieldValueAfterBaseSet, 43))
                 return "member-trimmed";

             var genericPropertyValue = InvokeGenericPropertyValue(type, instance, "PublicProperty");
            InvokeGenericSetPropertyValue(type, instance, "PublicProperty", 93);
            var genericPropertyValueAfterSet = InvokeGenericPropertyValue(type, instance, "PublicProperty");
            var pathPropertyValue = Reflections.GetPropertyValueByPath(instance, type, "PublicProperty");
            Reflections.SetPropertyValueByPath(instance, type, "PublicProperty", 97);
            var pathPropertyValueAfterSet = Reflections.GetPropertyValueByPath(instance, type, "PublicProperty");
            if (!Equals(genericPropertyValue, 83)
                || !Equals(genericPropertyValueAfterSet, 93)
                || !Equals(pathPropertyValue, 93)
                || !Equals(pathPropertyValueAfterSet, 97))
                return "member-trimmed";

            setterOnlyProperty.SetValue(instance, 107);
            var publicPathOptions = new MemberPathOptions { IncludeFields = false };
            var options = new MemberPathOptions { IncludeFields = false, IncludeNonPublic = true };
            if (Reflections.HasMemberPath(type, "ReflectedValue", publicPathOptions)
                || Reflections.TryGetMemberValueByPath(instance, "ReflectedValue", out _, publicPathOptions)
                || Reflections.TrySetMemberValueByPath(instance, "ReflectedValue", 74, publicPathOptions)
                || !Reflections.HasMemberPath(type, "ReflectedValue", options))
                return "member-trimmed";
            if (!Reflections.TrySetMemberValueByPath(instance, "InheritedProperty", 97, options)
                || !Reflections.TryGetMemberValueByPath(instance, "InheritedProperty", out var inheritedPathValue, options)
                || !Equals(inheritedPathValue, 97))
                return "member-trimmed";

            return Reflections.TryGetMemberValueByPath(instance, "ReflectedValue", out var value, options)
                && Equals(value, 73)
                ? "metadata-preserved"
                : "member-trimmed";
        }
        catch (MissingMemberException exception)
        {
            WriteBoundaryDiagnostic(exception);
            return "member-trimmed";
        }
        catch (MemberAccessException exception)
        {
            WriteBoundaryDiagnostic(exception);
            return "member-trimmed";
        }
        catch (TargetInvocationException exception)
        {
            WriteBoundaryDiagnostic(exception);
            return "member-trimmed";
        }
        catch (TypeLoadException exception)
        {
            WriteBoundaryDiagnostic(exception);
            return "member-trimmed";
        }
        catch (ArgumentException exception)
        {
            WriteBoundaryDiagnostic(exception);
            return "member-trimmed";
        }
        catch (InvalidOperationException exception)
        {
            WriteBoundaryDiagnostic(exception);
            return "member-trimmed";
        }
        catch (IOException exception)
        {
            WriteBoundaryDiagnostic(exception);
            return "member-trimmed";
        }
        catch (UnauthorizedAccessException exception)
        {
            WriteBoundaryDiagnostic(exception);
            return "member-trimmed";
        }
    }

    private static void WriteBoundaryDiagnostic(Exception exception)
    {
        if (string.Equals(
                Environment.GetEnvironmentVariable("BING_REFLECTION_TRIM_PROBE_DIAGNOSTICS"),
                "1",
                StringComparison.Ordinal))
            Console.Error.WriteLine($"reflection-plugin-load-diagnostic:{exception}");
    }

    private static bool ScanAssemblyDirectory(string sourceAssemblyPath, Assembly expectedAssembly)
    {
        var temporaryDirectory = Path.Combine(
            Path.GetTempPath(), "bing-reflection-assembly-scan-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temporaryDirectory);
        try
        {
            var isolatedAssemblyPath = Path.Combine(temporaryDirectory, Path.GetFileName(sourceAssemblyPath));
            File.Copy(sourceAssemblyPath, isolatedAssemblyPath);
            var assemblies = Reflections.GetAssemblies(temporaryDirectory);
            return assemblies.Any(candidate => string.Equals(
                candidate.FullName, expectedAssembly.FullName, StringComparison.Ordinal));
        }
        finally
        {
            try
            {
                Directory.Delete(temporaryDirectory, recursive: true);
            }
            catch (IOException)
            {
                // 临时目录清理失败不改变扫描结论，进程退出后由系统回收。
            }
            catch (UnauthorizedAccessException)
            {
                // 临时目录清理失败不改变扫描结论，进程退出后由系统回收。
            }
        }
    }

    private static object InvokeGenericFieldValue(Type type, object instance, string fieldName)
    {
        var accessor = FindGenericFieldAccessor(nameof(Reflections.GetFieldValue), 2);
        return accessor.MakeGenericMethod(type).Invoke(null, new object[] { instance, fieldName })!;
    }

    private static void InvokeGenericSetFieldValue(Type type, object instance, string fieldName, object value)
    {
        var accessor = FindGenericFieldAccessor(nameof(Reflections.SetFieldValue), 3);
        accessor.MakeGenericMethod(type).Invoke(null, new[] { instance, fieldName, value });
    }

    private static MethodInfo FindGenericFieldAccessor(string methodName, int parameterCount) =>
        typeof(Reflections).GetMethods()
            .Single(methodInfo => methodInfo.Name == methodName
                && methodInfo.IsGenericMethodDefinition
                && methodInfo.GetParameters().Length == parameterCount
                && !methodInfo.GetParameters()[0].ParameterType.IsByRef);

    private static object InvokeGenericPropertyValue(Type type, object instance, string propertyName)
    {
        var accessor = FindGenericPropertyAccessor(nameof(Reflections.GetPropertyValue), 2);
        return accessor.MakeGenericMethod(type).Invoke(null, new object[] { instance, propertyName })!;
    }

    private static void InvokeGenericSetPropertyValue(Type type, object instance, string propertyName, object value)
    {
        var accessor = FindGenericPropertyAccessor(nameof(Reflections.SetPropertyValue), 3);
        accessor.MakeGenericMethod(type).Invoke(null, new[] { instance, propertyName, value });
    }

    private static MethodInfo FindGenericPropertyAccessor(string methodName, int parameterCount) =>
        typeof(Reflections).GetMethods()
            .Single(methodInfo => methodInfo.Name == methodName
                && methodInfo.IsGenericMethodDefinition
                && methodInfo.GetParameters().Length == parameterCount);

    private static TMember InvokeGenericMember<TMember>(string methodName, Type type, string memberName)
        where TMember : MemberInfo
    {
        var accessor = typeof(Reflections).GetMethods()
            .Single(methodInfo => methodInfo.Name == methodName
                && methodInfo.IsGenericMethodDefinition
                && methodInfo.GetParameters().Length == 1);
        return (TMember)accessor.MakeGenericMethod(type)
            .Invoke(null, new object[] { memberName })!;
    }

    private static TMember InvokeGenericMemberWithBindingFlags<TMember>(
        string methodName,
        Type type,
        string memberName,
        BindingFlags bindingFlags)
        where TMember : MemberInfo
    {
        var accessor = typeof(Reflections).GetMethods()
            .Single(methodInfo => methodInfo.Name == methodName
                && methodInfo.IsGenericMethodDefinition
                && methodInfo.GetParameters().Length == 2);
        return (TMember)accessor.MakeGenericMethod(type)
            .Invoke(null, new object[] { memberName, bindingFlags })!;
    }

    private static TElement[] InvokeGenericArray<TElement>(string methodName, Type type, params object[] arguments)
    {
        var accessor = typeof(Reflections).GetMethods()
            .Single(methodInfo => methodInfo.Name == methodName
                && methodInfo.IsGenericMethodDefinition
                && methodInfo.GetParameters().Length == arguments.Length);
        return (TElement[])accessor.MakeGenericMethod(type).Invoke(null, arguments)!;
    }

    private sealed class ResolverLoadContext : AssemblyLoadContext
    {
        private readonly string _assemblyPath;
        private readonly AssemblyDependencyResolver _resolver;

        public ResolverLoadContext(string hostPath, string assemblyPath)
            : base("ReflectionTrimProbe.UnrootedPlugin.Resolver", isCollectible: true)
        {
            _assemblyPath = assemblyPath;
            _resolver = new AssemblyDependencyResolver(File.Exists(hostPath) ? hostPath : assemblyPath);
        }

        protected override Assembly? Load(AssemblyName assemblyName)
        {
            if (string.Equals(assemblyName.Name, AssemblyName, StringComparison.OrdinalIgnoreCase))
                return LoadFromAssemblyPath(_assemblyPath);

            // 框架程序集必须复用默认上下文；否则裁剪目录中的 System.ComponentModel
            // 会与 probe 默认上下文中的 DescriptionAttribute 产生类型身份分裂。
            if (IsFrameworkAssembly(assemblyName.Name))
                return AssemblyLoadContext.Default.LoadFromAssemblyName(assemblyName);

            var resolvedPath = _resolver.ResolveAssemblyToPath(assemblyName);
            return resolvedPath is null ? null : LoadFromAssemblyPath(resolvedPath);
        }

        private static bool IsFrameworkAssembly(string? name) =>
            name is "System" or "netstandard"
                || name?.StartsWith("System.", StringComparison.Ordinal) == true
                || name?.StartsWith("Microsoft.", StringComparison.Ordinal) == true;
    }
}
