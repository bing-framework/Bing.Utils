using System.Linq.Expressions;
using Bing.Tests.Samples;

namespace Bing.Expressions;

public class LambdaParsingRegressionTests
{
    [Fact]
    public void GetGroupPredicates_MixedGroups_RebuildsSameResultsAndEvaluationOrder()
    {
        Expression<Func<PredicateProbe, bool>> andWithNestedOr = probe =>
            probe.Read(0) && (probe.Read(1) || probe.Read(2));
        Expression<Func<PredicateProbe, bool>> nestedOrWithAnd = probe =>
            (probe.Read(0) || probe.Read(1)) && probe.Read(2);
        Expression<Func<PredicateProbe, bool>> twoConjunctions = probe =>
            (probe.Read(0) && probe.Read(1)) || (probe.Read(2) && probe.Read(3));
        Expression<Func<PredicateProbe, bool>> orWithNestedAnd = probe =>
            probe.Read(0) || (probe.Read(1) && probe.Read(2));

        AssertEquivalentForAllInputs(andWithNestedOr, 3);
        AssertEquivalentForAllInputs(nestedOrWithAnd, 3);
        AssertEquivalentForAllInputs(twoConjunctions, 4);
        AssertEquivalentForAllInputs(orWithNestedAnd, 3);

        var groups = Lambdas.GetGroupPredicates(andWithNestedOr);
        groups.Count.ShouldBe(1);
        groups[0].Count.ShouldBe(2);
        groups[0][1].NodeType.ShouldBe(ExpressionType.OrElse);

        groups = Lambdas.GetGroupPredicates(nestedOrWithAnd);
        groups.Count.ShouldBe(1);
        groups[0].Count.ShouldBe(2);
        groups[0][0].NodeType.ShouldBe(ExpressionType.OrElse);
    }

    [Fact]
    public void GetGroupPredicates_PureAndAndOr_KeepExpectedGroupShape()
    {
        Expression<Func<PredicateProbe, bool>> allAnd = probe =>
            probe.Read(0) && probe.Read(1) && probe.Read(2);
        Expression<Func<PredicateProbe, bool>> allOr = probe =>
            probe.Read(0) || probe.Read(1) || probe.Read(2);

        var andGroups = Lambdas.GetGroupPredicates(allAnd);
        andGroups.Count.ShouldBe(1);
        andGroups[0].Count.ShouldBe(3);

        var orGroups = Lambdas.GetGroupPredicates(allOr);
        orGroups.Count.ShouldBe(3);
        orGroups.All(group => group.Count == 1).ShouldBeTrue();
    }

    [Fact]
    public void GetGroupPredicates_UserDefinedLogicalOperators_RemainLeafExpressions()
    {
        Expression<Func<OverloadedBoolean, OverloadedBoolean>> andExpression = value => value && value;
        Expression<Func<OverloadedBoolean, OverloadedBoolean>> orExpression = value => value || value;

        var andGroups = Lambdas.GetGroupPredicates(andExpression);
        andGroups.Count.ShouldBe(1);
        andGroups[0].Count.ShouldBe(1);
        andGroups[0][0].ShouldBe(andExpression.Body);

        var orGroups = Lambdas.GetGroupPredicates(orExpression);
        orGroups.Count.ShouldBe(1);
        orGroups[0].Count.ShouldBe(1);
        orGroups[0][0].ShouldBe(orExpression.Body);
    }

    [Fact]
    public void GetValue_ParameterIndependentExpressions_EvaluatesValuesAndConversions()
    {
        var holder = new CapturedHolder
        {
            Child = new CapturedHolder { Value = 23 }
        };
        Expression<Func<Sample, bool>> nestedCapturedField = sample => sample.IntValue == holder.Child.Value;
        Expression<Func<Sample, bool>> staticField = sample => sample.IntValue == ValueHolder.StaticValue;
        var capturedValue = 7;
        Expression<Func<Sample, bool>> convertedValue = sample => sample.LongValue == (long)capturedValue;
        Expression<Func<Sample, bool>> calculatedValue = sample => sample.IntValue == Math.Abs(-11);

        Lambdas.GetValue(nestedCapturedField).ShouldBe(23);
        Lambdas.GetValue(staticField).ShouldBe(ValueHolder.StaticValue);
        Lambdas.GetValue(convertedValue).ShouldBe(7L);
        Lambdas.GetValue(calculatedValue).ShouldBe(11);
    }

