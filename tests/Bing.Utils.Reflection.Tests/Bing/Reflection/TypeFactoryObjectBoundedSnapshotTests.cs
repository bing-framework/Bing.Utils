using System.Collections;
using System.Collections.Generic;

namespace Bing.Reflection;

[Trait("Bing.Reflection", "DynamicTypeFactory")]
public class TypeFactoryObjectBoundedSnapshotTests
{
    [Fact]
    public void CreateInstance_OverLimitDictionary_StopsAtFirstExcessEntry()
    {
        var values = new UnderreportedCountDictionary(TypeFactory.MaxProperties + 1000);

        var exception = Should.Throw<ArgumentException>(() => TypeVisit.CreateInstance(values));

        exception.ParamName.ShouldBe("values");
        values.EntriesRead.ShouldBe(TypeFactory.MaxProperties + 1);
    }

    private sealed class UnderreportedCountDictionary : Dictionary<string, object>, IDictionary<string, object>
    {
        private readonly int _entryCount;

        internal UnderreportedCountDictionary(int entryCount)
        {
            _entryCount = entryCount;
        }

        internal int EntriesRead { get; private set; }

        int ICollection<KeyValuePair<string, object>>.Count => 0;

        IEnumerator<KeyValuePair<string, object>> IEnumerable<KeyValuePair<string, object>>.GetEnumerator()
        {
            for (var index = 0; index < _entryCount; index++)
            {
                EntriesRead++;
                yield return new KeyValuePair<string, object>($"Property{index}", index);
            }
        }

        IEnumerator IEnumerable.GetEnumerator() =>
            ((IEnumerable<KeyValuePair<string, object>>)this).GetEnumerator();
    }
}
