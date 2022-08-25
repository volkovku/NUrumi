using System;

namespace NUrumi.Storages.Safe
{
    public sealed class StorageValueSet<TValue>
    {
        private const int None = -1;

        private int[] _entityIndex;
        private TValue[] _values;
        private int _freeValues;

        public StorageValueSet(int initialCapacity)
        {
            _entityIndex = new int[initialCapacity];
            ReverseIndex = new int[initialCapacity];
            _values = new TValue[initialCapacity];
            _freeValues = initialCapacity;
        }

        public int Count;
        public int[] ReverseIndex;

        public bool TryGet(int entityIndex, out TValue value)
        {
            if (entityIndex >= _entityIndex.Length)
            {
                value = default;
                return false;
            }

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
            if (entityIndex >= _entityIndex.Length)
            {
                oldValue = default;
                return false;
            }

            var valueIndex = _entityIndex[entityIndex] - 1;
            if (valueIndex == None)
            {
                oldValue = default;
                return false;
            }

            oldValue = _values[valueIndex];
            _entityIndex[entityIndex] = None + 1;

            var lastValueIndex = _values.Length - _freeValues - 1;
            if (lastValueIndex == valueIndex)
            {
                _freeValues += 1;
                Count -= 1;
            }
            else
            {
                var movedEntityIndex = ReverseIndex[lastValueIndex];
                _entityIndex[movedEntityIndex] = valueIndex + 1;
                _values[valueIndex] = _values[lastValueIndex];
                ReverseIndex[valueIndex] = movedEntityIndex;
                _freeValues += 1;
                Count -= 1;
            }

            return true;
        }

        public bool Set(int entityIndex, TValue value, out TValue oldValue)
        {
            if (entityIndex >= _entityIndex.Length)
            {
                var newIndex = new int[entityIndex << 1];
                Array.Copy(_entityIndex, newIndex, _entityIndex.Length);
                _entityIndex = newIndex;
            }

            var currentIndex = _entityIndex[entityIndex] - 1;
            if (currentIndex != None)
            {
                oldValue = _values[currentIndex];
                _values[currentIndex] = value;
                return false;
            }

            if (_freeValues == 0)
            {
                var newSize = _values.Length << 1;
                var newReverseIndex = new int[newSize];
                var newValues = new TValue[newSize];

                Array.Copy(ReverseIndex, newReverseIndex, ReverseIndex.Length);
                Array.Copy(_values, newValues, _values.Length);

                _freeValues = _values.Length;
                _values = newValues;
                ReverseIndex = newReverseIndex;
            }

            var valueIndex = _values.Length - _freeValues;
            _entityIndex[entityIndex] = valueIndex + 1;
            ReverseIndex[valueIndex] = entityIndex;
            _values[valueIndex] = value;
            _freeValues -= 1;
            Count += 1;

            oldValue = default;
            return true;
        }
    }
}