using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using NUrumi.Extensions;

namespace NUrumi
{
    public sealed class Storage
    {
        private readonly Context _context;
        private readonly int _valuesInitialCapacity;
        private readonly int _entitiesInitialCapacity;

        private object[] _extensions;
        private StorageValueSet<bool>[] _components;
        private IStorageValueSet[] _valueSet;

        public Storage(
            Context context,
            int entitiesInitialCapacity,
            int componentsInitialCapacity = 512,
            int fieldsInitialCapacity = 512,
            int valuesInitialCapacity = 512,
            int extensionsInitialCapacity = 512)
        {
            _context = context;
            _entitiesInitialCapacity = entitiesInitialCapacity;
            _valuesInitialCapacity = valuesInitialCapacity;
            _valueSet = new IStorageValueSet[Math.Max(1, fieldsInitialCapacity)];
            _components = new StorageValueSet<bool>[Math.Max(1, componentsInitialCapacity)];
            _extensions = new object[Math.Max(1, extensionsInitialCapacity)];
        }

        public void Add<TExtension>(TExtension extension) where TExtension : Extension<TExtension>
        {
            var extensionIndex = extension.Index;
            if (_extensions.Length <= extensionIndex)
            {
                Array.Resize(ref _extensions, extensionIndex << 1);
            }

            _extensions[extensionIndex] = extension;
        }

        public bool TryGet<TExtension>(out TExtension extension) where TExtension : Extension<TExtension>
        {
            var extensionIndex = ExtensionIndex<TExtension>.Index;
            if (_extensions.Length <= extensionIndex)
            {
                extension = default;
                return false;
            }

            extension = (TExtension) _extensions[extensionIndex];
            return extension != null;
        }

        public FieldQuickAccess<TValue> GetFieldQuickAccess<TValue>(IField<TValue> field)
        {
            var fieldIndex = field.Index;
            if (fieldIndex >= _valueSet.Length)
            {
                Array.Resize(ref _valueSet, fieldIndex << 1);
            }

            var valueSet = (StorageValueSet<TValue>) _valueSet[fieldIndex];
            if (valueSet == null)
            {
                valueSet = new StorageValueSet<TValue>(
                    Math.Max(_entitiesInitialCapacity, _context.EntitiesCount),
                    _valuesInitialCapacity);

                _valueSet[fieldIndex] = valueSet;
            }

            return new FieldQuickAccess<TValue>(
                GetComponentIndex(field.ComponentIndex),
                valueSet,
                fieldIndex);
        }

        public void Collect(Filter filter, List<int> destination)
        {
            StorageValueSet<bool> setOfEntities = null;

            var baseLineIndex = 0;
            foreach (var componentIndex in filter.Include)
            {
                if (componentIndex >= _components.Length)
                {
                    return;
                }

                var ix = _components[componentIndex];
                if (ix == null)
                {
                    return;
                }

                if (setOfEntities == null || ix.Count < setOfEntities.Count)
                {
                    setOfEntities = ix;
                    baseLineIndex = componentIndex;
                }
            }

            if (setOfEntities == null)
            {
                return;
            }

            for (var i = 0; i < setOfEntities.Count; i++)
            {
                var entityIndex = setOfEntities.ReverseIndex[i];
                var found = true;
                foreach (var componentIndex in filter.Include)
                {
                    if (componentIndex == baseLineIndex)
                    {
                        continue;
                    }

                    var ix = _components[componentIndex];
                    if (!ix.TryGet(entityIndex, out _))
                    {
                        found = false;
                        break;
                    }
                }

                if (!found)
                {
                    continue;
                }

                foreach (var componentIndex in filter.Exclude)
                {
                    var ix = _components[componentIndex];
                    if (ix == null)
                    {
                        continue;
                    }

                    if (ix.TryGet(entityIndex, out _))
                    {
                        found = false;
                        break;
                    }
                }

                if (found)
                {
                    destination.Add(entityIndex);
                }
            }
        }

        public bool Has<TComponent>(EntityId entityId) where TComponent : Component<TComponent>, new()
        {
            return Has(Component.InstanceOf<TComponent>(), entityId.Index);
        }

