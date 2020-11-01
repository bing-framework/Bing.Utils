using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Bing.Expressions;
using Bing.Extensions;
using Bing.Helpers;
using Bing.Tests.Samples;
using Xunit;
using Xunit.Abstractions;
using Enum = Bing.Helpers.Enum;

namespace Bing.Utils.Tests.Helpers
{
    /// <summary>
    /// 测试Lambda表达式操作
    /// </summary>
    public class LambdasTest : TestBase
    {
        public LambdasTest(ITestOutputHelper output) : base(output)
        {
        }

        #region GetMember(获取成员)

        /// <summary>
        /// 测试 - 获取成员
        /// </summary>
        [Fact]
        public void Test_GetMember()
        {
            Assert.Null(Lambdas.GetMember(null));

            Expression<Func<Sample, string>> expression = t => t.StringValue;
            Assert.Equal("StringValue", Lambdas.GetMember(expression).Name);

            Expression<Func<Sample, string>> expression2 = t => t.Test2.StringValue;
            Assert.Equal("StringValue",((PropertyInfo)Lambdas.GetMember(expression2)).Name);
        }

        #endregion

        #region GetName(获取成员名称)

        /// <summary>
        /// 测试 - 获取成员名称
        /// </summary>
        [Fact]
        public void Test_GetName()
        {
            Assert.Equal("", Lambdas.GetName(null));

            Expression<Func<Sample, string>> expression = t => t.StringValue;
            Assert.Equal("StringValue", Lambdas.GetName(expression));

            Expression<Func<Sample, string>> expression2 = t => t.Test2.StringValue;
            Assert.Equal("Test2.StringValue", Lambdas.GetName(expression2));

            Expression<Func<Sample, string>> expression3 = t => t.Test2.Test3.StringValue;
            Assert.Equal("Test2.Test3.StringValue", Lambdas.GetName(expression3));

            Expression<Func<Sample, int?>> expression4 = t => t.NullableIntValue;
            Assert.Equal("NullableIntValue", Lambdas.GetName(expression4));

            Expression<Func<Sample, int?>> expression5 = t => t.IntValue;
            Assert.Equal("IntValue", Lambdas.GetName(expression5));

            Expression<Func<Sample, bool>> expression6 = t => t.BoolValue;
            Assert.Equal("BoolValue", Lambdas.GetName(expression6));

            expression6 = t => !t.BoolValue;
            Assert.Equal("BoolValue", Lambdas.GetName(expression6));

            expression6 = t => !t.Test2.BoolValue;
            Assert.Equal("Test2.BoolValue", Lambdas.GetName(expression6));
        }

        #endregion

        #region GetNames(获取成员名称列表)

        /// <summary>
        /// 测试获取成员名称列表
        /// </summary>
        [Fact]
        public void Test_GetNames()
        {
            Expression<Func<Sample, object[]>> expression = (t => new object[] { t.Test2.StringValue, t.IntValue });
            Assert.Equal(2, Lambdas.GetNames(expression).Count);
            Assert.Equal("Test2.StringValue", Lambdas.GetNames(expression)[0]);
            Assert.Equal("IntValue", Lambdas.GetNames(expression)[1]);
        }

        #endregion

        #region GetLastName(获取最后一级成员名称)

        /// <summary>
        /// 测试获取成员名称 - 返回类型为Object
        /// </summary>
        [Fact]
        public void Test_GetLastName_Object()
        {
            Expression<Func<Sample, object>> expression = t => t.StringValue == "A";
            Assert.Equal("StringValue", Lambdas.GetLastName(expression));
        }

        /// <summary>
        /// 测试获取成员名称 - 返回类型为bool
        /// </summary>
        [Fact]
        public void Test_GetLastName_Bool()
        {
            Assert.Empty(Lambdas.GetLastName(null));

            Expression<Func<Sample, bool>> expression = t => t.StringValue == "A";
            Assert.Equal("StringValue", Lambdas.GetLastName(expression));

            Expression<Func<Sample, bool>> expression2 = t => t.Test2.IntValue == 1;
            Assert.Equal("IntValue", Lambdas.GetLastName(expression2));

            Expression<Func<Sample, bool>> expression3 = t => t.Test2.Test3.StringValue == "B";
            Assert.Equal("StringValue", Lambdas.GetLastName(expression3));

            expression3 = t => !t.Test2.BoolValue;
            Assert.Equal("BoolValue", Lambdas.GetLastName(expression3));

            expression = t => t.StringValue == "A";
            Assert.Empty(Lambdas.GetLastName(expression, true));

            var value = "a";
            expression = t => t.StringValue == value;
            Assert.Empty(Lambdas.GetLastName(expression, true));

            var sample = new Sample();
            expression = t => t.StringValue == sample.StringValue;
            Assert.Empty(Lambdas.GetLastName(expression, true));
        }

