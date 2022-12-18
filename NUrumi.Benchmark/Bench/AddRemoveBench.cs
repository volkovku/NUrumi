using System.Collections.Generic;
using System.Numerics;
using BenchmarkDotNet.Attributes;
using Leopotam.EcsLite;

namespace NUrumi.Benchmark.Bench
{
    public class AddRemoveBench
    {
        private const int EntitiesCount = 100000;

        [Benchmark]
        public void Urumi()
        {
            var context = new Context<UrumiRegistry>();
            var urumiOnlyPos = context.CreateQuery(QueryFilter
                .Include(context.Registry.Position)
                .Exclude(context.Registry.Velocity));

            var urumiPosAndVel = context.CreateQuery(QueryFilter
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

        [Benchmark]
        public void Leo()
        {
            var leo = new EcsWorld();
            var leoOnlyPos = leo
                .Filter<LeoPosition>()
                .Exc<LeoVelocity>()
                .End();

            var leoPosAndVel = leo
                .Filter<LeoPosition>()
                .Inc<LeoVelocity>()
                .End();

            var position = leo.GetPool<LeoPosition>();
            var velocity = leo.GetPool<LeoVelocity>();

            for (var i = 0; i < EntitiesCount; i++)
            {
                var entity = leo.NewEntity();
                ref var pos = ref position.Add(entity);
                pos.Value = Vector2.One;

                if (i % 2 == 0)
                {
                    ref var vel = ref velocity.Add(entity);
                    vel.Value = Vector2.Zero;
                }
            }

            foreach (var entity in leoOnlyPos)
            {
                ref var vel = ref velocity.Add(entity);
                vel.Value = Vector2.Zero;
            }

            foreach (var entity in leoPosAndVel)
            {
                leo.DelEntity(entity);
            }
        }

        private readonly List<GameEntity> _iter = new List<GameEntity>();

        [Benchmark]
        public void Entitas()
        {
            var context = new GameContext();
            var entitasOnlyPos = context.GetGroup(
                GameMatcher
                    .AllOf(GameMatcher.PerfTestEntitasPosition)
                    .NoneOf(GameMatcher.PerfTestEntitasVelocity));

            var entitasPosAndVel = context.GetGroup(GameMatcher.AllOf(
                GameMatcher.PerfTestEntitasPosition,
                GameMatcher.PerfTestEntitasVelocity));

            for (var i = 0; i < EntitiesCount; i++)
            {
                var entity = context.CreateEntity();
                entity.AddPerfTestEntitasPosition(Vector2.One);
                if (i % 2 == 0)
                {
                    entity.AddPerfTestEntitasVelocity(Vector2.Zero);
                }
            }

            entitasOnlyPos.GetEntities(_iter);
            foreach (var entity in _iter)
            {
                entity.AddPerfTestEntitasVelocity(Vector2.Zero);
            }

            entitasPosAndVel.GetEntities(_iter);
            foreach (var entity in _iter)
            {
                entity.Destroy();
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