// namespace NUrumi
// {
//     public sealed class Default<TValue> : IFieldBehaviour<TValue>
//     {
//         public bool TryGet<TComponent>(
//             Storage storage,
//             EntityId entityId,
//             TComponent component,
//             int fieldIndex,
//             out TValue value)
//             where TComponent : Component<TComponent>, new()
//         {
//             return storage.TryGet(entityId, component, fieldIndex, out value);
//         }
//
//         public void Set<TComponent>(
//             Storage storage,
//             EntityId entityId,
//             TComponent component,
//             int fieldIndex,
//             TValue value)
//             where TComponent : Component<TComponent>, new()
//         {
//             storage.Set(entityId, component, fieldIndex, value, out _);
//         }
//
//         public bool Remove(Storage storage, EntityId entityId, int fieldIndex, out TValue oldValue)
//         {
//             return storage.Remove(entityId, fieldIndex, out oldValue);
//         }
//
//         public bool TryGet(
//             Storage storage,
//             StorageValueSet<TValue> valueSet,
//             int fieldIndex,
//             EntityId entityId,
//             out TValue value)
//         {
//             return valueSet.TryGet(entityId.Index, out value);
//         }
//
//         public bool Set(
//             Storage storage,
//             StorageValueSet<TValue> valueSet,
//             int fieldIndex,
//             EntityId entityId,
//             TValue value)
//         {
//             return valueSet.Set(entityId.Index, value, out _);
//         }
//
//         public bool Remove(
//             Storage storage,
//             StorageValueSet<TValue> valueSet,
//             int fieldIndex,
//             EntityId entityId,
//             out TValue oldValue)
//         {
//             return valueSet.Remove(entityId.Index, out oldValue);
//         }
//     }
// }