        /// <summary>
        /// 测试获取成员名称 - 运算符
        /// </summary>
        [Fact]
        public void Test_GetLastName_Operation()
        {
            Expression<Func<Sample, bool>> expression = t => t.Test2.IntValue != 1;
            Assert.Equal("IntValue", Lambdas.GetLastName(expression));

            expression = t => t.Test2.IntValue > 1;
            Assert.Equal("IntValue", Lambdas.GetLastName(expression));

            expression = t => t.Test2.IntValue < 1;
            Assert.Equal("IntValue", Lambdas.GetLastName(expression));

            expression = t => t.Test2.IntValue >= 1;
            Assert.Equal("IntValue", Lambdas.GetLastName(expression));

            expression = t => t.Test2.IntValue <= 1;
            Assert.Equal("IntValue", Lambdas.GetLastName(expression));
        }

        /// <summary>
        /// 测试获取成员名称 - 可空类型
        /// </summary>
        [Fact]
        public void Test_GetLastName_Nullable()
        {
            Expression<Func<Sample, bool>> expression = t => t.NullableIntValue == 1;
            Assert.Equal("NullableIntValue", Lambdas.GetLastName(expression));

            expression = t => t.NullableDecimalValue == 1.5M;
            Assert.Equal("NullableDecimalValue", Lambdas.GetLastName(expression));
        }

        /// <summary>
        /// 测试获取成员名称 - 方法
        /// </summary>
        [Fact]
        public void Test_GetLastName_Method()
        {
            Expression<Func<Sample, bool>> expression = t => t.StringValue.Contains("A");
            Assert.Equal("StringValue", Lambdas.GetLastName(expression));

            expression = t => t.Test2.StringValue.Contains("B");
            Assert.Equal("StringValue", Lambdas.GetLastName(expression));

            expression = t => t.Test2.Test3.StringValue.StartsWith("C");
            Assert.Equal("StringValue", Lambdas.GetLastName(expression));

            var test = new Sample { Email = "a" };
            expression = t => t.StringValue.Contains(test.Email);
            Assert.Equal("StringValue", Lambdas.GetLastName(expression));
        }

        /// <summary>
        /// 测试获取成员名称 - 实例
        /// </summary>
        [Fact]
        public void Test_GetLastName_Instance()
        {
            var test = new Sample() { StringValue = "a", Test2 = new Sample2() { StringValue = "b", Test3 = new Sample3() { StringValue = "c" } } };

            Expression<Func<string>> expression = () => test.StringValue;
            Assert.Empty(Lambdas.GetLastName(expression));

            Expression<Func<string>> expression2 = () => test.Test2.StringValue;
            Assert.Empty(Lambdas.GetLastName(expression2));

            Expression<Func<string>> expression3 = () => test.Test2.Test3.StringValue;
            Assert.Empty(Lambdas.GetLastName(expression3));
        }

        /// <summary>
        /// 测试获取成员名称 - 复杂类型
        /// </summary>
        [Fact]
        public void Test_GetLastName_Complex()
        {
            var test = new Sample() { StringValue = "a", Test2 = new Sample2() { StringValue = "b" } };

            Expression<Func<Sample, bool>> expression = t => t.StringValue == test.StringValue;
            Assert.Equal("StringValue", Lambdas.GetLastName(expression));
            Expression<Func<Sample, bool>> expression2 = t => t.StringValue == test.Test2.StringValue;
            Assert.Equal("StringValue", Lambdas.GetLastName(expression2));

            Expression<Func<Sample, bool>> expression3 = t => t.StringValue.Contains(test.StringValue);
            Assert.Equal("StringValue", Lambdas.GetLastName(expression3));
            Expression<Func<Sample, bool>> expression4 = t => t.StringValue.Contains(test.Test2.StringValue);
            Assert.Equal("StringValue", Lambdas.GetLastName(expression4));
        }

        /// <summary>
        /// 测试获取成员名称 - 枚举
        /// </summary>
        [Fact]
        public void Test_GetLastName_Enum()
        {
            var test1 = new Sample { NullableEnumValue = EnumSample.C };

            Expression<Func<Sample, bool>> expression = test => test.EnumValue == EnumSample.D;
            Assert.Equal("EnumValue", Lambdas.GetLastName(expression));

            expression = test => test.EnumValue == test1.NullableEnumValue;
            Assert.Equal("EnumValue", Lambdas.GetLastName(expression));

            expression = test => test.NullableEnumValue == EnumSample.E;
            Assert.Equal("NullableEnumValue", Lambdas.GetLastName(expression));

            expression = test => test.NullableEnumValue == test1.NullableEnumValue;
            Assert.Equal("NullableEnumValue", Lambdas.GetLastName(expression));

            test1.NullableEnumValue = null;
            expression = test => test.NullableEnumValue == test1.NullableEnumValue;
            Assert.Equal("NullableEnumValue", Lambdas.GetLastName(expression));
        }

        /// <summary>
        /// 测试获取成员名称 - 集合
        /// </summary>
        [Fact]
        public void Test_GetLastName_List()
        {
            var list = new List<string> { "a", "b" };
            Expression<Func<Sample, bool>> expression = t => list.Contains(t.Test2.StringValue);
            Assert.Equal("StringValue", Lambdas.GetLastName(expression));
        }

