using NUrumi.Extensions;

namespace NUrumi
{
    public interface IStorage
    {
        bool TryGet<TExtension>(out TExtension extension)
            where TExtension : Extension<TExtension>;

        void Add<TExtension>(TExtension extension)
            where TExtension : Extension<TExtension>;

        void RemoveEntity(EntityId entityId);
        
        bool Has<TComponent>(EntityId entityId)
            where TComponent : Component<TComponent>, new();

        bool Remove<TComponent>(EntityId id)
            where TComponent : Component<TComponent>, new();

        bool TryGet<TComponent, TValue>(EntityId entityId, TComponent component, int fieldIndex, out TValue value)
            where TComponent : Component<TComponent>, new();

        bool Set<TComponent, TValue>(
            EntityId entityId,
            TComponent component,
            int fieldIndex,
            TValue value,
            out TValue oldValue)
            where TComponent : Component<TComponent>, new();

        bool Remove<TValue>(EntityId entityId, int fieldIndex, out TValue oldValue);
    }
}