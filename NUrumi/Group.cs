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
        }

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
        public EntitiesSet.Enumerator GetEnumerator()
        {
            return _entities.GetEnumerator();
        }

        void IGroup.Update(int entityIndex, bool added)
        {
            if (_singleInclude)
            {
                if (added)
                {
                    _entities.Add(entityIndex);
                }
                else
                {
                    _entities.Remove(entityIndex);
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

                _entities.Remove(entityIndex);
                return;
            }

            _entities.Add(entityIndex);
        }

        internal void ResizeEntities(int newSize)
        {
            _entities.ResizeEntities(newSize);
        }
    }
}