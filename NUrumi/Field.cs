using System.Runtime.CompilerServices;

namespace NUrumi
{
    /// <summary>
    /// Represents a field of component which can effective operate with struct types.
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
        /// <param name="entityId">An entity identity.</param>
        /// <returns>A value of this field in entity.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TValue Get(int entityId)
        {
            return _storage.Get<TValue>(entityId, _offset);
        }

        /// <summary>
        /// Returns this field value from entity with specified index as reference.
        /// </summary>
        /// <param name="entityId">An entity identity.</param>
        /// <returns>A value of this field in entity.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe ref TValue GetRef(int entityId)
        {
            return ref *((TValue*) _storage.Get(_offset, entityId));
        }

        /// <summary>
        /// Returns this field value from entity with specified index as reference.
        /// If value does not set then set it as default and returns it as a reference.
        /// </summary>
        /// <param name="entityId">An entity identity.</param>
        /// <returns>A value of this field in entity.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe ref TValue GetOrAdd(int entityId)
        {
            return ref *((TValue*) _storage.GetOrSet<TValue>(entityId, _offset));
        }

        /// <summary>
        /// Returns this field value from entity with specified index as reference.
        /// If value does not set then set it as default and returns it as a reference.
        /// </summary>
        /// <param name="entityId">An entity identity.</param>
        /// <param name="value">A value which should be set if entity does not have value.</param>
        /// <returns>A value of this field in entity.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe ref TValue GetOrSet(int entityId, TValue value)
        {
            return ref *((TValue*) _storage.GetOrSet(entityId, _offset, value));
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
            _storage.Set(entityId, _offset, value);
        }

        void IField.Init(string name, int fieldIndex, int fieldOffset, ComponentStorageData storage)
        {
            _name = name;
            _index = fieldIndex;
            _offset = fieldOffset;
            _storage = storage;
        }
    }

    public static class FieldCompanion
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TValue Get<TValue>(this int entityId, IField<TValue> field) where TValue : unmanaged
        {
            return field.Get(entityId);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TValue Get<TValue>(this int entityId, RefField<TValue> field) where TValue : class
        {
            return field.Get(entityId);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TValue Get<TComponent, TValue>(this int entityId, Component<TComponent>.OfRef<TValue> component)
            where TComponent : Component<TComponent>, new()
            where TValue : class
        {
            return component.Get(entityId);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref TValue GetRef<TValue>(this int entityId, Field<TValue> field) where TValue : unmanaged
        {
            return ref field.GetRef(entityId);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref TValue GetRef<TComponent, TValue>(
            this int entityId,
            Component<TComponent>.Of<TValue> component)
            where TComponent : Component<TComponent>, new()
            where TValue : unmanaged
        {
            return ref component.GetRef(entityId);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryGet<TValue>(this int entityId, IField<TValue> field, out TValue value)
            where TValue : unmanaged
        {
            return field.TryGet(entityId, out value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryGet<TValue>(this int entityId, RefField<TValue> field, out TValue value)
            where TValue : class
        {
            return field.TryGet(entityId, out value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryGet<TComponent, TValue>(
            this int entityId,
            Component<TComponent>.OfRef<TValue> component,
            out TValue value)
            where TComponent : Component<TComponent>, new()
            where TValue : class
        {
            return component.TryGet(entityId, out value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Set<TValue>(this int entityId, IField<TValue> field, TValue value) where TValue : unmanaged
        {
            field.Set(entityId, value);
            return entityId;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Set<TValue>(this int entityId, Field<TValue> field, TValue value) where TValue : unmanaged
        {
            field.Set(entityId, value);
            return entityId;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Set<TValue>(this int entityId, RefField<TValue> field, TValue value) where TValue : class
        {
            field.Set(entityId, value);
            return entityId;
        }
    }

    public interface IField<TValue> : IField
    {
        TValue Get(int entityIndex);
        bool TryGet(int entityIndex, out TValue value);
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