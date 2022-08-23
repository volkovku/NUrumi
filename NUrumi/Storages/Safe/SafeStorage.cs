﻿using System;
using NUrumi.Extensions;

namespace NUrumi.Storages.Safe
{
    public sealed class SafeStorage : IStorage
    {
        private object[] _extensions;
        private bool[][] _components;
        private object[] _valueSet;

        public SafeStorage(
            int componentsInitialCapacity = 100,
            int fieldsInitialCapacity = 100,
            int extensionsInitialCapacity = 5)
        {
            _valueSet = new object[Math.Max(1, fieldsInitialCapacity)];
            _components = new bool[Math.Max(1, componentsInitialCapacity)][];
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

        public bool Has<TComponent>(EntityId entityId) where TComponent : Component<TComponent>, new()
        {
            return Has(Component.InstanceOf<TComponent>(), entityId.Index);
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
                valueSet = new SafeValueSet<TValue>(100);
                _valueSet[fieldIndex] = valueSet;
            }

            if (valueSet.Set(entityId.Index, value, out oldValue))
            {
                AddComponentPresents(entityId.Index, component.Index);
                return true;
            }

            return false;
        }

        private bool Has<TComponent>(TComponent component, int entityIndex)
            where TComponent : Component<TComponent>, new()
        {
            var componentIndex = component.Index;
            if (componentIndex >= _components.Length)
            {
                return false;
            }

            var entitiesExistence = _components[componentIndex];
            if (entitiesExistence == null || entitiesExistence.Length <= entityIndex)
            {
                return false;
            }

            return entitiesExistence[entityIndex];
        }

        private void AddComponentPresents(int entityIndex, int componentIndex)
        {
            if (componentIndex >= _components.Length)
            {
                var newComponents = new bool[componentIndex * 2][];
                Array.Copy(_components, newComponents, _components.Length);
                _components = newComponents;
            }

            var entitiesExistence = _components[componentIndex];
            if (entitiesExistence == null)
            {
                entitiesExistence = new bool[entityIndex + 1];
                _components[componentIndex] = entitiesExistence;
            }
            else if (entityIndex >= entitiesExistence.Length)
            {
                var newEntitiesExistence = new bool[entityIndex * 2];
                Array.Copy(entitiesExistence, newEntitiesExistence, entitiesExistence.Length);
                entitiesExistence = newEntitiesExistence;
                _components[componentIndex] = entitiesExistence;
            }

            entitiesExistence[entityIndex] = true;
        }
    }
}