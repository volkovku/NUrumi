using System;
using System.Runtime.CompilerServices;

namespace NUrumi
{
    /// <summary>
    /// Represents a special kind of field which can be used to track self changes.
    /// </summary>
    /// <typeparam name="TValue"></typeparam>
    public class ReactiveField<TValue> : IField<TValue> where TValue : unmanaged, IEquatable<TValue>
    {
        private int _index;
        private int _offset;
        private string _name;
        private ComponentStorageData _storage;

        /// <summary>
        /// Describes an event handler of reactive field value changes.
        /// </summary>
        /// <param name="component">A component which field value was changed</param>
        /// <param name="field">A field which value was changed</param>
        /// <param name="entityId">An identifier of an entity which value was changed</param>
        public delegate void OnReactiveFieldValueChangedEventHandler(
            IComponent component,
            IField field,
            int entityId,
            TValue? oldValue,
            TValue newValue);

        /// <summary>
        /// An event which raises when field value changes.
        /// </summary>
        public event OnReactiveFieldValueChangedEventHandler OnValueChanged;

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
            TValue? oldValue = default;
            if (_storage.TryGet<TValue>(entityId, _offset, out var prev))
            {
                if (prev.Equals(value))
                {
                    return;
                }

                oldValue = prev;
            }

            _storage.Set(entityId, _offset, value);

            RaiseValueChanged(entityId, oldValue, value);
        }

        private void RaiseValueChanged(int entityId, TValue? oldValue, TValue newValue)
        {
            var handler = OnValueChanged;
            handler?.Invoke(_storage.Component, this, entityId, oldValue, newValue);
        }

        void IField.Init(string name, int fieldIndex, int fieldOffset, ComponentStorageData storage)
        {
            _name = name;
            _index = fieldIndex;
            _offset = fieldOffset;
            _storage = storage;
        }
    }
}