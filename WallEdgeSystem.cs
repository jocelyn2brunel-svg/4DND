using System;
using System.Collections.Generic;
using System.Linq;

namespace _4DND
{
    /// <summary>
    /// Cardinal edge direction for walls between two adjacent tiles.
    /// A wall on the North edge of tile (x,y,z) is the same as a wall
    /// on the South edge of tile (x,y+1,z).
    /// </summary>
    public enum WallSide
    {
        North, // +Y edge
        East,  // +X edge
        South, // -Y edge
        West   // -X edge
    }

    /// <summary>
    /// Stores wall edges between tiles. Walls are separations, not full tiles.
    /// Each wall is stored as a canonical edge key so that the north edge of (x,y,z)
    /// and the south edge of (x,y+1,z) map to the same entry.
    /// </summary>
    public class WallEdgeSystem
    {
        /// <summary>
        /// Canonical edge key. We always store the edge from the perspective
        /// of the cell with the smaller coordinate:
        ///   - Horizontal (Y-normal) edges: stored as (x, min(y), z, North)
        ///   - Vertical (X-normal) edges: stored as (min(x), y, z, East)
        /// </summary>
        private readonly HashSet<(int x, int y, int z, WallSide side)> _edges = new();

        /// <summary>
        /// Serializable list of edges for save/load.
        /// </summary>
        public List<WallEdgeData> SerializedEdges
        {
            get => _edges.Select(e => new WallEdgeData { X = e.x, Y = e.y, Z = e.z, Side = e.side }).ToList();
            set
            {
                _edges.Clear();
                if (value == null) return;
                foreach (var e in value)
                    _edges.Add((e.X, e.Y, e.Z, e.Side));
            }
        }

        private static (int x, int y, int z, WallSide side) Canonicalize(int x, int y, int z, WallSide side)
        {
            return side switch
            {
                WallSide.North => (x, y, z, WallSide.North),       // +Y edge of (x,y) 
                WallSide.South => (x, y - 1, z, WallSide.North),   // -Y edge of (x,y) = +Y edge of (x,y-1)
                WallSide.East  => (x, y, z, WallSide.East),        // +X edge of (x,y)
                WallSide.West  => (x - 1, y, z, WallSide.East),    // -X edge of (x,y) = +X edge of (x-1,y)
                _ => (x, y, z, side)
            };
        }

        /// <summary>
        /// Add a wall on the given side of tile (x,y,z).
        /// </summary>
        public void SetWall(int x, int y, int z, WallSide side)
        {
            _edges.Add(Canonicalize(x, y, z, side));
        }

        /// <summary>
        /// Remove a wall on the given side of tile (x,y,z).
        /// </summary>
        public bool RemoveWall(int x, int y, int z, WallSide side)
        {
            return _edges.Remove(Canonicalize(x, y, z, side));
        }

        /// <summary>
        /// Check if there is a wall on the given side of tile (x,y,z).
        /// </summary>
        public bool HasWall(int x, int y, int z, WallSide side)
        {
            return _edges.Contains(Canonicalize(x, y, z, side));
        }

        /// <summary>
        /// Check if there is a wall between two adjacent tiles.
        /// Returns true if (x1,y1,z1) and (x2,y2,z2) are horizontal neighbors
        /// and there is a wall edge between them.
        /// </summary>
        public bool HasWallBetween(int x1, int y1, int z1, int x2, int y2, int z2)
        {
            if (z1 != z2) return false;

            int dx = x2 - x1;
            int dy = y2 - y1;

            // Only check cardinal neighbors
            if (dx == 1 && dy == 0) return HasWall(x1, y1, z1, WallSide.East);
            if (dx == -1 && dy == 0) return HasWall(x1, y1, z1, WallSide.West);
            if (dx == 0 && dy == 1) return HasWall(x1, y1, z1, WallSide.North);
            if (dx == 0 && dy == -1) return HasWall(x1, y1, z1, WallSide.South);

            // Diagonal movement: blocked if either of the two shared cardinal edges has a wall
            if (dx == 1 && dy == 1)
                return HasWall(x1, y1, z1, WallSide.East) || HasWall(x1, y1, z1, WallSide.North)
                    || HasWall(x2, y2, z1, WallSide.West) || HasWall(x2, y2, z1, WallSide.South);
            if (dx == 1 && dy == -1)
                return HasWall(x1, y1, z1, WallSide.East) || HasWall(x1, y1, z1, WallSide.South)
                    || HasWall(x2, y2, z1, WallSide.West) || HasWall(x2, y2, z1, WallSide.North);
            if (dx == -1 && dy == 1)
                return HasWall(x1, y1, z1, WallSide.West) || HasWall(x1, y1, z1, WallSide.North)
                    || HasWall(x2, y2, z1, WallSide.East) || HasWall(x2, y2, z1, WallSide.South);
            if (dx == -1 && dy == -1)
                return HasWall(x1, y1, z1, WallSide.West) || HasWall(x1, y1, z1, WallSide.South)
                    || HasWall(x2, y2, z1, WallSide.East) || HasWall(x2, y2, z1, WallSide.North);

            return false;
        }

