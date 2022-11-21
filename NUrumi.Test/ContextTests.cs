using System;
using System.Diagnostics;
using System.Numerics;
using FluentAssertions;
using NUnit.Framework;

namespace NUrumi.Test
{
    [TestFixture]
    public class ContextTests
    {
        [Test]
        public void ContextRegistryInitialization()
        {
            var context = new Context<TestRegistry>();
            context.Registry.Test.Should().NotBeNull();
            context.Registry.Test.Index.Should().Be(0);

            context.Registry.Test.Field1.Index.Should().Be(0);
            context.Registry.Test.Field1.Offset.Should().Be(0);
            context.Registry.Test.Field1.ValueSize.Should().Be(4);
            context.Registry.Test.Field1.Name.Should().Be("Field1");

            context.Registry.Test.Field2.Index.Should().Be(1);
            context.Registry.Test.Field2.Offset.Should().Be(4);
            context.Registry.Test.Field2.ValueSize.Should().Be(1);
            context.Registry.Test.Field2.Name.Should().Be("Field2");

            context.Registry.Test.Field3.Index.Should().Be(2);
            context.Registry.Test.Field3.Offset.Should().Be(5);
            context.Registry.Test.Field3.ValueSize.Should().Be(8);
            context.Registry.Test.Field3.Name.Should().Be("Field3");
        }

        [Test]
        public void ContextEntities()
        {
            var context = new Context<TestRegistry>(config: new Config
            {
                InitialEntitiesCapacity = 1,
                InitialReuseEntitiesBarrier = 2
            });

            // Create entities
            var entity1 = context.CreateEntity();
            context.IsAlive(entity1).Should().BeTrue();
            context.LiveEntitiesCount.Should().Be(1);
            EntityId.Index(entity1).Should().Be(0);
            EntityId.Gen(entity1).Should().Be(1);

            var entity2 = context.CreateEntity();
            context.IsAlive(entity2).Should().BeTrue();
            context.LiveEntitiesCount.Should().Be(2);
            EntityId.Index(entity2).Should().Be(1);
            EntityId.Gen(entity2).Should().Be(1);

            var entity3 = context.CreateEntity();
            context.IsAlive(entity3).Should().BeTrue();
            context.LiveEntitiesCount.Should().Be(3);
            EntityId.Index(entity3).Should().Be(2);
            EntityId.Gen(entity3).Should().Be(1);

            // Ensure that new entities does not affect each other
            context.IsAlive(entity1).Should().BeTrue();
            context.IsAlive(entity2).Should().BeTrue();
            context.IsAlive(entity3).Should().BeTrue();

            // Remove entity in a middle position
            context.RemoveEntity(entity2).Should().BeTrue();
            context.IsAlive(entity1).Should().BeTrue();
            context.IsAlive(entity2).Should().BeFalse();
            context.IsAlive(entity3).Should().BeTrue();
            context.LiveEntitiesCount.Should().Be(2);
            context.RecycledEntitiesCount.Should().Be(1);

            // It should not reuse entities till reuse barrier not reached
            var entity4 = context.CreateEntity();
            var entity5 = context.CreateEntity();
            context.LiveEntitiesCount.Should().Be(4);
            context.IsAlive(entity4).Should().BeTrue();
            context.IsAlive(entity5).Should().BeTrue();
            EntityId.Index(entity4).Should().Be(3);
            EntityId.Gen(entity4).Should().Be(1);
            EntityId.Index(entity5).Should().Be(4);
            EntityId.Gen(entity5).Should().Be(1);

            // It should reuse entity index when reuse barrier was reached
            context.RemoveEntity(entity5);
            context.RemoveEntity(entity4);
            context.LiveEntitiesCount.Should().Be(2);
            context.RecycledEntitiesCount.Should().Be(3);
            context.IsAlive(entity1).Should().BeTrue();
            context.IsAlive(entity2).Should().BeFalse();
            context.IsAlive(entity3).Should().BeTrue();
            context.IsAlive(entity4).Should().BeFalse();
            context.IsAlive(entity5).Should().BeFalse();

            var entity6 = context.CreateEntity();
            var entity7 = context.CreateEntity();
            context.IsAlive(entity1).Should().BeTrue();
            context.IsAlive(entity2).Should().BeFalse();
            context.IsAlive(entity3).Should().BeTrue();
            context.IsAlive(entity4).Should().BeFalse();
            context.IsAlive(entity5).Should().BeFalse();
            context.IsAlive(entity6).Should().BeTrue();
            context.IsAlive(entity7).Should().BeTrue();

            context.LiveEntitiesCount.Should().Be(4);
            context.RecycledEntitiesCount.Should().Be(1);

            EntityId.Index(entity6).Should().Be(1);
            EntityId.Gen(entity6).Should().Be(2);
            EntityId.Index(entity7).Should().Be(4);
            EntityId.Gen(entity7).Should().Be(2);
        }