        /// <summary>
        /// 测试获取成员名称 - 静态成员
        /// </summary>
        [Fact]
        public void Test_GetLastName_Static()
        {
            Expression<Func<Sample, bool>> expression = t => t.StringValue == Sample.StaticString;
            Assert.Equal("StringValue", Lambdas.GetLastName(expression));

            expression = t => t.StringValue == Sample.StaticSample.StringValue;
            Assert.Equal("StringValue", Lambdas.GetLastName(expression));

            expression = t => t.Test2.StringValue == Sample.StaticString;
            Assert.Equal("StringValue", Lambdas.GetLastName(expression));

            expression = t => t.Test2.StringValue == Sample.StaticSample.StringValue;
            Assert.Equal("StringValue", Lambdas.GetLastName(expression));

            expression = t => t.Test2.StringValue.Contains(Sample.StaticSample.StringValue);
            Assert.Equal("StringValue", Lambdas.GetLastName(expression));
        }

        /// <summary>
        /// 测试获取成员名称 - 返回右侧表达式名称
        /// </summary>
        [Fact]
        public void Test_GetLastName_Right()
        {
            Expression<Func<Sample, Sample2, bool>> expression = (l, r) => l.DisplayValue == r.Test3.StringValue;
            Assert.Equal("DisplayValue", Lambdas.GetLastName(expression));
            Assert.Equal("StringValue", Lambdas.GetLastName(expression, true));
        }

        #endregion

        #region GetLastNames(获取最后一级成员名称列表)

        /// <summary>
        /// 获取最后一级成员名称列表
        /// </summary>
        [Fact]
        public void Test_GetLastNames()
        {
            Expression<Func<Sample, object[]>> expression = (t => new object[] { t.Test2.StringValue, t.IntValue });
            Assert.Equal(2, Lambdas.GetLastNames(expression).Count);
            Assert.Equal("StringValue", Lambdas.GetLastNames(expression)[0]);
            Assert.Equal("IntValue", Lambdas.GetLastNames(expression)[1]);
        }

        #endregion

        #region GetValue(获取成员值)

        /// <summary>
        /// 测试获取成员值 - 返回类型为Object
        /// </summary>
        [Fact]
        public void Test_GetValue_Object()
        {
            Expression<Func<Sample, object>> expression = t => t.StringValue == "A";
            Assert.Equal("A", Lambdas.GetValue(expression));
        }

        /// <summary>
        /// 测试获取成员值
        /// </summary>
        [Fact]
        public void Test_GetValue()
        {
            Assert.Null(Lambdas.GetValue(null));

            Expression<Func<Sample, bool>> expression = t => t.StringValue == "A";
            Assert.Equal("A", Lambdas.GetValue(expression));

            Expression<Func<Sample, bool>> expression2 = t => t.Test2.IntValue == 1;
            Assert.Equal(1, Lambdas.GetValue(expression2));

            Expression<Func<Sample, bool>> expression3 = t => t.Test2.Test3.StringValue == "B";
            Assert.Equal("B", Lambdas.GetValue(expression3));

            var value = Guid.NewGuid();
            Expression<Func<Sample, bool>> expression4 = t => t.GuidValue == value;
            Assert.Equal(value, Lambdas.GetValue(expression4));

            Expression<Func<Sample, bool>> expression5 = t => 1 == t.Test2.IntValue;
            Assert.Equal(1, Lambdas.GetValue(expression5));
        }

        /// <summary>
        /// 测试获取成员值 - 布尔属性
        /// </summary>
        [Fact]
        public void Test_GetValue_Bool()
        {
            Expression<Func<Sample, bool>> expression = t => t.BoolValue;
            Assert.Equal("True", Lambdas.GetValue(expression).ToString());

            expression = t => !t.BoolValue;
            Assert.Equal("False", Lambdas.GetValue(expression).ToString());

            expression = t => t.Test2.BoolValue;
            Assert.Equal("True", Lambdas.GetValue(expression).ToString());

            expression = t => !t.Test2.BoolValue;
            Assert.Equal("False", Lambdas.GetValue(expression).ToString());

            expression = t => t.BoolValue == true;
            Assert.Equal("True", Lambdas.GetValue(expression).ToString());

            expression = t => t.BoolValue == false;
            Assert.Equal("False", Lambdas.GetValue(expression).ToString());
        }

        /// <summary>
        /// 测试获取成员值 - Guid.NewGuid
        /// </summary>
        [Fact]
        public void Test_GetValue_NewGuid()
        {
            Expression<Func<Sample, bool>> expression = t => t.GuidValue == Guid.NewGuid();
            var value = Lambdas.GetValue(expression);
            Assert.NotEqual(Guid.Empty, Conv.ToGuid(value));
        }

