using System;
using System.Text;

namespace NUrumi
{
    /// <summary>
    /// Represents a reference to an entity in a context.
    /// </summary>
    public readonly struct Entity
    {
        private readonly Context _context;
        private readonly IStorage _storage;

        /// <summary>
        /// Initializes a new instance of reference on an entity in a context.
        /// </summary>
        /// <param name="context">A context where entity live.</param>
        /// <param name="storage">A context storage.</param>
        /// <param name="id">An identifier of an entity.</param>
        public Entity(Context context, IStorage storage, EntityId id)
        {
            _context = context;
            _storage = storage;
            Id = id;
        }

        /// <summary>
        /// An unique identifier of this entity in a context.
        /// </summary>
        public readonly EntityId Id;

        /// <summary>
        /// Determines is this entity has specified component.
        /// </summary>
        /// <typeparam name="TComponent">Component to lookup.</typeparam>
        /// <returns>Returns true if entity has component otherwise returns false.</returns>
        public bool Has<TComponent>() where TComponent : Component<TComponent>, new()
        {
            EnsureAlive();
            return _storage.Has<TComponent>(Id);
        }

        /// <summary>
        /// Removes specified component from entity.
        /// </summary>
        /// <typeparam name="TComponent">Component to lookup.</typeparam>
        /// <returns>True if component was removed otherwise false.</returns>
        public bool Remove<TComponent>() where TComponent : Component<TComponent>, new()
        {
            EnsureAlive();
            return _storage.Remove<TComponent>(Id);
        }

        /// <summary>
        /// Returns value of specified component field. 
        /// </summary>
        /// <param name="field">A component field provider.</param>
        /// <typeparam name="TComponent">A type of component.</typeparam>
        /// <typeparam name="TValue">A type of field value.</typeparam>
        /// <returns>Returns value of specified component field.</returns>
        /// <exception cref="Exception">If component not set throws an exception.</exception>
        public TValue Get<TComponent, TValue>(Func<TComponent, IField<TValue>> field)
            where TComponent : Component<TComponent>, new()
        {
            EnsureAlive();
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
            EnsureAlive();
            var component = Component.InstanceOf<TComponent>();
            return field(component).TryGet(_storage, Id, component, out value);
        }

        public void Set<TComponent, TValue>(Func<TComponent, IField<TValue>> field, TValue value)
            where TComponent : Component<TComponent>, new()
        {
            EnsureAlive();
            var component = Component.InstanceOf<TComponent>();
            field(component).Set(_storage, Id, component, value);
        }

        public With<TComponent> With<TComponent>()
            where TComponent : Component<TComponent>, new()
        {
            EnsureAlive();
            var component = Component.InstanceOf<TComponent>();
            return new With<TComponent>(Id, _context, _storage, component);
        }

        public void Destroy()
        {
            _context.Destroy(Id);
        }

        public void DumpTo(StringBuilder sb)
        {
        }

        private void EnsureAlive()
        {
            if (!_context.IsAlive(Id))
            {
                throw new NUrumiException(
                    "Entity destroyed (" +
                    $"entity_ix={Id.Index}," +
                    $"entity_gen={Id.Generation})");
            }
        }
    }
}