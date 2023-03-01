using System;
using FluentAssertions;
using NUnit.Framework;

namespace NUrumi.Test
{
    [TestFixture]
    public class PrimaryKeyTests
    {
        public class Registry : Registry<Registry>
        {
            public ExternalIdComponent ExternalIdIndex;
        }

        public class ExternalIdComponent : Component<ExternalIdComponent>.OfPrimaryKey<ExternalEntityId>
        {
        }

        public readonly struct ExternalEntityId : IEquatable<ExternalEntityId>
        {
            public ExternalEntityId(int externalId)
            {
                ExternalId = externalId;
            }

            public readonly int ExternalId;

            public bool Equals(ExternalEntityId other)
            {
                return ExternalId == other.ExternalId;
            }

            public override bool Equals(object obj)
            {
                return obj is ExternalEntityId other && Equals(other);
            }

            public override int GetHashCode()
            {
                return ExternalId;
            }

            public static bool operator ==(ExternalEntityId left, ExternalEntityId right)
            {
                return left.Equals(right);
            }

            public static bool operator !=(ExternalEntityId left, ExternalEntityId right)
            {
                return !left.Equals(right);
            }
        }

        [Test]
        public void PrimaryKeyTest()
        {
            // Initialize
            var context = new Context<Registry>();
            var externalIdIndex = context.Registry.ExternalIdIndex;

            var externalId1 = new ExternalEntityId(1);
            var externalId2 = new ExternalEntityId(3);
            var externalId3 = new ExternalEntityId(5);
            var externalId4 = new ExternalEntityId(2);

            // Populate
            var entity1 = context
                .CreateEntity()
                .Set(externalIdIndex, externalId1);

            var entity2 = context
                .CreateEntity()
                .Set(externalIdIndex, externalId2);

            var entity3 = context
                .CreateEntity()
                .Set(externalIdIndex, externalId3);

            var entity4 = context
                .CreateEntity()
                .Set(externalIdIndex, externalId4);

            var entity5 = context
                .CreateEntity();

            // Check values
            var checks = new[]
            {
                ValueTuple.Create(entity1, externalId1),
                ValueTuple.Create(entity2, externalId2),
                ValueTuple.Create(entity3, externalId3),
                ValueTuple.Create(entity4, externalId4),
            };

            foreach (var (entity, requiredExternalId) in checks)
            {
                entity.Has(externalIdIndex).Should().BeTrue();
                entity.TryGet(externalIdIndex, out var entityExternalId).Should().BeTrue();
                entityExternalId.Should().Be(requiredExternalId);
                entity.Get(externalIdIndex).Should().Be(requiredExternalId);

                externalIdIndex.TryGetEntityByKey(requiredExternalId, out var foundEntity).Should().BeTrue();
                foundEntity.Should().Be(entity);
                externalIdIndex.GetEntityByKey(requiredExternalId).Should().Be(entity);
            }

            entity5.Has(externalIdIndex).Should().BeFalse();
            entity5.TryGet(externalIdIndex, out _).Should().BeFalse();
            new Action(() => entity5.Set(externalIdIndex, externalId1))
                .Should().Throw<Exception>()
                .Which.Message.Should().Contain("Entity for key already exists");

            entity1.Remove(externalIdIndex);
            entity1.Has(externalIdIndex).Should().BeFalse();
            entity1.TryGet(externalIdIndex, out _).Should().BeFalse();
            externalIdIndex.TryGetEntityByKey(externalId1, out _).Should().BeFalse();
            new Action(() => externalIdIndex.GetEntityByKey(externalId1))
                .Should().Throw<Exception>()
                .Which.Message.Should().Contain("Entity not found");

            entity5.Set(externalIdIndex, externalId1);
            entity5.Has(externalIdIndex).Should().BeTrue();
            entity5.TryGet(externalIdIndex, out var entity5ExternalId).Should().BeTrue();
            entity5ExternalId.Should().Be(externalId1);
        }
    }
}