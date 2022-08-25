using System.Threading;

namespace NUrumi
{
    public struct FieldWith<TBehaviour, TValue> : IField, IField<TValue>
        where TBehaviour : IFieldBehaviour<TValue>, new()
    {
        private int _index;
        private int _componentIndex;

        public int Index => _index;
        public string Name { get; private set; }
        public int ComponentIndex => _componentIndex;

        public bool TryGet<TComponent>(Storage storage, EntityId entityId, TComponent component, out TValue value)
            where TComponent : Component<TComponent>, new()
        {
            return FieldBehaviours<TBehaviour, TValue>.Instance.TryGet(
                storage,
                entityId,
                component,
                _index,
                out value);
        }

        public void Set<TComponent>(Storage storage, EntityId entityId, TComponent component, TValue value)
            where TComponent : Component<TComponent>, new()
        {
            FieldBehaviours<TBehaviour, TValue>.Instance.Set(
                storage,
                entityId,
                component,
                _index,
                value);
        }

        private bool Remove(Storage storage, EntityId entityId)
        {
            return FieldBehaviours<TBehaviour, TValue>.Instance.Remove(
                storage,
                entityId,
                _index,
                out _);
        }

        bool IField.Remove(Storage storage, EntityId entityId)
        {
            return Remove(storage, entityId);
        }

        bool IField<TValue>.Remove(Storage storage, EntityId entityId)
        {
            return Remove(storage, entityId);
        }

        void IField.SetMetaData(int index, int componentIndex, string name)
        {
            _index = index;
            _componentIndex = componentIndex;
            Name = name;
        }
    }

    public interface IField<TValue>
    {
        int Index { get; }
        int ComponentIndex { get; }

        bool TryGet<TComponent>(Storage storage, EntityId entityId, TComponent component, out TValue value)
            where TComponent : Component<TComponent>, new();

        void Set<TComponent>(Storage storage, EntityId entityId, TComponent component, TValue value)
            where TComponent : Component<TComponent>, new();

        bool Remove(Storage storage, EntityId entityId);
    }

    internal interface IField
    {
        int Index { get; }
        string Name { get; }
        int ComponentIndex { get; }

        void SetMetaData(int index, int componentIndex, string name);
        bool Remove(Storage storage, EntityId entityId);
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