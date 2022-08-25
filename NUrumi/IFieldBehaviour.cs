namespace NUrumi
{
    public interface IFieldBehaviour<TValue>
    {
        bool TryGet<TComponent>(
            Storage storage,
            EntityId entityId,
            TComponent component,
            int fieldIndex,
            out TValue value)
            where TComponent : Component<TComponent>, new();

        void Set<TComponent>(
            Storage storage,
            EntityId entityId,
            TComponent component,
            int fieldIndex,
            TValue value)
            where TComponent : Component<TComponent>, new();

        bool Remove(
            Storage storage,
            EntityId entityId,
            int fieldIndex,
            out TValue oldValue);

        bool TryGet(
            Storage storage,
            FieldQuickAccess<TValue> quickAccess,
            EntityId entityId,
            out TValue value);

        void Set(
            Storage storage,
            FieldQuickAccess<TValue> quickAccess,
            EntityId entityId,
            TValue value);

        bool Remove(
            Storage storage,
            FieldQuickAccess<TValue> quickAccess,
            EntityId entityId,
            out TValue oldValue);
    }
}