        public void RemoveEntity(EntityId id)
        {
            var entityIndex = id.Index;
            for (var componentIndex = 0; componentIndex < _components.Length; componentIndex++)
            {
                var ix = _components[componentIndex];
                if (ix == null)
                {
                    continue;
                }

                if (!ix.Remove(entityIndex, out _))
                {
                    continue;
                }

                var component = Component.InstanceOf(componentIndex);
                foreach (var field in component.Fields)
                {
                    field.Remove(this, id);
                }
            }
        }

        public bool Remove<TComponent>(EntityId id) where TComponent : Component<TComponent>, new()
        {
            var component = Component.InstanceOf<TComponent>();
            var componentIndex = component.Index;
            if (componentIndex >= _components.Length)
            {
                return false;
            }

            var entityIndex = id.Index;
            var ix = _components[componentIndex];
            if (ix == null)
            {
                return false;
            }

            if (!ix.Remove(entityIndex, out _))
            {
                return false;
            }

            foreach (var field in component.Fields)
            {
                field.Remove(this, id);
            }

            return true;
        }

        public bool TryGet<TComponent, TValue>(
            EntityId entityId,
            TComponent component,
            int fieldIndex,
            out TValue value)
            where TComponent : Component<TComponent>, new()
        {
            if (fieldIndex >= _valueSet.Length)
            {
                value = default;
                return false;
            }

            var valueSet = (StorageValueSet<TValue>) _valueSet[fieldIndex];
            if (valueSet.TryGet(entityId.Index, out value))
            {
                return true;
            }

            var internalComponent = (IInternalComponent) component;
            if (internalComponent.Fields.Count == 1)
            {
                return false;
            }

            return Has(component, entityId.Index);
        }

        public bool Set<TComponent, TValue>(
            EntityId entityId,
            TComponent component,
            int fieldIndex,
            TValue value,
            out TValue oldValue)
            where TComponent : Component<TComponent>, new()
        {
            if (fieldIndex >= _valueSet.Length)
            {
                Array.Resize(ref _valueSet, fieldIndex << 1);
            }

            var valueSet = (StorageValueSet<TValue>) _valueSet[fieldIndex];
            if (valueSet == null)
            {
                valueSet = new StorageValueSet<TValue>(
                    Math.Max(_entitiesInitialCapacity, _context.EntitiesCount),
                    _valuesInitialCapacity);

                _valueSet[fieldIndex] = valueSet;
            }

            if (valueSet.Set(entityId.Index, value, out oldValue))
            {
                AddComponentPresents(entityId.Index, component.Index);
                return true;
            }

            return false;
        }

        public bool Remove<TValue>(EntityId entityId, int fieldIndex, out TValue oldValue)
        {
            if (fieldIndex >= _valueSet.Length)
            {
                oldValue = default;
                return false;
            }

            var valueSet = (StorageValueSet<TValue>) _valueSet[fieldIndex];
            return valueSet.Remove(entityId.Index, out oldValue);
        }

        public void ResizeEntities(int newSize)
        {
            for (var i = 0; i < _components.Length; i++)
            {
                _components[i]?.ResizeEntities(newSize);
            }

            for (var i = 0; i < _valueSet.Length; i++)
            {
                _valueSet[i]?.ResizeEntities(newSize);
            }
        }

        private bool Has<TComponent>(TComponent component, int entityIndex)
            where TComponent : Component<TComponent>, new()
        {
            var componentIndex = component.Index;
            if (componentIndex >= _components.Length)
            {
                return false;
            }

            var ix = _components[componentIndex];
            return ix != null && ix.TryGet(entityIndex, out _);
        }

        private void AddComponentPresents(int entityIndex, int componentIndex)
        {
            GetComponentIndex(componentIndex).Set(entityIndex, true, out _);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private StorageValueSet<bool> GetComponentIndex(int componentIndex)
        {
            var components = _components;
            if (componentIndex >= components.Length)
            {
                Array.Resize(ref _components, componentIndex << 1);
                components = _components;
            }

            var ix = components[componentIndex];
            if (ix == null)
            {
                ix = new StorageValueSet<bool>(
                    Math.Max(_entitiesInitialCapacity, _context.EntitiesCount),
                    _valuesInitialCapacity);

                components[componentIndex] = ix;
            }

            return ix;
        }
    }
}