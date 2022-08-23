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
            var storage = new SafeStorage();
            var bob = new Entity(storage, new EntityId(0, 0));
            var alice = new Entity(storage, new EntityId(1, 0));

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