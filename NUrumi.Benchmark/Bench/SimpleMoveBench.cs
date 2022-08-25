using System;
using System.Collections.Generic;
using System.Numerics;
using BenchmarkDotNet.Attributes;
using Leopotam.EcsLite;
using NUrumi.Storages.Safe;

namespace NUrumi.Benchmark.Bench
{
    public class SimpleMoveBench
    {
        private const int EntitiesCount = 1000000;

        private readonly Context _urumi;
        private readonly Filter _urumiFilter;
        private readonly List<Entity> _entitiesToMove;

        private readonly EcsWorld _leo;
        private readonly EcsFilter _leoFilter;

        public SimpleMoveBench()
        {
            _urumi = new Context(new Storage());
            _urumiFilter = Filter.With<UrumiVelocity>();

            _leo = new EcsWorld();
            _leoFilter = _leo.Filter<LeoPosition>().End();

            var leoPosPool = _leo.GetPool<LeoPosition>();
            var leoVelocityPool = _leo.GetPool<LeoVelocity>();

            var random = new Random();
            for (var i = 0; i < EntitiesCount; i++)
            {
                var position = new Vector2(random.Next(), random.Next());
                var velocity = new Vector2(random.Next(), random.Next());

                var urumi = _urumi.Create();
                urumi.With<UrumiPosition>().Set(_ => _.Value, position);

                var leo = _leo.NewEntity();
                ref var leoPos = ref leoPosPool.Add(leo);
                leoPos.Value = position;

                if (i % 2 == 0)
                {
                    urumi.With<UrumiVelocity>().Set(_ => _.Value, velocity);
                    ref var leoVelocity = ref leoVelocityPool.Add(leo);
                    leoVelocity.Value = velocity;
                }
            }
            
            _entitiesToMove = new List<Entity>();
            _urumi.Collect(_urumiFilter, _entitiesToMove);

        }

        [Benchmark]
        public int Urumi()
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

            return stub;
        }

        [Benchmark]
        public int Leo()
        {
            var stub = 0;

            var leoPosPool = _leo.GetPool<LeoPosition>();
            var leoVelocityPool = _leo.GetPool<LeoVelocity>();

            foreach (var entity in _leoFilter)
            {
                ref var velocity = ref leoVelocityPool.Get(entity);
                ref var position = ref leoPosPool.Get(entity);
                position.Value += velocity.Value;
                stub += (int) velocity.Value.X;
            }

            return stub;
        }

        private class UrumiPosition : Component<UrumiPosition>
        {
            public FieldWith<Default<Vector2>, Vector2> Value;
        }

        private class UrumiVelocity : Component<UrumiVelocity>
        {
            public FieldWith<Default<Vector2>, Vector2> Value;
        }

        private struct LeoPosition
        {
            public Vector2 Value;
        }

        private struct LeoVelocity
        {
            public Vector2 Value;
        }
    }
}