    [Fact]
    public void GetValue_NegatedCapturedBoolean_EvaluatesOperandWhileKeepingQueryConvention()
    {
        var capturedValue = false;
        Expression<Func<Sample, bool>> capturedNegation = sample => sample.BoolValue == !capturedValue;
        Expression<Func<Sample, bool>> directQueryBoolean = sample => sample.BoolValue;
        Expression<Func<Sample, bool>> negatedQueryBoolean = sample => !sample.BoolValue;

        Lambdas.GetValue(capturedNegation).ShouldBe(true);
        Lambdas.GetValue(directQueryBoolean).ShouldBe(true);
        Lambdas.GetValue(negatedQueryBoolean).ShouldBe(false);
    }

    [Fact]
    public void GetValue_ComparisonWithParameterOnRight_AndActualNull_SelectsValueSide()
    {
        Expression<Func<Sample, bool>> reversedComparison = sample => 1 == sample.IntValue;
        string nullValue = null!;
        Expression<Func<Sample, bool>> nullComparison = sample => sample.StringValue == nullValue;

        Lambdas.GetValue(reversedComparison).ShouldBe(1);
        Lambdas.GetValue(nullComparison).ShouldBeNull();
    }

    [Fact]
    public void GetValue_ComparisonValueSubtrees_AreEvaluatedAsTheirActualValues()
    {
        var text = "match";
        var number = 2;
        Expression<Func<Sample, bool>> stringSearch = sample => sample.BoolValue == text.Contains("a");
        Expression<Func<Sample, bool>> closedBooleanComparison = sample => sample.BoolValue == (number > 3);

        Lambdas.GetValue(stringSearch).ShouldBe(true);
        Lambdas.GetValue(closedBooleanComparison).ShouldBe(false);
    }

    [Fact]
    public void GetValue_EnumComparisons_PreserveNullableEnumValuesInBothOperandOrders()
    {
        var holder = new Sample { NullableEnumValue = EnumSample.C };
        Expression<Func<Sample, bool>> nonNullableQuery = sample => sample.EnumValue == holder.NullableEnumValue;
        Expression<Func<Sample, bool>> nullableQuery = sample => sample.NullableEnumValue == holder.NullableEnumValue;
        Expression<Func<Sample, bool>> reversedQuery = sample => holder.NullableEnumValue == sample.EnumValue;

        Lambdas.GetValue(nonNullableQuery).ShouldBe(EnumSample.C);
        Lambdas.GetValue(nullableQuery).ShouldBe(EnumSample.C);
        Lambdas.GetValue(reversedQuery).ShouldBe(EnumSample.C);

        holder.NullableEnumValue = null;

        Lambdas.GetValue(nonNullableQuery).ShouldBeNull();
        Lambdas.GetValue(nullableQuery).ShouldBeNull();
        Lambdas.GetValue(reversedQuery).ShouldBeNull();
    }

    [Fact]
    public void GetValue_ComplexQuerySide_OnlyEvaluatesIndependentValue()
    {
        Expression<Func<Sample, bool>> chainedMethods = sample => sample.StringValue.Trim().ToUpper() == "VALUE";
        Expression<Func<Sample, bool>> checkedNumber = sample => checked((long)sample.IntValue) == 7L;

        Lambdas.GetValue(chainedMethods).ShouldBe("VALUE");
        Lambdas.GetValue(checkedNumber).ShouldBe(7L);
    }

    [Fact]
    public void GetValue_UnsupportedParameterDependentExpression_ThrowsNotSupportedException()
    {
        Expression<Func<Sample, bool>> expression = sample => sample.IntValue > Math.Abs(sample.IntValue);

        Should.Throw<NotSupportedException>(() => Lambdas.GetValue(expression));
    }

    [Fact]
    public void GetValue_StringSearchMethods_KeepExtractingQueryArgument()
    {
        Expression<Func<Sample, bool>> contains = sample => sample.StringValue.Contains("needle");
        Expression<Func<Sample, bool>> startsWith = sample => sample.StringValue.StartsWith("prefix");
        Expression<Func<Sample, bool>> endsWith = sample => sample.StringValue.EndsWith("suffix");

        Lambdas.GetValue(contains).ShouldBe("needle");
        Lambdas.GetValue(startsWith).ShouldBe("prefix");
        Lambdas.GetValue(endsWith).ShouldBe("suffix");
    }

