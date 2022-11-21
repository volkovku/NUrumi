using System.Runtime.CompilerServices;

namespace NUrumi
{
    /// <summary>
    /// Represents a field of component.
    /// </summary>
    /// <typeparam name="TValue">A type of component field.</typeparam>
    public sealed class Field<TValue> : IField<TValue> where TValue : unmanaged
    {
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
        /// <param name="entityIndex">An index of an entity.</param>
        /// <returns>A value of this field in entity.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TValue Get(int entityIndex)
        {
            return _storage.Get<TValue>(entityIndex, _offset);
        }

        /// <summary>
        /// Returns this field value from entity with specified index as reference.
        /// </summary>
        /// <param name="entityIndex">An index of an entity.</param>
        /// <returns>A value of this field in entity.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe ref TValue GetRef(int entityIndex)
        {
            return ref *((TValue*) _storage.Get(entityIndex, _offset));
        }

        /// <summary>
        /// Returns this field value from entity with specified index as reference.
        /// If value does not set then set it as default and returns it as a reference.
        /// </summary>
        /// <param name="entityIndex">An index of an entity.</param>
        /// <returns>A value of this field in entity.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe ref TValue GetOrAdd(int entityIndex)
        {
            return ref *((TValue*) _storage.GetOrSet<TValue>(entityIndex, _offset));
        }

        /// <summary>
        /// Returns this field value from entity with specified index as reference.
        /// If value does not set then set it as default and returns it as a reference.
        /// </summary>
        /// <param name="entityIndex">An index of an entity.</param>
        /// <param name="value">A value which should be set if entity does not have value.</param>
        /// <returns>A value of this field in entity.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe ref TValue GetOrSet(int entityIndex, TValue value)
        {
            return ref *((TValue*) _storage.GetOrSet<TValue>(entityIndex, _offset, value));
        }

        /// <summary>
        /// Sets field value to entity with specified index.
        /// </summary>
        /// <param name="entityIndex">An index of an entity.</param>
        /// <param name="value">A value to set.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set(int entityIndex, TValue value)
        {
            _storage.Set(entityIndex, _offset, value);
        }

        void IField.Init(string name, int fieldIndex, int fieldOffset, ComponentStorageData storage)
        {
            _name = name;
            _index = fieldIndex;
            _offset = fieldOffset;
            _storage = storage;
        }
    }

    public interface IField<TValue> : IField where TValue : unmanaged
    {
        TValue Get(int entityIndex);
        void Set(int entityIndex, TValue value);
    }

    public interface IField
    {
        int Index { get; }
        int Offset { get; }
        int ValueSize { get; }
        string Name { get; }
        void Init(string name, int fieldIndex, int fieldOffset, ComponentStorageData storage);
    }
}