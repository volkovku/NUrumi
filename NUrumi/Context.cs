using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace NUrumi
{
    public sealed class Context
    {
        private const short DeadGen = 0;
        private const short GenInc = 1;

        private readonly List<int> _reusableCollector = new List<int>();
        private readonly Queue<EntityId> _freeEntities = new Queue<EntityId>();
        private readonly Storage _storage;
        private readonly int _entityReuseBarrier;
        private short[] _aliveEntities;
        private int _nextEntityIndex;

        public Context(Storage storage, int entityInitialCapacity = 512, int entityReuseBarrier = 1000)
        {
            _storage = storage;
            _aliveEntities = new short[Math.Max(1, entityInitialCapacity)];
            _entityReuseBarrier = entityReuseBarrier;
        }

        public Entity Create()
        {
            var id = GetEntityId();
            SetAlive(id);
            return new Entity(this, _storage, id);
        }

        public Entity Create(EntityId id)
        {
            if (!IsFree(id.Index))
            {
                throw new NUrumiException(
                    "Entity index already in use (" +
                    $"entity_ix={id.Index}," +
                    $"entity_gen={id.Generation})");
            }

            SetAlive(id);
            return new Entity(this, _storage, id);
        }

        public void Destroy(EntityId id)
        {
            if (!IsAlive(id))
            {
                throw new NUrumiException(
                    "Entity not exists or destroyed (" +
                    $"entity_ix={id.Index}," +
                    $"entity_gen={id.Generation})");
            }

            _storage.RemoveEntity(id);
            _aliveEntities[id.Index] = DeadGen;
            _freeEntities.Enqueue(id);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Entity Get(EntityId id)
        {
            if (Find(id, out var entity))
            {
                return entity;
            }

            throw new NUrumiException(
                "Entity not exists of destroyed (" +
                $"entity_ix={id.Index}," +
                $"entity_gen={id.Generation})");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Find(EntityId id, out Entity entity)
        {
            if (!IsAlive(id))
            {
                entity = default;
                return false;
            }

            entity = new Entity(this, _storage, id);
            return true;
        }
        
        public FieldQuickAccess<TValue> QuickAccessOf<TComponent, TValue>(Func<TComponent, IField<TValue>> field)
            where TComponent : Component<TComponent>, new()
        {
            return _storage.GetFieldQuickAccess(field(Component.InstanceOf<TComponent>()));
        }

        public void Collect(Filter filter, List<EntityId> destination)
        {
            _reusableCollector.Clear();
            _storage.Collect(filter, _reusableCollector);
            foreach (var entityIndex in _reusableCollector)
            {
                destination.Add(new EntityId(entityIndex, _aliveEntities[entityIndex]));
            }
        }

        public void Collect(Filter filter, List<Entity> destination)
        {
            _reusableCollector.Clear();
            _storage.Collect(filter, _reusableCollector);
            foreach (var entityIndex in _reusableCollector)
            {
                destination.Add(Get(new EntityId(entityIndex, _aliveEntities[entityIndex])));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsAlive(EntityId entityId)
        {
            var index = entityId.Index;
            if (index >= _aliveEntities.Length)
            {
                return false;
            }

            return _aliveEntities[index] == entityId.Generation;
        }

        private void SetAlive(EntityId entityId)
        {
            var index = entityId.Index;
            if (index >= _aliveEntities.Length)
            {
                Array.Resize(ref _aliveEntities, index << 1);
            }

            _aliveEntities[index] = entityId.Generation;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsFree(int entityIndex)
        {
            if (entityIndex >= _aliveEntities.Length)
            {
                return true;
            }

            return _aliveEntities[entityIndex] == DeadGen;
        }

        private EntityId GetEntityId()
        {
            while (_freeEntities.Count >= _entityReuseBarrier)
            {
                var id = _freeEntities.Dequeue();
                if (IsFree(id.Index))
                {
                    return new EntityId(id.Index, (short)(id.Generation + GenInc));
                }
            }

            while (true)
            {
                var entityIndex = _nextEntityIndex;
                if (IsFree(entityIndex))
                {
                    _nextEntityIndex += 1;
                    return new EntityId(entityIndex, 1);
                }
            }
        }
    }
}