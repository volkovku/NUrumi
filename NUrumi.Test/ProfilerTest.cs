using System;
using System.Collections.Generic;
using System.Numerics;
using NUnit.Framework;
using NUrumi.Storages.Safe;

namespace NUrumi.Test
{
    [TestFixture]
    public class ProfilerTest
    {
        private const int EntitiesCount = 1000000;
        
        private readonly Context _urumi;
        private readonly Filter _urumiFilter;

        public ProfilerTest()
        {
            const int entitiesCount = 1000000;

            _urumi = new Context(new Storage());
            _urumiFilter = Filter.With<UrumiVelocity>();

            var random = new Random();
            for (var i = 0; i < entitiesCount; i++)
            {
                var position = new Vector2(random.Next(), random.Next());
                var velocity = new Vector2(random.Next(), random.Next());

                var entity = _urumi.Create();
                entity.With<UrumiPosition>().Set(_ => _.Value, position);
                if (i % 2 == 0)
                {
                    entity.With<UrumiVelocity>().Set(_ => _.Value, velocity);
                }
            }
        }
        
        [Test]
        public void Test()
        {
            var entitiesToMove = new List<Entity>();
            _urumi.Collect(_urumiFilter, entitiesToMove);

            var stub = 0;
            foreach (var entity in entitiesToMove)
            {
                var velocity = entity.With<UrumiVelocity>().Get(_ => _.Value);
                var position = entity.With<UrumiPosition>().Get(_ => _.Value);
                entity.With<UrumiPosition>().Set(_ => _.Value, position + velocity);
                stub += (int) velocity.X;
            }

            Console.WriteLine(stub);
        }

        private class UrumiPosition : Component<UrumiPosition>
        {
            public FieldWith<Default<Vector2>, Vector2> Value;
        }

        private class UrumiVelocity : Component<UrumiVelocity>
        {
            public FieldWith<Default<Vector2>, Vector2> Value;
        }
    }
}