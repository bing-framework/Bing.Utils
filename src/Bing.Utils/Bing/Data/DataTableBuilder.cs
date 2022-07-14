using System;
using System.Data;
using System.Reflection;
using System.Reflection.Emit;

namespace Bing.Data
{
    /// <summary>
    /// 数据表构建器
    /// </summary>
    /// <typeparam name="T">类型</typeparam>
    internal class DataTableBuilder<T>
    {
        /// <summary>
        /// 获取值方法
        /// </summary>
        // ReSharper disable once StaticMemberInGenericType
        private static readonly MethodInfo GetValueMethod = typeof(DataRow).GetMethod("get_Item", new[] { typeof(int) });

        /// <summary>
        /// 判断是否为<see cref="DBNull"/>方法
        /// </summary>
        // ReSharper disable once StaticMemberInGenericType
        private static readonly MethodInfo IsDbNullMethod = typeof(DataRow).GetMethod("IsNull", new[] { typeof(int) });

        /// <summary>
        /// 加载数据
        /// </summary>
        /// <param name="dataRecord">数据记录</param>
        /// <returns>泛型对象</returns>
        private delegate T Load(DataRow dataRecord);

        /// <summary>
        /// 加载处理器
        /// </summary>
        private Load _handler;

        /// <summary>
        /// 初始化一个<see cref="DataTableBuilder{T}"/>类型的实例
        /// </summary>
        private DataTableBuilder()
        {
        }

        /// <summary>
        /// 构建
        /// </summary>
        /// <param name="dataRecord">数据记录</param>
        /// <returns>泛型对象</returns>
        public T Build(DataRow dataRecord) => _handler(dataRecord);

        /// <summary>
        /// 创建数据表构建器
        /// </summary>
        /// <param name="dataRecord">数据记录</param>
        /// <returns>数据表构建器</returns>
        public static DataTableBuilder<T> CreateBuilder(DataRow dataRecord)
        {
            DynamicMethod methodCreateEntity = new DynamicMethod("DynamicCreateEntity", typeof(T), new[]
            {
                typeof(DataRow)
            }, typeof(T), true);
            var generator = methodCreateEntity.GetILGenerator();
            var result = generator.DeclareLocal(typeof(T));
            generator.Emit(OpCodes.Newobj, typeof(T).GetConstructor(Type.EmptyTypes));
            generator.Emit(OpCodes.Stloc, result);
            for (var i = 0; i < dataRecord.ItemArray.Length; i++)
            {
                var propertyInfo = typeof(T).GetProperty(dataRecord.Table.Columns[i].ColumnName);
                var endIfLabel = generator.DefineLabel();
                if (propertyInfo == null || propertyInfo.GetSetMethod() == null)
                    continue;
                generator.Emit(OpCodes.Ldarg_0);
                generator.Emit(OpCodes.Ldc_I4, i);
                generator.Emit(OpCodes.Callvirt, IsDbNullMethod);
                generator.Emit(OpCodes.Brtrue, endIfLabel);
                generator.Emit(OpCodes.Ldloc, result);
                generator.Emit(OpCodes.Ldarg_0);
                generator.Emit(OpCodes.Ldc_I4, i);
                generator.Emit(OpCodes.Callvirt, GetValueMethod);
                generator.Emit(OpCodes.Unbox_Any, propertyInfo.PropertyType);
                generator.Emit(OpCodes.Callvirt, propertyInfo.GetSetMethod());
                generator.MarkLabel(endIfLabel);
            }

            generator.Emit(OpCodes.Ldloc, result);
            generator.Emit(OpCodes.Ret);
            return new DataTableBuilder<T>
            {
                _handler = (Load)methodCreateEntity.CreateDelegate(typeof(Load))
            };
        }

    }
}