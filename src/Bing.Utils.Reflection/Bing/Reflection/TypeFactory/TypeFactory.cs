using System.Collections.Concurrent;
using System.Reflection;
using System.Reflection.Emit;
using Bing.Dynamic;

// ReSharper disable once CheckNamespace
namespace Bing.Reflection;

#if !NETSTANDARD

/// <summary>
/// 类型工厂
/// </summary>
internal static partial class TypeFactory
{
    /// <summary>
    /// 程序集名称
    /// </summary>
    private const string AssemblyName = "Bing.Reflection.NRefDynamic";

    /// <summary>
    /// 生成类型字典
    /// </summary>
    private static readonly ConcurrentDictionary<string, Type> _generatedTypes = new();

    /// <summary>
    /// 模型构建器
    /// </summary>
    private static readonly ModuleBuilder _moduleBuilder = AssemblyBuilder
        .DefineDynamicAssembly(new AssemblyName(AssemblyName), AssemblyBuilderAccess.Run)
        .DefineDynamicModule(AssemblyName);

    /// <summary>
    /// 对象构造函数
    /// </summary>
    private static readonly ConstructorInfo _objectConstructor = typeof(object).GetTypeInfo().GetConstructor(Type.EmptyTypes);

    /// <summary>
    /// 创建动态类型
    /// </summary>
    /// <param name="properties">属性字典</param>
    /// <returns>动态类型</returns>
    public static Type CreateType(IDictionary<string, Type> properties)
    {
        var id = SignType(string.Join("|", properties.Select(_ => $"{_.Key}={_.Value.FullName}")));
        if (_generatedTypes.TryGetValue(id, out var type))
            return type;

        var typeBuilder = _moduleBuilder.DefineType(
            $"{AssemblyName}.Dynamic_{Guid.NewGuid().ToString("N").ToUpper()}",
            TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.Sealed,
            typeof(DynamicBase));

        var fields = new Dictionary<string, FieldBuilder>(properties.Count);

        #region 属性

        foreach (var item in properties)
        {
            var field = typeBuilder.DefineField($"field_{item.Key}", item.Value, FieldAttributes.Private);
            fields[item.Key] = field;

            var property = typeBuilder.DefineProperty(item.Key, PropertyAttributes.None, item.Value, null);

            var getter = typeBuilder.DefineMethod(
                $"get_{item.Key}",
                MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig,
                item.Value,
                null);

            var getterILGenerator = getter.GetILGenerator();
            getterILGenerator.Emit(OpCodes.Ldarg_0);
            getterILGenerator.Emit(OpCodes.Ldfld, field);
            getterILGenerator.Emit(OpCodes.Ret);
            property.SetGetMethod(getter);

            var setter = typeBuilder.DefineMethod(
                $"set_{item.Key}",
                MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName,
                null,
                new[] { item.Value });
            setter.DefineParameter(1, ParameterAttributes.In, "value");
            var setterILGenerator = setter.GetILGenerator();
            setterILGenerator.Emit(OpCodes.Ldarg_0);
            setterILGenerator.Emit(OpCodes.Ldarg_1);
            setterILGenerator.Emit(OpCodes.Stfld, field);
            setterILGenerator.Emit(OpCodes.Ret);
            property.SetSetMethod(setter);
        }

        #endregion

        #region 构造函数

        var defaultConstructor = typeBuilder.DefineConstructor(
            MethodAttributes.Public | MethodAttributes.HideBySig,
            CallingConventions.HasThis,
            null);
        var defaultConstructorILGenerator = defaultConstructor.GetILGenerator();
        defaultConstructorILGenerator.Emit(OpCodes.Ldarg_0);
        defaultConstructorILGenerator.Emit(OpCodes.Call, _objectConstructor);
        defaultConstructorILGenerator.Emit(OpCodes.Ret);

        var parametersConstructor = typeBuilder.DefineConstructor(
            MethodAttributes.Public | MethodAttributes.HideBySig,
            CallingConventions.HasThis,
            properties.Values.ToArray());
        var parametersConstructorILGenerator = parametersConstructor.GetILGenerator();
        parametersConstructorILGenerator.Emit(OpCodes.Ldarg_0);
        parametersConstructorILGenerator.Emit(OpCodes.Call, _objectConstructor);
        for (var i = 0; i < properties.Count; i++)
        {
            var item = properties.ElementAt(i);
            parametersConstructor.DefineParameter(i + 1, ParameterAttributes.None, item.Key);
            parametersConstructorILGenerator.Emit(OpCodes.Ldarg_0);

            if (i == 0)
                parametersConstructorILGenerator.Emit(OpCodes.Ldarg_1);
            else if (i == 1)
                parametersConstructorILGenerator.Emit(OpCodes.Ldarg_2);
            else if (i == 2)
                parametersConstructorILGenerator.Emit(OpCodes.Ldarg_3);
            else if (i < 255)
                parametersConstructorILGenerator.Emit(OpCodes.Ldarg_S, (byte)(i + 1));
            else
                parametersConstructorILGenerator.Emit(OpCodes.Ldarg, unchecked((short)(i + 1)));
            parametersConstructorILGenerator.Emit(OpCodes.Stfld, fields[item.Key]);
        }

        parametersConstructorILGenerator.Emit(OpCodes.Ret);

        #endregion

        type = typeBuilder.CreateTypeInfo()!.UnderlyingSystemType;

        _generatedTypes.TryAdd(id, type);

        return type;
    }
}

#endif
