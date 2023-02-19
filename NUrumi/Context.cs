using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace NUrumi
{
    /// <summary>
    /// Represents a context where entities are leave.
    /// </summary>
    /// <typeparam name="TRegistry">A registry with components of this context.</typeparam>
    public sealed class Context<TRegistry> where TRegistry : Registry<TRegistry>, new()
    {
        private readonly ComponentStorageData[] _componentStorages;
        private readonly int _reuseEntitiesBarrier;
        private readonly Queue<int> _recycledEntities;
        private readonly Dictionary<IGroupFilter, Group<TRegistry>> _groups;
        private readonly List<IContextResizeCallback> _resizeCallbacks;

        private int[] _entities;
        private int _entitiesCount;

        /// <summary>
        /// Initializes a new instance of the Context class.
        /// </summary>
        /// <param name="registry">A registry with components of this context.</param>
        /// <param name="config">A configuration of this context.</param>
        public Context(TRegistry registry = null, Config config = null)
        {
            if (config == null)
            {
                config = new Config();
            }

            if (registry == null)
            {
                registry = Activator.CreateInstance<TRegistry>();
            }

            Registry = registry;
            _entities = new int[config.InitialEntitiesCapacity];
            _entitiesCount = 0;
            _recycledEntities = new Queue<int>();
            _reuseEntitiesBarrier = config.InitialReuseEntitiesBarrier;
            _resizeCallbacks = new List<IContextResizeCallback>();
            _componentStorages = InitRegistry(registry, config, _resizeCallbacks);
            _groups = new Dictionary<IGroupFilter, Group<TRegistry>>();
            Components = _componentStorages.Select(_ => _.Component).ToArray();
        }

        /// <summary>
        /// A registry with components of this context.
        /// </summary>
        public readonly TRegistry Registry;

        /// <summary>
        /// A collection of components of this context.
        /// </summary>
        public readonly IReadOnlyList<IComponent> Components;

        /// <summary>
        /// Count of leave entities in this context.
        /// </summary>
        public int LiveEntitiesCount => _entitiesCount - _recycledEntities.Count;

        /// <summary>
        /// Count of recycled entities.
        /// </summary>
        public int RecycledEntitiesCount => _recycledEntities.Count;

        /// <summary>
        /// Creates a new entity in this context.
        /// </summary>
        /// <returns>Returns an identity of new entity.</returns>
        public int CreateEntity()
        {
            int entityId;
            if (_recycledEntities.Count >= _reuseEntitiesBarrier)
            {
                entityId = _recycledEntities.Dequeue();
                ref var gen = ref _entities[entityId];
                gen = -gen + 1;
                return entityId;
            }

            entityId = _entitiesCount + 1;
            if (entityId >= _entities.Length)
            {
                var newSize = entityId << 1;
                Array.Resize(ref _entities, newSize);

                for (var i = 0; i < _componentStorages.Length; i++)
                {
                    _componentStorages[i].ResizeEntities(newSize);
                }

                foreach (var group in _groups.Values)
                {
                    group.ResizeEntities(newSize);
                }

                foreach (var resizeCallback in _resizeCallbacks)
                {
                    resizeCallback.ResizeEntities(newSize);
                }
            }

            _entities[entityId] = 1;
            _entitiesCount += 1;

            return entityId;
        }

        /// <summary>
        /// Removes entity from this context.
        /// </summary>
        /// <param name="entityId">An identifier of an entity to remove.</param>
        /// <returns>True if entity was removed, otherwise false.</returns>
        public bool RemoveEntity(int entityId)
        {
            ref var gen = ref _entities[entityId];
            if (gen <= 0)
            {
                return false;
            }

            gen *= -1;
            _recycledEntities.Enqueue(entityId);

            var componentStorages = _componentStorages;
            for (var i = 0; i < componentStorages.Length; i++)
            {
                var storage = componentStorages[i];
                storage.Remove(entityId);
            }

            return true;
        }

        /// <summary>
        /// Determines is entity with specified identifier are leave.
        /// </summary>
        /// <param name="entityId">An identifier of entity to check is it alive.</param>
        /// <param name="gen">A generation of alive entity.</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsAlive(int entityId, out int gen)
        {
            gen = _entities[entityId];
            return gen > 0;
        }

        /// <summary>
        /// Determines is entity with specified identifier are leave.
        /// </summary>
        /// <param name="entityId">An identifier of entity to check is it alive.</param>
        /// <param name="gen">A generation of alive entity.</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsAlive(int entityId, int gen)
        {
            return _entities[entityId] == gen;
        }

        /// <summary>
        /// Creates entities group with specified filter.
        /// </summary>
        /// <param name="filter">A filter for entities.</param>
        /// <returns>Return group with entities correspond to specified filter.</returns>
        public Group<TRegistry> CreateGroup(IGroupFilter filter)
        {
            if (_groups.TryGetValue(filter, out var group))
            {
                return group;
            }

            group = Group<TRegistry>.Create(this, filter, _entities.Length);
            for (var i = 0; i < _entities.Length; i++)
            {
                if (_entities[i] <= 0)
                {
                    continue;
                }

                group.Update(i);
            }

            _groups.Add(filter, group);
            return group;
        }

        /// <summary>
        /// Create collector.
        /// </summary>
        /// <returns>Returns new instance of collector.</returns>
        public Collector<TRegistry> CreateCollector()
        {
            var collector = new Collector<TRegistry>(this, _entities.Length);
            _resizeCallbacks.Add(collector);
            return collector;
        }

        private static ComponentStorageData[] InitRegistry(
            TRegistry registry,
            Config config,
            List<IContextResizeCallback> contextResizeCallbacks)
        {
            var componentIndex = 0;
            var componentStorages = new List<ComponentStorageData>();
            var registryType = typeof(TRegistry);
            foreach (var componentFieldInfo in registryType.GetFields())
            {
                var componentType = componentFieldInfo.FieldType;
                if (!typeof(IComponent).IsAssignableFrom(componentType))
                {
                    continue;
                }

                var component = (IComponent) Activator.CreateInstance(componentFieldInfo.FieldType);
                componentFieldInfo.SetValue(registry, component);

                var componentSize = 0;
                var fieldInfos = GetFields(componentType).Distinct().ToArray();
                foreach (var valueFieldInfo in fieldInfos)
                {
                    var valueField =
                        (IField) valueFieldInfo.GetValue(component)
                        ?? (IField) Activator.CreateInstance(valueFieldInfo.FieldType);

                    var valueSize = valueField.ValueSize;
                    componentSize += valueSize;
                }

                var storage = new ComponentStorageData(
                    component,
                    componentSize,
                    config.InitialEntitiesCapacity,
                    config.InitialComponentRecordsCapacity);

                var fieldIndex = 0;
                var fieldOffset = ComponentStorageData.ReservedSize;
                var fields = new List<IField>();
                foreach (var valueFieldInfo in fieldInfos)
                {
                    var valueField =
                        (IField) valueFieldInfo.GetValue(component)
                        ?? (IField) Activator.CreateInstance(valueFieldInfo.FieldType);

                    var valueSize = valueField.ValueSize;
                    valueField.Init(valueFieldInfo.Name, fieldIndex, fieldOffset, storage);
                    valueFieldInfo.SetValue(component, valueField);

                    if (valueField is IContextResizeCallback resizeCallback)
                    {
                        contextResizeCallbacks.Add(resizeCallback);
                    }

                    fieldIndex += 1;
                    fieldOffset += valueSize;
                    fields.Add(valueField);
                }

                component.Init(componentIndex, fields.ToArray(), storage);
                componentStorages.Add(storage);
                componentIndex += 1;
            }

            return componentStorages.ToArray();
        }

        private static IEnumerable<FieldInfo> GetFields(Type type)
        {
            while (true)
            {
                foreach (var valueFieldInfo in type.GetFields(
                             BindingFlags.Instance |
                             BindingFlags.Public |
                             BindingFlags.NonPublic))
                {
                    if (!typeof(IField).IsAssignableFrom(valueFieldInfo.FieldType))
                    {
                        continue;
                    }

                    yield return valueFieldInfo;
                }

                if (type.BaseType != null)
                {
                    type = type.BaseType;
                    continue;
                }

                break;
            }
        }
    }
}