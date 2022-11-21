using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using NUrumi.Exceptions;

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
            _componentStorages = InitRegistry(registry, config);
        }

        /// <summary>
        /// A registry with components of this context.
        /// </summary>
        public readonly TRegistry Registry;

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
        public long CreateEntity()
        {
            int entityIndex;
            if (_recycledEntities.Count >= _reuseEntitiesBarrier)
            {
                entityIndex = _recycledEntities.Dequeue();
                ref var gen = ref _entities[entityIndex];
                gen = -gen + 1;
                return EntityId.Create(gen, entityIndex);
            }

            entityIndex = _entitiesCount;
            if (entityIndex == _entities.Length)
            {
                var newSize = entityIndex << 1;
                Array.Resize(ref _entities, newSize);
                for (var i = 0; i < _componentStorages.Length; i++)
                {
                    _componentStorages[i].ResizeEntities(newSize);
                }
            }

            _entities[entityIndex] = 1;
            _entitiesCount += 1;

            return EntityId.Create(1, entityIndex);
        }

        /// <summary>
        /// Removes entity from this context.
        /// </summary>
        /// <param name="entityId">An identifier of an entity to remove.</param>
        /// <returns>True if entity was removed, otherwise false.</returns>
        public bool RemoveEntity(long entityId)
        {
            var entityIndex = EntityId.Index(entityId);
            ref var gen = ref _entities[entityIndex];
            if (gen <= 0)
            {
                return false;
            }

            gen *= -1;
            _recycledEntities.Enqueue(entityIndex);

            var componentStorages = _componentStorages;
            for (var i = 0; i < componentStorages.Length; i++)
            {
                var storage = componentStorages[i];
                storage.Remove(entityIndex);
            }

            return true;
        }

        /// <summary>
        /// Determines is entity with specified identifier are leave.
        /// </summary>
        /// <param name="entityId"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsAlive(long entityId)
        {
            return _entities[EntityId.Index(entityId)] == EntityId.Gen(entityId);
        }

        private static ComponentStorageData[] InitRegistry(TRegistry registry, Config config)
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
                foreach (var valueFieldInfo in componentType.GetFields())
                {
                    if (!typeof(IField).IsAssignableFrom(valueFieldInfo.FieldType))
                    {
                        continue;
                    }

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
                    config.InitialComponentRecordsCapacity,
                    config.InitialComponentRecycledRecordsCapacity);

                var fieldIndex = 0;
                var fieldOffset = 0;
                var fields = new List<IField>();
                foreach (var valueFieldInfo in componentType.GetFields())
                {
                    if (!typeof(IField).IsAssignableFrom(valueFieldInfo.FieldType))
                    {
                        continue;
                    }

                    var valueField =
                        (IField) valueFieldInfo.GetValue(component)
                        ?? (IField) Activator.CreateInstance(valueFieldInfo.FieldType);

                    var valueSize = valueField.ValueSize;
                    valueField.Init(valueFieldInfo.Name, fieldIndex, fieldOffset, storage);
                    valueFieldInfo.SetValue(component, valueField);

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

        public TValue Get<TField, TValue>(Func<TRegistry, TField> field, long entityId)
            where TField : IField<TValue>
            where TValue : unmanaged
        {
            EnsureAlive(entityId);
            return field(Registry).Get(EntityId.Index(entityId));
        }

        public void Set<TField, TValue>(TField field, long entityId, TValue value)
            where TField : IField<TValue>
            where TValue : unmanaged
        {
            EnsureAlive(entityId);
            field.Set(EntityId.Index(entityId), value);
        }

        public void Set<TField, TValue>(Func<TRegistry, TField> field, long entityId, TValue value)
            where TField : IField<TValue>
            where TValue : unmanaged
        {
            EnsureAlive(entityId);
            field(Registry).Set(EntityId.Index(entityId), value);
        }

        public bool Has<TComponent>(Func<TRegistry, TComponent> component, long entityId)
            where TComponent : Component<TComponent>, new()
        {
            EnsureAlive(entityId);
            return component(Registry).IsAPartOf(EntityId.Index(entityId));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureAlive(long entityId)
        {
            if (!IsAlive(entityId))
            {
                throw new NUrumiException(
                    "Access to dead entity (" +
                    $"entity_index={EntityId.Index(entityId)}," +
                    $"entity_gen={EntityId.Gen(entityId)})");
            }
        }
    }
}