using System;
using System.Collections.Generic;
using NUrumi.Exceptions;

namespace NUrumi
{
    /// <summary>
    /// Represents a collection of entities which collected through observation of context changes.
    /// </summary>
    public sealed class Collector<TRegistry> :
        IDisposable,
        IContextResizeCallback
        where TRegistry : Registry<TRegistry>, new()
    {
        private readonly Context<TRegistry> _context;
        private readonly EntitiesSet _collectedEntities;
        private readonly HashSet<Group<TRegistry>> _listenedOnAddGroups = new HashSet<Group<TRegistry>>();
        private readonly HashSet<Group<TRegistry>> _listenedOnRemoveGroups = new HashSet<Group<TRegistry>>();
        private readonly List<Action> _unsubscribeActions = new List<Action>();
        private bool _disposed;

        internal Collector(Context<TRegistry> context, int initialCapacity)
        {
            _context = context;
            _collectedEntities = new EntitiesSet(initialCapacity);
        }

        ~Collector()
        {
            Dispose();
        }

        /// <summary>
        /// Gets count of collected entities.
        /// </summary>
        public int Count => _collectedEntities.EntitiesCount;

        /// <summary>
        /// Determines is specified entity was collected.
        /// </summary>
        /// <param name="entity">An entity to check</param>
        /// <returns>If entity in collector returns true; otherwise false</returns>
        public bool Has(int entity) => _collectedEntities.Has(entity);

        /// <summary>
        /// Watches entities which were added to specified group.
        /// </summary>
        /// <param name="group">A group to watch.</param>
        /// <returns>Returns an instance of this collector.</returns>
        public Collector<TRegistry> WatchEntitiesAddedTo(Group<TRegistry> group)
        {
            if (!_listenedOnAddGroups.Add(group))
            {
                throw new NUrumiException($"Collector already listen on add to group: {group}");
            }

            if (group.Context != _context)
            {
                throw new NUrumiException("Can't listen events from other context.");
            }

            var action = new Group<TRegistry>.GroupChangedEvent(
                (entity, added) =>
                {
                    if (!added)
                    {
                        return;
                    }

                    _collectedEntities.Add(entity);
                });

            group.OnGroupChanged += action;
            _unsubscribeActions.Add(() => group.OnGroupChanged -= action);
            _listenedOnAddGroups.Add(group);

            return this;
        }

        /// <summary>
        /// Watches entities which were removed to specified group.
        /// </summary>
        /// <param name="group">A group to watch.</param>
        /// <returns>Returns an instance of this collector.</returns>
        public Collector<TRegistry> WatchEntitiesRemovedFrom(Group<TRegistry> group)
        {
            if (!_listenedOnRemoveGroups.Add(group))
            {
                throw new NUrumiException($"Collector already listen on remove fromm group: {group}");
            }

            if (group.Context != _context)
            {
                throw new NUrumiException("Can't listen events from other context.");
            }

            var action = new Group<TRegistry>.GroupChangedEvent(
                (entity, added) =>
                {
                    if (added)
                    {
                        return;
                    }

                    _collectedEntities.Add(entity);
                });

            group.OnGroupChanged += action;
            _unsubscribeActions.Add(() => group.OnGroupChanged -= action);
            _listenedOnRemoveGroups.Add(group);

            return this;
        }

        /// <summary>
        /// Watches entities which field value was changed.
        /// </summary>
        /// <param name="field">A field to watch.</param>
        /// <returns>Returns an instance of this collector.</returns>
        public Collector<TRegistry> WatchChangesOf<TValue>(ReactiveField<TValue> field)
            where TValue : unmanaged, IEquatable<TValue>
        {
            var action = new ReactiveField<TValue>.OnReactiveFieldValueChangedEventHandler(
                (component, _, entity, value, newValue) => { _collectedEntities.Add(entity); });

            field.OnValueChanged += action;
            _unsubscribeActions.Add(() => field.OnValueChanged -= action);

            return this;
        }

        /// <summary>
        /// Removes all collected entities from this collector.
        /// </summary>
        public void Clear()
        {
            _collectedEntities.Clear();
        }

        /// <summary>
        /// Enumerates all collected entities.
        /// </summary>
        /// <returns>Returns an enumerator of collected entities.</returns>
        public EntitiesSet.Enumerator GetEnumerator()
        {
            return _collectedEntities.GetEnumerator();
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            foreach (var unsubscribeAction in _unsubscribeActions)
            {
                unsubscribeAction();
            }

            _unsubscribeActions.Clear();
            _collectedEntities.Clear();
            _disposed = true;
        }

        void IContextResizeCallback.ResizeEntities(int newSize)
        {
            _collectedEntities.ResizeEntities(newSize);
        }
    }
}