        /// <summary>
        /// 测试获取成员值 - Guid属性
        /// </summary>
        [Fact]
        public void Test_GetValue_Guid()
        {
            var id = Guid.NewGuid();
            Expression<Func<Sample, bool>> expression = t => t.GuidValue == id;
            var value = Lambdas.GetValue(expression);
            Assert.NotEqual(Guid.Empty, Conv.ToGuid(value));
        }

        /// <summary>
        /// 测试获取成员之 - Guid属性
        /// </summary>
        [Fact]
        public void Test_GetValue_Guid_Method()
        {
            var id = Guid.NewGuid();
            Assert.NotEqual(Guid.Empty, Conv.ToGuid(GetGuidValue(id)));
        }

        private object GetGuidValue(Guid id)
        {
            Expression<Func<Sample, bool>> expression = t => t.GuidValue == id;
            var value = Lambdas.GetValue(expression);
            return value;
        }

        /// <summary>
        /// 测试获取成员值 - DateTime.Now
        /// </summary>
        [Fact]
        public void Test_GetValue_DateTimeNow()
        {
            Expression<Func<Sample, bool>> expression = t => t.DateValue == DateTime.Now;
            var value = Lambdas.GetValue(expression);
            Assert.NotNull(Conv.ToDateOrNull(value));
        }

        /// <summary>
        /// 测试获取成员值 - 运算符
        /// </summary>
        [Fact]
        public void Test_GetValue_Operation()
        {
            Expression<Func<Sample, bool>> expression = t => t.Test2.IntValue != 1;
            Assert.Equal(1, Lambdas.GetValue(expression));

            expression = t => t.Test2.IntValue > 1;
            Assert.Equal(1, Lambdas.GetValue(expression));

            expression = t => t.Test2.IntValue < 1;
            Assert.Equal(1, Lambdas.GetValue(expression));

            expression = t => t.Test2.IntValue >= 1;
            Assert.Equal(1, Lambdas.GetValue(expression));

            expression = t => t.Test2.IntValue <= 1;
            Assert.Equal(1, Lambdas.GetValue(expression));
        }

        /// <summary>
        /// 测试获取成员值 - 可空类型
        /// </summary>
        [Fact]
        public void Test_GetValue_Nullable()
        {
            Expression<Func<Sample, bool>> expression = t => t.NullableIntValue == 1;
            Assert.Equal(1, Lambdas.GetValue(expression));

            expression = t => t.NullableDecimalValue == 1.5M;
            Assert.Equal(1.5M, Lambdas.GetValue(expression));

            var sample = new Sample();
            expression = t => t.BoolValue == sample.NullableBoolValue;
            Assert.Null(Lambdas.GetValue(expression));
        }

        /// <summary>
        /// 测试获取成员值 - 方法
        /// </summary>
        [Fact]
        public void Test_GetValue_Method()
        {
            Expression<Func<Sample, bool>> expression = t => t.StringValue.Contains("A");
            Assert.Equal("A", Lambdas.GetValue(expression));

            expression = t => t.Test2.StringValue.Contains("B");
            Assert.Equal("B", Lambdas.GetValue(expression));

            expression = t => t.Test2.Test3.StringValue.StartsWith("C");
            Assert.Equal("C", Lambdas.GetValue(expression));

            var test = new Sample { Email = "a" };
            expression = t => t.StringValue.Contains(test.Email);
            Assert.Equal("a", Lambdas.GetValue(expression));
        }

        /// <summary>
        /// 测试获取成员值 - 实例
        /// </summary>
        [Fact]
        public void Test_GetValue_Instance()
        {
            var test = new Sample() { StringValue = "a", BoolValue = true, Test2 = new Sample2() { StringValue = "b", Test3 = new Sample3() { StringValue = "c" } } };

            Expression<Func<string>> expression = () => test.StringValue;
            Assert.Equal("a", Lambdas.GetValue(expression));

            Expression<Func<string>> expression2 = () => test.Test2.StringValue;
            Assert.Equal("b", Lambdas.GetValue(expression2));

            Expression<Func<string>> expression3 = () => test.Test2.Test3.StringValue;
            Assert.Equal("c", Lambdas.GetValue(expression3));

            Expression<Func<bool>> expression4 = () => test.BoolValue;
            Assert.True(Conv.ToBool(Lambdas.GetValue(expression4)));
        }

        /// <summary>
        /// 测试获取成员值 - 复杂类型
        /// </summary>
        [Fact]
        public void Test_GetValue_Complex()
        {
            var test = new Sample() { StringValue = "a", Test2 = new Sample2() { StringValue = "b" } };

            Expression<Func<Sample, bool>> expression = t => t.StringValue == test.StringValue;
            Assert.Equal("a", Lambdas.GetValue(expression));
            Expression<Func<Sample, bool>> expression2 = t => t.StringValue == test.Test2.StringValue;
            Assert.Equal("b", Lambdas.GetValue(expression2));

            Expression<Func<Sample, bool>> expression3 = t => t.StringValue.Contains(test.StringValue);
            Assert.Equal("a", Lambdas.GetValue(expression3));
            Expression<Func<Sample, bool>> expression4 = t => t.StringValue.Contains(test.Test2.StringValue);
            Assert.Equal("b", Lambdas.GetValue(expression4));
        }

