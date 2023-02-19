using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using NUrumi.Exceptions;

namespace NUrumi
{
    public sealed class ComponentStorageData
    {
        public const int ReservedSize = sizeof(int); // index

        internal readonly IComponent Component;
        internal int[] Entities;

        private readonly int _componentSize;
        private int _recordsCapacity;
        private unsafe byte* _records;
        private int _recordsLastOffset;
        private int _recordsCount;

        private IUpdateCallback[] _updateCallbacks = new IUpdateCallback[10];
        private int _updateCallbacksCount;

        public unsafe ComponentStorageData(
            IComponent component,
            int componentSize,
            int entitiesInitialCapacity,
            int recordsInitialCapacity)
        {
            Component = component;
            Entities = new int[entitiesInitialCapacity];

            _componentSize = componentSize + ReservedSize;
            _records = (byte*) Marshal.AllocHGlobal(recordsInitialCapacity * _componentSize);
            _recordsCapacity = recordsInitialCapacity;
            _recordsCount = 0;
            _recordsLastOffset = 0;
            FillWithZero(_records, _recordsCapacity * _componentSize);
        }

        public int EntitiesCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _recordsCount;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ResizeEntities(int newSize)
        {
            Array.Resize(ref Entities, newSize);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Has(int entityIndex)
        {
            return Entities[entityIndex] != 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe ref TValue Get<TValue>(
            int entityIndex,
            int fieldOffset)
            where TValue : unmanaged
        {
            var offset = Entities[entityIndex] + fieldOffset;
            if (offset != fieldOffset)
            {
                return ref *(TValue*) (_records + offset);
            }

            // Extract method for performance optimization
            // .net generates a lot of boilerplate IL code if exception is thrown in this scope
            return ref *(TValue*) ThrowComponentNotFound(fieldOffset, entityIndex);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref TValue GetOrSet<TValue>(
            int entityIndex,
            int fieldOffset)
            where TValue : unmanaged
        {
            return ref GetOrSet<TValue>(entityIndex, fieldOffset, default);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe ref TValue GetOrSet<TValue>(
            int entityIndex,
            int fieldOffset,
            TValue value)
            where TValue : unmanaged
        {
            var recordOffset = Entities[entityIndex];
            if (recordOffset == 0)
            {
                Set(entityIndex, fieldOffset, ref value);
            }

            recordOffset = Entities[entityIndex];
            var recordFieldOffset = recordOffset + +fieldOffset;
            return ref *(TValue*) (_records + recordFieldOffset);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool TryGet<TValue>(
            int entityIndex,
            int fieldOffset,
            out TValue result)
            where TValue : unmanaged
        {
            var recordOffset = Entities[entityIndex];
            var recordFieldOffset = recordOffset + fieldOffset;
            result = *(TValue*) (_records + recordFieldOffset);
            return recordOffset != 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set<TValue>(
            int entityIndex,
            int fieldOffset,
            TValue value)
            where TValue : unmanaged
        {
            Set(entityIndex, fieldOffset, ref value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void Set<TValue>(
            int entityIndex,
            int fieldOffset,
            ref TValue value)
            where TValue : unmanaged
        {
            var entities = Entities;
            var recordOffset = entities[entityIndex];
            if (recordOffset == 0)
            {
                NotifyBeforeChanges(entityIndex, true);

                var recordIndex = _recordsCount;
                if (recordIndex == _recordsCapacity - 1)
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

                recordOffset = (recordIndex + 1) * _componentSize;
                *(int*) (_records + recordOffset) = entityIndex;
                _recordsCount += 1;
                _recordsLastOffset = recordOffset;
                entities[entityIndex] = recordOffset;

                NotifyAfterChanges(entityIndex, true);
            }

            var p = _records + recordOffset + fieldOffset;
            *(TValue*) p = value;
        }

        public unsafe bool Remove(int entityIndex)
        {
            var entities = Entities;
            ref var recordOffset = ref entities[entityIndex];
            if (recordOffset == 0)
            {
                // Record already removed
                return false;
            }

            NotifyBeforeChanges(entityIndex, false);

            var p = _records;
            var lastRecordOffset = _recordsLastOffset;
            var componentSize = _componentSize;
            if (recordOffset == lastRecordOffset)
            {
                Buffer.MemoryCopy(p, p + recordOffset, componentSize, componentSize);
                _recordsCount -= 1;
                _recordsLastOffset -= componentSize;
                recordOffset = 0;

                NotifyAfterChanges(entityIndex, false);

                return true;
            }

            var recordEntityIndex = *(int*) (_records + lastRecordOffset);
            entities[recordEntityIndex] = recordOffset;

            Buffer.MemoryCopy(p + lastRecordOffset, p + recordOffset, componentSize, componentSize);
            Buffer.MemoryCopy(p, p + lastRecordOffset, componentSize, componentSize);

            recordOffset = 0;
            _recordsCount -= 1;
            _recordsLastOffset -= componentSize;

            NotifyAfterChanges(entityIndex, false);

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void NotifyBeforeChanges(int entityIndex, bool added)
        {
            var queriesCount = _updateCallbacksCount;
            if (queriesCount == 0)
            {
                return;
            }

            var queries = _updateCallbacks;
            for (var i = 0; i < queriesCount; i++)
            {
                queries[i].BeforeChange(entityIndex, added);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void NotifyAfterChanges(int entityIndex, bool added)
        {
            var queriesCount = _updateCallbacksCount;
            if (queriesCount == 0)
            {
                return;
            }

            var queries = _updateCallbacks;
            for (var i = 0; i < queriesCount; i++)
            {
                queries[i].AfterChange(entityIndex, added);
            }
        }

        private unsafe void* ThrowComponentNotFound(int fieldOffset, int entityIndex)
        {
            var field = Component.Fields.Single(_ => _.Offset == fieldOffset);
            throw NUrumiExceptions.ComponentNotFound(entityIndex, Component, field);
        }

        internal void AddUpdateCallback(IUpdateCallback callback)
        {
            var index = _updateCallbacksCount;
            if (index == _updateCallbacks.Length)
            {
                Array.Resize(ref _updateCallbacks, _updateCallbacksCount << 1);
            }

            _updateCallbacks[index] = callback;
            _updateCallbacksCount += 1;
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