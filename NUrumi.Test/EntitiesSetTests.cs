using FluentAssertions;
using NUnit.Framework;

namespace NUrumi.Test
{
    [TestFixture]
    public class EntitiesSetTests
    {
        [Test]
        public void EntitiesSetTest()
        {
            var set = new EntitiesSet(10);
            set.EntitiesCount.Should().Be(0);
            set.Has(1).Should().BeFalse();

            set.Add(1).Should().Be(EntitiesSet.Applied);
            set.Has(1).Should().BeTrue();
            set.Add(1).Should().Be(EntitiesSet.AppliedEarly);

            set.Remove(1).Should().Be(EntitiesSet.Applied);
            set.Has(1).Should().BeFalse();
            set.Remove(1).Should().Be(EntitiesSet.AppliedEarly);

            for (var i = 0; i < 10; i++)
            {
                set.Add(i).Should().Be(EntitiesSet.Applied);
                set.Has(i).Should().BeTrue();
                set.EntitiesCount.Should().Be(i + 1);
            }

            set.Clear();
            set.EntitiesCount.Should().Be(0);

            for (var i = 0; i < 10; i++)
            {
                set.Has(i).Should().BeFalse();
            }
        }
    }
}