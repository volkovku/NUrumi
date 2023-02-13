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

        internal static unsafe void FillWithZero(byte* array, int size)
        {
            var p = array;
            for (var i = 0; i < size; i++)
            {
                *p = 0;
                p += 1;
            }
        }
    }

    public static class UnsafeComponentStorage
    {
        public static int EntitiesCount(this ComponentStorageData data) => data.RecordsCount;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ResizeEntities(this ComponentStorageData data, int newSize)
        {
            Array.Resize(ref data.Entities, newSize);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Has(this ComponentStorageData data, int entityIndex)
        {
            return data.Entities[entityIndex] != 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe ref TValue Get<TValue>(
            this ComponentStorageData data,
            int entityIndex,
            int fieldOffset)
            where TValue : unmanaged
        {
            return ref *((TValue*) Get(data, fieldOffset, entityIndex));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void* Get(this ComponentStorageData data, int fieldOffset, int entityIndex)
        {
            var recordOffset = data.Entities[entityIndex];
            if (recordOffset != 0)
            {
                return data.Records + recordOffset + fieldOffset;
            }

            // Extract method for performance optimization
            // .net generates a lot of boilerplate IL code if exception is thrown in this scope
            return ThrowComponentNotFound(data, fieldOffset, entityIndex);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void* GetOrSet<TValue>(
            this ComponentStorageData data,
            int entityIndex,
            int fieldOffset)
            where TValue : unmanaged
        {
            return GetOrSet<TValue>(data, entityIndex, fieldOffset, default);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void* GetOrSet<TValue>(
            this ComponentStorageData data,
            int entityIndex,
            int fieldOffset,
            TValue value)
            where TValue : unmanaged
        {
            var recordOffset = data.Entities[entityIndex];
            if (recordOffset == 0)
            {
                Set(data, entityIndex, fieldOffset, ref value);
            }

            recordOffset = data.Entities[entityIndex];
            var recordFieldOffset = recordOffset + +fieldOffset;
            return data.Records + recordFieldOffset;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe bool TryGet<TValue>(
            this ComponentStorageData data,
            int entityIndex,
            int fieldOffset,
            out TValue result)
            where TValue : unmanaged
        {
            var recordOffset = data.Entities[entityIndex];
            var recordFieldOffset = recordOffset + fieldOffset;
            result = *(TValue*) (data.Records + recordFieldOffset);
            return recordOffset != 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Set<TValue>(
            this ComponentStorageData data,
            int entityIndex,
            int fieldOffset,
            TValue value)
            where TValue : unmanaged
        {
            Set(data, entityIndex, fieldOffset, ref value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void Set<TValue>(
            this ComponentStorageData data,
            int entityIndex,
            int fieldOffset,
            ref TValue value)
            where TValue : unmanaged
        {
            var entities = data.Entities;
            var recordOffset = entities[entityIndex];
            if (recordOffset == 0)
            {
                var recordIndex = data.RecordsCount;
                if (recordIndex == data.RecordsCapacity - 1)
                {
                    var newCapacity = data.RecordsCapacity << 1;
                    var newSize = newCapacity * data.ComponentSize;
                    var oldSize = data.RecordsCapacity * data.ComponentSize;
                    var newRecords = (byte*) Marshal.AllocHGlobal(newSize);
                    ComponentStorageData.FillWithZero(newRecords + oldSize, newSize - oldSize);
                    Buffer.MemoryCopy(data.Records, newRecords, newSize, oldSize);
                    Marshal.FreeHGlobal((IntPtr) data.Records);
                    data.Records = newRecords;
                    data.RecordsCapacity = newCapacity;
                }

                recordOffset = (recordIndex + 1) * data.ComponentSize;
                *(int*) (data.Records + recordOffset) = entityIndex;
                data.RecordsCount += 1;
                data.RecordsLastOffset = recordOffset;
                entities[entityIndex] = recordOffset;

                UpdateQueries(data, entityIndex, true);
            }

            var p = data.Records + recordOffset + fieldOffset;
            *(TValue*) p = value;
        }

        public static unsafe bool Remove(this ComponentStorageData data, int entityIndex)
        {
            var entities = data.Entities;
            ref var recordOffset = ref entities[entityIndex];
            if (recordOffset == 0)
            {
                // Record already removed
                return false;
            }

            var p = data.Records;
            var lastRecordOffset = data.RecordsLastOffset;
            var componentSize = data.ComponentSize;
            if (recordOffset == lastRecordOffset)
            {
                Buffer.MemoryCopy(p, p + recordOffset, componentSize, componentSize);
                data.RecordsCount -= 1;
                data.RecordsLastOffset -= componentSize;
                recordOffset = 0;

                UpdateQueries(data, entityIndex, false);

                return true;
            }

            var recordEntityIndex = *(int*) (data.Records + lastRecordOffset);
            entities[recordEntityIndex] = recordOffset;

            Buffer.MemoryCopy(p + lastRecordOffset, p + recordOffset, componentSize, componentSize);
            Buffer.MemoryCopy(p, p + lastRecordOffset, componentSize, componentSize);

            recordOffset = 0;
            data.RecordsCount -= 1;
            data.RecordsLastOffset -= componentSize;

            UpdateQueries(data, entityIndex, false);

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void UpdateQueries(ComponentStorageData data, int entityIndex, bool added)
        {
            var queries = data.UpdateCallbacks;
            var queriesCount = data.UpdateCallbacksCount;
            for (var i = 0; i < queriesCount; i++)
            {
                queries[i].Update(entityIndex, added);
            }
        }

        private static unsafe void* ThrowComponentNotFound(ComponentStorageData data, int fieldOffset, int entityIndex)
        {
            var field = data.Component.Fields.Single(_ => _.Offset == fieldOffset);
            throw NUrumiExceptions.ComponentNotFound(entityIndex, data.Component, field);
        }
    }
}