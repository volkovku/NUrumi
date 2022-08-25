using System;
using System.Runtime.CompilerServices;

namespace NUrumi
{
    public readonly struct With<TComponent> where TComponent : Component<TComponent>, new()
    {
        private readonly EntityId _entityId;
#if DEBUG
        private readonly Context _context;
#endif
        private readonly Storage _storage;
        private readonly TComponent _component;

#if DEBUG
        internal With(EntityId entityId, Context context, Storage storage, TComponent component)
        {
            _entityId = entityId;
            _context = context;
            _storage = storage;
            _component = component;
        }
#else
        internal With(EntityId entityId, Storage storage, TComponent component)
        {
            _entityId = entityId;
            _storage = storage;
            _component = component;
        }
#endif

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TValue Get<TValue>(Func<TComponent, IField<TValue>> field)
        {
#if DEBUG
            EnsureAlive();
#endif
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public With<TComponent> Set<TValue>(Func<TComponent, IField<TValue>> field, TValue value)
        {
#if DEBUG
            EnsureAlive();
#endif
            field(_component).Set(_storage, _entityId, _component, value);
            return this;
        }

#if DEBUG
        private void EnsureAlive()
        {
            if (!_context.IsAlive(_entityId))
            {
                throw new NUrumiException(
                    "Entity destroyed (" +
                    $"entity_ix={_entityId.Index}," +
                    $"entity_gen={_entityId.Generation})");
            }
        }
#endif
    }
}