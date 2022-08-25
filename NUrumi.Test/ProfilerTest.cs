using System;
using System.Collections.Generic;
using System.Numerics;
using NUnit.Framework;

namespace NUrumi.Test
{
    [TestFixture]
    public class ProfilerTest
    {
        private const int EntitiesCount = 1000000;
        
        private readonly Context _urumi;
        private readonly Filter _urumiFilter;
        private readonly List<Entity> _entitiesToMove = new List<Entity>();

        public ProfilerTest()
        {
            const int entitiesCount = 1000000;

            _urumi = new Context();
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
            
            _urumi.Collect(_urumiFilter, _entitiesToMove);
        }
        
        [Test]
        public void Test()
        {
            var velocityQuickAccess = _urumi.QuickAccessOf<UrumiVelocity, Vector2>(_ => _.Value);
            var positionQuickAccess = _urumi.QuickAccessOf<UrumiPosition, Vector2>(_ => _.Value);

            var stub = 0;
            foreach (var entity in _entitiesToMove)
            {
                var velocity = velocityQuickAccess.Get(entity.Id);
                var position = positionQuickAccess.Get(entity.Id);
                positionQuickAccess.Set(entity.Id, position + velocity);
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