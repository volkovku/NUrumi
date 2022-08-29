using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace NUrumi
{
    public sealed class UnsafeComponentStorage
    {
        private const int ReservedSize = 0;

        private readonly int _componentSize;
        private int[] _entities;
        private int[] _recycledRecords;
        private int _recycledRecordsCount;
        private unsafe byte* _records;
        private int _recordsCount;
        private int _recordsCapacity;

        public unsafe UnsafeComponentStorage(
            int componentSize,
            int entitiesInitialCapacity,
            int recordsInitialCapacity,
            int recycledRecordsInitialCapacity)
        {
            _componentSize = componentSize + ReservedSize;
            _entities = new int[entitiesInitialCapacity];
            _records = (byte*) Marshal.AllocHGlobal(recordsInitialCapacity * _componentSize);
            _recordsCapacity = recordsInitialCapacity;
            _recordsCount = 1; // 0 index record used as default values entity
            _recycledRecords = new int[recycledRecordsInitialCapacity];
            _recycledRecordsCount = 0;

            FillWithZero(_records, _recordsCapacity * _componentSize);
        }

        public int RecordsCount => _recordsCount - 1;
        public int RecycledRecordsCount => _recycledRecordsCount;

        public void ResizeEntities(int newSize)
        {
            Array.Resize(ref _entities, newSize);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Has(int entityIndex)
        {
            return _entities[entityIndex] != 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe ref TValue Get<TValue>(int entityIndex, int fieldOffset) where TValue : unmanaged
        {
            return ref *((TValue*) Get(entityIndex, fieldOffset));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void* Get(int entityIndex, int fieldOffset)
        {
            var recordIndex = _entities[entityIndex];
            // if (recordIndex == 0)
            // {
            //     // TODO: Specify exception to better explanation
            //     throw new NUrumiException(
            //         "Entity does not has component (" +
            //         $"entity_index={entityIndex}," +
            //         $"field_index={fieldOffset})");
            // }
            
            var recordOffset = recordIndex * _componentSize;
            var recordFieldOffset = recordOffset + ReservedSize + fieldOffset;

            return _records + recordFieldOffset;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set<TValue>(int entityIndex, int fieldOffset, TValue value) where TValue : unmanaged
        {
            Set(entityIndex, fieldOffset, ref value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void Set<TValue>(int entityIndex, int fieldOffset, ref TValue value) where TValue : unmanaged
        {
            var entities = _entities;
            var recordIndex = entities[entityIndex];
            if (recordIndex == 0)
            {
                if (_recycledRecordsCount > 0)
                {
                    recordIndex = _recycledRecords[--_recycledRecordsCount];
                }
                else
                {
                    recordIndex = _recordsCount;
                    if (recordIndex == _recordsCapacity)
                    {
                        var newCapacity = _recordsCapacity << 1;
                        var newSize = newCapacity * _componentSize;
                        var oldSize = _recordsCapacity * _componentSize;
                        var newRecords = (byte*) Marshal.AllocHGlobal(newSize);
                        FillWithZero(newRecords + oldSize, newSize - oldSize);
                        Buffer.MemoryCopy(_records, newRecords, newSize, oldSize);
                        Marshal.FreeHGlobal((IntPtr) _records);
                        _records = newRecords;
                        _recordsCapacity = newCapacity;
                    }

                    _recordsCount += 1;
                }

                entities[entityIndex] = recordIndex;
            }

            var recordOffset = recordIndex * _componentSize;
            var p = _records + recordOffset + ReservedSize + fieldOffset;
            *(TValue*) p = value;
        }

        public unsafe bool Remove(int entityIndex)
        {
            var entities = _entities;
            ref var recordIndex = ref entities[entityIndex];
            if (recordIndex == 0)
            {
                return false;
            }

            var recycledRecords = _recycledRecords;
            var p = _records;
            var componentSize = _componentSize;
            var recordOffset = recordIndex * componentSize;
            ref var recycledRecordsCount = ref _recycledRecordsCount;

            if (recycledRecordsCount == recycledRecords.Length)
            {
                Array.Resize(ref _recycledRecords, recycledRecordsCount << 1);
                recycledRecords = _recycledRecords;
            }

            Buffer.MemoryCopy(p, p + recordOffset, componentSize, componentSize);

            recycledRecords[recycledRecordsCount] = recordIndex;
            recycledRecordsCount += 1;
            recordIndex = 0;

            return true;
        }

        private static unsafe void FillWithZero(byte* array, int size)
        {
            var p = array;
            for (var i = 0; i < size; i++)
            {
                *p = 0;
                p += 1;
            }
        }
    }
}