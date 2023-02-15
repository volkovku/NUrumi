using System.Linq;
using FluentAssertions;
using NUnit.Framework;

namespace NUrumi.Test
{
    [TestFixture]
    public class RelationTests
    {
        [Test]
        public void RelationTest()
        {
            // Configure
            // ---------
            var context = new Context<RelationRegistry>();
            var likes = context.Registry.Likes;

            var apples = context.CreateEntity();
            var burgers = context.CreateEntity();
            var sweets = context.CreateEntity();

            var alice = context.CreateEntity();
            var bob = context.CreateEntity();

            // Populate set of relations
            // -------------------------
            alice.Add(likes, apples);
            alice.Add(likes, sweets);

            bob.Add(likes, burgers);
            bob.Add(likes, sweets);

            // Check what likes alice
            // ---------------------
            alice.Has(likes, apples).Should().BeTrue();
            alice.Has(likes, sweets).Should().BeTrue();
            alice.Has(likes, burgers).Should().BeFalse();
            
            var aliceLikes = alice.Relationship(likes);
            aliceLikes.Count.Should().Be(2);
            aliceLikes.Contains(apples).Should().BeTrue();
            aliceLikes.Contains(burgers).Should().BeFalse();
            aliceLikes.Contains(sweets).Should().BeTrue();

            // Check what likes bob
            // ---------------------
            bob.Has(likes, apples).Should().BeFalse();
            bob.Has(likes, burgers).Should().BeTrue();
            bob.Has(likes, sweets).Should().BeTrue();

            var bobLikes = bob.Relationship(likes);
            bobLikes.Count.Should().Be(2);
            bobLikes.Contains(apples).Should().BeFalse();
            bobLikes.Contains(sweets).Should().BeTrue();
            bobLikes.Contains(burgers).Should().BeTrue();

            // Check who like apples
            // ---------------------
            var whoLikeApples = apples.Target(likes);
            whoLikeApples.Count.Should().Be(1);
            whoLikeApples.Contains(alice).Should().BeTrue();
            whoLikeApples.Contains(bob).Should().BeFalse();

            // Check who like burgers
            // ----------------------
            var whoLikeBurgers = burgers.Target(likes);
            whoLikeBurgers.Count.Should().Be(1);
            whoLikeBurgers.Contains(alice).Should().BeFalse();
            whoLikeBurgers.Contains(bob).Should().BeTrue();

            // Check who like sweets
            // ---------------------
            var whoLikeSweets = sweets.Target(likes);
            whoLikeSweets.Count.Should().Be(2);
            whoLikeSweets.Contains(alice).Should().BeTrue();
            whoLikeSweets.Contains(bob).Should().BeTrue();

            // Alice don't like apples anymore
            // --------------------------------
            alice.Remove(likes, apples);

            aliceLikes = alice.Relationship(likes);
            aliceLikes.Count.Should().Be(1);
            aliceLikes.Contains(apples).Should().BeFalse();
            aliceLikes.Contains(sweets).Should().BeTrue();
            aliceLikes.Contains(burgers).Should().BeFalse();

            whoLikeApples = apples.Target(likes);
            whoLikeApples.Count.Should().Be(0);
            whoLikeApples.Contains(alice).Should().BeFalse();
            whoLikeApples.Contains(bob).Should().BeFalse();

            // There is no sweets in our menu anymore
            // --------------------------------------
            context.RemoveEntity(burgers);

            bobLikes.Count.Should().Be(1);
            bobLikes.Contains(apples).Should().BeFalse();
            bobLikes.Contains(sweets).Should().BeTrue();
            bobLikes.Contains(burgers).Should().BeFalse();

            // Bob goes out
            // ------------
            context.RemoveEntity(bob);

            whoLikeApples = apples.Target(likes);
            whoLikeApples.Count.Should().Be(0);
            whoLikeApples.Contains(alice).Should().BeFalse();
            whoLikeApples.Contains(bob).Should().BeFalse();

            whoLikeSweets = sweets.Target(likes);
            whoLikeSweets.Count.Should().Be(1);
            whoLikeSweets.Contains(alice).Should().BeTrue();
            whoLikeSweets.Contains(bob).Should().BeFalse();
        }


        public class RelationRegistry : Registry<RelationRegistry>
        {
            public Likes Likes;
        }

        public class Likes : Relation<Likes>
        {
        }
    }
}