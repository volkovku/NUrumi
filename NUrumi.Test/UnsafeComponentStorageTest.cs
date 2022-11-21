using FluentAssertions;
using NUnit.Framework;

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

            field1.Should().Be(0);
            field2.Should().Be(4);

            storage.Has(entity1).Should().BeFalse();
            storage.RecordsCount().Should().Be(0);
            storage.RecycledRecordsCount().Should().Be(0);

            // Set values for 1st entity
            storage.Set(entity1, field1, 1);
            storage.Has(entity1).Should().BeTrue();
            storage.Get<int>(entity1, field1).Should().Be(1);
            storage.RecordsCount().Should().Be(1);
            storage.RecycledRecordsCount().Should().Be(0);

            storage.Set(entity1, field2, 2);
            storage.Has(entity1).Should().BeTrue();
            storage.Get<int>(entity1, field2).Should().Be(2);
            storage.RecordsCount().Should().Be(1);
            storage.RecycledRecordsCount().Should().Be(0);

            // Set values for 2nd entity
            storage.Set(entity2, field1, 3);
            storage.Has(entity2).Should().BeTrue();
            storage.Get<int>(entity2, field1).Should().Be(3);
            storage.RecordsCount().Should().Be(2);
            storage.RecycledRecordsCount().Should().Be(0);

            storage.Set(entity2, field2, 4);
            storage.Has(entity2).Should().BeTrue();
            storage.Get<int>(entity2, field2).Should().Be(4);
            storage.RecordsCount().Should().Be(2);
            storage.RecycledRecordsCount().Should().Be(0);

            // Ensure that 1st entity values does not affected
            storage.Get<int>(entity1, field1).Should().Be(1);
            storage.Get<int>(entity1, field2).Should().Be(2);

            // Remove 1st entity
            storage.Remove(entity1).Should().BeTrue();
            storage.Has(entity1).Should().BeFalse();
            storage.Has(entity2).Should().BeTrue();
            storage.RecordsCount().Should().Be(2);
            storage.RecycledRecordsCount().Should().Be(1);

//            storage.Get<int>(entity1, field1).Should().Be(default);
            storage.Get<int>(entity1, field2).Should().Be(default);

            storage.Get<int>(entity2, field1).Should().Be(3);
            storage.Get<int>(entity2, field2).Should().Be(4);

            // Use recycled record to new entity
            storage.Set(entity3, field1, 5);
            storage.Set(entity3, field2, 6);
            storage.Has(entity3).Should().BeTrue();
            storage.Get<int>(entity3, field1).Should().Be(5);
            storage.Get<int>(entity3, field2).Should().Be(6);
            storage.RecordsCount().Should().Be(2);
            storage.RecycledRecordsCount().Should().Be(0);

            storage.Has(entity1).Should().BeFalse();
            storage.Has(entity2).Should().BeTrue();
            storage.Get<int>(entity1, field1).Should().Be(default);
            storage.Get<int>(entity1, field2).Should().Be(default);

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

            storage.RecordsCount().Should().Be(3);
            storage.RecycledRecordsCount().Should().Be(0);

            // Remove all entities
            storage.Remove(entity1).Should().BeTrue();
            storage.Remove(entity2).Should().BeTrue();
            storage.Remove(entity3).Should().BeTrue();

            storage.Has(entity1).Should().BeFalse();
            storage.Has(entity2).Should().BeFalse();
            storage.Has(entity3).Should().BeFalse();

            storage.Get<int>(entity1, field1).Should().Be(default);
            storage.Get<int>(entity1, field2).Should().Be(default);

            storage.Get<int>(entity2, field1).Should().Be(default);
            storage.Get<int>(entity2, field2).Should().Be(default);

            storage.Get<int>(entity3, field1).Should().Be(default);
            storage.Get<int>(entity3, field2).Should().Be(default);

            storage.RecordsCount().Should().Be(3);
            storage.RecycledRecordsCount().Should().Be(3);
        }

        // [Test]
        // public void Test()
        // {
        //     var storage = new ComponentStorageData(
        //         sizeof(int) * 2,
        //         10000000,
        //         512,
        //         512);
        //
        //     var sw = Stopwatch.StartNew();
        //     for (var i = 0; i < 1000000; i++)
        //     {
        //         storage.Set(i, 0, i * 2);
        //         storage.Set(i, sizeof(int), (i * 2) + 1);
        //     }
        //
        //     sw.Stop();
        //     Console.WriteLine(sw.Elapsed.TotalMilliseconds);
        //
        //     sw.Restart();
        //     var xx = 0;
        //
        //     for (var i = 0; i < 1000000; i++)
        //     {
        //         xx += storage.Get<int>(i, 0);
        //         xx += storage.Get<int>(i, sizeof(int));
        //     }
        //
        //     sw.Stop();
        //     Console.WriteLine(xx);
        //     Console.WriteLine(sw.Elapsed.TotalMilliseconds);
        //
        //     for (var i = 0; i < 1000000; i++)
        //     {
        //         Assert.AreEqual(i * 2, storage.Get<int>(i, 0));
        //         Assert.AreEqual((i * 2) + 1, storage.Get<int>(i, sizeof(int)));
        //     }
        // }
        //
        // [Test]
        // public void TestLeo()
        // {
        //     var world = new EcsWorld();
        //     var pool = world.GetPool<Component>();
        //     for (var i = 0; i < 10000000; i++)
        //     {
        //         world.NewEntity();
        //     }
        //
        //     var sw = Stopwatch.StartNew();
        //     for (var i = 0; i < 1000000; i++)
        //     {
        //         ref var c = ref pool.Add(i);
        //         c.X = i * 2;
        //         c.Y = i * 2 + 1;
        //     }
        //
        //     sw.Stop();
        //     Console.WriteLine(sw.Elapsed.TotalMilliseconds);
        //
        //     sw.Restart();
        //     var xx = 0;
        //
        //     for (var i = 0; i < 1000000; i++)
        //     {
        //         ref var c = ref pool.Get(i);
        //         xx += c.X;
        //         xx += c.Y;
        //     }
        //
        //     sw.Stop();
        //     Console.WriteLine(xx);
        //     Console.WriteLine(sw.Elapsed.TotalMilliseconds);
        // }
        
        private struct Component
        {
            public int X;
            public int Y;
        }
    }
}