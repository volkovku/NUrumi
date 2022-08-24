using System;
using System.Collections.Generic;
using NUrumi.Extensions;

namespace NUrumi.Storages.Safe
{
    public sealed class SafeStorage : IStorage
    {
        private readonly int _valuesSetInitialCapacity;

        private object[] _extensions;
        private SafeValueSet<bool>[] _components;
        private object[] _valueSet;

        public SafeStorage(
            int componentsInitialCapacity = 100,
            int fieldsInitialCapacity = 100,
            int valuesSetInitialCapacity = 100,
            int extensionsInitialCapacity = 5)
        {
            _valuesSetInitialCapacity = valuesSetInitialCapacity;
            _valueSet = new object[Math.Max(1, fieldsInitialCapacity)];
            _components = new SafeValueSet<bool>[Math.Max(1, componentsInitialCapacity)];
            _extensions = new object[Math.Max(1, extensionsInitialCapacity)];
        }

        public void Add<TExtension>(TExtension extension) where TExtension : Extension<TExtension>
        {
            var extensionIndex = extension.Index;
            if (_extensions.Length <= extensionIndex)
            {
                var newExtensions = new object[extensionIndex * 2];
                Array.Copy(_extensions, newExtensions, _extensions.Length);
                _extensions = newExtensions;
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

        public void Collect(Filter filter, List<int> destination)
        {
            SafeValueSet<bool> setOfEntities = null;

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

            var valueSet = (SafeValueSet<TValue>) _valueSet[fieldIndex];
            if (valueSet.TryGet(entityId.Index, out value))
            {
                return true;
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
                var newValueSet = new object[fieldIndex * 2];
                Array.Copy(_valueSet, newValueSet, _valueSet.Length);
                _valueSet = newValueSet;
            }

            var valueSet = (SafeValueSet<TValue>) _valueSet[fieldIndex];
            if (valueSet == null)
            {
                valueSet = new SafeValueSet<TValue>(_valuesSetInitialCapacity);
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

            var valueSet = (SafeValueSet<TValue>) _valueSet[fieldIndex];
            return valueSet.Remove(entityId.Index, out oldValue);
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
            if (componentIndex >= _components.Length)
            {
                var newComponents = new SafeValueSet<bool>[componentIndex << 1];
                Array.Copy(_components, newComponents, _components.Length);
                _components = newComponents;
            }

            var ix = _components[componentIndex];
            if (ix == null)
            {
                ix = new SafeValueSet<bool>(10);
                _components[componentIndex] = ix;
            }

            ix.Set(entityIndex, true, out _);
        }
    }
}