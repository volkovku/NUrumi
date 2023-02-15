using System;

namespace NUrumi
{
    /// <summary>
    /// Represents a field for store class type values.
    /// It less efficient than working with struct types but can be useful in some cases.
    /// </summary>
    /// <typeparam name="TValue"></typeparam>
    public class RefField<TValue> :
        IField<TValue>,
        IUpdateCallback,
        IContextResizeCallback
        where TValue : class
    {
        private int _index;
        private int _offset;
        private string _name;
        private ComponentStorageData _storage;

        private TValue[] _values = new TValue[100];
        private int?[] _entities;
        private int _valuesCount;
        private int[] _freeValues = new int[100];
        private int _freeValuesCount;

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
        public int ValueSize => sizeof(int);

        /// <summary>
        /// Returns this field value from entity with specified index.
        /// </summary>
        /// <param name="entityIndex">An entity identity.</param>
        /// <returns>A value of this field in entity.</returns>
        public TValue Get(int entityIndex)
        {
            var ix = _storage.Get<int>(entityIndex, _offset);
            return _values[ix];
        }

        /// <summary>
        /// Try to get this field value from entity with specified index.
        /// </summary>
        /// <param name="entityIndex">An entity identity.</param>
        /// <param name="result">A field value if exists.</param>
        /// <returns>Returns true if value exists, otherwise false.</returns>
        public bool TryGet(int entityIndex, out TValue result)
        {
            if (!_storage.TryGet<int>(entityIndex, _offset, out var ix))
            {
                result = default;
                return false;
            }

            result = _values[ix];
            return true;
        }

        public void Set(int entityIndex, TValue value)
        {
            int ix;
            if (_freeValuesCount > 0)
            {
                var freeIx = _freeValuesCount - 1;
                _freeValuesCount = freeIx;
                ix = _freeValues[freeIx];
            }
            else
            {
                ix = _valuesCount;
                _valuesCount++;
                if (ix == _values.Length)
                {
                    Array.Resize(ref _values, _valuesCount << 1);
                }
            }

            _values[ix] = value;
            _entities[entityIndex] = ix;
            _storage.Set(entityIndex, _offset, ix);
        }

        void IField.Init(string name, int fieldIndex, int fieldOffset, ComponentStorageData storage)
        {
            _name = name;
            _index = fieldIndex;
            _offset = fieldOffset;
            _entities = new int?[storage.Entities.Length];
            _storage = storage;
            _storage.AddUpdateCallback(this);
        }

        void IUpdateCallback.BeforeChange(int entityIndex, bool added)
        {
        }

        void IUpdateCallback.AfterChange(int entityIndex, bool added)
        {
            if (added)
            {
                return;
            }

            var optValueIndex = _entities[entityIndex];
            if (!optValueIndex.HasValue)
            {
                return;
            }

            var ix = optValueIndex.Value;
            _values[ix] = default;

            var freeIx = _freeValuesCount;
            _freeValuesCount++;
            if (freeIx == _freeValues.Length)
            {
                Array.Resize(ref _freeValues, freeIx << 1);
            }

            _freeValues[freeIx] = ix;
        }

        void IContextResizeCallback.ResizeEntities(int newSize)
        {
            Array.Resize(ref _entities, newSize);
        }
    }
}