    [Theory]
    [InlineData("abc", true)]
    [InlineData("xyz", false)]
    public void GetValue_ContainsWithComputedArgument_ReturnsBooleanValue(string text, bool expected)
    {
        Expression<Func<CollectionSample, bool>> instance = sample => sample.Values.Contains(text.Contains("a"));
        Expression<Func<CollectionSample, bool>> extension = sample => Enumerable.Contains(sample.Values, text.Contains("a"));

        Lambdas.GetValue(instance).ShouldBe(expected);
        Lambdas.GetValue(extension).ShouldBe(expected);
    }

    [Fact]
    public void GetValue_ContainsWithComputedCollection_ReturnsEvaluatedSequence()
    {
        var values = new[] { 1, 2, 3 };
        Expression<Func<Sample, bool>> expression = sample => values.Where(value => value > 1).Contains(sample.IntValue);

        ((IEnumerable<int>)Lambdas.GetValue(expression)).ShouldBe(new[] { 2, 3 });
    }

    [Fact]
    public void GetValue_MemberFastPath_ReadsLatestValuesAndCallsEachGetterOnce()
    {
        var captured = 7;
        Expression<Func<Sample, bool>> field = sample => sample.IntValue == captured;
        Lambdas.GetValue(field).ShouldBe(7);
        captured = 9;
        Lambdas.GetValue(field).ShouldBe(9);

        var holder = new CountingValues { Stored = 11 };
        Expression<Func<Sample, bool>> property = sample => sample.IntValue == holder.Child.Number;
        Lambdas.GetValue(property).ShouldBe(11);
        holder.Reads.ShouldBe(2);
        holder.Stored = 13;
        Lambdas.GetValue(property).ShouldBe(13);
        holder.Reads.ShouldBe(4);

        Expression<Func<Sample, bool>> fallback = sample => sample.IntValue == holder.Child.Number + 1;
        Lambdas.GetValue(fallback).ShouldBe(14);
        holder.Reads.ShouldBe(6);
    }

    [Fact]
    public void GetValue_MemberFastPath_PreservesNullAndGetterExceptions()
    {
        CountingValues missing = null!;
        Expression<Func<int>> nullTarget = () => missing.Number;
        Should.Throw<NullReferenceException>(() => Lambdas.GetValue(nullTarget));
        var failure = new InvalidOperationException("getter failed");
        var holder = new ThrowingValue(failure);
        Expression<Func<int>> throwing = () => holder.Number;
        Should.Throw<InvalidOperationException>(() => Lambdas.GetValue(throwing)).ShouldBeSameAs(failure);
    }

    [Fact]
    public void GetValue_NullableAndMutableStructReceivers_KeepInterpreterSemantics()
    {
        int? optional = null;
        Expression<Func<bool>> hasValue = () => optional.HasValue;
        Expression<Func<int>> value = () => optional.Value;
        Lambdas.GetValue(hasValue).ShouldBe(false);
        Should.Throw<InvalidOperationException>(() => Lambdas.GetValue(value));
        optional = 5;
        Lambdas.GetValue(hasValue).ShouldBe(true);
        Lambdas.GetValue(value).ShouldBe(5);

        var holder = new StructHolder();
        Expression<Func<int>> mutating = () => holder.Value.Next;
        var expected = mutating.Compile(preferInterpretation: true)();
        var expectedState = holder.Value.Count;
        holder.Value = default;
        Lambdas.GetValue(mutating).ShouldBe(expected);
        holder.Value.Count.ShouldBe(expectedState);
    }

    [Fact]
    public void GetValue_BoundParameters_AreNotMistakenForQueryParameters()
    {
        var local = Expression.Variable(typeof(int), "local");
        var block = Expression.Block(new[] { local }, Expression.Assign(local, Expression.Constant(4)),
            Expression.Add(local, Expression.Constant(1)));
        Lambdas.GetValue(block).ShouldBe(5);

        var error = Expression.Parameter(typeof(Exception), "error");
        var guarded = Expression.TryCatch(Expression.Throw(Expression.Constant(new Exception("error")), typeof(string)),
            Expression.Catch(error, Expression.Property(error, nameof(Exception.Message))));
        Lambdas.GetValue(guarded).ShouldBe("error");

        var values = new[] { 1, 2, 3 };
        Expression<Func<Sample, bool>> independent = sample => sample.BoolValue == values.Any(item => item > 1);
        Expression<Func<Sample, bool>> dependent = sample => sample.BoolValue == values.Any(item => item > sample.IntValue);
        Lambdas.GetValue(independent).ShouldBe(true);
        Should.Throw<NotSupportedException>(() => Lambdas.GetValue(dependent));
    }

