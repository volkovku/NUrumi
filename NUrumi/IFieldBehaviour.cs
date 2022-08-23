namespace NUrumi
{
    public interface IFieldBehaviour<TValue>
    {
        bool TryGet<TComponent>(
            IStorage storage,
            EntityId entityId,
            TComponent component,
            int fieldIndex,
            out TValue value)
            where TComponent : Component<TComponent>, new();

        void Set<TComponent>(
            IStorage storage,
            EntityId entityId,
            TComponent component,
            int fieldIndex,
            TValue value)
            where TComponent : Component<TComponent>, new();
    }
}