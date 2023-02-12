using System;
using System.Runtime.CompilerServices;

namespace NUrumi
{
    public sealed class EntitiesSet
    {
        internal const int Deferred = -1;
        internal const int AppliedEarly = 0;
        internal const int Applied = 1;

        public static readonly EntitiesSet Empty = new EntitiesSet(0);

        private int[] _entitiesIndex;
        private int[] _denseEntities;
        private int _entitiesCount;

        private int[] _deferredOperations;
        private int _deferredOperationsCount;
        private int _locksCount;

        public EntitiesSet(int entitiesCapacity)
        {
            _entitiesIndex = new int[entitiesCapacity];
            _denseEntities = new int[entitiesCapacity];
            _entitiesCount = 0;

            _deferredOperations = new int[100];
            _deferredOperationsCount = 0;
            _locksCount = 0;
        }

        public int EntitiesCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _entitiesCount;
        }

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal int Add(int entityIndex)
        {
            if (AddDeferredOperation(false, entityIndex))
            {
                return Deferred;
            }

            return AddWithoutLockChecks(entityIndex);
        }

        private int AddWithoutLockChecks(int entityIndex)
        {
            var denseIndex = _entitiesIndex[entityIndex];
            if (denseIndex != 0)
            {
                // Already added
                return AppliedEarly;
            }

            denseIndex = _entitiesCount;
            if (denseIndex == _denseEntities.Length)
            {
                Array.Resize(ref _denseEntities, _entitiesCount << 1);
            }

            _entitiesIndex[entityIndex] = denseIndex + 1;
            _denseEntities[denseIndex] = entityIndex;
            _entitiesCount += 1;

            return Applied;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal int Remove(int entityIndex)
        {
            if (AddDeferredOperation(false, entityIndex))
            {
                return Deferred;
            }

            return RemoveWithoutLockChecks(entityIndex);
        }

        private int RemoveWithoutLockChecks(int entityIndex)
        {
            var denseIndex = _entitiesIndex[entityIndex];
            if (denseIndex == 0)
            {
                // Already removed`
                return AppliedEarly;
            }

            if (denseIndex == _entitiesCount)
            {
                _entitiesIndex[entityIndex] = 0;
                _entitiesCount -= 1;
                return Applied;
            }

            _entitiesCount -= 1;
            var lastIndex = _denseEntities[_entitiesCount];
            _denseEntities[denseIndex - 1] = lastIndex;
            _entitiesIndex[lastIndex] = denseIndex;
            _entitiesIndex[entityIndex] = 0;

            return Applied;
        }

        private void Unlock()
        {
            _locksCount -= 1;
            if (_locksCount == 0 && _deferredOperationsCount > 0)
            {
                for (var i = 0; i < _deferredOperationsCount; i++)
                {
                    ref var operation = ref _deferredOperations[i];
                    if (operation >= 0)
                    {
                        AddWithoutLockChecks(operation);
                    }
                    else
                    {
                        RemoveWithoutLockChecks(-(operation + 1));
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

            var ix = ++_deferredOperationsCount - 1;
            if (ix == _deferredOperations.Length)
            {
                Array.Resize(ref _deferredOperations, ix << 1);
            }

            _deferredOperations[ix] = added ? entityIndex : -entityIndex - 1;
            return true;
        }

        public struct Enumerator : IDisposable
        {
            private readonly EntitiesSet _set;
            private readonly int[] _entities;
            private readonly int _count;
            private int _idx;

            public Enumerator(EntitiesSet set)
            {
                _set = set;
                _entities = set._denseEntities;
                _count = set._entitiesCount;
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
                _set.Unlock();
            }
        }
    }
}