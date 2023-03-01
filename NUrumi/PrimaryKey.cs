using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using NUrumi.Exceptions;

namespace NUrumi
{
    /// <summary>
    /// Represents a primary key field.
    /// Only one entity with specified key can exist in a context.
    /// </summary>
    /// <typeparam name="TKey">A type of a primary key.</typeparam>
    public class PrimaryKey<TKey> :
        IField<TKey>,
        IUpdateCallback
        where TKey : unmanaged, IEquatable<TKey>
    {
        private readonly Dictionary<TKey, int> _primaryIndex = new Dictionary<TKey, int>();

        private int _index;
        private int _offset;
        private string _name;
        private ComponentStorageData _storage;

        /// <summary>
        /// An zero-based index of the field in the component.
        /// </summary>
        // ReSharper disable once ConvertToAutoPropertyWithPrivateSetter
        // Use internal fields for performance reasons
        public int Index => _index;

        /// <summary>
        /// An data offset of the field in the component in bytes.
        /// </summary>
        // ReSharper disable once ConvertToAutoPropertyWithPrivateSetter
        // Use internal fields for performance reasons
        public int Offset => _offset;

        /// <summary>
        /// A name of this field.
        /// </summary>
        // ReSharper disable once ConvertToAutoPropertyWithPrivateSetter
        // Use internal fields for performance reasons
        public string Name => _name;

        /// <summary>
        /// A size of this field value in bytes.
        /// </summary>
        public int ValueSize
        {
            get
            {
                unsafe
                {
                    return sizeof(TKey);
                }
            }
        }

        /// <summary>
        /// Returns this field value from entity with specified index.
        /// </summary>
        /// <param name="entityId">An entity identity.</param>
        /// <returns>A value of this field in entity.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TKey Get(int entityId)
        {
            return _storage.Get<TKey>(entityId, _offset);
        }

        /// <summary>
        /// Try to get this field value from entity with specified index.
        /// </summary>
        /// <param name="entityId">An entity identity.</param>
        /// <param name="result">A field value if exists.</param>
        /// <returns>Returns true if value exists, otherwise false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGet(int entityId, out TKey result)
        {
            return _storage.TryGet(entityId, _offset, out result);
        }

        /// <summary>
        /// Sets field value to entity with specified index.
        /// </summary>
        /// <param name="entityId">An entity identity.</param>
        /// <param name="key">A value to set.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set(int entityId, TKey key)
        {
            if (_primaryIndex.TryGetValue(key, out var currentEntityId))
            {
                if (currentEntityId == entityId)
                {
                    return;
                }

                ThrowEntityAlreadyExistsException(key, currentEntityId, entityId);
            }

            if (_storage.TryGet<TKey>(entityId, _offset, out var prev))
            {
                if (prev.Equals(key))
                {
                    return;
                }

                _primaryIndex.Remove(prev);
            }

            _storage.Set(entityId, _offset, key);
            _primaryIndex[key] = entityId;
        }

        /// <summary>
        /// Returns an entity which associated with specified key.
        /// </summary>
        /// <param name="key">A key to lookup entity.</param>
        /// <returns>An identifier of entity which associated with specified key.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetEntityByKey(TKey key)
        {
            return TryGetEntityByKey(key, out var entityId)
                ? entityId
                : ThrowEntityNotFound(key);
        }

        /// <summary>
        /// Try to find an entity associated with the specified key.
        /// </summary>
        /// <param name="key">A key to lookup entity.</param>
        /// <param name="entityId">An identifier of found entity.</param>
        /// <returns>Returns true if entity found; otherwise false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetEntityByKey(TKey key, out int entityId)
        {
            return _primaryIndex.TryGetValue(key, out entityId);
        }

        void IField.Init(string name, int fieldIndex, int fieldOffset, ComponentStorageData storage)
        {
            _name = name;
            _index = fieldIndex;
            _offset = fieldOffset;
            _storage = storage;
            _storage.AddUpdateCallback(this);
        }

        void IUpdateCallback.BeforeChange(int entityIndex, bool added)
        {
            if (added)
            {
                return;
            }

            if (_storage.TryGet<TKey>(entityIndex, _offset, out var key))
            {
                _primaryIndex.Remove(key);
            }
        }

        void IUpdateCallback.AfterChange(int entityId, bool added)
        {
        }

        private static void ThrowEntityAlreadyExistsException(
            TKey key,
            int currentEntity,
            int targetEntity)
        {
            throw new NUrumiException(
                "Primary key constraint failed. Entity for key already exists (" +
                $"key={key}," +
                $"current_entity_id={currentEntity}," +
                $"target_entity_id={targetEntity})");
        }

        private static int ThrowEntityNotFound(TKey key)
        {
            throw new NUrumiException($"Entity not found (key={key})");
        }
    }

