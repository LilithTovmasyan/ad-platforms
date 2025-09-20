using Microsoft.AspNetCore.Mvc.Rendering;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Collections.Immutable;
using System.IO.Pipelines;

namespace AdPlatforms.Services
{
    public sealed class AdPlatformsService
    {
        private readonly AdPlatformsStore _store;
        public AdPlatformsService(AdPlatformsStore store) => _store = store;

        public LoadResult LoadFromText(string text)
        {
            var errors = new List<string>();
            var mapBuilder = ImmutableDictionary.CreateBuilder<string, ImmutableHashSet<string>>(StringComparer.OrdinalIgnoreCase);

            if (string.IsNullOrWhiteSpace(text))
            {
                errors.Add("Empty body");
                return LoadResult.Fail(errors);
            }

            var lines = text.Split(['\n', '\r'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var raw in lines)
            {
                var line = raw.Trim();
                if (line.Length == 0) continue;

                var idx = line.IndexOf(':');
                if (idx <= 0 || idx == line.Length - 1)
                {
                    errors.Add($"Invalid line (expected 'Name:/loc1,/loc2'): '{line}'");
                    continue;
                }

                var name = line[..idx].Trim();
                var locationsPart = line[(idx + 1)..].Trim();
                if (string.IsNullOrWhiteSpace(name)) { errors.Add($"Empty platform name: '{line}'"); continue; }
                if (string.IsNullOrWhiteSpace(locationsPart)) { errors.Add($"No locations: '{line}'"); continue; }

                var locs = locationsPart.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                foreach (var loc in locs)
                {
                    if (!IsValidLocation(loc)) { errors.Add($"Invalid location '{loc}' for '{name}'"); continue; }

                    var set = mapBuilder.TryGetValue(loc, out var existing)
                        ? existing
                        : ImmutableHashSet.Create<string>(StringComparer.OrdinalIgnoreCase);

                    set = set.Add(name);
                    mapBuilder[loc] = set;
                }
            }

            if (mapBuilder.Count == 0)
            {
                errors.Add("No valid entries parsed");
                return LoadResult.Fail(errors);
            }

            _store.Replace(mapBuilder.ToImmutable());
            return LoadResult.Success(_store.UniquePlatformsCount, _store.LocationKeyCount, errors);

        }

        public IReadOnlyList<string> FindByLocation(string location)
        {
            if (!IsValidLocation(location)) return Array.Empty<string>();

            var snapshot = _store.Snapshot;
            if (snapshot.Count == 0) return Array.Empty<string>();

            var prefixes = EnumeratePrefixes(location);
            var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var p in prefixes)
            {
                if (snapshot.TryGetValue(p, out var set))
                {
                    result.UnionWith(set);
                }
            }

            return result.OrderBy(s => s, StringComparer.OrdinalIgnoreCase).ToArray();
        }

        private static IEnumerable<string> EnumeratePrefixes(string location)
        {
            yield return location;
            int pos = location.LastIndexOf('/');
            while (pos > 0)
            {
                var parent = location[..pos];
                if (parent.Length > 0) yield return parent;
                pos = parent.LastIndexOf('/');
            }
        }

        private static bool IsValidLocation(string loc) =>
            !string.IsNullOrWhiteSpace(loc) && loc.StartsWith('/') && !loc.Contains(' ');
    }

    // Small helper class for load results
    public record LoadResult(bool IsSuccess, int PlatformsCount, int LocationKeyCount, List<string> Errors)
    {
        public static LoadResult Success(int platformsCount, int locationKeyCount, List<string>? errors = null) =>
            new(true, platformsCount, locationKeyCount, errors ?? new());

        public static LoadResult Fail(List<string> errors) =>
            new(false, 0, 0, errors);
    }

}
