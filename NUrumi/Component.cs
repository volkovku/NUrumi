using System.Collections.Generic;

namespace NUrumi
{
    /// <summary>
    /// Represents a component.
    /// </summary>
    /// <typeparam name="TComponent">A type of derived component.</typeparam>
    public abstract partial class Component<TComponent> :
        IComponent
        where TComponent : Component<TComponent>, new()
    {
        private int _index;
        private IField[] _fields;

        /// <summary>
        /// An index of component in registry.
        /// </summary>
        public int Index => _index;

        /// <summary>
        /// A collection of fields associated with this component.
        /// </summary>
        public IReadOnlyList<IField> Fields => _fields;

        /// <summary>
        /// A component data storage.
        /// </summary>
        public ComponentStorageData Storage;

        /// <summary>
        /// Determines is this component a part of entity with specified identifier.
        /// </summary>
        /// <param name="entityId">An entity identity.</param>
        /// <returns>Returns true if this component is a part of entity; otherwise false.</returns>
        public bool IsAPartOf(int entityId)
        {
            return Storage.Has(entityId);
        }

        /// <summary>
        /// Removes component from entity with specified identifier.
        /// </summary>
        /// <param name="entityId">An identifier of an entity.</param>
        /// <returns>Returns true if component was removed, otherwise false.</returns>
        public bool RemoveFrom(int entityId)
        {
            return Storage.Remove(entityId);
        }

        ComponentStorageData IComponent.Storage => Storage;

        void IComponent.Init(int index, IField[] fields, ComponentStorageData storage)
        {
            _index = index;
            _fields = fields;
            Storage = storage;
        }
    }

    public static class ComponentCompanion
    {
        public static bool Remove<TComponent>(this int entityId, Component<TComponent> component)
            where TComponent : Component<TComponent>, new()
        {
            return component.RemoveFrom(entityId);
        }
    }

    public interface IComponent
    {
        int Index { get; }
        ComponentStorageData Storage { get; }
        IReadOnlyList<IField> Fields { get; }
        void Init(int index, IField[] fields, ComponentStorageData storage);
    }
}