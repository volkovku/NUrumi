using FluentAssertions;
using NUnit.Framework;
using NUrumi.Exceptions;

namespace NUrumi.Test
{
    [TestFixture]
    public class UnsafeComponentStorageTest
    {
        public class TestComponent : Component<TestComponent>
        {
            public Field<int> Field1;
            public Field<int> Field2;
        }

        public class TestRegistry : Registry<TestRegistry>
        {
            public TestComponent Test;
        }

        [Test]
        public void CommonScenario()
        {
            var context = new Context<TestRegistry>();

            var storage = context.Registry.Test.Storage;
            var field1 = context.Registry.Test.Field1.Offset;
            var field2 = context.Registry.Test.Field2.Offset;
            var entity1 = 0;
            var entity2 = 1;
            var entity3 = 3;

            field1.Should().Be(ComponentStorageData.ReservedSize + 0);
            field2.Should().Be(ComponentStorageData.ReservedSize + 4);

            storage.Has(entity1).Should().BeFalse();
            storage.EntitiesCount.Should().Be(0);

            // Set values for 1st entity
            storage.Set(entity1, field1, 1);
            storage.Has(entity1).Should().BeTrue();
            storage.Get<int>(entity1, field1).Should().Be(1);
            storage.EntitiesCount.Should().Be(1);
            
            storage.Set(entity1, field2, 2);
            storage.Has(entity1).Should().BeTrue();
            storage.Get<int>(entity1, field2).Should().Be(2);
            storage.EntitiesCount.Should().Be(1);

            // Set values for 2nd entity
            storage.Set(entity2, field1, 3);
            storage.Has(entity2).Should().BeTrue();
            storage.Get<int>(entity2, field1).Should().Be(3);
            storage.EntitiesCount.Should().Be(2);

            storage.Set(entity2, field2, 4);
            storage.Has(entity2).Should().BeTrue();
            storage.Get<int>(entity2, field2).Should().Be(4);
            storage.EntitiesCount.Should().Be(2);

            // Ensure that 1st entity values does not affected
            storage.Get<int>(entity1, field1).Should().Be(1);
            storage.Get<int>(entity1, field2).Should().Be(2);

            // Remove 1st entity
            storage.Remove(entity1).Should().BeTrue();
            storage.Has(entity1).Should().BeFalse();
            storage.Has(entity2).Should().BeTrue();
            storage.EntitiesCount.Should().Be(1);

            storage.Get<int>(entity2, field1).Should().Be(3);
            storage.Get<int>(entity2, field2).Should().Be(4);

            // Use recycled record to new entity
            storage.Set(entity3, field1, 5);
            storage.Set(entity3, field2, 6);
            storage.Has(entity3).Should().BeTrue();
            storage.Get<int>(entity3, field1).Should().Be(5);
            storage.Get<int>(entity3, field2).Should().Be(6);
            storage.EntitiesCount.Should().Be(2);

            storage.Has(entity1).Should().BeFalse();
            storage.Has(entity2).Should().BeTrue();
            storage.Get<int>(entity2, field1).Should().Be(3);
            storage.Get<int>(entity2, field2).Should().Be(4);

            // Set value for 1st entity again
            storage.Set(entity1, field1, 7);
            storage.Set(entity1, field2, 8);

            storage.Has(entity1).Should().BeTrue();
            storage.Has(entity2).Should().BeTrue();
            storage.Has(entity3).Should().BeTrue();

            storage.Get<int>(entity1, field1).Should().Be(7);
            storage.Get<int>(entity1, field2).Should().Be(8);

            storage.Get<int>(entity2, field1).Should().Be(3);
            storage.Get<int>(entity2, field2).Should().Be(4);

            storage.Get<int>(entity3, field1).Should().Be(5);
            storage.Get<int>(entity3, field2).Should().Be(6);

            storage.EntitiesCount.Should().Be(3);

            // Remove all entities
            storage.Remove(entity1).Should().BeTrue();
            storage.EntitiesCount.Should().Be(2);
            
            storage.Remove(entity2).Should().BeTrue();
            storage.EntitiesCount.Should().Be(1);
            
            storage.Remove(entity3).Should().BeTrue();
            storage.EntitiesCount.Should().Be(0);

            storage.Has(entity1).Should().BeFalse();
            storage.Has(entity2).Should().BeFalse();
            storage.Has(entity3).Should().BeFalse();

            storage
                .Invoking(s => s.Get<int>(entity1, field1))
                .Should().Throw<NUrumiComponentNotFoundException>();

            storage
                .Invoking(s => s.Get<int>(entity1, field2))
                .Should().Throw<NUrumiComponentNotFoundException>();

            storage
                .Invoking(s => s.Get<int>(entity2, field1))
                .Should().Throw<NUrumiComponentNotFoundException>();

            storage
                .Invoking(s => s.Get<int>(entity2, field2))
                .Should().Throw<NUrumiComponentNotFoundException>();

            storage
                .Invoking(s => s.Get<int>(entity3, field1))
                .Should().Throw<NUrumiComponentNotFoundException>();

            storage
                .Invoking(s => s.Get<int>(entity3, field2))
                .Should().Throw<NUrumiComponentNotFoundException>();

            storage.EntitiesCount.Should().Be(0);
        }
    }
}