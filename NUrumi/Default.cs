namespace NUrumi
{
    public sealed class Default<TValue> : IFieldBehaviour<TValue>
    {
        public bool TryGet<TComponent>(
            Storage storage,
            EntityId entityId,
            TComponent component,
            int fieldIndex,
            out TValue value)
            where TComponent : Component<TComponent>, new()
        {
            return storage.TryGet(entityId, component, fieldIndex, out value);
        }

        public void Set<TComponent>(
            Storage storage,
            EntityId entityId,
            TComponent component,
            int fieldIndex,
            TValue value)
            where TComponent : Component<TComponent>, new()
        {
            storage.Set(entityId, component, fieldIndex, value, out _);
        }

        public bool Remove(Storage storage, EntityId entityId, int fieldIndex, out TValue oldValue)
        {
            return storage.Remove(entityId, fieldIndex, out oldValue);
        }

        public bool TryGet(
            Storage storage,
            FieldQuickAccess<TValue> quickAccess,
            EntityId entityId,
            out TValue value)
        {
            return quickAccess.TryGet(entityId, out value);
        }

        public void Set(
            Storage storage,
            FieldQuickAccess<TValue> quickAccess,
            EntityId entityId,
            TValue value)
        {
            quickAccess.Set(entityId, value, out _);
        }

        public bool Remove(
            Storage storage,
            FieldQuickAccess<TValue> quickAccess,
            EntityId entityId,
            out TValue oldValue)
        {
            return quickAccess.Remove(entityId, out oldValue);
        }
    }
}