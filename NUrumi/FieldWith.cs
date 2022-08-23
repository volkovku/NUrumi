using System.Threading;

namespace NUrumi
{
    public struct FieldWith<TBehaviour, TValue> : IField, IField<TValue>
        where TBehaviour : IFieldBehaviour<TValue>, new()
    {
        private int _index;

        public int Index => _index;

        public bool TryGet<TComponent>(IStorage storage, EntityId entityId, TComponent component, out TValue value)
            where TComponent : Component<TComponent>, new()
        {
            return FieldBehaviours<TBehaviour, TValue>.Instance.TryGet(
                storage,
                entityId,
                component,
                _index,
                out value);
        }

        public void Set<TComponent>(IStorage storage, EntityId entityId, TComponent component, TValue value)
            where TComponent : Component<TComponent>, new()
        {
            FieldBehaviours<TBehaviour, TValue>.Instance.Set(
                storage,
                entityId,
                component,
                _index,
                value);
        }

        void IField.SetIndex(int index)
        {
            _index = index;
        }
    }

    public interface IField<TValue>
    {
        bool TryGet<TComponent>(IStorage storage, EntityId entityId, TComponent component, out TValue value)
            where TComponent : Component<TComponent>, new();

        void Set<TComponent>(IStorage storage, EntityId entityId, TComponent component, TValue value)
            where TComponent : Component<TComponent>, new();
    }

    internal interface IField
    {
        void SetIndex(int index);
    }

    internal static class FieldIndex
    {
        private static int _nextIndex;

        public static int Next()
        {
            return Interlocked.Increment(ref _nextIndex);
        }
    }

    internal static class FieldBehaviours<TBehaviour, TValue>
        where TBehaviour : IFieldBehaviour<TValue>, new()
    {
        internal static readonly TBehaviour Instance = new TBehaviour();
    }
}