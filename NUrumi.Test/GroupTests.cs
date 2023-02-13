using System;
using System.Numerics;
using FluentAssertions;
using NUnit.Framework;
using NUrumi.Test.Model;

namespace NUrumi.Test
{
    [TestFixture]
    public class GroupTests
    {
        [Test]
        public void GroupsShouldCorrectManageEntities()
        {
            var context = new Context<TestRegistry>();
            var position = context.Registry.Position;
            var velocity = context.Registry.Velocity;

            var groupPosition = context.CreateGroup(GroupFilter.Include(context.Registry.Position));
            var groupVelocity = context.CreateGroup(GroupFilter.Include(context.Registry.Velocity));
            var groupWithBoth = context.CreateGroup(
                GroupFilter
                    .Include(context.Registry.Position)
                    .Include(context.Registry.Velocity));

            var entityWithPosition = context.CreateEntity();
            position.Set(entityWithPosition, Vector2.One);
            groupWithBoth.EntitiesCount.Should().Be(0);

            var entityWithVelocity = context.CreateEntity();
            velocity.Set(entityWithVelocity, Vector2.One);
            groupWithBoth.EntitiesCount.Should().Be(0);

            var entityWithBoth1 = context.CreateEntity();
            position.Set(entityWithBoth1, Vector2.Zero);
            velocity.Set(entityWithBoth1, Vector2.One);
            groupWithBoth.EntitiesCount.Should().Be(1);

            var entityWithBoth2 = context.CreateEntity();
            position.Set(entityWithBoth2, Vector2.One);
            velocity.Set(entityWithBoth2, Vector2.Zero);
            groupWithBoth.EntitiesCount.Should().Be(2);

            var entitiesInGroup = (int[]) null;
            var entitiesInGroupCount = groupWithBoth.GetEntities(ref entitiesInGroup);
            entitiesInGroupCount.Should().Be(2);
            entitiesInGroup.Length.Should().Be(2);
            entitiesInGroup[0].Should().Be(entityWithBoth1);
            entitiesInGroup[1].Should().Be(entityWithBoth2);

            context.Registry.Velocity.RemoveFrom(entityWithBoth1).Should().Be(true);
            groupWithBoth.EntitiesCount.Should().Be(1);

            context.Registry.Position.RemoveFrom(entityWithBoth2).Should().Be(true);
            groupWithBoth.EntitiesCount.Should().Be(0);

            groupPosition.EntitiesCount.Should().Be(2);
            groupPosition.GetEntities(ref entitiesInGroup);
            entitiesInGroup[0].Should().Be(entityWithPosition);
            entitiesInGroup[1].Should().Be(entityWithBoth1);

            groupVelocity.EntitiesCount.Should().Be(2);
            groupVelocity.GetEntities(ref entitiesInGroup);
            entitiesInGroup[0].Should().Be(entityWithVelocity);
            entitiesInGroup[1].Should().Be(entityWithBoth2);
        }

        [Test]
        public void GroupsShouldCorrectManageEntitiesWithDeferredOperations()
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
        public void GroupsShouldRaiseEventsWhenEntityAddedOrRemoved()
        {
            var context = new Context<TestRegistry>();
            var position = context.Registry.Position;
            var velocity = context.Registry.Velocity;
            var group = context.CreateGroup(GroupFilter.Include(position).Include(velocity));

            var changesCount = 0;
            var changedEntity = -1;
            var changeOperation = false;

            void TestAdd(int entityIndex, Vector2 newPosition, Vector2 newVelocity)
            {
                var changesBefore = changesCount;
                changeOperation = false;

                entityIndex.Set(position, newPosition);
                changesCount.Should().Be(changesBefore);
                entityIndex.Set(velocity, newVelocity);
                changesCount.Should().Be(changesBefore + 1);
                changedEntity.Should().Be(entityIndex);
                changeOperation.Should().BeTrue();
            }

            void TestAddedEarly(int entityIndex, Vector2 newPosition, Vector2 newVelocity)
            {
                var changesBefore = changesCount;
                entityIndex.Set(position, newPosition);
                entityIndex.Set(velocity, newVelocity);
                changesCount.Should().Be(changesBefore);
            }

            void TestRemove(int entityIndex)
            {
                var changesBefore = changesCount;
                changeOperation = true;

                entityIndex.Remove(position);
                changesCount.Should().Be(changesBefore + 1);
                changesBefore = changesCount;

                changeOperation.Should().BeFalse();
                entityIndex.Remove(velocity);
                changesCount.Should().Be(changesBefore);
            }

            var rnd = new Random();

            Vector2 RandomVector()
            {
                return new Vector2(rnd.Next(), rnd.Next());
            }

            group.OnGroupChanged += (entity, add) =>
            {
                changesCount += 1;
                changedEntity = entity;
                changeOperation = add;
            };

            var entity1 = context.CreateEntity();
            var entity2 = context.CreateEntity();
            var entity3 = context.CreateEntity();

            TestAdd(entity1, RandomVector(), RandomVector());
            TestAddedEarly(entity1, RandomVector(), RandomVector());
            TestAddedEarly(entity1, RandomVector(), RandomVector());

            TestAdd(entity2, RandomVector(), RandomVector());
            TestAddedEarly(entity2, RandomVector(), RandomVector());

            TestAdd(entity3, RandomVector(), RandomVector());
            TestAddedEarly(entity3, RandomVector(), RandomVector());

            TestRemove(entity2);
            TestAdd(entity2, RandomVector(), RandomVector());
            TestAddedEarly(entity2, RandomVector(), RandomVector());

            // Deferred
            var changesBeforeDeferred = changesCount;
            foreach (var entity in group)
            {
                entity.Remove(position);
                changesCount.Should().Be(changesBeforeDeferred);
                entity.Remove(velocity);
                changesCount.Should().Be(changesBeforeDeferred);
            }

            changesCount.Should().Be(changesBeforeDeferred + 3);
        }
    }
}