using NUrumi.Storages.Safe;

namespace NUrumi
{
    public sealed class FieldQuickAccess<TValue>
    {
        private readonly StorageValueSet<TValue> _valueSet;

        public readonly int FieldIndex;

        public FieldQuickAccess(StorageValueSet<TValue> valueSet, int fieldIndex)
        {
            _valueSet = valueSet;
            FieldIndex = fieldIndex;
        }

        public TValue Get(EntityId entityId)
        {
            if (!TryGet(entityId, out var value))
            {
                throw new NUrumiException(
                    "Entity does not has component (" +
                    $"entity_ix={entityId.Index}," +
                    $"entity_gen={entityId.Generation})");
            }

            return value;
        }

        public bool TryGet(EntityId entityId, out TValue value)
        {
            return _valueSet.TryGet(entityId.Index, out value);
        }

        public void Set(EntityId entityId, TValue value)
        {
            _valueSet.Set(entityId.Index, value, out _);
        }

        public bool Set(EntityId entityId, TValue value, out TValue oldValue)
        {
            return _valueSet.Set(entityId.Index, value, out oldValue);
        }

        public bool Remove(EntityId entityId, out TValue oldValue)
        {
            return _valueSet.Remove(entityId.Index, out oldValue);
        }
    }
}