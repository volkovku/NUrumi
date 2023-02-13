using System;
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
        private readonly Group _urumiGroup;

        private readonly EcsWorld _leo;
        private readonly EcsFilter _leoFilter;

        private GameContext _entitas;
        private IGroup<GameEntity> _entitasGroup;

        public SimpleMoveBench()
        {
            _urumi = new Context<UrumiRegistry>();
            var cmp = _urumi.Registry;
            var urumiPosition = cmp.Position;
            var urumiVelocity = cmp.Velocity;
            _urumiGroup = _urumi.CreateGroup(GroupFilter
                .Include(cmp.Position)
                .Include(cmp.Velocity));

            _leo = new EcsWorld();
            _leoFilter = _leo
                .Filter<LeoPosition>()
                .Inc<LeoVelocity>()
                .End();

            _entitas = new GameContext();
            _entitasGroup = _entitas.GetGroup(GameMatcher.AllOf(
                GameMatcher.PerfTestEntitasPosition,
                GameMatcher.PerfTestEntitasVelocity));

            var leoPosPool = _leo.GetPool<LeoPosition>();
            var leoVelocityPool = _leo.GetPool<LeoVelocity>();

            var random = new Random();
            for (var i = 0; i < EntitiesCount; i++)
            {
                var position = new Vector2(random.Next(), random.Next());
                var velocity = new Vector2(random.Next(), random.Next());

                var urumiEntity = _urumi.CreateEntity();
                urumiPosition.Set(urumiEntity, position);

                var leo = _leo.NewEntity();
                ref var leoPos = ref leoPosPool.Add(leo);
                leoPos.Value = position;

                var ent = _entitas.CreateEntity();
                ent.AddPerfTestEntitasPosition(position);

                if (i % 2 == 0)
                {
                    urumiVelocity.Set(urumiEntity, velocity);
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
            var positionCmp = cmp.Position;
            var velocityCmp = cmp.Velocity;

            foreach (var entity in _urumiGroup)
            {
                ref var velocity = ref entity.GetRef(velocityCmp);
                ref var position = ref entity.GetRef(positionCmp);
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
                ref var velocity = ref leoVelocityPool.Get(entity);
                ref var position = ref leoPosPool.Get(entity);
                position.Value += velocity.Value;
                stub += (int) velocity.Value.X;
            }

            return stub;
        }

        [Benchmark]
        public int Entitas()
        {
            var stub = 0;
            foreach (var entity in _entitasGroup)
            {
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

        private class UrumiPosition : Component<UrumiPosition>.Of<Vector2>
        {
        }

        private class UrumiVelocity : Component<UrumiVelocity>.Of<Vector2>
        {
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