        /// <summary>
        /// 测试获取成员值 - 枚举
        /// </summary>
        [Fact]
        public void Test_GetValue_Enum()
        {
            var test1 = new Sample { NullableEnumValue = EnumSample.C };

            Expression<Func<Sample, bool>> expression = test => test.EnumValue == EnumSample.D;
            Assert.Equal(EnumSample.D.Value(), Enum.GetValue<EnumSample>(Lambdas.GetValue(expression)));

            expression = test => test.EnumValue == test1.NullableEnumValue;
            Assert.Equal(EnumSample.C, Lambdas.GetValue(expression));

            expression = test => test.NullableEnumValue == EnumSample.E;
            Assert.Equal(EnumSample.E, Lambdas.GetValue(expression));

            expression = test => test.NullableEnumValue == test1.NullableEnumValue;
            Assert.Equal(EnumSample.C, Lambdas.GetValue(expression));

            test1.NullableEnumValue = null;
            expression = test => test.NullableEnumValue == test1.NullableEnumValue;
            Assert.Null(Lambdas.GetValue(expression));
        }

        /// <summary>
        /// 测试获取成员值 - 集合
        /// </summary>
        [Fact]
        public void Test_GetValue_List()
        {
            var list = new List<string> { "a", "b" };
            Expression<Func<Sample, bool>> expression = t => list.Contains(t.StringValue);
            Assert.Equal(list, Lambdas.GetValue(expression));
        }

        /// <summary>
        /// 测试获取成员值 - 静态成员
        /// </summary>
        [Fact]
        public void Test_GetValue_Static()
        {
            Expression<Func<Sample, bool>> expression = t => t.StringValue == Sample.StaticString;
            Assert.Equal("TestStaticString", Lambdas.GetValue(expression));

            expression = t => t.StringValue == Sample.StaticSample.StringValue;
            Assert.Equal("TestStaticSample", Lambdas.GetValue(expression));

            expression = t => t.Test2.StringValue == Sample.StaticString;
            Assert.Equal("TestStaticString", Lambdas.GetValue(expression));

            expression = t => t.Test2.StringValue == Sample.StaticSample.StringValue;
            Assert.Equal("TestStaticSample", Lambdas.GetValue(expression));

            expression = t => t.Test2.StringValue.Contains(Sample.StaticSample.StringValue);
            Assert.Equal("TestStaticSample", Lambdas.GetValue(expression));
        }

        #endregion

        #region GetOperator(获取查询操作符)

        /// <summary>
        /// 获取查询操作符 - 返回类型为Object
        /// </summary>
        [Fact]
        public void Test_GetOperator_Object()
        {
            Expression<Func<Sample, object>> expression = t => t.StringValue == "A";
            Assert.Equal(Operator.Equal, Lambdas.GetOperator(expression));
        }

        /// <summary>
        /// 获取查询操作符 - 返回类型为bool
        /// </summary>
        [Fact]
        public void Test_GetOperator()
        {
            Assert.Null(Lambdas.GetOperator(null));

            Expression<Func<Sample, bool>> expression = t => t.StringValue == "A";
            Assert.Equal(Operator.Equal, Lambdas.GetOperator(expression));

            Expression<Func<Sample, bool>> expression2 = t => t.Test2.IntValue == 1;
            Assert.Equal(Operator.Equal, Lambdas.GetOperator(expression2));

            Expression<Func<Sample, bool>> expression3 = t => t.Test2.Test3.StringValue == "B";
            Assert.Equal(Operator.Equal, Lambdas.GetOperator(expression3));
        }

        /// <summary>
        /// 获取查询操作符 - 运算符
        /// </summary>
        [Fact]
        public void Test_GetOperator_Operation()
        {
            Expression<Func<Sample, bool>> expression = t => t.Test2.IntValue != 1;
            Assert.Equal(Operator.NotEqual, Lambdas.GetOperator(expression));

            expression = t => t.Test2.IntValue > 1;
            Assert.Equal(Operator.Greater, Lambdas.GetOperator(expression));

            expression = t => t.Test2.IntValue < 1;
            Assert.Equal(Operator.Less, Lambdas.GetOperator(expression));

            expression = t => t.Test2.IntValue >= 1;
            Assert.Equal(Operator.GreaterEqual, Lambdas.GetOperator(expression));

            expression = t => t.Test2.IntValue <= 1;
            Assert.Equal(Operator.LessEqual, Lambdas.GetOperator(expression));
        }

        /// <summary>
        /// 获取查询操作符 - 可空类型
        /// </summary>
        [Fact]
        public void Test_GetOperator_Nullable()
        {
            Expression<Func<Sample, bool>> expression = t => t.NullableIntValue == 1;
            Assert.Equal(Operator.Equal, Lambdas.GetOperator(expression));

            expression = t => t.NullableDecimalValue == 1.5M;
            Assert.Equal(Operator.Equal, Lambdas.GetOperator(expression));
        }

