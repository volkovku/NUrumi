using System;
using System.Numerics;
using FluentAssertions;
using NUnit.Framework;
using NUrumi.Test.Model;

namespace NUrumi.Test
{
    [TestFixture]
    public class CollectorTests
    {
        [Test]
        public void CollectorTest()
        {
            var context = new Context<TestRegistry>();
            var position = context.Registry.Position;
            var velocity = context.Registry.Velocity;
            var health = context.Registry.Health.Value;

            var rnd = new Random();

            Vector2 RandomVector()
            {
                return new Vector2(rnd.Next(), rnd.Next());
            }

            var group = context.CreateGroup(GroupFilter.Include(position).Include(velocity));
            var entityForCheckRemove = context
                .CreateEntity()
                .Set(position, RandomVector())
                .Set(velocity, RandomVector());

            var collector = context
                .CreateCollector()
                .WatchEntitiesAddedTo(group)
                .WatchEntitiesRemovedFrom(group)
                .WatchChangesOf(health);

            var entity1 = context.CreateEntity();
            var entity2 = context.CreateEntity();
            var entity3 = context.CreateEntity();
            var entity4 = context.CreateEntity();

            // Just created collector should not has any entity
            // ------------------------------------------------
            collector.Count.Should().Be(0);
            collector.Has(entity1).Should().BeFalse();
            collector.Has(entity2).Should().BeFalse();
            collector.Has(entity3).Should().BeFalse();
            collector.Has(entity4).Should().BeFalse();
            collector.Has(entityForCheckRemove).Should().BeFalse();

            // Collector should catch entity when it added to group
            // ----------------------------------------------------
            entity1.Set(position, RandomVector());
            collector.Count.Should().Be(0);
            collector.Has(entity1).Should().BeFalse();

            entity1.Set(velocity, RandomVector());
            collector.Count.Should().Be(1);
            collector.Has(entity1).Should().BeTrue();

            // Collector should not register changes of same entity twice
            // ----------------------------------------------------------
            entity1.Set(health, rnd.Next());
            collector.Count.Should().Be(1);
            collector.Has(entity1).Should().BeTrue();

            // Collector should catch entity when specified field value were changed
            // ---------------------------------------------------------------------
            entity2.Set(health, rnd.Next());
            collector.Count.Should().Be(2);
            collector.Has(entity1).Should().BeTrue();
            collector.Has(entity2).Should().BeTrue();
            collector.Has(entityForCheckRemove).Should().BeFalse();

            // Collector should catch entity which removed from specified group
            // ----------------------------------------------------------------
            entityForCheckRemove.Remove(position);
            collector.Count.Should().Be(3);
            collector.Has(entity1).Should().BeTrue();
            collector.Has(entity2).Should().BeTrue();
            collector.Has(entityForCheckRemove).Should().BeTrue();

            // After clearing collector should be empty
            // ----------------------------------------
            collector.Clear();
            collector.Count.Should().Be(0);
            collector.Has(entity1).Should().BeFalse();
            collector.Has(entity2).Should().BeFalse();
            
            // If value of entity not changed it should not be tracked
            // -------------------------------------------------------
            var prev = entity2.Get(health);
            entity2.Set(health, prev);
            collector.Count.Should().Be(0);
            collector.Has(entity1).Should().BeFalse();
            collector.Has(entity2).Should().BeFalse();

            // Otherwise it should be tracked
            // -------------------------------------------------------
            entity2.Set(health, prev + 1);
            collector.Count.Should().Be(1);
            collector.Has(entity1).Should().BeFalse();
            collector.Has(entity2).Should().BeTrue();

            // After disposing collector should not track any changes
            // ------------------------------------------------------
            collector.Dispose();

            entity3.Set(position, RandomVector());
            entity3.Set(velocity, RandomVector());
            entity4.Set(health, rnd.Next());

            collector.Count.Should().Be(0);
            collector.Has(entity1).Should().BeFalse();
            collector.Has(entity2).Should().BeFalse();
            collector.Has(entity3).Should().BeFalse();
            collector.Has(entity4).Should().BeFalse();
        }
    }
}