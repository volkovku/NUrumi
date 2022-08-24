namespace NUrumi
{
    public sealed class Default<TValue> : IFieldBehaviour<TValue>
    {
        public bool TryGet<TComponent>(
            IStorage storage,
            EntityId entityId,
            TComponent component,
            int fieldIndex,
            out TValue value)
            where TComponent : Component<TComponent>, new()
        {
            return storage.TryGet(entityId, component, fieldIndex, out value);
        }

        public void Set<TComponent>(
            IStorage storage,
            EntityId entityId,
            TComponent component,
            int fieldIndex,
            TValue value)
            where TComponent : Component<TComponent>, new()
        {
            storage.Set(entityId, component, fieldIndex, value, out _);
        }

        public bool Remove(IStorage storage, EntityId entityId, int fieldIndex, out TValue oldValue)
        {
            return storage.Remove(entityId, fieldIndex, out oldValue);
        }
    }
}