        /// <summary>
        /// 获取查询操作符 - 方法
        /// </summary>
        [Fact]
        public void Test_GetOperator_Method()
        {
            Expression<Func<Sample, bool>> expression = t => t.StringValue.Contains("A");
            Assert.Equal(Operator.Contains, Lambdas.GetOperator(expression));

            expression = t => t.Test2.StringValue.EndsWith("B");
            Assert.Equal(Operator.Ends, Lambdas.GetOperator(expression));

            expression = t => t.Test2.Test3.StringValue.StartsWith("C");
            Assert.Equal(Operator.Starts, Lambdas.GetOperator(expression));
        }

        /// <summary>
        /// 获取查询操作符 - 复杂类型
        /// </summary>
        [Fact]
        public void Test_GetOperator_Complex()
        {
            var test = new Sample() { StringValue = "a", Test2 = new Sample2() { StringValue = "b" } };

            Expression<Func<Sample, bool>> expression = t => t.StringValue == test.StringValue;
            Assert.Equal(Operator.Equal, Lambdas.GetOperator(expression));
            Expression<Func<Sample, bool>> expression2 = t => t.StringValue != test.Test2.StringValue;
            Assert.Equal(Operator.NotEqual, Lambdas.GetOperator(expression2));

            Expression<Func<Sample, bool>> expression3 = t => t.StringValue.Contains(test.StringValue);
            Assert.Equal(Operator.Contains, Lambdas.GetOperator(expression3));
            Expression<Func<Sample, bool>> expression4 = t => t.StringValue.EndsWith(test.Test2.StringValue);
            Assert.Equal(Operator.Ends, Lambdas.GetOperator(expression4));
        }

        /// <summary>
        /// 获取查询操作符 - 枚举
        /// </summary>
        [Fact]
        public void Test_GetOperator_Enum()
        {
            var test1 = new Sample { NullableEnumValue = EnumSample.C };

            Expression<Func<Sample, bool>> expression = test => test.EnumValue == EnumSample.D;
            Assert.Equal(Operator.Equal, Lambdas.GetOperator(expression));

            expression = test => test.EnumValue == test1.NullableEnumValue;
            Assert.Equal(Operator.Equal, Lambdas.GetOperator(expression));

            expression = test => test.NullableEnumValue == EnumSample.E;
            Assert.Equal(Operator.Equal, Lambdas.GetOperator(expression));

            expression = test => test.NullableEnumValue == test1.NullableEnumValue;
            Assert.Equal(Operator.Equal, Lambdas.GetOperator(expression));
        }

        /// <summary>
        /// 获取查询操作符 - 集合
        /// </summary>
        [Fact]
        public void Test_GetOperator_List()
        {
            var list = new List<string> { "a", "b" };
            Expression<Func<Sample, bool>> expression = t => list.Contains(t.StringValue);
            Assert.Equal(Operator.Contains, Lambdas.GetOperator(expression));
        }

        /// <summary>
        /// 获取查询操作符 - 静态成员
        /// </summary>
        [Fact]
        public void Test_GetOperator_Static()
        {
            Expression<Func<Sample, bool>> expression = t => t.StringValue == Sample.StaticString;
            Assert.Equal(Operator.Equal, Lambdas.GetOperator(expression));

            expression = t => t.StringValue == Sample.StaticSample.StringValue;
            Assert.Equal(Operator.Equal, Lambdas.GetOperator(expression));

            expression = t => t.Test2.StringValue == Sample.StaticString;
            Assert.Equal(Operator.Equal, Lambdas.GetOperator(expression));

            expression = t => t.Test2.StringValue == Sample.StaticSample.StringValue;
            Assert.Equal(Operator.Equal, Lambdas.GetOperator(expression));

            expression = t => t.Test2.StringValue.Contains(Sample.StaticSample.StringValue);
            Assert.Equal(Operator.Contains, Lambdas.GetOperator(expression));
        }

        #endregion

        #region GetParameter(获取参数)

        /// <summary>
        /// 测试获取参数
        /// </summary>
        [Fact]
        public void Test_GetParameter()
        {
            Assert.Null(Lambdas.GetParameter(null));

            Expression<Func<Sample, object>> expression = test => test.StringValue == "A";
            Assert.Equal("test", Lambdas.GetParameter(expression).ToString());

            expression = test => test.Test2.IntValue == 1;
            Assert.Equal("test", Lambdas.GetParameter(expression).ToString());

            expression = test => test.Test2.Test3.StringValue == "B";
            Assert.Equal("test", Lambdas.GetParameter(expression).ToString());

            expression = test => test.Test2.IntValue;
            Assert.Equal("test", Lambdas.GetParameter(expression).ToString());

            expression = test => test.Test2.Test3.StringValue.Contains("B");
            Assert.Equal("test", Lambdas.GetParameter(expression).ToString());
        }

