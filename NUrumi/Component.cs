using System.Collections.Generic;

namespace NUrumi
{
    public abstract class Component<TComponent> :
        IComponent
        where TComponent : Component<TComponent>, new()
    {
        private int _index;
        private IField[] _fields;

        public int Index => _index;
        public IReadOnlyList<IField> Fields => _fields;
        public UnsafeComponentStorage Storage;

        public void Init(int index, IField[] fields, UnsafeComponentStorage storage)
        {
            _index = index;
            _fields = fields;
            Storage = storage;
        }

        public bool Contains(int entityIndex)
        {
            return Storage.Has(entityIndex);
        }
    }

    public interface IComponent
    {
        void Init(int index, IField[] fields, UnsafeComponentStorage storage);
    }
}