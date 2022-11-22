using System;
using System.Runtime.CompilerServices;

namespace NUrumi
{
    public sealed class Query
    {
        private readonly bool[] _conditions;
        private readonly ComponentStorageData[] _componentStorages;
        private readonly bool _singleInclude;

        private int[] _entitiesIndex;
        private int[] _denseEntities;
        private int _entitiesCount;

        private int _locksCount;
        private DeferredOperation[] _deferredOperations;
        private int _deferredOperationsCount;

        public static Query Create(IQueryFilter filter, int entitiesCapacity)
        {
            var componentsCount = filter.Included.Count + filter.Excluded.Count;
            var conditions = new bool[componentsCount];
            var componentStorages = new ComponentStorageData[componentsCount];
            var query = new Query(conditions, componentStorages, entitiesCapacity);

            var i = 0;
            foreach (var component in filter.Included)
            {
                conditions[i] = true;
                componentStorages[i] = component.Storage;
                component.Storage.AddQuery(query);
                i++;
            }

            foreach (var component in filter.Excluded)
            {
                conditions[i] = true;
                componentStorages[i] = component.Storage;
                component.Storage.AddQuery(query);
                i++;
            }

            return query;
        }

        private Query(
            bool[] conditions,
            ComponentStorageData[] componentStorages,
            int entitiesCapacity)
        {
            _conditions = conditions;
            _componentStorages = componentStorages;
            _singleInclude = conditions.Length == 1 && conditions[0];
            _entitiesIndex = new int[entitiesCapacity];
            _denseEntities = new int[entitiesCapacity];
        }

        public int EntitiesCount => _entitiesCount;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetEntities(ref int[] result)
        {
            var entitiesCount = _entitiesCount;
            if (result == null)
            {
                result = new int[entitiesCount];
            }
            else if (result.Length < entitiesCount)
            {
                Array.Resize(ref result, entitiesCount);
            }

            Array.Copy(_denseEntities, result, entitiesCount);
            return entitiesCount;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Enumerator GetEnumerator()
        {
            _locksCount += 1;
            return new Enumerator(this);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void ResizeEntities(int newSize)
        {
            Array.Resize(ref _entitiesIndex, newSize);
        }

        internal void Update(int entityIndex)
        {
            for (var i = 0; i < _conditions.Length; i++)
            {
                if (_conditions[i] == _componentStorages[i].Has(entityIndex))
                {
                    continue;
                }

                Remove(entityIndex);
                return;
            }

            Add(entityIndex);
        }

        internal void Update(int entityIndex, bool added)
        {
            if (_singleInclude)
            {
                if (added)
                {
                    Add(entityIndex);
                }
                else
                {
                    Remove(entityIndex);
                }

                return;
            }

            Update(entityIndex);
        }

        private void Add(int entityIndex)
        {
            if (AddDeferredOperation(false, entityIndex))
            {
                return;
            }

            AddWithoutLockChecks(entityIndex);
        }

        private void AddWithoutLockChecks(int entityIndex)
        {
            var denseIndex = _entitiesIndex[entityIndex];
            if (denseIndex != 0)
            {
                // Already added
                return;
            }

            denseIndex = _entitiesCount;
            if (denseIndex == _denseEntities.Length)
            {
                Array.Resize(ref _denseEntities, _entitiesCount << 1);
            }

            _entitiesIndex[entityIndex] = denseIndex + 1;
            _denseEntities[denseIndex] = entityIndex;
            _entitiesCount += 1;
        }

        private void Remove(int entityIndex)
        {
            if (AddDeferredOperation(false, entityIndex))
            {
                return;
            }

            RemoveWithoutLockChecks(entityIndex);
        }

        private void RemoveWithoutLockChecks(int entityIndex)
        {
            var denseIndex = _entitiesIndex[entityIndex];
            if (denseIndex == 0)
            {
                // Already removed`
                return;
            }

            if (denseIndex == _entitiesCount)
            {
                _entitiesIndex[entityIndex] = 0;
                _entitiesCount -= 1;
                return;
            }

            _entitiesCount -= 1;
            var lastIndex = _denseEntities[_entitiesCount];
            _denseEntities[denseIndex - 1] = lastIndex;
            _entitiesIndex[lastIndex] = denseIndex;
            _entitiesIndex[entityIndex] = 0;
        }

        private void Unlock()
        {
            _locksCount -= 1;
            if (_locksCount == 0 && _deferredOperationsCount > 0)
            {
                for (var i = 0; i < _deferredOperationsCount; i++)
                {
                    ref var operation = ref _deferredOperations[i];
                    if (operation.Added)
                    {
                        AddWithoutLockChecks(operation.EntityIndex);
                    }
                    else
                    {
                        RemoveWithoutLockChecks(operation.EntityIndex);
                    }
                }

                _deferredOperationsCount = 0;
            }
        }

        private bool AddDeferredOperation(bool added, int entityIndex)
        {
            if (_locksCount == 0)
            {
                return false;
            }

            var ix = _deferredOperationsCount++ - 1;
            if (ix == _deferredOperations.Length)
            {
                Array.Resize(ref _deferredOperations, ix << 1);
            }

            ref var operation = ref _deferredOperations[ix];
            operation.Added = added;
            operation.EntityIndex = entityIndex;

            return true;
        }

        public struct Enumerator : IDisposable
        {
            private readonly Query _query;
            private readonly int[] _entities;
            private readonly int _count;
            private int _idx;

            public Enumerator(Query query)
            {
                _query = query;
                _entities = query._denseEntities;
                _count = query._entitiesCount;
                _idx = -1;
            }

            public int Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => _entities[_idx];
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                return ++_idx < _count;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Dispose()
            {
                _query.Unlock();
            }
        }

        public struct DeferredOperation
        {
            public bool Added;
            public int EntityIndex;
        }
    }
}