using System;

namespace NUrumi
{
    public struct EntityId : IEquatable<EntityId>
    {
        public EntityId(int index, short generation)
        {
            Index = index;
            Generation = generation;
        }

        public readonly int Index;
        public readonly short Generation;

        public bool Equals(EntityId other)
        {
            return Index == other.Index && Generation == other.Generation;
        }

        public override bool Equals(object obj)
        {
            return obj is EntityId other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Index * 397) ^ Generation;
            }
        }

        public static bool operator ==(EntityId left, EntityId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(EntityId left, EntityId right)
        {
            return !left.Equals(right);
        }
    }
}