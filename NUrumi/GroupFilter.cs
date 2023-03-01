using System;
using System.Collections.Generic;
using NUrumi.Exceptions;

namespace NUrumi
{
    public interface IGroupFilter
    {
        IReadOnlyCollection<IComponent> Included { get; }
        IReadOnlyCollection<IComponent> Excluded { get; }
        IGroupFilter Include(IComponent component);
        IGroupFilter Exclude(IComponent component);
    }

    public static class GroupFilter
    {
        public static IGroupFilter Include(IComponent component)
        {
            return new Instance(
                new HashSet<IComponent> {component},
                new HashSet<IComponent>(),
                component.GetHashCode());
        }

        public static IGroupFilter Exclude(IComponent component)
        {
            return new Instance(
                new HashSet<IComponent>(),
                new HashSet<IComponent> {component},
                component.GetHashCode());
        }

        private class Instance : IGroupFilter, IEquatable<Instance>
        {
            private readonly HashSet<IComponent> _include;
            private readonly HashSet<IComponent> _exclude;
            private readonly int _hashCode;

            public IReadOnlyCollection<IComponent> Included => _include;
            public IReadOnlyCollection<IComponent> Excluded => _exclude;

            // ReSharper disable once MemberHidesStaticFromOuterClass
            public IGroupFilter Include(IComponent component)
            {
                if (_exclude.Contains(component))
                {
                    throw new NUrumiException(
                        $"Group filter already has component '{component.GetType().Name}' " +
                        "in exclude part");
                }

                return new Instance(
                    new HashSet<IComponent>(_include) {component},
                    _exclude,
                    (_hashCode * 397) ^ component.GetHashCode());
            }

            // ReSharper disable once MemberHidesStaticFromOuterClass
            public IGroupFilter Exclude(IComponent component)
            {
                if (_include.Contains(component))
                {
                    throw new NUrumiException(
                        $"Group filter already has component '{component.GetType().Name}' " +
                        "in include part");
                }

                return new Instance(
                    _include,
                    new HashSet<IComponent>(_exclude) {component},
                    (_hashCode * 397) ^ component.GetHashCode());
            }

            internal Instance(HashSet<IComponent> include, HashSet<IComponent> exclude, int hashCode)
            {
                _include = include;
                _exclude = exclude;
                _hashCode = hashCode;
            }

            public bool Equals(Instance other)
            {
                if (ReferenceEquals(null, other)) return false;
                if (ReferenceEquals(this, other)) return true;

                if (_hashCode != other._hashCode
                    || _include.Count != other._include.Count
                    || _exclude.Count != other._exclude.Count)
                {
                    return false;
                }

                foreach (var component in _include)
                {
                    if (!other._include.Contains(component))
                    {
                        return false;
                    }
                }

                foreach (var component in _exclude)
                {
                    if (!other._exclude.Contains(component))
                    {
                        return false;
                    }
                }

                return true;
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != GetType()) return false;
                return Equals((Instance) obj);
            }

            public override int GetHashCode()
            {
                return _hashCode;
            }

            public static bool operator ==(Instance left, Instance right)
            {
                return Equals(left, right);
            }

            public static bool operator !=(Instance left, Instance right)
            {
                return !Equals(left, right);
            }
        }
    }
}