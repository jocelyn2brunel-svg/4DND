#nullable enable

using System;
using System.Collections.Generic;

namespace _4DND
{
    public class InfiniteGrid3D<T>
    {
        private readonly Dictionary<(int x, int y, int z), T> _cells = new();
        private readonly Func<T>? _defaultFactory;

        public InfiniteGrid3D(Func<T>? defaultFactory = null)
        {
            _defaultFactory = defaultFactory;
        }

        public void Set(int x, int y, int z, T value) => _cells[(x, y, z)] = value;

        public T? Get(int x, int y, int z, T? defaultValue = default)
        {
            if (_cells.TryGetValue((x, y, z), out var v)) return v;
            return _defaultFactory != null ? _defaultFactory() : defaultValue;
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
            foreach (var kv in _cells) yield return kv;
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
