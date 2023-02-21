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
        private unsafe byte*[] _entities;

        private readonly int _componentSize;
        private int _recordsCapacity;
        private unsafe byte* _records;
        private unsafe byte* _zero;
        private unsafe byte* _recordsLast;
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
            _entities = new byte*[entitiesInitialCapacity];

            _componentSize = componentSize + ReservedSize;
            _records = (byte*) Marshal.AllocHGlobal(recordsInitialCapacity * _componentSize);
            _zero = (byte*) Marshal.AllocHGlobal(_componentSize);
            _recordsCapacity = recordsInitialCapacity;
            _recordsCount = 0;
            _recordsLast = _records;
            FillWithZero(_records, _recordsCapacity * _componentSize);
            FillWithZero(_zero, _componentSize);
        }

        public unsafe int EntitiesCapacity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _entities.Length;
        }

        public int EntitiesCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _recordsCount;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void ResizeEntities(int newSize)
        {
            var newEntities = new byte*[newSize];
            Array.Copy(_entities, newEntities, _entities.Length);
            _entities = newEntities;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool Has(int entityIndex)
        {
            return _entities[entityIndex] != null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe ref TValue Get<TValue>(
            int entityIndex,
            int fieldOffset)
            where TValue : unmanaged
        {
            var record = _entities[entityIndex];
            if (record != null)
            {
                return ref *(TValue*) (record + fieldOffset);
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
            var record = _entities[entityIndex];
            if (record == null)
            {
                Set(entityIndex, fieldOffset, ref value);
            }

            return ref *(TValue*) (record + fieldOffset);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool TryGet<TValue>(
            int entityIndex,
            int fieldOffset,
            out TValue result)
            where TValue : unmanaged
        {
            var record = _entities[entityIndex];
            if (record == null)
            {
                result = default;
                return false;
            }

            result = *(TValue*) (record + fieldOffset);
            return true;
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
            var entities = _entities;
            var record = entities[entityIndex];
            if (record == null)
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

                    record = _records;
                    for (var i = 0; i < recordIndex; i++)
                    {
                        _entities[*(int*) record] = record;
                        record += _componentSize;
                    }
                }
                else
                {
                    record = _records + (recordIndex * _componentSize);
                }

                *(int*) record = entityIndex;
                _recordsCount += 1;
                _recordsLast = record;
                entities[entityIndex] = record;

                NotifyAfterChanges(entityIndex, true);
            }

            *(TValue*) (record + fieldOffset) = value;
        }

        public unsafe bool Remove(int entityIndex)
        {
            var entities = _entities;
            ref var record = ref entities[entityIndex];
            if (record == null)
            {
                // Record already removed
                return false;
            }

            NotifyBeforeChanges(entityIndex, false);

            var lastRecord = _recordsLast;
            var componentSize = _componentSize;
            if (record == lastRecord)
            {
                Buffer.MemoryCopy(_zero, record, componentSize, componentSize);
                _recordsCount -= 1;
                _recordsLast -= componentSize;
                record = null;

                NotifyAfterChanges(entityIndex, false);

                return true;
            }

            var recordEntityIndex = *(int*) lastRecord;
            entities[recordEntityIndex] = record;

            Buffer.MemoryCopy(lastRecord, record, componentSize, componentSize);
            Buffer.MemoryCopy(_zero, lastRecord, componentSize, componentSize);

            record = null;
            _recordsCount -= 1;
            _recordsLast -= componentSize;

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