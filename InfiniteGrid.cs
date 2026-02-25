#nullable enable

using System;
using System.Collections.Generic;

namespace _4DND
{
    public class InfiniteGrid<T>
    {
        private readonly Dictionary<(int x, int y), T> _cells = new();
        private readonly Func<int, int, T>? _defaultFactory;
        public bool AutoCache { get; set; } = false;

        public InfiniteGrid(Func<int, int, T>? defaultFactory = null)
        {
            _defaultFactory = defaultFactory;
        }

        public void Set(int x, int y, T value) => _cells[(x, y)] = value;

        public T? Get(int x, int y, T? defaultValue = default)
        {
            if (_cells.TryGetValue((x, y), out var v)) return v;
            if (_defaultFactory == null) return defaultValue;

            var result = _defaultFactory(x, y);
            if (AutoCache && !EqualityComparer<T>.Default.Equals(result, default))
            {
                _cells[(x, y)] = result;
            }
            return result;
        }

        public bool Remove(int x, int y) => _cells.Remove((x, y));

        public IEnumerable<(int x, int y)> Neighbors(int x, int y, bool diag = false)
        {
            yield return (x - 1, y);
            yield return (x + 1, y);
            yield return (x, y - 1);
            yield return (x, y + 1);
            if (diag)
            {
                yield return (x - 1, y - 1);
                yield return (x - 1, y + 1);
                yield return (x + 1, y - 1);
                yield return (x + 1, y + 1);
            }
        }

        public IEnumerable<KeyValuePair<(int x, int y), T>> EnumerateNonEmpty()
        {
            foreach (var kv in _cells) yield return kv;
        }

        public IEnumerable<(int x, int y)> Region(int xmin, int xmax, int ymin, int ymax)
        {
            for (int y = ymin; y <= ymax; y++)
                for (int x = xmin; x <= xmax; x++)
                    yield return (x, y);
        }

        public (int minX, int maxX, int minY, int maxY)? BoundingBox()
        {
            if (_cells.Count == 0) return null;
            int minX = int.MaxValue, maxX = int.MinValue, minY = int.MaxValue, maxY = int.MinValue;
            foreach (var (coord, _) in _cells)
            {
                if (coord.x < minX) minX = coord.x;
                if (coord.x > maxX) maxX = coord.x;
                if (coord.y < minY) minY = coord.y;
                if (coord.y > maxY) maxY = coord.y;
            }
            return (minX, maxX, minY, maxY);
        }

        public IEnumerable<(int x, int y)> SpiralCoords(int cx = 0, int cy = 0)
        {
            int x = cx, y = cy;
            yield return (x, y);
            int step = 1;
            while (true)
            {
                for (int i = 0; i < step; i++) { x += 1; yield return (x, y); } // right
                for (int i = 0; i < step; i++) { y += 1; yield return (x, y); } // up
                step++;
                for (int i = 0; i < step; i++) { x -= 1; yield return (x, y); } // left
                for (int i = 0; i < step; i++) { y -= 1; yield return (x, y); } // down
                step++;
            }
        }
    }
}