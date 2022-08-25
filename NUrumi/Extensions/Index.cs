using System;
using System.Collections.Generic;

namespace NUrumi.Extensions
{
    public sealed class Index<TValue> :
        IFieldBehaviour<TValue>
        where TValue : IEquatable<TValue>
    {
        public bool TryGet<TComponent>(
            Storage storage,
            EntityId entityId,
            TComponent component,
            int fieldIndex,
            out TValue value)
            where TComponent : Component<TComponent>, new()
        {
            return storage.TryGet(entityId, component, fieldIndex, out value);
        }

        public void Set<TComponent>(
            Storage storage,
            EntityId entityId,
            TComponent component,
            int fieldIndex,
            TValue value)
            where TComponent : Component<TComponent>, new()
        {
            if (!storage.TryGet<IndexExtension<TValue>>(out var extension))
            {
                extension = new IndexExtension<TValue>();
                storage.Add(extension);
            }

            if (!storage.Set(entityId, component, fieldIndex, value, out var oldValue))
            {
                extension.Remove(fieldIndex, entityId, oldValue);
            }

            extension.Set(fieldIndex, entityId, value);
        }

        public bool Remove(Storage storage, EntityId entityId, int fieldIndex, out TValue oldValue)
        {
            if (!storage.TryGet<IndexExtension<TValue>>(out var extension))
            {
                extension = new IndexExtension<TValue>();
                storage.Add(extension);
            }

            if (storage.Remove(entityId, fieldIndex, out oldValue))
            {
                extension.Remove(fieldIndex, entityId, oldValue);
                return true;
            }

            return false;
        }

        public bool TryGet(
            Storage storage,
            FieldQuickAccess<TValue> quickAccess,
            EntityId entityId,
            out TValue value)
        {
            return quickAccess.TryGet(entityId, out value);
        }

        public void Set(
            Storage storage,
            FieldQuickAccess<TValue> quickAccess,
            EntityId entityId,
            TValue value)
        {
            if (!storage.TryGet<IndexExtension<TValue>>(out var extension))
            {
                extension = new IndexExtension<TValue>();
                storage.Add(extension);
            }

            if (!quickAccess.Set(entityId, value, out var oldValue))
            {
                extension.Remove(quickAccess.FieldIndex, entityId, oldValue);
            }

            extension.Set(quickAccess.FieldIndex, entityId, value);
        }

        public bool Remove(
            Storage storage,
            FieldQuickAccess<TValue> quickAccess,
            EntityId entityId,
            out TValue oldValue)
        {
            if (!storage.TryGet<IndexExtension<TValue>>(out var extension))
            {
                extension = new IndexExtension<TValue>();
                storage.Add(extension);
            }

            if (quickAccess.Remove(entityId, out oldValue))
            {
                extension.Remove(quickAccess.FieldIndex, entityId, oldValue);
                return true;
            }

            return false;
        }
    }

    public static class IndexCompanion
    {
        public static IReadOnlyCollection<EntityId> FindWith<TComponent, TValue>(
            this Storage storage,
            Func<TComponent, FieldWith<Index<TValue>, TValue>> field,
            TValue value)
            where TComponent : Component<TComponent>, new()
            where TValue : IEquatable<TValue>
        {
            if (!storage.TryGet<IndexExtension<TValue>>(out var extension))
            {
                return Array.Empty<EntityId>();
            }

            var component = Component.InstanceOf<TComponent>();
            var f = field(component);
            return extension.Get(f.Index, value);
        }
    }

    public sealed class IndexExtension<TValue> :
        Extension<IndexExtension<TValue>>
        where TValue : IEquatable<TValue>
    {
        private readonly Dictionary<Key, HashSet<EntityId>> _index = new Dictionary<Key, HashSet<EntityId>>();

        public IReadOnlyCollection<EntityId> Get(int fieldIndex, TValue value)
        {
            var key = new Key(fieldIndex, value);
            if (!_index.TryGetValue(key, out var set))
            {
                return Array.Empty<EntityId>();
            }

            return set;
        }

        public void Set(int fieldIndex, EntityId entityId, TValue value)
        {
            var key = new Key(fieldIndex, value);
            if (!_index.TryGetValue(key, out var set))
            {
                set = new HashSet<EntityId>();
                _index.Add(key, set);
            }

            set.Add(entityId);
        }

        public void Remove(int fieldIndex, EntityId entityId, TValue value)
        {
            var key = new Key(fieldIndex, value);
            if (!_index.TryGetValue(key, out var set))
            {
                return;
            }

            set.Remove(entityId);
        }

        private readonly struct Key : IEquatable<Key>
        {
            public Key(int fieldIndex, TValue value)
            {
                _fieldIndex = fieldIndex;
                _value = value;
            }

            private readonly int _fieldIndex;
            private readonly TValue _value;

            public bool Equals(Key other)
            {
                return _fieldIndex == other._fieldIndex && _value.Equals(other._value);
            }

            public override bool Equals(object obj)
            {
                return obj is Key other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (_fieldIndex * 397) ^ EqualityComparer<TValue>.Default.GetHashCode(_value);
                }
            }

            public static bool operator ==(Key left, Key right)
            {
                return left.Equals(right);
            }

            public static bool operator !=(Key left, Key right)
            {
                return !left.Equals(right);
            }
        }
    }
}