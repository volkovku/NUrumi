using System;

namespace NUrumi
{
    public sealed class StorageValueSet<TValue> : IStorageValueSet
    {
        private const int None = -1;

        private int[] _entityIndex;
        private TValue[] _values;
        private int _freeValuesInitial;

        public StorageValueSet(int entitiesInitialCapacity, int valuesInitialCapacity)
        {
            _entityIndex = new int[entitiesInitialCapacity];
            ReverseIndex = new int[valuesInitialCapacity];
            _values = new TValue[valuesInitialCapacity];
            _freeValuesInitial = valuesInitialCapacity;
        }

        public int Count => _values.Length - _freeValuesInitial;
        public int[] ReverseIndex;

        public void ResizeEntities(int newSize)
        {
            Array.Resize(ref _entityIndex, newSize);
        }

        public TValue Get(int entityIndex)
        {
            var valueIndex = _entityIndex[entityIndex] - 1;
            if (valueIndex == None)
            {
                throw new NUrumiException($"Entity field value not found (entity_ix={entityIndex})");
            }

            return _values[valueIndex];
        }

        public bool TryGet(int entityIndex, out TValue value)
        {
            var valueIndex = _entityIndex[entityIndex] - 1;
            if (valueIndex == None)
            {
                value = default;
                return false;
            }

            value = _values[valueIndex];
            return true;
        }

        public bool Remove(int entityIndex, out TValue oldValue)
        {
            var valueIndex = _entityIndex[entityIndex] - 1;
            if (valueIndex == None)
            {
                oldValue = default;
                return false;
            }

            oldValue = _values[valueIndex];
            _entityIndex[entityIndex] = None + 1;

            var lastValueIndex = _values.Length - _freeValuesInitial - 1;
            if (lastValueIndex == valueIndex)
            {
                _freeValuesInitial += 1;
            }
            else
            {
                var movedEntityIndex = ReverseIndex[lastValueIndex];
                _entityIndex[movedEntityIndex] = valueIndex + 1;
                _values[valueIndex] = _values[lastValueIndex];
                ReverseIndex[valueIndex] = movedEntityIndex;
                _freeValuesInitial += 1;
            }

            return true;
        }

        public bool Set(int entityIndex, TValue value, out TValue oldValue)
        {
            var currentIndex = _entityIndex[entityIndex] - 1;
            if (currentIndex != None)
            {
                oldValue = _values[currentIndex];
                _values[currentIndex] = value;
                return false;
            }

            if (_freeValuesInitial == 0)
            {
                var newSize = _values.Length << 1;
                var newReverseIndex = new int[newSize];
                var newValues = new TValue[newSize];

                Array.Copy(ReverseIndex, newReverseIndex, ReverseIndex.Length);
                Array.Copy(_values, newValues, _values.Length);

                _freeValuesInitial = _values.Length;
                _values = newValues;
                ReverseIndex = newReverseIndex;
            }

            var valueIndex = _values.Length - _freeValuesInitial;
            _entityIndex[entityIndex] = valueIndex + 1;
            ReverseIndex[valueIndex] = entityIndex;
            _values[valueIndex] = value;
            _freeValuesInitial -= 1;

            oldValue = default;
            return true;
        }
    }

    public interface IStorageValueSet
    {
        void ResizeEntities(int newSize);
    }
}