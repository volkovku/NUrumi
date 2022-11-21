using System;
using System.Collections.Generic;
using System.Numerics;
using BenchmarkDotNet.Attributes;
using Entitas;
using Leopotam.EcsLite;

namespace NUrumi.Benchmark.Bench
{
    public class SimpleMoveBench
    {
        private const int EntitiesCount = 100000;

        private readonly Context<UrumiRegistry> _urumi;
        private readonly EcsWorld _leo;
        private readonly EcsFilter _leoFilter;

        private GameContext _entitas;
        private IGroup<GameEntity> _entitasGroup;

        public SimpleMoveBench()
        {
            _urumi = new Context<UrumiRegistry>();
            var cmp = _urumi.Registry;

            _leo = new EcsWorld();
            _leoFilter = _leo.Filter<LeoPosition>().End();

            _entitas = new GameContext();
            _entitasGroup = _entitas.GetGroup(GameMatcher.PerfTestEntitasPosition);

            var leoPosPool = _leo.GetPool<LeoPosition>();
            var leoVelocityPool = _leo.GetPool<LeoVelocity>();

            var random = new Random();
            for (var i = 0; i < EntitiesCount; i++)
            {
                var position = new Vector2(random.Next(), random.Next());
                var velocity = new Vector2(random.Next(), random.Next());

                var urumi = _urumi.CreateEntity();
                _urumi.Set(cmp.Position.Value, urumi, position);

                var leo = _leo.NewEntity();
                ref var leoPos = ref leoPosPool.Add(leo);
                leoPos.Value = position;

                var ent = _entitas.CreateEntity();
                ent.AddPerfTestEntitasPosition(position);

                if (i % 2 == 0)
                {
                    _urumi.Set(cmp.Velocity.Value, urumi, velocity);
                    ref var leoVelocity = ref leoVelocityPool.Add(leo);
                    leoVelocity.Value = velocity;
                    ent.AddPerfTestEntitasVelocity(velocity);
                }
            }
        }

        [Benchmark]
        public int Urumi()
        {
            var stub = 0;
            var cmp = _urumi.Registry;
            var positionField = cmp.Position.Value;
            var velocityField = cmp.Velocity.Value;

            for (var i = 0; i < EntitiesCount; i++)
            {
                if (!velocityField.TryGet(i, out var velocity))
                {
                    continue;
                }

                ref var position = ref positionField.GetRef(i);
                position += velocity;
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
                if (!leoVelocityPool.Has(entity))
                {
                    continue;
                }

                ref var velocity = ref leoVelocityPool.Get(entity);
                ref var position = ref leoPosPool.Get(entity);
                position.Value += velocity.Value;
                stub += (int) velocity.Value.X;
            }

            return stub;
        }

        private List<GameEntity> _entitasIter = new List<GameEntity>();

        [Benchmark]
        public int Entitas()
        {
            var stub = 0;

            _entitasGroup.GetEntities(_entitasIter);
            for (var i = 0; i < _entitasIter.Count; i++)
            {
                var entity = _entitasIter[i];
                if (!entity.hasPerfTestEntitasVelocity)
                {
                    continue;
                }

                var velocity = entity.perfTestEntitasVelocity.Value;
                var positionCmp = entity.perfTestEntitasPosition;
                positionCmp.Value += velocity;
                stub += (int) velocity.X;
            }

            return stub;
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