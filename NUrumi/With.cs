using System;

namespace NUrumi
{
    public readonly struct With<TComponent> where TComponent : Component<TComponent>, new()
    {
        private readonly EntityId _entityId;
        private readonly IStorage _storage;
        private readonly TComponent _component;

        internal With(EntityId entityId, IStorage storage, TComponent component)
        {
            _entityId = entityId;
            _storage = storage;
            _component = component;
        }

        public TValue Get<TValue>(Func<TComponent, IField<TValue>> field)
        {
            if (!field(_component).TryGet(_storage, _entityId, _component, out var value))
            {
                throw new Exception(
                    "Entity does not has component (" +
                    $"entity_ix={_entityId.Index}," +
                    $"entity_gen={_entityId.Generation}," +
                    $"component={typeof(TComponent).Name})");
            }

            return value;
        }

        public With<TComponent> Set<TValue>(Func<TComponent, IField<TValue>> field, TValue value)
        {
            field(_component).Set(_storage, _entityId, _component, value);
            return this;
        }
    }
}