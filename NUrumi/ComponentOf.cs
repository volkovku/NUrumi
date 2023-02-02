using System.Runtime.CompilerServices;

namespace NUrumi
{
    public abstract partial class Component<TComponent> where TComponent : Component<TComponent>, new()
    {
        /// <summary>
        /// Represents a shortcut to components with only one field.
        /// </summary>
        public abstract class Of<TValue> : Component<TComponent> where TValue : unmanaged
        {
#pragma warning disable CS0649
            private Field<TValue> _field;
#pragma warning restore CS0649

            /// <summary>
            /// Returns this field value from entity with specified index.
            /// </summary>
            /// <param name="entityId">An entity identity.</param>
            /// <returns>A value of this field in entity.</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public TValue Get(int entityId) => _field.Get(entityId);

            /// <summary>
            /// Returns this field value from entity with specified index as reference.
            /// </summary>
            /// <param name="entityId">An entity identity.</param>
            /// <returns>A value of this field in entity.</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ref TValue GetRef(int entityId) => ref _field.GetRef(entityId);

            /// <summary>
            /// Returns this field value from entity with specified index as reference.
            /// If value does not set then set it as default and returns it as a reference.
            /// </summary>
            /// <param name="entityId">An entity identity.</param>
            /// <returns>A value of this field in entity.</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ref TValue GetOrAdd(int entityId) => ref _field.GetOrAdd(entityId);

            /// <summary>
            /// Returns this field value from entity with specified index as reference.
            /// If value does not set then set it as default and returns it as a reference.
            /// </summary>
            /// <param name="entityId">An entity identity.</param>
            /// <param name="value">A value which should be set if entity does not have value.</param>
            /// <returns>A value of this field in entity.</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ref TValue GetOrSet(int entityId, TValue value) => ref _field.GetOrSet(entityId, value);

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

            public static implicit operator Field<TValue>(Of<TValue> componentOf)
            {
                return componentOf._field;
            }
        }
    }
}