using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace NUrumi
{
    public static class Component
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TComponent InstanceOf<TComponent>() where TComponent : Component<TComponent>, new()
        {
            return ComponentIndex<TComponent>.Instance;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static IInternalComponent InstanceOf(int componentIndex)
        {
            return ComponentIndex.Get(componentIndex);
        }
    }

    public abstract class Component<TComponent> :
        IInternalComponent
        where TComponent : Component<TComponent>, new()
    {
        public readonly int Index = ComponentIndex<TComponent>.Index;
        internal IReadOnlyCollection<IField> Fields;
        IReadOnlyCollection<IField> IInternalComponent.Fields => Fields;
    }

    internal interface IInternalComponent
    {
        IReadOnlyCollection<IField> Fields { get; }
    }

    // ReSharper disable once UnusedTypeParameter
    internal static class ComponentIndex<TComponent> where TComponent : Component<TComponent>, new()
    {
        // ReSharper disable once StaticMemberInGenericType
        internal static readonly int Index;

        internal static readonly TComponent Instance;

        static ComponentIndex()
        {
            Index = ComponentIndex.GetNextIndex();
            Instance = new TComponent();

            var componentType = typeof(TComponent);
            var fields = componentType.GetFields();
            var componentFields = new List<IField>();
            foreach (var field in fields)
            {
                if (!(field.GetValue(Instance) is IField ecsField))
                {
                    continue;
                }

                ecsField.SetMetaData(FieldIndex.Next(), field.Name);
                field.SetValue(Instance, ecsField);
                componentFields.Add(ecsField);
            }

            Instance.Fields = componentFields;
            ComponentIndex.Set(Index, Instance);
        }
    }

    internal static class ComponentIndex
    {
        private static int _nextIndex;
        private static IInternalComponent[] _components = new IInternalComponent[10];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static IInternalComponent Get(int componentIndex)
        {
            return _components[componentIndex];
        }

        internal static void Set(int componentIndex, IInternalComponent component)
        {
            if (componentIndex >= _components.Length)
            {
                Array.Resize(ref _components, componentIndex << 1);
            }

            _components[componentIndex] = component;
        }

        internal static int GetNextIndex()
        {
            return Interlocked.Increment(ref _nextIndex) - 1;
        }
    }
}