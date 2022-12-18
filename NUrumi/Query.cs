namespace NUrumi
{
    public sealed class Query : IQuery
    {
        private readonly bool[] _conditions;
        private readonly ComponentStorageData[] _componentStorages;
        private readonly bool _singleInclude;
        private readonly EntitiesSet _entities;

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
            _entities = new EntitiesSet(entitiesCapacity);
        }

        public int EntitiesCount => _entities.EntitiesCount;

        public int GetEntities(ref int[] result)
        {
            return _entities.GetEntities(ref result);
        }

        public EntitiesSet.Enumerator GetEnumerator()
        {
            return _entities.GetEnumerator();
        }

        void IQuery.Update(int entityIndex, bool added)
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