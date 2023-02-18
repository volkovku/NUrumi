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
        internal readonly int ComponentSize;
        internal int[] Entities;
        internal int RecordsCapacity;
        internal unsafe byte* Records;
        internal int RecordsLastOffset;
        internal int RecordsCount;

        internal IUpdateCallback[] UpdateCallbacks = new IUpdateCallback[10];
        internal int UpdateCallbacksCount;

        public unsafe ComponentStorageData(
            IComponent component,
            int componentSize,
            int entitiesInitialCapacity,
            int recordsInitialCapacity)
        {
            Component = component;
            ComponentSize = componentSize + ReservedSize;
            Entities = new int[entitiesInitialCapacity];
            Records = (byte*) Marshal.AllocHGlobal(recordsInitialCapacity * ComponentSize);
            RecordsCapacity = recordsInitialCapacity;
            RecordsCount = 0;
            RecordsLastOffset = 0;
            FillWithZero(Records, RecordsCapacity * ComponentSize);
        }

        public int EntitiesCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => RecordsCount;
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
            return ref *((TValue*) Get(fieldOffset, entityIndex));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void* Get(int fieldOffset, int entityIndex)
        {
            var recordOffset = Entities[entityIndex];
            if (recordOffset != 0)
            {
                return Records + recordOffset + fieldOffset;
            }

            // Extract method for performance optimization
            // .net generates a lot of boilerplate IL code if exception is thrown in this scope
            return ThrowComponentNotFound(fieldOffset, entityIndex);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void* GetOrSet<TValue>(
            int entityIndex,
            int fieldOffset)
            where TValue : unmanaged
        {
            return GetOrSet<TValue>(entityIndex, fieldOffset, default);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void* GetOrSet<TValue>(
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
            return Records + recordFieldOffset;
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
            result = *(TValue*) (Records + recordFieldOffset);
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

                var recordIndex = RecordsCount;
                if (recordIndex == RecordsCapacity - 1)
                {
                    var newCapacity = RecordsCapacity << 1;
                    var newSize = newCapacity * ComponentSize;
                    var oldSize = RecordsCapacity * ComponentSize;
                    var newRecords = (byte*) Marshal.AllocHGlobal(newSize);
                    ComponentStorageData.FillWithZero(newRecords + oldSize, newSize - oldSize);
                    Buffer.MemoryCopy(Records, newRecords, newSize, oldSize);
                    Marshal.FreeHGlobal((IntPtr) Records);
                    Records = newRecords;
                    RecordsCapacity = newCapacity;
                }

                recordOffset = (recordIndex + 1) * ComponentSize;
                *(int*) (Records + recordOffset) = entityIndex;
                RecordsCount += 1;
                RecordsLastOffset = recordOffset;
                entities[entityIndex] = recordOffset;

                NotifyAfterChanges(entityIndex, true);
            }

            var p = Records + recordOffset + fieldOffset;
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

            var p = Records;
            var lastRecordOffset = RecordsLastOffset;
            var componentSize = ComponentSize;
            if (recordOffset == lastRecordOffset)
            {
                Buffer.MemoryCopy(p, p + recordOffset, componentSize, componentSize);
                RecordsCount -= 1;
                RecordsLastOffset -= componentSize;
                recordOffset = 0;

                NotifyAfterChanges(entityIndex, false);

                return true;
            }

            var recordEntityIndex = *(int*) (Records + lastRecordOffset);
            entities[recordEntityIndex] = recordOffset;

            Buffer.MemoryCopy(p + lastRecordOffset, p + recordOffset, componentSize, componentSize);
            Buffer.MemoryCopy(p, p + lastRecordOffset, componentSize, componentSize);

            recordOffset = 0;
            RecordsCount -= 1;
            RecordsLastOffset -= componentSize;

            NotifyAfterChanges(entityIndex, false);

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void NotifyBeforeChanges(int entityIndex, bool added)
        {
            var queriesCount = UpdateCallbacksCount;
            if (queriesCount == 0)
            {
                return;
            }

            var queries = UpdateCallbacks;
            for (var i = 0; i < queriesCount; i++)
            {
                queries[i].BeforeChange(entityIndex, added);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void NotifyAfterChanges(int entityIndex, bool added)
        {
            var queriesCount = UpdateCallbacksCount;
            if (queriesCount == 0)
            {
                return;
            }

            var queries = UpdateCallbacks;
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
            var index = UpdateCallbacksCount;
            if (index == UpdateCallbacks.Length)
            {
                Array.Resize(ref UpdateCallbacks, UpdateCallbacksCount << 1);
            }

            UpdateCallbacks[index] = callback;
            UpdateCallbacksCount += 1;
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