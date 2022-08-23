using System.Threading;

namespace NUrumi
{
    public static class Component
    {
        public static TComponent InstanceOf<TComponent>() where TComponent : Component<TComponent>, new()
        {
            return ComponentIndex<TComponent>.Instance;
        }
    }

    public abstract class Component<TComponent> where TComponent : Component<TComponent>, new()
    {
        public readonly int Index = ComponentIndex<TComponent>.Index;
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
            foreach (var field in fields)
            {
                if (field.GetValue(Instance) is IField ecsField)
                {
                    ecsField.SetIndex(FieldIndex.Next());
                    field.SetValue(Instance, ecsField);
                }
            }
        }
    }

    internal static class ComponentIndex
    {
        private static int _nextIndex;

        internal static int GetNextIndex()
        {
            return Interlocked.Increment(ref _nextIndex) - 1;
        }
    }
}