﻿using System.Collections.Generic;
using System.Numerics;
using FluentAssertions;
using NUnit.Framework;
using NUrumi.Test.Model;

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
            context.Registry.Test.Field1.Offset.Should().Be(ComponentStorageData.ReservedSize + 0);
            context.Registry.Test.Field1.ValueSize.Should().Be(4);
            context.Registry.Test.Field1.Name.Should().Be("Field1");

            context.Registry.Test.Field2.Index.Should().Be(1);
            context.Registry.Test.Field2.Offset.Should().Be(ComponentStorageData.ReservedSize + 4);
            context.Registry.Test.Field2.ValueSize.Should().Be(1);
            context.Registry.Test.Field2.Name.Should().Be("Field2");

            context.Registry.Test.Field3.Index.Should().Be(2);
            context.Registry.Test.Field3.Offset.Should().Be(ComponentStorageData.ReservedSize + 5);
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
            context.IsAlive(entity1, out var entity1Gen).Should().BeTrue();
            context.LiveEntitiesCount.Should().Be(1);
            entity1.Should().Be(1);
            entity1Gen.Should().Be(1);

            var entity2 = context.CreateEntity();
            context.IsAlive(entity2, out var entity2Gen).Should().BeTrue();
            context.LiveEntitiesCount.Should().Be(2);
            entity2.Should().Be(2);
            entity2Gen.Should().Be(1);

            var entity3 = context.CreateEntity();
            context.IsAlive(entity3, out var entity3Gen).Should().BeTrue();
            context.LiveEntitiesCount.Should().Be(3);
            entity3.Should().Be(3);
            entity3Gen.Should().Be(1);

            // Ensure that new entities does not affect each other
            context.IsAlive(entity1, out _).Should().BeTrue();
            context.IsAlive(entity2, out _).Should().BeTrue();
            context.IsAlive(entity3, out _).Should().BeTrue();

            // Remove entity in a middle position
            context.RemoveEntity(entity2).Should().BeTrue();
            context.IsAlive(entity1, out _).Should().BeTrue();
            context.IsAlive(entity2, out _).Should().BeFalse();
            context.IsAlive(entity3, out _).Should().BeTrue();
            context.LiveEntitiesCount.Should().Be(2);
            context.RecycledEntitiesCount.Should().Be(1);

            // It should not reuse entities till reuse barrier not reached
            var entity4 = context.CreateEntity();
            var entity5 = context.CreateEntity();
            context.LiveEntitiesCount.Should().Be(4);
            context.IsAlive(entity4, out var entity4Gen).Should().BeTrue();
            context.IsAlive(entity5, out var entity5Gen).Should().BeTrue();
            entity4.Should().Be(4);
            entity4Gen.Should().Be(1);
            entity5.Should().Be(5);
            entity5Gen.Should().Be(1);

            // It should reuse entity index when reuse barrier was reached
            context.RemoveEntity(entity5);
            context.RemoveEntity(entity4);
            context.LiveEntitiesCount.Should().Be(2);
            context.RecycledEntitiesCount.Should().Be(3);
            context.IsAlive(entity1, out _).Should().BeTrue();
            context.IsAlive(entity2, out _).Should().BeFalse();
            context.IsAlive(entity3, out _).Should().BeTrue();
            context.IsAlive(entity4, out _).Should().BeFalse();
            context.IsAlive(entity5, out _).Should().BeFalse();

            var entity6 = context.CreateEntity();
            var entity7 = context.CreateEntity();
            context.IsAlive(entity1, entity1Gen).Should().BeTrue();
            context.IsAlive(entity2, entity2Gen).Should().BeFalse();
            context.IsAlive(entity3, entity3Gen).Should().BeTrue();
            context.IsAlive(entity4, entity4Gen).Should().BeFalse();
            context.IsAlive(entity5, entity5Gen).Should().BeFalse();
            context.IsAlive(entity6, out var entity6Gen).Should().BeTrue();
            context.IsAlive(entity7, out var entity7Gen).Should().BeTrue();

            context.LiveEntitiesCount.Should().Be(4);
            context.RecycledEntitiesCount.Should().Be(1);

            entity6.Should().Be(2);
            entity6Gen.Should().Be(2);
            entity7.Should().Be(5);
            entity7Gen.Should().Be(2);
        }

        [Test]
        public void SetFieldValuesByRef()
        {
            var context = new Context<TestRegistry>();
            var position = context.Registry.Position;
            var velocity = context.Registry.Velocity;

            var entityId = context.CreateEntity();
            position.Set(entityId, Vector2.Zero);
            velocity.Set(entityId, Vector2.One);

            ref var positionRef = ref position.GetRef(entityId);
            ref var velocityRef = ref velocity.GetRef(entityId);
            positionRef += velocityRef;

            position.Get(entityId).Should().Be(Vector2.One);
        }

        [Test]
        public void EntityValues()
        {
            var context = new Context<TestRegistry>();
            var testComponent = context.Registry.Test;
            var field1 = context.Registry.Test.Field1;
            var field2 = context.Registry.Test.Field2;
            var field3 = context.Registry.Test.Field3;

            var entityId = context.CreateEntity();
            field1.Set(entityId, int.MaxValue / 2);
            field2.Set(entityId, 2);
            field3.Set(entityId, long.MaxValue / 2);

            context.IsAlive(entityId, out _).Should().BeTrue();
            testComponent.IsAPartOf(entityId).Should().BeTrue();
            field1.Get(entityId).Should().Be(int.MaxValue / 2);
            field2.Get(entityId).Should().Be(2);
            field3.Get(entityId).Should().Be(long.MaxValue / 2);
        }


        [Test]
        public void QueriesDeferredOperations()
        {
            var context = new Context<TestRegistry>();
            var positionComponent = context.Registry.Position;
            var position = context.Registry.Position;
            var velocity = context.Registry.Velocity;
            var group = context.CreateGroup(
                GroupFilter
                    .Include(context.Registry.Position)
                    .Include(context.Registry.Velocity));

            var entitiesCount = 100;
            for (var i = 0; i < entitiesCount; i++)
            {
                var entity = context.CreateEntity();
                entity.Set(position, Vector2.One);
                entity.Set(velocity, Vector2.Zero);
                group.EntitiesCount.Should().Be(i + 1);
            }

            var ix = 0;
            foreach (var entity in group)
            {
                if (ix % 2 == 0)
                {
                    entity.Remove(positionComponent);
                }

                group.EntitiesCount.Should().Be(entitiesCount);
                ix += 1;
            }

            group.EntitiesCount.Should().Be(entitiesCount / 2);
        }

        [Test]
        public void IndexField()
        {
            var context = new Context<TestRegistry>();
            var parentComponent = context.Registry.Parent;
            var parent = parentComponent.Value;

            var parentEntity = context.CreateEntity();

            var childEntity1 = context.CreateEntity();
            childEntity1.Set(parent, parentEntity);

            var childEntity2 = context.CreateEntity();
            childEntity2.Set(parent, parentEntity);

            var childEntity3 = context.CreateEntity();
            childEntity3.Set(parent, parentEntity);

            var children = new List<int>();
            foreach (var child in parent.GetEntitiesAssociatedWith(parentEntity))
            {
                children.Add(child);
            }

            children.Count.Should().Be(3);
            children.Contains(childEntity1).Should().BeTrue();
            children.Contains(childEntity2).Should().BeTrue();
            children.Contains(childEntity3).Should().BeTrue();

            parentComponent.RemoveFrom(childEntity2);

            children.Clear();
            foreach (var child in parent.GetEntitiesAssociatedWith(parentEntity))
            {
                children.Add(child);
            }

            children.Count.Should().Be(2);
            children.Contains(childEntity1).Should().BeTrue();
            children.Contains(childEntity2).Should().BeFalse();
            children.Contains(childEntity3).Should().BeTrue();
        }
    }
}