        #endregion

        #region GetCriteriaCount(获取查询条件个数)

        /// <summary>
        /// 测试获取查询条件个数
        /// </summary>
        [Fact]
        public void Test_GetConditionCount()
        {
            Assert.Equal(0, Lambdas.GetConditionCount(null));

            Expression<Func<Sample, bool>> expression = test => test.StringValue == "A";
            Assert.Equal(1, Lambdas.GetConditionCount(expression));

            expression = test => test.StringValue == "A" && test.StringValue == "B";
            Assert.Equal(2, Lambdas.GetConditionCount(expression));

            expression = test => test.StringValue == "A" || test.StringValue == "B";
            Assert.Equal(2, Lambdas.GetConditionCount(expression));

            expression = test => test.StringValue == "A" && test.StringValue == "B" || test.StringValue == "C";
            Assert.Equal(3, Lambdas.GetConditionCount(expression));

            expression = test => test.Test2.StringValue == "A" && test.StringValue == "B" || test.StringValue == "C";
            Assert.Equal(3, Lambdas.GetConditionCount(expression));

            expression = t => t.StringValue.Contains("A");
            Assert.Equal(1, Lambdas.GetConditionCount(expression));

            expression = t => t.StringValue.Contains("A") && t.StringValue == "A";
            Assert.Equal(2, Lambdas.GetConditionCount(expression));

            expression = t => t.StringValue.Contains("A") || t.Test2.StringValue == "A";
            Assert.Equal(2, Lambdas.GetConditionCount(expression));
        }

        #endregion

        #region GetAttribute(获取特性)

        /// <summary>
        /// 测试获取特性
        /// </summary>
        [Fact]
        public void Test_GetAttribute()
        {
            DisplayAttribute attribute = Lambdas.GetAttribute<Sample, string, DisplayAttribute>(t => t.StringValue);
            Assert.Equal("字符串值", attribute.Name);
        }

        #endregion

        #region GetAttributes(获取特性列表)

        /// <summary>
        /// 测试获取特性列表
        /// </summary>
        [Fact]
        public void Test_GetAttributes()
        {
            IEnumerable<ValidationAttribute> attributes = Lambdas.GetAttributes<Sample, string, ValidationAttribute>(t => t.StringValue);
            Assert.Equal(2, attributes.Count());
        }

        #endregion

        #region Constant(获取常量表达式)

        /// <summary>
        /// 测试获取常量表达式
        /// </summary>
        [Fact]
        public void Test_Constant()
        {
            var constantExpression = Lambdas.Constant(1);
            Assert.Equal(typeof(int), constantExpression.Type);
        }

        /// <summary>
        /// 测试获取常量表达式
        /// </summary>
        [Fact]
        public void Test_Constant_2()
        {
            Expression<Func<Sample, int?>> property = t => t.NullableIntValue;
            var constantExpression = Lambdas.Constant(1, property);
            Assert.Equal(typeof(int?), constantExpression.Type);
        }

        #endregion

        #region Equal(创建等于表达式)

        /// <summary>
        /// 测试创建等于表达式
        /// </summary>
        [Fact]
        public void Test_Equal()
        {
            Expression<Func<Sample, bool>> expected = t => t.IntValue == 1;
            Assert.Equal(expected.ToString(), Lambdas.Equal<Sample>("IntValue", 1).ToString());

            Expression<Func<Sample, bool>> expected2 = t => t.Test2.IntValue == 1;
            Assert.Equal(expected2.ToString(), Lambdas.Equal<Sample>("Test2.IntValue", 1).ToString());
        }

        #endregion

        #region NotEqual(创建不等于表达式)

        /// <summary>
        /// 测试创建不等于表达式
        /// </summary>
        [Fact]
        public void Test_NotEqual()
        {
            Expression<Func<Sample, bool>> expected = t => t.IntValue != 1;
            Assert.Equal(expected.ToString(), Lambdas.NotEqual<Sample>("IntValue", 1).ToString());

            Expression<Func<Sample, bool>> expected2 = t => t.Test2.IntValue != 1;
            Assert.Equal(expected2.ToString(), Lambdas.NotEqual<Sample>("Test2.IntValue", 1).ToString());
        }

        #endregion

        #region Greater(创建大于表达式)

        /// <summary>
        /// 测试创建大于表达式
        /// </summary>
        [Fact]
        public void Test_Greater()
        {
            Expression<Func<Sample, bool>> expected = t => t.IntValue > 1;
            Assert.Equal(expected.ToString(), Lambdas.Greater<Sample>("IntValue", 1).ToString());

            Expression<Func<Sample, bool>> expected2 = t => t.Test2.IntValue > 1;
            Assert.Equal(expected2.ToString(), Lambdas.Greater<Sample>("Test2.IntValue", 1).ToString());
        }

        #endregion

        #region GreaterEqual(创建大于等于表达式)

