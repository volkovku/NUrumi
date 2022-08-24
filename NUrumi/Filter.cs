using System.Collections.Generic;

namespace NUrumi
{
    public sealed class Filter
    {
        private readonly HashSet<int> _include;
        private readonly HashSet<int> _exclude;

        public static Filter With<TComponent>() where TComponent : Component<TComponent>, new()
        {
            return new Filter(
                new HashSet<int> {Component.InstanceOf<TComponent>().Index},
                new HashSet<int>());
        }

        public IReadOnlyCollection<int> Include => _include;
        public IReadOnlyCollection<int> Exclude => _exclude;

        private Filter(HashSet<int> include, HashSet<int> exclude)
        {
            _include = include;
            _exclude = exclude;
        }

        public Filter And<TComponent>() where TComponent : Component<TComponent>, new()
        {
            var include = new HashSet<int>(_include) {Component.InstanceOf<TComponent>().Index};
            return new Filter(include, _exclude);
        }

        public Filter AndNot<TComponent>() where TComponent : Component<TComponent>, new()
        {
            var exclude = new HashSet<int>(_exclude) {Component.InstanceOf<TComponent>().Index};
            return new Filter(_include, exclude);
        }
    }
}