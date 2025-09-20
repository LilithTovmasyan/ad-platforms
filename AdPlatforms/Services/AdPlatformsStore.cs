using System.Collections.Immutable;

namespace AdPlatforms.Services
{
    public sealed class AdPlatformsStore
    {
        private ImmutableDictionary<string, ImmutableHashSet<string>> _byLocation =
            ImmutableDictionary.Create<string, ImmutableHashSet<string>>(StringComparer.OrdinalIgnoreCase);

        public ImmutableDictionary<string, ImmutableHashSet<string>> Snapshot => _byLocation;

        public int LocationKeyCount => _byLocation.Count;

        public int UniquePlatformsCount =>
            _byLocation.SelectMany(kvp => kvp.Value)
                       .Distinct(StringComparer.OrdinalIgnoreCase)
                       .Count();

        public void Replace(ImmutableDictionary<string, ImmutableHashSet<string>> newMap)
        {
            // Atomically replace the dictionary reference
            Interlocked.Exchange(ref _byLocation, newMap);
        }
    }
}
