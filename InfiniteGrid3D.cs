#nullable enable

using System;
using System.Collections.Generic;

namespace _4DND
{
    public class InfiniteGrid3D<T>
    {
        private readonly Dictionary<(int x, int y, int z), T> _cells = new();
        private readonly Func<int, int, int, T>? _defaultFactory;
        public bool AutoCache { get; set; } = false;

        public InfiniteGrid3D(Func<int, int, int, T>? defaultFactory = null)
        {
            _defaultFactory = defaultFactory;
        }

        public void Set(int x, int y, int z, T value) => _cells[(x, y, z)] = value;

        public void Clear() => _cells.Clear();

        public T? Get(int x, int y, int z, T? defaultValue = default)
        {
            if (_cells.TryGetValue((x, y, z), out var v)) return v;
            if (_defaultFactory == null) return defaultValue;

            var result = _defaultFactory(x, y, z);
            if (AutoCache && !EqualityComparer<T>.Default.Equals(result, default))
            {
                _cells[(x, y, z)] = result;
            }
            return result;
        }

        public bool Remove(int x, int y, int z) => _cells.Remove((x, y, z));

        public IEnumerable<(int x, int y, int z)> Neighbors(int x, int y, int z, bool diag = false, bool includeVertical = true)
        {
            // Horizontal neighbors
            yield return (x - 1, y, z);
            yield return (x + 1, y, z);
            yield return (x, y - 1, z);
            yield return (x, y + 1, z);
            
            if (diag)
            {
                yield return (x - 1, y - 1, z);
                yield return (x - 1, y + 1, z);
                yield return (x + 1, y - 1, z);
                yield return (x + 1, y + 1, z);
            }
            
            // Vertical neighbors
            if (includeVertical)
            {
                yield return (x, y, z - 1);
                yield return (x, y, z + 1);
            }
        }

        public IEnumerable<KeyValuePair<(int x, int y, int z), T>> EnumerateNonEmpty()
        {
            var snapshot = new KeyValuePair<(int x, int y, int z), T>[_cells.Count];
            ((ICollection<KeyValuePair<(int x, int y, int z), T>>)_cells).CopyTo(snapshot, 0);
            foreach (var kv in snapshot) yield return kv;
        }

        public IEnumerable<(int x, int y, int z)> Region(int xmin, int xmax, int ymin, int ymax, int zmin, int zmax)
        {
            for (int z = zmin; z <= zmax; z++)
                for (int y = ymin; y <= ymax; y++)
                    for (int x = xmin; x <= xmax; x++)
                        yield return (x, y, z);
        }

        public (int minX, int maxX, int minY, int maxY, int minZ, int maxZ)? BoundingBox()
        {
            if (_cells.Count == 0) return null;
            int minX = int.MaxValue, maxX = int.MinValue;
            int minY = int.MaxValue, maxY = int.MinValue;
            int minZ = int.MaxValue, maxZ = int.MinValue;
            
            foreach (var (coord, _) in _cells)
            {
                if (coord.x < minX) minX = coord.x;
                if (coord.x > maxX) maxX = coord.x;
                if (coord.y < minY) minY = coord.y;
                if (coord.y > maxY) maxY = coord.y;
                if (coord.z < minZ) minZ = coord.z;
                if (coord.z > maxZ) maxZ = coord.z;
            }
            return (minX, maxX, minY, maxY, minZ, maxZ);
        }

        /// <summary>
        /// Pre-populates the cache for a rectangular region by calling the default factory
        /// for every cell that is not already cached. This avoids a lag spike when the
        /// renderer first queries a large number of uncached cells in a single frame.
        /// </summary>
        public void PreWarm(int centerX, int centerY, int z, int radius)
        {
            if (_defaultFactory == null || !AutoCache) return;

            for (int y = centerY - radius; y <= centerY + radius; y++)
            {
                for (int x = centerX - radius; x <= centerX + radius; x++)
                {
                    var key = (x, y, z);
                    if (_cells.ContainsKey(key)) continue;

                    var result = _defaultFactory(x, y, z);
                    if (!EqualityComparer<T>.Default.Equals(result, default))
                    {
                        _cells[key] = result;
                    }
                }
            }
        }

        public IEnumerable<(int x, int y, int z)> SpiralCoords(int cx = 0, int cy = 0, int cz = 0)
        {
            int x = cx, y = cy;
            yield return (x, y, cz);
            int step = 1;
            while (true)
            {
                for (int i = 0; i < step; i++) { x += 1; yield return (x, y, cz); }
                for (int i = 0; i < step; i++) { y += 1; yield return (x, y, cz); }
                step++;
                for (int i = 0; i < step; i++) { x -= 1; yield return (x, y, cz); }
                for (int i = 0; i < step; i++) { y -= 1; yield return (x, y, cz); }
                step++;
            }
        }
    }
}
