using System.Runtime.CompilerServices;

namespace NUrumi
{
    public sealed class Field<TValue> : IField<TValue> where TValue : unmanaged
    {
        public int Index;
        public int Offset;
        public UnsafeComponentStorage Storage;

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

        public void Init(int fieldIndex, int fieldOffset, UnsafeComponentStorage storage)
        {
            Index = fieldIndex;
            Offset = fieldOffset;
            Storage = storage;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TValue Get(int entityIndex)
        {
            return Storage.Get<TValue>(entityIndex, Offset);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe ref TValue GetRef(int entityIndex)
        {
            return ref *((TValue*) Storage.Get(entityIndex, Offset));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set(int entityIndex, TValue value)
        {
            Storage.Set(entityIndex, Offset, value);
        }
    }

    public interface IField<TValue> : IField where TValue : unmanaged
    {
        TValue Get(int entityIndex);
        void Set(int entityIndex, TValue value);
    }

    public interface IField
    {
        int ValueSize { get; }
        void Init(int fieldIndex, int fieldOffset, UnsafeComponentStorage storage);
    }
}