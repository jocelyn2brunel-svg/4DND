using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace _4DND
{
    public enum DungeonDoorType
    {
        Wooden,
        Stone,
        Iron,
        Portcullis,
        Secret
    }

    public class DungeonDoorState
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Z { get; set; }
        public DungeonDoorType Type { get; set; }
        public bool IsOpen { get; set; }
        public bool IsLocked { get; set; }
        public bool IsBarred { get; set; }
        public int CurrentHP { get; set; }
        public int MaxHP { get; set; }
        public int LockDC { get; set; } = 15;
        public int StrengthDC { get; set; } = 15;
    }

    public class DungeonStairs
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Z { get; set; }
        public int TargetZ { get; set; }
        public bool IsUp { get; set; }
    }

    public class DungeonRoom
    {
        public Rectangle Bounds { get; set; }
        public int Z { get; set; }
        public string Description { get; set; } = "";
        // Future: public List<string> Contents { get; set; }
    }

    public class DungeonData
    {
        public string Name { get; set; } = "";
        public int WorldX { get; set; } // Axial Q
        public int WorldY { get; set; } // Axial R
        public int Seed { get; set; }

        public List<DungeonRoom> Rooms { get; set; } = new();
        public List<DungeonDoorState> Doors { get; set; } = new();
        public List<DungeonStairs> Stairs { get; set; } = new();

        // Tiles that are part of the dungeon (relative to dungeon origin)
        // This is for persistence of changes if we don't use the global ExploredTiles for everything
        public Dictionary<(int x, int y, int z), TileType> LayoutOverrides { get; set; } = new();
    }
}