        [Test]
        public void SetFieldValuesByRef()
        {
            var context = new Context<TestRegistry>();
            var position = context.Registry.Position.Value;
            var velocity = context.Registry.Velocity.Value;

            var entityId = context.CreateEntity();
            var entityIndex = EntityId.Index(entityId);
            position.Set(entityIndex, Vector2.Zero);
            velocity.Set(entityIndex, Vector2.One);

            ref var positionRef = ref position.GetRef(entityIndex);
            ref var velocityRef = ref velocity.GetRef(entityIndex);
            positionRef += velocityRef;

            position.Get(entityIndex).Should().Be(Vector2.One);
        }

        [Test]
        public void EntityValues()
        {
            var context = new Context<TestRegistry>();

            var entityId = context.CreateEntity();
            context.Set(_ => _.Test.Field1, entityId, int.MaxValue / 2);
            context.Set(_ => _.Test.Field2, entityId, (byte) 2);
            context.Set(_ => _.Test.Field3, entityId, long.MaxValue / 2);

            context.IsAlive(entityId).Should().BeTrue();
            context.Has(_ => _.Test, entityId);
            context.Get<Field<int>, int>(_ => _.Test.Field1, entityId).Should().Be(int.MaxValue / 2);
            context.Get<Field<byte>, byte>(_ => _.Test.Field2, entityId).Should().Be(2);
            context.Get<Field<long>, long>(_ => _.Test.Field3, entityId).Should().Be(long.MaxValue / 2);
        }

        [Test]
        public void Performance()
        {
            var context = new Context<TestRegistry>();
            var testCmp = context.Registry.Test;
            var cmpStorage = testCmp.Storage;

            var sw = Stopwatch.StartNew();
            for (var i = 0; i < 1000000; i++)
            {
                var entityId = context.CreateEntity();
                var entityIndex = EntityId.Index(entityId);
                testCmp.Field1.Set(entityIndex, i * 2);
                testCmp.Field3.Set(entityIndex, (i * 2) + 1);
            }

            sw.Stop();
            Console.WriteLine(sw.Elapsed.TotalMilliseconds);

            sw.Restart();
            var xx = 0;

            for (var i = 0; i < 1000000; i++)
            {
                var entityId = EntityId.Create(1, i);
                xx += context.Get<Field<int>, int>(_ => _.Test.Field1, entityId);
                xx += (int) context.Get<Field<long>, long>(_ => _.Test.Field3, entityId);
            }

            sw.Stop();
            Console.WriteLine(xx);
            Console.WriteLine(sw.Elapsed.TotalMilliseconds);

            for (var i = 0; i < 1000000; i++)
            {
                var entityId = EntityId.Create(1, i);
                context.Get<Field<int>, int>(_ => _.Test.Field1, entityId).Should().Be(i * 2);
                context.Get<Field<long>, long>(_ => _.Test.Field3, entityId).Should().Be((i * 2) + 1);
            }
        }

        private sealed class TestRegistry : Registry<TestRegistry>
        {
            public TestComponent Test;
            public Position Position;
            public Velocity Velocity;
        }

        private sealed class TestComponent : Component<TestComponent>
        {
            public Field<int> Field1;
            public Field<byte> Field2;
            public Field<long> Field3;
        }

        public sealed class Position : Component<Position>
        {
            public Field<Vector2> Value;
        }

        public sealed class Velocity : Component<Velocity>
        {
            public Field<Vector2> Value;
        }
    }
}