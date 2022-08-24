using System.Threading;

namespace NUrumi
{
    public struct FieldWith<TBehaviour, TValue> : IField, IField<TValue>
        where TBehaviour : IFieldBehaviour<TValue>, new()
    {
        private int _index;

        public int Index => _index;
        public string Name { get; private set; }

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

        private bool Remove(IStorage storage, EntityId entityId)
        {
            return FieldBehaviours<TBehaviour, TValue>.Instance.Remove(
                storage,
                entityId,
                _index,
                out _);
        }

        bool IField.Remove(IStorage storage, EntityId entityId)
        {
            return Remove(storage, entityId);
        }

        bool IField<TValue>.Remove(IStorage storage, EntityId entityId)
        {
            return Remove(storage, entityId);
        }

        void IField.SetMetaData(int index, string name)
        {
            _index = index;
            Name = name;
        }
    }

    public interface IField<TValue>
    {
        bool TryGet<TComponent>(IStorage storage, EntityId entityId, TComponent component, out TValue value)
            where TComponent : Component<TComponent>, new();

        void Set<TComponent>(IStorage storage, EntityId entityId, TComponent component, TValue value)
            where TComponent : Component<TComponent>, new();

        bool Remove(IStorage storage, EntityId entityId);
    }
    
    internal interface IField
    {
        int Index { get; }
        string Name { get; }

        void SetMetaData(int index, string name);
        bool Remove(IStorage storage, EntityId entityId);
    }

    internal static class FieldIndex
    {
        private static int _nextIndex;

        public static int Next()
        {
            return Interlocked.Increment(ref _nextIndex) - 1;
        }
    }

    internal static class FieldBehaviours<TBehaviour, TValue>
        where TBehaviour : IFieldBehaviour<TValue>, new()
    {
        internal static readonly TBehaviour Instance = new TBehaviour();
    }
}