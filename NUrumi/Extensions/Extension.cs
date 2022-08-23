using System.Threading;

namespace NUrumi.Extensions
{
    public abstract class Extension<TExtension> where TExtension : Extension<TExtension>
    {
        public readonly int Index = ExtensionIndex<TExtension>.Index;
    }

    internal static class ExtensionIndex<TExtension> where TExtension : Extension<TExtension>
    {
        // ReSharper disable once StaticMemberInGenericType
        internal static readonly int Index;

        static ExtensionIndex()
        {
            Index = ExtensionIndex.GetNextIndex();
        }
    }

    internal static class ExtensionIndex
    {
        private static int _nextIndex;

        internal static int GetNextIndex()
        {
            return Interlocked.Increment(ref _nextIndex) - 1;
        }
    }
}