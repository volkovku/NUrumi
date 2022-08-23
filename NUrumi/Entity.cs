using System;

namespace NUrumi
{
    public struct Entity
    {
        private readonly IStorage _storage;

        public Entity(IStorage storage, EntityId id)
        {
            _storage = storage;
            Id = id;
        }

        public readonly EntityId Id;

        public bool Has<TComponent>() where TComponent : Component<TComponent>, new()
        {
            return _storage.Has<TComponent>(Id);
        }

        public TValue Get<TComponent, TValue>(Func<TComponent, IField<TValue>> field)
            where TComponent : Component<TComponent>, new()
        {
            var component = Component.InstanceOf<TComponent>();
            if (!field(component).TryGet(_storage, Id, component, out var value))
            {
                throw new Exception(
                    "Entity does not has component (" +
                    $"entity_ix={Id.Index}," +
                    $"entity_gen={Id.Generation}," +
                    $"component={typeof(TComponent).Name})");
            }

            return value;
        }

        public bool TryGet<TComponent, TValue>(Func<TComponent, IField<TValue>> field, out TValue value)
            where TComponent : Component<TComponent>, new()
        {
            var component = Component.InstanceOf<TComponent>();
            return field(component).TryGet(_storage, Id, component, out value);
        }

        public void Set<TComponent, TValue>(Func<TComponent, IField<TValue>> field, TValue value)
            where TComponent : Component<TComponent>, new()
        {
            var component = Component.InstanceOf<TComponent>();
            field(component).Set(_storage, Id, component, value);
        }

        public With<TComponent> With<TComponent>()
            where TComponent : Component<TComponent>, new()
        {
            var component = Component.InstanceOf<TComponent>();
            return new With<TComponent>(Id, _storage, component);
        }
    }
}