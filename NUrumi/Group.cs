using System;
using System.Runtime.CompilerServices;

namespace NUrumi
{
    /// <summary>
    /// Represents a group of entities.
    /// </summary>
    public sealed class Group : IGroup
    {
        private readonly bool[] _conditions;
        private readonly ComponentStorageData[] _componentStorages;
        private readonly bool _singleInclude;
        private readonly EntitiesSet _entities;
        private int[] _deferredOperations;
        private int _deferredOperationsCount;
        private int _locksCount;

        /// <summary>
        /// An event which raised when the group was changed.
        /// </summary>
        /// <param name="entityIndex">An index of entity which was added or removed from group.</param>
        /// <param name="add">If true - an entity was added; otherwise - removed.</param>
        public delegate void GroupChangedEvent(int entityIndex, bool add);

        /// <summary>
        /// Creates a new group with specified filter and initial capacity.
        /// </summary>
        /// <param name="filter">A filter of entities as composition of components.</param>
        /// <param name="entitiesCapacity">An initial entities capacity.</param>
        /// <returns>Returns new group.</returns>
        public static Group Create(IGroupFilter filter, int entitiesCapacity)
        {
            var componentsCount = filter.Included.Count + filter.Excluded.Count;
            var conditions = new bool[componentsCount];
            var componentStorages = new ComponentStorageData[componentsCount];
            var group = new Group(conditions, componentStorages, entitiesCapacity);

            var i = 0;
            foreach (var component in filter.Included)
            {
                conditions[i] = true;
                componentStorages[i] = component.Storage;
                component.Storage.AddGroup(group);
                i++;
            }

            foreach (var component in filter.Excluded)
            {
                conditions[i] = true;
                componentStorages[i] = component.Storage;
                component.Storage.AddGroup(group);
                i++;
            }

            return group;
        }

        private Group(
            bool[] conditions,
            ComponentStorageData[] componentStorages,
            int entitiesCapacity)
        {
            _conditions = conditions;
            _componentStorages = componentStorages;
            _singleInclude = conditions.Length == 1 && conditions[0];
            _entities = new EntitiesSet(entitiesCapacity);
            _deferredOperations = new int[100];
        }

        /// <summary>
        /// An event which raised when this group changed.
        /// </summary>
        public event GroupChangedEvent OnGroupChanged;

        /// <summary>
        /// Count of entities in this group.
        /// </summary>
        public int EntitiesCount => _entities.EntitiesCount;

        /// <summary>
        /// Returns entities to specified array.
        /// If array length is less then required it will be automatically extended.
        /// </summary>
        /// <param name="result">A destination array.</param>
        /// <returns>Returns a count of written entities.</returns>
        public int GetEntities(ref int[] result)
        {
            return _entities.GetEntities(ref result);
        }

        /// <summary>
        /// Enumerates an entities in this group.
        /// </summary>
        /// <returns>Returns an enumerator of entities.</returns>
        public Enumerator GetEnumerator()
        {
            _locksCount += 1;
            return new Enumerator(this, _entities.GetEnumerator());
        }

        void IGroup.Update(int entityIndex, bool added)
        {
            if (_singleInclude)
            {
                if (added)
                {
                    AddInternal(entityIndex);
                }
                else
                {
                    RemoveInternal(entityIndex);
                }

                return;
            }

            Update(entityIndex);
        }

        internal void Update(int entityIndex)
        {
            for (var i = 0; i < _conditions.Length; i++)
            {
                if (_conditions[i] == _componentStorages[i].Has(entityIndex))
                {
                    continue;
                }

                RemoveInternal(entityIndex);
                return;
            }

            AddInternal(entityIndex);
        }

        internal void ResizeEntities(int newSize)
        {
            _entities.ResizeEntities(newSize);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AddInternal(int entityIndex)
        {
            var resolution = _entities.Add(entityIndex);
            if (resolution == EntitiesSet.AppliedEarly)
            {
                return;
            }

            if (resolution == EntitiesSet.Applied)
            {
                var h = OnGroupChanged;
                h?.Invoke(entityIndex, true);
                return;
            }

            var ix = ++_deferredOperationsCount - 1;
            if (ix == _deferredOperations.Length)
            {
                Array.Resize(ref _deferredOperations, ix << 1);
            }

            _deferredOperations[ix] = entityIndex;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void RemoveInternal(int entityIndex)
        {
            var resolution = _entities.Remove(entityIndex);
            if (resolution == EntitiesSet.AppliedEarly)
            {
                return;
            }

            if (resolution == EntitiesSet.Applied)
            {
                var h = OnGroupChanged;
                h?.Invoke(entityIndex, true);
                return;
            }

            var ix = ++_deferredOperationsCount - 1;
            if (ix == _deferredOperations.Length)
            {
                Array.Resize(ref _deferredOperations, ix << 1);
            }

            _deferredOperations[ix] = -entityIndex - 1;
        }

        private void Unlock()
        {
            _locksCount -= 1;

            if (_locksCount == 0 && _deferredOperationsCount > 0)
            {
                var h = OnGroupChanged;
                if (h == null)
                {
                    _deferredOperationsCount = 0;
                    return;
                }

                for (var i = 0; i < _deferredOperationsCount; i++)
                {
                    ref var operation = ref _deferredOperations[i];
                    if (operation > 0)
                    {
                        h(operation, true);
                    }
                    else
                    {
                        h(-(operation + 1), false);
                    }
                }

                _deferredOperationsCount = 0;
            }
        }

        public struct Enumerator : IDisposable
        {
            private readonly Group _group;
            private EntitiesSet.Enumerator _setEnumerator;

            public Enumerator(Group group, EntitiesSet.Enumerator setEnumerator)
            {
                _group = group;
                _setEnumerator = setEnumerator;
            }

            public int Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => _setEnumerator.Current;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                return _setEnumerator.MoveNext();
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Dispose()
            {
                _setEnumerator.Dispose();
                _group.Unlock();
            }
        }
    }
}