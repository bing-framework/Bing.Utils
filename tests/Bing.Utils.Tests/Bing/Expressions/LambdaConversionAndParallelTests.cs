using System.Linq.Expressions;

namespace Bing.Expressions;

/// <summary>覆盖 checked 转换成员解析与 PLINQ 搜索值提取。</summary>
public class LambdaConversionAndParallelTests
{
    [Fact]
    public void CheckedConversion_MemberMetadataMatchesOrdinaryConversion()
    {
        Expression<Func<Model, short>> checkedExpression = model => checked((short)model.Number);
        Expression<Func<Model, short>> ordinaryExpression = model => (short)model.Number;

        foreach (var expression in new Expression<Func<Model, short>>[] { checkedExpression, ordinaryExpression })
        {
            Lambdas.GetName(expression).ShouldBe("Number");
            Lambdas.GetLastName(expression).ShouldBe("Number");
            Lambdas.GetMember(expression).Name.ShouldBe("Number");
            Lambdas.GetType(expression).ShouldBe(typeof(int));
            Lambdas.GetParameter(expression).ShouldBe(expression.Parameters[0]);
        }
    }

    [Fact]
    public void CheckedConversion_GetValueEvaluatesCurrentValueAndKeepsOverflowChecks()
    {
        var captured = 123;
        Expression<Func<Model, bool>> expression = model =>
            checked((short)model.Number) == checked((short)captured);

        Lambdas.GetValue(expression).ShouldBe(123);
        captured = int.MaxValue;
        Should.Throw<OverflowException>(() => Lambdas.GetValue(expression));
        captured = -234;
        Lambdas.GetValue(expression).ShouldBe(-234);
    }

    [Fact]
    public void ParallelContains_ExtractsMembersAndIndependentOperands()
    {
        int[] ids = { 1, 2, 3 };
        Expression<Func<Model, bool>> contains = model => ids.AsParallel().Contains(model.Number);
        Expression<Func<Model, bool>> containsWithComparer = model =>
            ids.AsParallel().Contains(model.Number, EqualityComparer<int>.Default);

        foreach (var expression in new[] { contains, containsWithComparer })
        {
            Lambdas.GetName(expression).ShouldBe("Number");
            Lambdas.GetLastName(expression).ShouldBe("Number");
            Lambdas.GetMember(expression).Name.ShouldBe("Number");
            var source = (ParallelQuery<int>)Lambdas.GetValue(expression);
            source.AsSequential().OrderBy(value => value).SequenceEqual(ids).ShouldBeTrue();
        }

        var capturedValue = 8;
        Expression<Func<Model, bool>> independentValue = model =>
            model.ParallelIds.AsParallel().Contains(capturedValue);
        Lambdas.GetValue(independentValue).ShouldBe(8);
        capturedValue = 9;
        Lambdas.GetValue(independentValue).ShouldBe(9);
    }

    [Fact]
    public void ParallelContains_DoesNotTreatComparerAsQueryValue()
    {
        Expression<Func<Model, bool>> dependentValue = model =>
            model.ParallelIds.AsParallel().Contains(model.Number);
        Expression<Func<Model, bool>> dependentValueWithComparer = model =>
            model.ParallelIds.AsParallel().Contains(model.Number, EqualityComparer<int>.Default);

        Should.Throw<NotSupportedException>(() => Lambdas.GetValue(dependentValue));
        Should.Throw<NotSupportedException>(() => Lambdas.GetValue(dependentValueWithComparer));
    }

    [Fact]
    public void EnumerableAndQueryableContains_KeepValueExtractionBehavior()
    {
        int[] ids = { 2, 4 };
        Expression<Func<Model, bool>> enumerable = model => ids.Contains(model.Number);
        Expression<Func<Model, bool>> queryable = model => ids.AsQueryable().Contains(model.Number);

        Lambdas.GetName(enumerable).ShouldBe("Number");
        Lambdas.GetValue(enumerable).ShouldBeSameAs(ids);
        Lambdas.GetName(queryable).ShouldBe("Number");
        ((IQueryable<int>)Lambdas.GetValue(queryable)).OrderBy(value => value).SequenceEqual(ids).ShouldBeTrue();
    }

    public sealed class Model
    {
        public int Number { get; set; }

        public int[] ParallelIds { get; set; } = Array.Empty<int>();
    }
}