        /// <summary>
        /// Get all wall edges for a given tile, useful for rendering.
        /// </summary>
        public IEnumerable<WallSide> GetWallsAt(int x, int y, int z)
        {
            if (HasWall(x, y, z, WallSide.North)) yield return WallSide.North;
            if (HasWall(x, y, z, WallSide.South)) yield return WallSide.South;
            if (HasWall(x, y, z, WallSide.East)) yield return WallSide.East;
            if (HasWall(x, y, z, WallSide.West)) yield return WallSide.West;
        }

        /// <summary>
        /// Check if a line from (x1,y1,z1) to (x2,y2,z2) crosses any wall edge.
        /// Used for line-of-sight. Steps along the line and checks each cell transition.
        /// </summary>
        public bool LineIntersectsWall(float startX, float startY, int z, float endX, float endY)
        {
            // Use a simple step-based approach: walk from start to end and
            // detect when we cross a cell boundary, checking wall edges at each crossing.
            float dx = endX - startX;
            float dy = endY - startY;
            float dist = MathF.Sqrt(dx * dx + dy * dy);
            if (dist < 0.001f) return false;

            int steps = (int)(dist * 4) + 1; // 4 samples per tile for good resolution
            float stepX = dx / steps;
            float stepY = dy / steps;

            float curX = startX;
            float curY = startY;
            int prevCellX = (int)MathF.Floor(curX);
            int prevCellY = (int)MathF.Floor(curY);

            for (int i = 1; i <= steps; i++)
            {
                curX += stepX;
                curY += stepY;
                int cellX = (int)MathF.Floor(curX);
                int cellY = (int)MathF.Floor(curY);

                if (cellX != prevCellX || cellY != prevCellY)
                {
                    if (HasWallBetween(prevCellX, prevCellY, z, cellX, cellY, z))
                        return true;
                    prevCellX = cellX;
                    prevCellY = cellY;
                }
            }

            return false;
        }

        public void Clear()
        {
            _edges.Clear();
        }

        /// <summary>
        /// Convenience: add walls on all four sides of a rectangular room perimeter,
        /// where the room interior spans [x, x+w-1] x [y, y+h-1] at level z.
        /// </summary>
        public void AddRoomWalls(int x, int y, int z, int w, int h)
        {
            // South wall (bottom edge of the room)
            for (int dx = 0; dx < w; dx++)
                SetWall(x + dx, y, z, WallSide.South);
            // North wall (top edge of the room)
            for (int dx = 0; dx < w; dx++)
                SetWall(x + dx, y + h - 1, z, WallSide.North);
            // West wall (left edge of the room)
            for (int dy = 0; dy < h; dy++)
                SetWall(x, y + dy, z, WallSide.West);
            // East wall (right edge of the room)
            for (int dy = 0; dy < h; dy++)
                SetWall(x + w - 1, y + dy, z, WallSide.East);
        }

        /// <summary>
        /// Remove walls on the given side for a range of tiles (used for doorways).
        /// </summary>
        public void RemoveWallSegment(int x, int y, int z, WallSide side, int length = 1)
        {
            for (int i = 0; i < length; i++)
            {
                switch (side)
                {
                    case WallSide.North:
                    case WallSide.South:
                        RemoveWall(x + i, y, z, side);
                        break;
                    case WallSide.East:
                    case WallSide.West:
                        RemoveWall(x, y + i, z, side);
                        break;
                }
            }
        }
    }

    /// <summary>
    /// Serializable wall edge data for JSON persistence.
    /// </summary>
    public class WallEdgeData
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Z { get; set; }
        public WallSide Side { get; set; }
    }
}
