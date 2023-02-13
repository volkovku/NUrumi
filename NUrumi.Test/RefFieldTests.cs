using FluentAssertions;
using NUnit.Framework;

namespace NUrumi.Test
{
    [TestFixture]
    public class RefFieldTests
    {
        [Test]
        public void RefFieldTest()
        {
            var context = new Context<TestRegistry>();
            var playerName = context.Registry.PlayerName;

            var thor = context
                .CreateEntity()
                .Set(playerName, "Thor");

            var loki = context
                .CreateEntity()
                .Set(playerName, "Loki");

            thor.Get(playerName).Should().Be("Thor");
            loki.Get(playerName).Should().Be("Loki");

            thor.Remove(playerName);
            thor.TryGet(playerName, out _).Should().BeFalse();
            loki.Get(playerName).Should().Be("Loki");

            thor.Set(playerName, "Thor");
            thor.Get(playerName).Should().Be("Thor");
            loki.Get(playerName).Should().Be("Loki");

            thor.TryGet(playerName, out var thorName).Should().BeTrue();
            thorName.Should().Be("Thor");

            loki.TryGet(playerName, out var lokiName).Should().BeTrue();
            lokiName.Should().Be("Loki");
        }

        public class TestRegistry : Registry<TestRegistry>
        {
            public PlayerName PlayerName;
        }

        public class PlayerName : Component<PlayerName>.OfRef<string>
        {
        }
    }
}