    public static class PrimaryKeyCompanion
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryGet<TValue>(
            this int entityId,
            PrimaryKey<TValue> field,
            out TValue value) where TValue : unmanaged, IEquatable<TValue>
        {
            return field.TryGet(entityId, out value);
        }
    }

    /// <summary>
    /// Represents a shortcut to components with only one field.
    /// </summary>
    public abstract partial class Component<TComponent> where TComponent : Component<TComponent>, new()
    {
        /// <summary>
        /// Represents a shortcut to components with only one PrimaryKey field.
        /// </summary>
        public abstract class OfPrimaryKey<TKey> : Component<TComponent> where TKey : unmanaged, IEquatable<TKey>
        {
#pragma warning disable CS0649
            private PrimaryKey<TKey> _field;
#pragma warning restore CS0649

            /// <summary>
            /// Returns this field value from entity with specified index.
            /// </summary>
            /// <param name="entityId">An entity identity.</param>
            /// <returns>A value of this field in entity.</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public TKey Get(int entityId) => _field.Get(entityId);

            /// <summary>
            /// Try to get this field value from entity with specified index.
            /// </summary>
            /// <param name="entityId">An entity identity.</param>
            /// <param name="result">A field value if exists.</param>
            /// <returns>Returns true if value exists, otherwise false.</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool TryGet(int entityId, out TKey result) => _field.TryGet(entityId, out result);

            /// <summary>
            /// Sets field value to entity with specified index.
            /// </summary>
            /// <param name="entityId">An entity identity.</param>
            /// <param name="value">A value to set.</param>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Set(int entityId, TKey value) => _field.Set(entityId, value);

            /// <summary>
            /// Returns an entity which associated with specified key.
            /// </summary>
            /// <param name="key">A key to lookup entity.</param>
            /// <returns>An identifier of entity which associated with specified key.</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public int GetEntityByKey(TKey key)
            {
                return _field.GetEntityByKey(key);
            }

            /// <summary>
            /// Try to find an entity associated with the specified key.
            /// </summary>
            /// <param name="key">A key to lookup entity.</param>
            /// <param name="entityId">An identifier of found entity.</param>
            /// <returns>Returns true if entity found; otherwise false.</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool TryGetEntityByKey(TKey key, out int entityId)
            {
                return _field.TryGetEntityByKey(key, out entityId);
            }

            public static implicit operator PrimaryKey<TKey>(OfPrimaryKey<TKey> componentOf)
            {
                return componentOf._field;
            }
        }
    }

    public static class ComponentPrimaryKeyCompanion
    {
        /// <summary>
        /// Returns this field value from entity with specified index.
        /// </summary>
        /// <param name="entityId">An entity identity.</param>
        /// <param name="component">A component which value should be return.</param>
        /// <returns>A value of this field in entity.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TValue Get<TComponent, TValue>(
            this int entityId,
            Component<TComponent>.OfPrimaryKey<TValue> component)
            where TComponent : Component<TComponent>, new()
            where TValue : unmanaged, IEquatable<TValue>
        {
            return component.Get(entityId);
        }

        /// <summary>
        /// Try to get this component value from entity with specified index.
        /// </summary>
        /// <param name="entityId">An entity identity.</param>
        /// <param name="component">A component which value should be return.</param>
        /// <param name="value">A component value if exists.</param>
        /// <returns>Returns true if value exists, otherwise false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryGet<TComponent, TValue>(
            this int entityId,
            Component<TComponent>.OfPrimaryKey<TValue> component,
            out TValue value)
            where TComponent : Component<TComponent>, new()
            where TValue : unmanaged, IEquatable<TValue>
        {
            return component.TryGet(entityId, out value);
        }

        /// <summary>
        /// Sets field value to entity with specified index.
        /// </summary>
        /// <param name="entityId">An entity identity.</param>
        /// <param name="component">A component which value should be set.</param>
        /// <param name="value">A value to set.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Set<TComponent, TValue>(
            this int entityId,
            Component<TComponent>.OfPrimaryKey<TValue> component,
            TValue value)
            where TComponent : Component<TComponent>, new()
            where TValue : unmanaged, IEquatable<TValue>
        {
            component.Set(entityId, value);
            return entityId;
        }
    }
}