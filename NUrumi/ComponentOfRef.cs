using System.Runtime.CompilerServices;

namespace NUrumi
{
    /// <summary>
    /// Represents a shortcut to components with only one field.
    /// </summary>
    public abstract partial class Component<TComponent> where TComponent : Component<TComponent>, new()
    {
        /// <summary>
        /// Represents a shortcut to components with only one field.
        /// </summary>
        public abstract class OfRef<TValue> : Component<TComponent> where TValue : class
        {
#pragma warning disable CS0649
            private RefField<TValue> _field;
#pragma warning restore CS0649

            /// <summary>
            /// Returns this field value from entity with specified index.
            /// </summary>
            /// <param name="entityId">An entity identity.</param>
            /// <returns>A value of this field in entity.</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public TValue Get(int entityId) => _field.Get(entityId);

            /// <summary>
            /// Try to get this field value from entity with specified index.
            /// </summary>
            /// <param name="entityId">An entity identity.</param>
            /// <param name="result">A field value if exists.</param>
            /// <returns>Returns true if value exists, otherwise false.</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool TryGet(int entityId, out TValue result) => _field.TryGet(entityId, out result);

            /// <summary>
            /// Sets field value to entity with specified index.
            /// </summary>
            /// <param name="entityId">An entity identity.</param>
            /// <param name="value">A value to set.</param>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Set(int entityId, TValue value) => _field.Set(entityId, value);

            public static implicit operator RefField<TValue>(OfRef<TValue> componentOf)
            {
                return componentOf._field;
            }
        }
    }

    public static class ComponentOfRefCompanion
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryGet<TComponent, TValue>(
            this int entityId,
            Component<TComponent>.OfRef<TValue> component,
            out TValue value)
            where TComponent : Component<TComponent>, new()
            where TValue : class
        {
            return component.TryGet(entityId, out value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Set<TComponent, TValue>(
            this int entityId,
            Component<TComponent>.OfRef<TValue> component,
            TValue value)
            where TComponent : Component<TComponent>, new()
            where TValue : class
        {
            component.Set(entityId, value);
            return entityId;
        }
    }
}