using System.Numerics;
using NUnit.Framework;

namespace NUrumi.Test
{
    [TestFixture]
    public class BenchCodeTest
    {
        private const int EntitiesCount = 100000;

        [Test]
        public void Test()
        {
            var context = new Context<UrumiRegistry>();
            var urumiOnlyPos = context.CreateGroup(GroupFilter
                .Include(context.Registry.Position)
                .Exclude(context.Registry.Velocity));

            var urumiPosAndVel = context.CreateGroup(GroupFilter
                .Include(context.Registry.Position)
                .Include(context.Registry.Velocity));

            var positionComponent = context.Registry.Position;
            var velocityComponent = context.Registry.Velocity;
            var position = positionComponent.Value;
            var velocity = velocityComponent.Value;

            for (var i = 0; i < EntitiesCount; i++)
            {
                var entity = context.CreateEntity();
                entity.Set(position, Vector2.One);
                if (i % 2 == 0)
                {
                    entity.Set(velocity, Vector2.Zero);
                }
            }

            foreach (var entity in urumiOnlyPos)
            {
                entity.Set(velocity, Vector2.Zero);
            }

            foreach (var entity in urumiPosAndVel)
            {
                context.RemoveEntity(entity);
            }
        }
        
        private class UrumiRegistry : Registry<UrumiRegistry>
        {
            public UrumiPosition Position;
            public UrumiVelocity Velocity;
        }

        private class UrumiPosition : Component<UrumiPosition>
        {
            public Field<Vector2> Value;
        }

        private class UrumiVelocity : Component<UrumiVelocity>
        {
            public Field<Vector2> Value;
        }
    }
}