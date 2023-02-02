using System.Runtime.CompilerServices;

namespace NUrumi
{
    public abstract partial class Component<TComponent> where TComponent : Component<TComponent>, new()
    {
        public abstract class Tag : Component<TComponent>
        {
#pragma warning disable CS0649
            private Field<bool> _field;
#pragma warning restore CS0649

            /// <summary>
            /// Marks entity with specified tag.
            /// </summary>
            /// <param name="entityId">An entity identity.</param>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Set(int entityId) => _field.Set(entityId, true);
        }
    }
}