using System.Collections.Generic;

namespace NUrumi
{
    /// <summary>
    /// Represents a component.
    /// </summary>
    /// <typeparam name="TComponent">A type of derived component.</typeparam>
    public abstract class Component<TComponent> :
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
        /// Determines is this component a part of entity with specified index.
        /// </summary>
        /// <param name="entityIndex">An index of entity to check.</param>
        /// <returns>Returns true if this component is a part of entity; otherwise false.</returns>
        public bool IsAPartOf(int entityIndex)
        {
            return Storage.Has(entityIndex);
        }

        ComponentStorageData IComponent.Storage => Storage;

        void IComponent.Init(int index, IField[] fields, ComponentStorageData storage)
        {
            _index = index;
            _fields = fields;
            Storage = storage;
        }
    }

    public interface IComponent
    {
        ComponentStorageData Storage { get; }
        IReadOnlyList<IField> Fields { get; }
        void Init(int index, IField[] fields, ComponentStorageData storage);
    }
}