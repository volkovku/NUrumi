using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace NUrumi
{
    /// <summary>
    /// Represents a field that indexes the entities by it value.
    /// </summary>
    /// <typeparam name="TValue"></typeparam>
    public sealed class IndexField<TValue> :
        IField<TValue>,
        IQuery,
        IContextResizeCallback
        where TValue : unmanaged, IEquatable<TValue>
    {
        private readonly Dictionary<TValue, EntitiesSet> _entities = new Dictionary<TValue, EntitiesSet>();
        private TValue?[] _currentValues;

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
                    return sizeof(TValue);
                }
            }
        }

        /// <summary>
        /// Returns this field value from entity with specified index.
        /// </summary>
        /// <param name="entityId">An entity identity.</param>
        /// <returns>A value of this field in entity.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TValue Get(int entityId)
        {
            return _storage.Get<TValue>(entityId, _offset);
        }

        /// <summary>
        /// Try to get this field value from entity with specified index.
        /// </summary>
        /// <param name="entityId">An entity identity.</param>
        /// <param name="result">A field value if exists.</param>
        /// <returns>Returns true if value exists, otherwise false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGet(int entityId, out TValue result)
        {
            return _storage.TryGet(entityId, _offset, out result);
        }

        /// <summary>
        /// Sets field value to entity with specified index.
        /// </summary>
        /// <param name="entityId">An entity identity.</param>
        /// <param name="value">A value to set.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set(int entityId, TValue value)
        {
            if (_storage.TryGet<TValue>(entityId, _offset, out var prev))
            {
                if (prev.Equals(value))
                {
                    return;
                }

                _entities[prev].Remove(entityId);
            }

            _storage.Set(entityId, _offset, value);
            _currentValues[entityId] = value;

            if (!_entities.TryGetValue(value, out var index))
            {
                index = new EntitiesSet(_storage.Entities.Length);
                _entities.Add(value, index);
            }

            index.Add(entityId);
        }

        /// <summary>
        /// Returns an enumerator of entities associated with the specified value.
        /// </summary>
        /// <param name="value">A value to search.</param>
        /// <returns>An enumerator of entities associated with specified value.</returns>
        public EntitiesSet GetEntitiesAssociatedWith(TValue value)
        {
            return _entities.TryGetValue(value, out var index) ? index : EntitiesSet.Empty;
        }

        void IField.Init(string name, int fieldIndex, int fieldOffset, ComponentStorageData storage)
        {
            _name = name;
            _index = fieldIndex;
            _offset = fieldOffset;
            _currentValues = new TValue?[storage.Entities.Length];
            _storage = storage;
            _storage.AddQuery(this);
        }

        void IQuery.Update(int entityId, bool added)
        {
            if (added)
            {
                return;
            }

            var currValue = _currentValues[entityId];
            if (!currValue.HasValue)
            {
                return;
            }

            _entities[currValue.Value].Remove(entityId);
        }

        void IContextResizeCallback.ResizeEntities(int newSize)
        {
            Array.Resize(ref _currentValues, newSize);
            foreach (var entitiesSet in _entities.Values)
            {
                entitiesSet.ResizeEntities(newSize);
            }
        }
    }
}