using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using NUnit.Framework;
using NUrumi.Extensions;
using NUrumi.Storages.Safe;

namespace NUrumi.Test
{
    [TestFixture]
    public class EntityTests
    {
        [Test]
        public void EntityBasics()
        {
            var storage = new SafeStorage(
                componentsInitialCapacity: 1,
                fieldsInitialCapacity: 1,
                extensionsInitialCapacity: 1,
                valuesSetInitialCapacity: 1);

            var context = new Context(storage);

            var bob = context.Create();
            var alice = context.Create();

            bob.With<PlayerName>().Set(_ => _.Value, "Bob");
            bob.With<Position>().Set(_ => _.Value, new Vector3(1, 2, 3));

            alice.With<PlayerName>().Set(_ => _.Value, "Alice");
            alice.With<Parent>().Set(_ => _.Id, bob.Id);

            Assert.AreEqual("Bob", bob.With<PlayerName>().Get(_ => _.Value));
            Assert.AreEqual(true, bob.Has<PlayerName>());
            Assert.AreEqual(true, bob.Has<Position>());

            Assert.AreEqual("Alice", alice.With<PlayerName>().Get(_ => _.Value));
            Assert.AreEqual(true, alice.Has<PlayerName>());
            Assert.AreEqual(false, alice.Has<Position>());

            bob.With<PlayerName>().Set(_ => _.Value, "Tom");
            Assert.AreEqual("Tom", bob.With<PlayerName>().Get(_ => _.Value));
            Assert.AreEqual("Alice", alice.With<PlayerName>().Get(_ => _.Value));

            var bobChildren = storage.FindWith<Parent, EntityId>(_ => _.Id, bob.Id);
            Assert.AreEqual(1, bobChildren.Count);
            Assert.AreEqual(alice.Id, bobChildren.First());

            alice.Remove<Parent>();
            var bobChildrenAfterDetach = storage.FindWith<Parent, EntityId>(_ => _.Id, bob.Id);
            Assert.AreEqual(0, bobChildrenAfterDetach.Count);
            Assert.AreEqual(false, alice.Has<Parent>());
        }

        [Test]
        public void EntityLivecycle()
        {
            var context = new Context(new SafeStorage());
            var entity = context.Create();
            Assert.AreEqual(true, context.IsAlive(entity.Id));

            entity.With<PlayerName>().Set(_ => _.Value, "Steve");
            entity.With<Position>().Set(_ => _.Value, Vector3.One);

            var foundEntity = context.Get(entity.Id);
            Assert.AreEqual(entity.Id, foundEntity.Id);
            Assert.AreEqual("Steve", foundEntity.With<PlayerName>().Get(_ => _.Value));
            Assert.AreEqual(Vector3.One, foundEntity.With<Position>().Get(_ => _.Value));

            foundEntity.Destroy();
            Assert.AreEqual(false, context.IsAlive(entity.Id));

            var reuseEntity = context.Create(foundEntity.Id);
            Assert.AreEqual(false, reuseEntity.Has<PlayerName>());
            Assert.AreEqual(false, reuseEntity.Has<Position>());

            var nextEntity = context.Create();
            Assert.AreNotEqual(nextEntity, reuseEntity);

            reuseEntity.With<PlayerName>().Set(_ => _.Value, "John");
            nextEntity.With<PlayerName>().Set(_ => _.Value, "Smith");
            Assert.AreEqual("John", reuseEntity.With<PlayerName>().Get(_ => _.Value));
            Assert.AreEqual("Smith", nextEntity.With<PlayerName>().Get(_ => _.Value));
        }

        [Test]
        public void EntityGenerations()
        {
            var reuseBarrier = 100;
            var context = new Context(new SafeStorage(), entityReuseBarrier: reuseBarrier);
            var usedIndexes = new HashSet<int>();
            for (var i = 0; i < reuseBarrier; i++)
            {
                var entity = context.Create();
                Assert.AreEqual(i, entity.Id.Index);
                Assert.AreEqual(true, usedIndexes.Add(entity.Id.Index));
                Assert.AreEqual(1, entity.Id.Generation);
                entity.Destroy();
            }

            for (var i = 0; i < reuseBarrier; i++)
            {
                var entity = context.Create();
                Assert.AreEqual(i, entity.Id.Index);
                Assert.AreEqual(false, usedIndexes.Add(entity.Id.Index));
                Assert.AreEqual(2, entity.Id.Generation);
                entity.Destroy();
            }

            var reused = context.Create();
            Assert.AreEqual(0, reused.Id.Index);
            Assert.AreEqual(3, reused.Id.Generation);

            for (var i = 0; i < reuseBarrier; i++)
            {
                var entity = context.Create();
                Assert.AreEqual(true, usedIndexes.Add(entity.Id.Index));
                Assert.AreEqual(1, entity.Id.Generation);
            }
        }

        private class PlayerName : Component<PlayerName>
        {
            public FieldWith<Default<string>, string> Value;
        }

        private class Position : Component<Position>
        {
            public FieldWith<Default<Vector3>, Vector3> Value;
        }

        private class Parent : Component<Parent>
        {
            public FieldWith<Index<EntityId>, EntityId> Id;
        }
    }
}