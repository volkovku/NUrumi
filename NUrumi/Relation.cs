using System;
using System.Collections.Generic;

namespace NUrumi
{
    /// <summary>
    /// Represents a relationship between two entities.
    /// Definitions:
    /// * relationship - used to refer to first element of pair
    /// * target - used to refer to second element of pair 
    /// </summary>
    /// <typeparam name="TRelationshipKind">Component which represents a kind of relationship: Likes, ChildOf and etc.</typeparam>
    public class Relation<TRelationshipKind> :
        Component<TRelationshipKind>,
        IUpdateCallback
        where TRelationshipKind : Component<TRelationshipKind>, new()
    {
        internal readonly Queue<HashSet<int>> Pool = new Queue<HashSet<int>>();

#pragma warning disable CS0649
        internal RefField<HashSet<int>> Direct;
        internal RefField<HashSet<int>> Reverse;
#pragma warning restore CS0649

        protected override void OnInit()
        {
            base.OnInit();
            Storage.AddUpdateCallback(this);
        }

        void IUpdateCallback.BeforeChange(int entity, bool added)
        {
            if (added)
            {
                return;
            }

            RemoveDirect(entity, out var direct);
            RemoveReverse(entity, out var reverse);

            if (direct != null)
            {
                direct.Clear();
                Pool.Enqueue(direct);
            }

            if (reverse != null)
            {
                reverse.Clear();
                Pool.Enqueue(reverse);
            }
        }

        void IUpdateCallback.AfterChange(int entity, bool added)
        {
        }

        private void RemoveDirect(int entity, out HashSet<int> direct)
        {
            if (!entity.TryGet(Direct, out direct))
            {
                return;
            }

            if (direct.Count == 0)
            {
                return;
            }

            foreach (var otherEntity in direct)
            {
                if (otherEntity.TryGet(Reverse, out var reverse))
                {
                    reverse.Remove(entity);
                }
            }
        }

        private void RemoveReverse(int entity, out HashSet<int> reverse)
        {
            if (!entity.TryGet(Reverse, out reverse))
            {
                return;
            }

            if (reverse.Count == 0)
            {
                return;
            }

            foreach (var otherEntity in reverse)
            {
                if (otherEntity.TryGet(Direct, out var direct))
                {
                    direct.Remove(entity);
                }
            }
        }
    }

    public static class Relation
    {
        /// <summary>
        /// Adds relationship between entity and other entity.
        /// </summary>
        /// <param name="entity">A first entity in relation.</param>
        /// <param name="relationKind">Component which represents a kind of relationship</param>
        /// <param name="otherEntity">A second entity in relation.</param>
        /// <typeparam name="TRelationshipKind">Component which represents a kind of relationship</typeparam>
        public static void Add<TRelationshipKind>(
            this int entity,
            Relation<TRelationshipKind> relationKind,
            int otherEntity)
            where TRelationshipKind : Component<TRelationshipKind>, new()
        {
            if (!entity.TryGet(relationKind.Direct, out var direct))
            {
                direct = CreateHashSet(relationKind);
                entity.Set(relationKind.Direct, direct);
            }

            if (!direct.Add(otherEntity))
            {
                return;
            }

            if (!otherEntity.TryGet(relationKind.Reverse, out var reverse))
            {
                reverse = CreateHashSet(relationKind);
                otherEntity.Set(relationKind.Reverse, reverse);
            }

            reverse.Add(entity);
        }

        /// <summary>
        /// Removes relationship between two entities.
        /// </summary>
        /// <param name="entity">A first entity in relation.</param>
        /// <param name="relationKind">Component which represents a kind of relationship.</param>
        /// <param name="otherEntity">A second entity in relation.</param>
        /// <typeparam name="TRelationshipKind">Component which represents a kind of relationship.</typeparam>
        public static void Remove<TRelationshipKind>(
            this int entity,
            Relation<TRelationshipKind> relationKind,
            int otherEntity)
            where TRelationshipKind : Component<TRelationshipKind>, new()
        {
            if (!entity.TryGet(relationKind.Direct, out var direct))
            {
                return;
            }

            direct.Remove(otherEntity);
            otherEntity.Get(relationKind.Reverse).Remove(entity);
        }

        /// <summary>
        /// Returns a collection of entities which associated with specified entity.
        /// </summary>
        /// <param name="entity">An entity which relations should be return.</param>
        /// <param name="relationKind">Component which represents a kind of relationship.</param>
        /// <typeparam name="TRelationshipKind">Component which represents a kind of relationship.</typeparam>
        /// <returns>Returns a collection of entities which associated with specified entity.</returns>
        public static IReadOnlyCollection<int> Relationship<TRelationshipKind>(
            this int entity,
            Relation<TRelationshipKind> relationKind)
            where TRelationshipKind : Component<TRelationshipKind>, new()
        {
            if (!entity.TryGet(relationKind.Direct, out var direct))
            {
                return Array.Empty<int>();
            }

            return direct;
        }

        /// <summary>
        /// Returns a collection of entities which associated with specified entity in reverse order.
        /// </summary>
        /// <param name="entity">An entity which reverse relations should be return.</param>
        /// <param name="relationKind">Component which represents a kind of relationship.</param>
        /// <typeparam name="TRelationshipKind">Component which represents a kind of relationship.</typeparam>
        /// <returns>Returns a collection of entities which associated with specified entity.</returns>
        public static IReadOnlyCollection<int> Target<TRelationshipKind>(
            this int entity,
            Relation<TRelationshipKind> relationKind)
            where TRelationshipKind : Component<TRelationshipKind>, new()
        {
            if (!entity.TryGet(relationKind.Reverse, out var reverse))
            {
                return Array.Empty<int>();
            }

            return reverse;
        }

        /// <summary>
        /// Determines is entity has relationship with other entity.
        /// </summary>
        /// <param name="entity">A first entity in relation.</param>
        /// <param name="relationKind">Component which represents a kind of relationship.</param>
        /// <param name="otherEntity">A second entity in relation.</param>
        /// <typeparam name="TRelationshipKind">Component which represents a kind of relationship.</typeparam>
        /// <returns>Returns true if relationship exists; otherwise returns false.</returns>
        public static bool Has<TRelationshipKind>(
            this int entity,
            Relation<TRelationshipKind> relationKind,
            int otherEntity)
            where TRelationshipKind : Component<TRelationshipKind>, new()
        {
            return entity.TryGet(relationKind.Direct, out var direct) && direct.Contains(otherEntity);
        }

        /// <summary>
        /// Determines is entity has reverse-relationship with other entity.
        /// </summary>
        /// <param name="entity">A first entity in relation.</param>
        /// <param name="relationKind">Component which represents a kind of relationship.</param>
        /// <param name="otherEntity">A second entity in relation.</param>
        /// <typeparam name="TRelationshipKind">Component which represents a kind of relationship.</typeparam>
        /// <returns>Returns true if reverse-relationship exists; otherwise returns false.</returns>
        public static bool Targets<TRelationshipKind>(
            this int entity, 
            Relation<TRelationshipKind> relationKind,
            int otherEntity)
            where TRelationshipKind : Component<TRelationshipKind>, new()
        {
            return entity.TryGet(relationKind.Reverse, out var reverse) && reverse.Contains(otherEntity);
        }

        private static HashSet<int> CreateHashSet<TComponent>(Relation<TComponent> component)
            where TComponent : Component<TComponent>, new()
        {
            return component.Pool.Count > 0 ? component.Pool.Dequeue() : new HashSet<int>();
        }
    }
}