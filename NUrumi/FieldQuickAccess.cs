namespace NUrumi
{
    public sealed class FieldQuickAccess<TValue>
    {
        private readonly StorageValueSet<bool> _component;
        private readonly StorageValueSet<TValue> _valueSet;

        public readonly int FieldIndex;

        public FieldQuickAccess(
            StorageValueSet<bool> component,
            StorageValueSet<TValue> valueSet,
            int fieldIndex)
        {
            _component = component;
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
            Set(entityId, value, out _);
        }

        public bool Set(EntityId entityId, TValue value, out TValue oldValue)
        {
            var entityIndex = entityId.Index;
            if (_valueSet.Set(entityIndex, value, out oldValue))
            {
                _component.Set(entityIndex, true, out _);
                return true;
            }

            return false;
        }

        public bool Remove(EntityId entityId, out TValue oldValue)
        {
            var entityIndex = entityId.Index;
            if (_valueSet.Remove(entityId.Index, out oldValue))
            {
                _component.Set(entityIndex, false, out _);
                return true;
            }

            return false;
        }
    }
}