        /// <summary>
        /// 测试创建大于等于表达式
        /// </summary>
        [Fact]
        public void Test_GreaterEqual()
        {
            Expression<Func<Sample, bool>> expected = t => t.IntValue >= 1;
            Assert.Equal(expected.ToString(), Lambdas.GreaterEqual<Sample>("IntValue", 1).ToString());

            Expression<Func<Sample, bool>> expected2 = t => t.Test2.IntValue >= 1;
            Assert.Equal(expected2.ToString(), Lambdas.GreaterEqual<Sample>("Test2.IntValue", 1).ToString());
        }

        #endregion

        #region Less(创建小于表达式)

        /// <summary>
        /// 测试创建小于表达式
        /// </summary>
        [Fact]
        public void Test_Less()
        {
            Expression<Func<Sample, bool>> expected = t => t.IntValue < 1;
            Assert.Equal(expected.ToString(), Lambdas.Less<Sample>("IntValue", 1).ToString());

            Expression<Func<Sample, bool>> expected2 = t => t.Test2.IntValue < 1;
            Assert.Equal(expected2.ToString(), Lambdas.Less<Sample>("Test2.IntValue", 1).ToString());
        }

        #endregion

        #region LessEqual(创建小于等于表达式)

        /// <summary>
        /// 测试创建小于等于表达式
        /// </summary>
        [Fact]
        public void Test_LessEqual()
        {
            Expression<Func<Sample, bool>> expected = t => t.IntValue <= 1;
            Assert.Equal(expected.ToString(), Lambdas.LessEqual<Sample>("IntValue", 1).ToString());

            Expression<Func<Sample, bool>> expected2 = t => t.Test2.IntValue <= 1;
            Assert.Equal(expected2.ToString(), Lambdas.LessEqual<Sample>("Test2.IntValue", 1).ToString());
        }

        #endregion

        #region Starts(调用StartsWith方法)

        /// <summary>
        /// 测试调用StartsWith方法
        /// </summary>
        [Fact]
        public void Test_Starts()
        {
            Expression<Func<Sample, bool>> expected = t => t.StringValue.StartsWith("A");
            Assert.Equal(expected.ToString(), Lambdas.Starts<Sample>("StringValue", "A").ToString());

            Expression<Func<Sample, bool>> expected2 = t => t.Test2.StringValue.StartsWith("A");
            Assert.Equal(expected2.ToString(), Lambdas.Starts<Sample>("Test2.StringValue", "A").ToString());

            Expression<Func<Sample, bool>> expected3 = t => t.Test2.Test3.StringValue.StartsWith("A");
            Assert.Equal(expected3.ToString(), Lambdas.Starts<Sample>("Test2.Test3.StringValue", "A").ToString());
        }

        #endregion

        #region Ends(调用EndsWith方法)

        /// <summary>
        /// 测试调用EndsWith方法
        /// </summary>
        [Fact]
        public void Test_Ends()
        {
            Expression<Func<Sample, bool>> expected = t => t.StringValue.EndsWith("A");
            Assert.Equal(expected.ToString(), Lambdas.Ends<Sample>("StringValue", "A").ToString());

            Expression<Func<Sample, bool>> expected2 = t => t.Test2.StringValue.EndsWith("A");
            Assert.Equal(expected2.ToString(), Lambdas.Ends<Sample>("Test2.StringValue", "A").ToString());

            Expression<Func<Sample, bool>> expected3 = t => t.Test2.Test3.StringValue.EndsWith("A");
            Assert.Equal(expected3.ToString(), Lambdas.Ends<Sample>("Test2.Test3.StringValue", "A").ToString());
        }

        #endregion

        #region Contains(调用Contains方法)

        /// <summary>
        /// 测试调用Contains方法
        /// </summary>
        [Fact]
        public void Test_Contains()
        {
            Expression<Func<Sample, bool>> expected = t => t.StringValue.Contains("A");
            Assert.Equal(expected.ToString(), Lambdas.Contains<Sample>("StringValue", "A").ToString());

            Expression<Func<Sample, bool>> expected2 = t => t.Test2.StringValue.Contains("A");
            Assert.Equal(expected2.ToString(), Lambdas.Contains<Sample>("Test2.StringValue", "A").ToString());

            Expression<Func<Sample, bool>> expected3 = t => t.Test2.Test3.StringValue.Contains("A");
            Assert.Equal(expected3.ToString(), Lambdas.Contains<Sample>("Test2.Test3.StringValue", "A").ToString());
        }

        #endregion

        #region ParsePredicate(解析为谓词表达式)

        /// <summary>
        /// 测试解析为谓词表达式
        /// </summary>
        [Fact]
        public void Test_ParsePredicate()
        {
            Assert.Equal("t => (t.StringValue == \"A\")", Lambdas.ParsePredicate<Sample2>("StringValue", "A", Operator.Equal).SafeString());
            Assert.Equal("t => t.StringValue.Contains(\"A\")", Lambdas.ParsePredicate<Sample2>("StringValue", "A", Operator.Contains).SafeString());
        }

        #endregion
    }
}
