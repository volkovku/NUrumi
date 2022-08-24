using System;
using System.Collections.Generic;

namespace NUrumi
{
    public sealed class Context
    {
        private const int DeadGen = 0;

        private readonly List<int> _reusableCollector = new List<int>();
        private readonly Queue<EntityId> _freeEntities = new Queue<EntityId>();
        private readonly IStorage _storage;
        private readonly int _entityReuseBarrier;
        private int[] _aliveEntities;
        private int _nextEntityIndex;

        public Context(IStorage storage, int entityInitialCapacity = 100, int entityReuseBarrier = 1000)
        {
            _storage = storage;
            _aliveEntities = new int[Math.Max(1, entityInitialCapacity)];
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
                var newAliveEntities = new int[index << 1];
                Array.Copy(_aliveEntities, newAliveEntities, _aliveEntities.Length);
                _aliveEntities = newAliveEntities;
            }

            _aliveEntities[index] = entityId.Generation;
        }

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
                    return new EntityId(id.Index, id.Generation + 1);
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