    [Fact]
    public void GetConditionCount_NegatedGroups_CountsLeavesWithoutExpandingMethodBodies()
    {
        Expression<Func<Sample, bool>> and = sample => !(sample.IntValue > 0 && sample.IntValue < 10);
        Expression<Func<Sample, bool>> mixed = sample => !(sample.BoolValue || !(sample.IntValue > 0 && sample.IntValue < 10));
        Expression<Func<Sample, object>> boxed = sample => !(sample.IntValue > 0 && sample.IntValue < 10);
        Expression<Func<Sample, bool>> method = sample => !new[] { sample.IntValue }.Any(item => item > 0 && item < 10);
        Expression<Func<OverloadedBoolean, bool>> overloaded = value => !value;

        Lambdas.GetConditionCount(and).ShouldBe(2);
        Lambdas.GetConditionCount(mixed).ShouldBe(3);
        Lambdas.GetConditionCount(boxed).ShouldBe(2);
        Lambdas.GetConditionCount(method).ShouldBe(1);
        Lambdas.GetConditionCount(overloaded).ShouldBe(1);
    }

    [Fact]
    public void GetConditionCount_OnlyCountsTopLevelLogicalPredicateLeaves()
    {
        Expression<Func<Sample, bool>> operatorText = sample => sample.StringValue == "AndAlso";
        Expression<Func<Sample, bool>> methodText = sample => sample.StringValue.Contains("OrElse");
        Expression<Func<Sample, bool>> combined = sample =>
            sample.StringValue.Contains("AndAlso") && sample.IntValue > 0 || sample.BoolValue;
        Expression<Func<Sample, bool>> nestedMethod = sample =>
            new[] { sample.BoolValue }.Any(value => value && !value);

        Lambdas.GetConditionCount(operatorText).ShouldBe(1);
        Lambdas.GetConditionCount(methodText).ShouldBe(1);
        Lambdas.GetConditionCount(combined).ShouldBe(3);
        Lambdas.GetConditionCount(nestedMethod).ShouldBe(1);
    }

    private static void AssertEquivalentForAllInputs(Expression<Func<PredicateProbe, bool>> expression,
        int inputCount)
    {
        var groups = Lambdas.GetGroupPredicates(expression);
        var groupExpressions = groups.Select(group => group.Aggregate(
            (left, right) => Expression.AndAlso(left, right)));
        var body = groupExpressions.Aggregate((left, right) => Expression.OrElse(left, right));
        var rebuilt = Expression.Lambda<Func<PredicateProbe, bool>>(body, expression.Parameters).Compile();
        var original = expression.Compile();

        for (var bits = 0; bits < 1 << inputCount; bits++)
        {
            var values = Enumerable.Range(0, inputCount)
                .Select(index => (bits & (1 << index)) != 0)
                .ToArray();
            var originalProbe = new PredicateProbe(values);
            var rebuiltProbe = new PredicateProbe(values);

            rebuilt(rebuiltProbe).ShouldBe(original(originalProbe));
            rebuiltProbe.Invocations.ShouldBe(originalProbe.Invocations);
        }
    }

    private sealed class PredicateProbe
    {
        private readonly bool[] _values;

        public PredicateProbe(bool[] values) => _values = values;

        public List<int> Invocations { get; } = new();

        public bool Read(int index)
        {
            Invocations.Add(index);
            return _values[index];
        }
    }

    private sealed class CapturedHolder
    {
        public CapturedHolder Child = null!;

        public int Value;
    }

    private sealed class CollectionSample
    {
        public List<bool> Values { get; } = new();
    }

    private sealed class CountingValues
    {
        public int Stored;
        public int Reads;
        public CountingValues Child { get { Reads++; return this; } }
        public int Number { get { Reads++; return Stored; } }
    }

    private sealed class ThrowingValue
    {
        private readonly Exception _failure;
        public ThrowingValue(Exception failure) => _failure = failure;
        public int Number => throw _failure;
    }

    private sealed class StructHolder { public MutableValue Value; }
    private struct MutableValue
    {
        public int Count;
        public int Next => ++Count;
    }

    private static class ValueHolder
    {
        public static readonly int StaticValue = 31;
    }

    private sealed class OverloadedBoolean
    {
        public static bool operator !(OverloadedBoolean value) => true;

        public static bool operator true(OverloadedBoolean value) => true;

        public static bool operator false(OverloadedBoolean value) => false;

        public static OverloadedBoolean operator &(OverloadedBoolean left, OverloadedBoolean right) => left;

        public static OverloadedBoolean operator |(OverloadedBoolean left, OverloadedBoolean right) => left;
    }
}
