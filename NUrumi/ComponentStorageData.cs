using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using NUrumi.Exceptions;

namespace NUrumi
{
    public sealed class ComponentStorageData
    {
        internal const int ReservedSize = 0;
        internal readonly IComponent Component;
        internal readonly int ComponentSize;
        internal int[] Entities;
        internal int[] RecycledRecords;
        internal int RecordsCapacity;
        internal unsafe byte* Records;
        internal int RecordsCount;
        internal int RecycledRecordsCount;

        public unsafe ComponentStorageData(
            IComponent component,
            int componentSize,
            int entitiesInitialCapacity,
            int recordsInitialCapacity,
            int recycledRecordsInitialCapacity)
        {
            Component = component;
            ComponentSize = componentSize + ReservedSize;
            Entities = new int[entitiesInitialCapacity];
            Records = (byte*) Marshal.AllocHGlobal(recordsInitialCapacity * ComponentSize);
            RecordsCapacity = recordsInitialCapacity;
            RecordsCount = 1; // 0 index record used as default values entity
            RecycledRecords = new int[recycledRecordsInitialCapacity];
            RecycledRecordsCount = 0;

            FillWithZero(Records, RecordsCapacity * ComponentSize);
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
        public static int RecordsCount(this ComponentStorageData data) => data.RecordsCount - 1;
        public static int RecycledRecordsCount(this ComponentStorageData data) => data.RecycledRecordsCount;

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
            return ref *((TValue*) Get(data, entityIndex, fieldOffset));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void* Get(this ComponentStorageData data, int entityIndex, int fieldOffset)
        {
            var recordIndex = data.Entities[entityIndex];
            // if (recordIndex == 0)
            // {
            //     var field = data.Component.Fields.Single(_ => _.Offset == fieldOffset);
            //     throw NUrumiExceptions.ComponentNotFound(entityIndex, data.Component, field);
            // }

            var recordOffset = recordIndex * data.ComponentSize;
            var recordFieldOffset = recordOffset + ComponentStorageData.ReservedSize + fieldOffset;

            return data.Records + recordFieldOffset;
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
            var recordIndex = data.Entities[entityIndex];
            if (recordIndex == 0)
            {
                Set(data, entityIndex, fieldOffset, ref value);
            }

            recordIndex = data.Entities[entityIndex];
            var recordOffset = recordIndex * data.ComponentSize;
            var recordFieldOffset = recordOffset + ComponentStorageData.ReservedSize + fieldOffset;

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
            var recordIndex = data.Entities[entityIndex];
            var recordOffset = recordIndex * data.ComponentSize;
            var recordFieldOffset = recordOffset + ComponentStorageData.ReservedSize + fieldOffset;
            result = *(TValue*) (data.Records + recordFieldOffset);
            return recordIndex != 0;
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
            var recordIndex = entities[entityIndex];
            if (recordIndex == 0)
            {
                if (data.RecycledRecordsCount > 0)
                {
                    recordIndex = data.RecycledRecords[--data.RecycledRecordsCount];
                }
                else
                {
                    recordIndex = data.RecordsCount;
                    if (recordIndex == data.RecordsCapacity)
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

                    data.RecordsCount += 1;
                }

                entities[entityIndex] = recordIndex;
            }

            var recordOffset = recordIndex * data.ComponentSize;
            var p = data.Records + recordOffset + ComponentStorageData.ReservedSize + fieldOffset;
            *(TValue*) p = value;
        }

        public static unsafe bool Remove(this ComponentStorageData data, int entityIndex)
        {
            var entities = data.Entities;
            ref var recordIndex = ref entities[entityIndex];
            if (recordIndex == 0)
            {
                return false;
            }

            var recycledRecords = data.RecycledRecords;
            var p = data.Records;
            var componentSize = data.ComponentSize;
            var recordOffset = recordIndex * componentSize;
            ref var recycledRecordsCount = ref data.RecycledRecordsCount;

            if (recycledRecordsCount == recycledRecords.Length)
            {
                Array.Resize(ref data.RecycledRecords, recycledRecordsCount << 1);
                recycledRecords = data.RecycledRecords;
            }

            Buffer.MemoryCopy(p, p + recordOffset, componentSize, componentSize);

            recycledRecords[recycledRecordsCount] = recordIndex;
            recycledRecordsCount += 1;
            recordIndex = 0;

            return true;
        }
    }
}