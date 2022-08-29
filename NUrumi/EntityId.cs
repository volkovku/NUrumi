using System.Runtime.CompilerServices;

namespace NUrumi
{
    public static class EntityId
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long Create(int gen, int index)
        {
            return ((long) gen << 32) + index;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Gen(long id)
        {
            return (int) (id >> 32);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Index(long id)
        {
            return (int) id;
        }
    }
}