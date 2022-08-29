using FluentAssertions;
using NUnit.Framework;

namespace NUrumi.Test
{
    [TestFixture]
    public class EntityIdTest
    {
        [Test]
        public void Test()
        {
            var id = EntityId.Create(2, 10);
            EntityId.Gen(id).Should().Be(2);
            EntityId.Index(id).Should().Be(10);
        }
    }
}