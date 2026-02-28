using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace _4DND;

public partial class Game1
{
    // Cached reachable positions for the movement perimeter overlay
    private HashSet<(int x, int y)>? _cachedReachableTiles;
    private int _cachedReachableViewLevel;
    private (int x, int y, int z, int movementRemaining) _cachedReachableKey;

    private void Initialize3DRendering()
    {
        _basicEffect = new BasicEffect(GraphicsDevice) { VertexColorEnabled = true, LightingEnabled = true };
        _basicEffect.EnableDefaultLighting();

        var cubeVertices = new VertexPositionNormalColor[] {
            new(new(-0.5f,-0.5f,0), Vector3.Down, Color.White), new(new(0.5f,-0.5f,0), Vector3.Down, Color.White), new(new(0.5f,0.5f,0), Vector3.Down, Color.White), new(new(-0.5f,0.5f,0), Vector3.Down, Color.White),
            new(new(-0.5f,-0.5f,1), Vector3.Up, Color.White), new(new(0.5f,-0.5f,1), Vector3.Up, Color.White), new(new(0.5f,0.5f,1), Vector3.Up, Color.White), new(new(-0.5f,0.5f,1), Vector3.Up, Color.White),
            new(new(-0.5f,-0.5f,0), Vector3.Left, Color.White), new(new(-0.5f,0.5f,0), Vector3.Left, Color.White), new(new(-0.5f,0.5f,1), Vector3.Left, Color.White), new(new(-0.5f,-0.5f,1), Vector3.Left, Color.White),
            new(new(0.5f,-0.5f,0), Vector3.Right, Color.White), new(new(0.5f,-0.5f,1), Vector3.Right, Color.White), new(new(0.5f,0.5f,1), Vector3.Right, Color.White), new(new(0.5f,0.5f,0), Vector3.Right, Color.White),
            new(new(-0.5f,-0.5f,0), Vector3.Forward, Color.White), new(new(0.5f,-0.5f,0), Vector3.Forward, Color.White), new(new(0.5f,-0.5f,1), Vector3.Forward, Color.White), new(new(-0.5f,-0.5f,1), Vector3.Forward, Color.White),
            new(new(-0.5f,0.5f,0), Vector3.Backward, Color.White), new(new(-0.5f,0.5f,1), Vector3.Backward, Color.White), new(new(0.5f,0.5f,1), Vector3.Backward, Color.White), new(new(0.5f,0.5f,0), Vector3.Backward, Color.White)
        };
        _cubeVertexBuffer = new VertexBuffer(GraphicsDevice, typeof(VertexPositionNormalColor), cubeVertices.Length, BufferUsage.WriteOnly);
        _cubeVertexBuffer.SetData(cubeVertices);
        var cubeIndices = new short[] { 0,1,2,0,2,3, 4,6,5,4,7,6, 8,9,10,8,10,11, 12,13,14,12,14,15, 16,17,18,16,18,19, 20,21,22,20,22,23 };
        _cubeIndexBuffer = new IndexBuffer(GraphicsDevice, IndexElementSize.SixteenBits, cubeIndices.Length, BufferUsage.WriteOnly);
        _cubeIndexBuffer.SetData(cubeIndices);

        var tileVertices = new VertexPositionNormalColor[] {
            new(new(-0.5f,-0.5f,0), Vector3.Up, Color.White), new(new(0.5f,-0.5f,0), Vector3.Up, Color.White), new(new(0.5f,0.5f,0), Vector3.Up, Color.White), new(new(-0.5f,0.5f,0), Vector3.Up, Color.White)
        };
        _tileVertexBuffer = new VertexBuffer(GraphicsDevice, typeof(VertexPositionNormalColor), tileVertices.Length, BufferUsage.WriteOnly);
        _tileVertexBuffer.SetData(tileVertices);
        var tileIndices = new short[] { 0, 1, 2, 0, 2, 3 };
        _tileIndexBuffer = new IndexBuffer(GraphicsDevice, IndexElementSize.SixteenBits, tileIndices.Length, BufferUsage.WriteOnly);
        _tileIndexBuffer.SetData(tileIndices);

        const int longitudeSegments = 16;
        const int hemisphereLatitudeSegments = 8;
        const int cylinderSegments = 1;
        const float capsuleRadius = 0.5f;
        const float capsuleBodyHeight = 1.0f;
        float hemisphereCenterBottom = capsuleRadius;
        float hemisphereCenterTop = hemisphereCenterBottom + capsuleBodyHeight;

        var capsuleVertices = new List<VertexPositionNormalColor>();
        var capsuleIndices = new List<short>();

        short AddCapsuleVertex(Vector3 position, Vector3 normal)
        {
            capsuleVertices.Add(new VertexPositionNormalColor(position, Vector3.Normalize(normal), Color.White));
            return (short)(capsuleVertices.Count - 1);
        }

        void AddTriangle(short a, short b, short c)
        {
            capsuleIndices.Add(a);
            capsuleIndices.Add(b);
            capsuleIndices.Add(c);
        }

        var cylinderRings = new List<short[]>();
        for (int ring = 0; ring <= cylinderSegments; ring++)
        {
            float t = ring / (float)cylinderSegments;
            float z = hemisphereCenterBottom + (t * capsuleBodyHeight);
            var ringIndices = new short[longitudeSegments + 1];
            for (int lon = 0; lon <= longitudeSegments; lon++)
            {
                float angle = MathHelper.TwoPi * lon / longitudeSegments;
                float x = MathF.Cos(angle) * capsuleRadius;
                float y = MathF.Sin(angle) * capsuleRadius;
                ringIndices[lon] = AddCapsuleVertex(new Vector3(x, y, z), new Vector3(x, y, 0f));
            }

            cylinderRings.Add(ringIndices);
        }

        for (int ring = 0; ring < cylinderSegments; ring++)
        {
            var lower = cylinderRings[ring];
            var upper = cylinderRings[ring + 1];
            for (int lon = 0; lon < longitudeSegments; lon++)
            {
                short l0 = lower[lon];
                short l1 = lower[lon + 1];
                short u0 = upper[lon];
                short u1 = upper[lon + 1];
                AddTriangle(l0, u0, u1);
                AddTriangle(l0, u1, l1);
            }
        }

        short[] BuildHemisphereRings(float centerZ, bool isUpper)
        {
            var seamRing = new short[longitudeSegments + 1];
            for (int lat = 1; lat <= hemisphereLatitudeSegments; lat++)
            {
                float t = lat / (float)hemisphereLatitudeSegments;
                float phi = t * MathHelper.PiOver2;
                float ringRadius = capsuleRadius * MathF.Cos(phi);
                float localZ = capsuleRadius * MathF.Sin(phi);
                float z = isUpper ? centerZ + localZ : centerZ - localZ;

                var currentRing = new short[longitudeSegments + 1];
                for (int lon = 0; lon <= longitudeSegments; lon++)
                {
                    float angle = MathHelper.TwoPi * lon / longitudeSegments;
                    float x = MathF.Cos(angle) * ringRadius;
                    float y = MathF.Sin(angle) * ringRadius;
                    Vector3 position = new Vector3(x, y, z);
                    Vector3 normal = position - new Vector3(0f, 0f, centerZ);
                    currentRing[lon] = AddCapsuleVertex(position, normal);
                }

                if (lat == 1)
                {
                    seamRing = currentRing;
                }
                else
                {
                    for (int lon = 0; lon < longitudeSegments; lon++)
                    {
                        short p0 = seamRing[lon];
                        short p1 = seamRing[lon + 1];
                        short c0 = currentRing[lon];
                        short c1 = currentRing[lon + 1];
                        if (isUpper)
                        {
                            AddTriangle(p0, c0, c1);
                            AddTriangle(p0, c1, p1);
                        }
                        else
                        {
                            AddTriangle(p0, c1, c0);
                            AddTriangle(p0, p1, c1);
                        }
                    }

                    seamRing = currentRing;
                }
            }

            short pole = AddCapsuleVertex(new Vector3(0f, 0f, isUpper ? centerZ + capsuleRadius : centerZ - capsuleRadius), isUpper ? Vector3.Up : Vector3.Down);
            for (int lon = 0; lon < longitudeSegments; lon++)
            {
                short a = seamRing[lon];
                short b = seamRing[lon + 1];
                if (isUpper) AddTriangle(a, pole, b);
                else AddTriangle(a, b, pole);
            }

            return seamRing;
        }

        var lowerHemisphereSeam = BuildHemisphereRings(hemisphereCenterBottom, false);
        var upperHemisphereSeam = BuildHemisphereRings(hemisphereCenterTop, true);

        var lowerCylinderRing = cylinderRings[0];
        var upperCylinderRing = cylinderRings[^1];
        for (int lon = 0; lon < longitudeSegments; lon++)
        {
            AddTriangle(lowerHemisphereSeam[lon], lowerCylinderRing[lon + 1], lowerCylinderRing[lon]);
            AddTriangle(lowerHemisphereSeam[lon], lowerHemisphereSeam[lon + 1], lowerCylinderRing[lon + 1]);

            AddTriangle(upperHemisphereSeam[lon], upperCylinderRing[lon], upperCylinderRing[lon + 1]);
            AddTriangle(upperHemisphereSeam[lon], upperCylinderRing[lon + 1], upperHemisphereSeam[lon + 1]);
        }

        _capsuleVertexBuffer = new VertexBuffer(GraphicsDevice, typeof(VertexPositionNormalColor), capsuleVertices.Count, BufferUsage.WriteOnly);
        _capsuleVertexBuffer.SetData(capsuleVertices.ToArray());
        _capsuleIndexBuffer = new IndexBuffer(GraphicsDevice, IndexElementSize.SixteenBits, capsuleIndices.Count, BufferUsage.WriteOnly);
        _capsuleIndexBuffer.SetData(capsuleIndices.ToArray());
        _capsulePrimitiveCount = capsuleIndices.Count / 3;
    }

    private void Draw3DGrid(int zLevel)
    {
        _basicEffect.World = Matrix.Identity;
        _basicEffect.LightingEnabled = true;
        _basicEffect.DiffuseColor = Vector3.One;

        _reusableWallVertices.Clear();
        _reusableTileVertices.Clear();
        _reusableLineVertices.Clear();
        _reusableVegetation.Clear();

        // 1. Draw procedural ground around camera
        int viewDist = 40; // View radius in tiles
        int minX = (int)Math.Floor(_cameraTarget.X - viewDist);
        int maxX = (int)Math.Ceiling(_cameraTarget.X + viewDist);
        int minY = (int)Math.Floor(_cameraTarget.Y - viewDist);
        int maxY = (int)Math.Ceiling(_cameraTarget.Y + viewDist);

        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                TileType type = _tacticalMap.Get(x, y, 0);
                if (type == TileType.Empty) continue;

                Color color = GetTileColor(type, x, y, 0, zLevel);
                if (color.A == 0) continue;

                if (IsDungeonDoor(type))
                {
                    // Doors still render as thin wall-like structures on their tile
                    AddThinWallVertices(_reusableWallVertices, x, y, 0, color);
                }
                else if (type == TileType.Tree || type == TileType.Shrub)
                {
                    // Draw grass under vegetation
                    Color groundColor = GetTileColor(TileType.Grass, x, y, 0, zLevel);
                    AddTileVertices(_reusableTileVertices, x, y, 0, TileType.Grass, groundColor);
                    _reusableVegetation.Add((x, y, 0, type));
                }
                else
                {
                    AddTileVertices(_reusableTileVertices, x, y, 0, type, color);
                    if (type == TileType.DifficultTerrain)
                    {
                        AddDifficultTerrainLines(_reusableLineVertices, x, y, 0);
                    }
                }

                // Draw wall edges for this tile
                AddWallEdgeVertices(_reusableWallVertices, x, y, 0, zLevel);
            }
        }

        // 2. Draw cached cells at upper levels and z=0 border within a bounded range.
        // Only consult previously-cached tiles (TryGetCached) to avoid triggering the
        // procedural factory for every coordinate and to keep cost O(viewArea) per frame
        // instead of O(totalCachedCells) which grows without bound.
        int extMinX = minX - 5;
        int extMaxX = maxX + 5;
        int extMinY = minY - 5;
        int extMaxY = maxY + 5;

        for (int cz = 0; cz <= zLevel; cz++)
        {
            for (int cy = extMinY; cy <= extMaxY; cy++)
            {
                for (int cx = extMinX; cx <= extMaxX; cx++)
                {
                    // Skip cells already drawn in the view box at z=0
                    if (cz == 0 && cx >= minX && cx <= maxX && cy >= minY && cy <= maxY) continue;

                    if (!_tacticalMap.TryGetCached(cx, cy, cz, out var type) || type == TileType.Empty) continue;

                    Color color = GetTileColor(type, cx, cy, cz, zLevel);
                    if (color.A == 0) continue;

                    if (IsDungeonDoor(type))
                    {
                        AddThinWallVertices(_reusableWallVertices, cx, cy, cz, color);
                    }
                    else if (type == TileType.Tree || type == TileType.Shrub)
                    {
                        TileType baseType = cz > 0 ? TileType.Floor : TileType.Grass;
                        Color groundColor = GetTileColor(baseType, cx, cy, cz, zLevel);
                        AddTileVertices(_reusableTileVertices, cx, cy, cz, baseType, groundColor);
                        _reusableVegetation.Add((cx, cy, cz, type));
                    }
                    else
                    {
                        AddTileVertices(_reusableTileVertices, cx, cy, cz, type, color);
                        if (type == TileType.DifficultTerrain)
                        {
                            AddDifficultTerrainLines(_reusableLineVertices, cx, cy, cz);
                        }
                        else if (type == TileType.BallBearings)
                        {
                            AddBallBearingsOverlay(_reusableLineVertices, cx, cy, cz);
                        }
                    }

                    // Draw wall edges for this tile
                    AddWallEdgeVertices(_reusableWallVertices, cx, cy, cz, zLevel);
                }
            }
        }

        // 3. Render batches
        if (_reusableWallVertices.Count > 0)
        {
            _basicEffect.World = Matrix.Identity;
            foreach (var pass in _basicEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, _reusableWallVertices.ToArray(), 0, _reusableWallVertices.Count / 3);
            }
        }

        if (_reusableTileVertices.Count > 0)
        {
            _basicEffect.World = Matrix.Identity;
            foreach (var pass in _basicEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, _reusableTileVertices.ToArray(), 0, _reusableTileVertices.Count / 3);
            }
        }

        if (_reusableLineVertices.Count > 0)
        {
            _basicEffect.World = Matrix.Identity;
            _basicEffect.LightingEnabled = false;
            foreach (var pass in _basicEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                GraphicsDevice.DrawUserPrimitives(PrimitiveType.LineList, _reusableLineVertices.ToArray(), 0, _reusableLineVertices.Count / 2);
            }
            _basicEffect.LightingEnabled = true;
        }

        // 4. Draw vegetation volumes
        foreach (var v in _reusableVegetation)
        {
            if (v.type == TileType.Tree)
                Draw3DTree(v.x, v.y, v.z, zLevel);
            else
                Draw3DShrub(v.x, v.y, v.z, zLevel);
        }
    }

    private void Draw3DShrub(int x, int y, int z, int zLevel)
    {
        float radius = 0.3f + Hash01(x, y, z, 200) * 0.2f;
        Color color = GetTileColor(TileType.Shrub, x, y, z, zLevel);
        Draw3DCapsule(x, y, z, radius, 0, color);
    }

    private void Draw3DTree(int x, int y, int z, int zLevel)
    {
        float trunkHeight = 0.6f + Hash01(x, y, z, 300) * 0.8f;
        float trunkRadius = 0.1f + Hash01(x, y, z, 301) * 0.05f;
        float canopyRadius = 0.4f + Hash01(x, y, z, 302) * 0.4f;

        Color trunkColor = new Color(101, 67, 33); // Brown
        Color leafColor = GetTileColor(TileType.Tree, x, y, z, zLevel);

        // Scale color for visibility
        if (_showVisionOverlay && _playerCreature != null)
        {
            bool isVisible = _visionSystem.IsVisible(x, y, z);
            Color tint = _visionSystem.GetFogOfWarTint(x, y, z, isVisible, _playerCreature);
            trunkColor = ApplyVisionTint(trunkColor, tint);
            if (trunkColor == Color.Transparent) return;
        }

        // Trunk (using a cube scaled thin and tall)
        Draw3DCube(x, y, z, 1.0f, trunkColor, new Vector3(trunkRadius, trunkRadius, trunkHeight));

        // Canopy (Sphere)
        Draw3DCapsule(x, y, z + trunkHeight, canopyRadius, 0, leafColor);
    }

    private Color GetTileColor(TileType type, int x, int y, int z, int zLevel)
    {
        Color baseColor = type switch
        {
            TileType.Floor => new Color(70, 145, 70),
            TileType.Grass => new Color(80, 180, 80),
            TileType.DifficultTerrain => new Color(139, 69, 19),
            TileType.BallBearings => new Color(180, 180, 190),
            TileType.Wall => new Color(100, 100, 110),
            TileType.Water => Color.CornflowerBlue,
            TileType.Sand => new Color(210, 180, 120),
            TileType.Snow => new Color(240, 240, 250),
            TileType.Ice => new Color(160, 200, 230),
            TileType.Mud => new Color(60, 50, 40),
            TileType.Rock => new Color(120, 120, 125),
            TileType.Tree => new Color(40, 80, 30),
            TileType.Shrub => new Color(60, 100, 40),
            TileType.DungeonFloor => new Color(60, 60, 65),
            TileType.DungeonWall => new Color(90, 90, 95),
            TileType.DungeonDoorWooden => new Color(100, 70, 40),
            TileType.DungeonDoorStone => new Color(110, 110, 115),
            TileType.DungeonDoorIron => new Color(70, 70, 80),
            TileType.DungeonPortcullis => new Color(50, 50, 55),
            TileType.DungeonSecretDoor => new Color(90, 90, 95),
            TileType.DungeonStairsUp => new Color(80, 80, 85),
            TileType.DungeonStairsDown => new Color(80, 80, 85),
            _ => Color.ForestGreen
        };

        // Deterministic tile-level variation to mimic texture diversity and reduce visible tiling.
        if (type == TileType.Grass || type == TileType.Floor || type == TileType.Sand || type == TileType.Snow || type == TileType.Rock || type == TileType.Tree || type == TileType.Shrub || type == TileType.DungeonFloor)
        {
            float dryness = Hash01(x, y, z, 11);
            float moisture = Hash01(x, y, z, 23);
            float wear = Hash01(x, y, z, 37);

            // Blend toward worn and humid variants while staying readable for tactical overlays.
            baseColor = Color.Lerp(baseColor, new Color(118, 106, 74), MathHelper.Clamp((wear - 0.72f) * 1.7f, 0f, 0.28f));
            baseColor = Color.Lerp(baseColor, new Color(52, 128, 78), MathHelper.Clamp((moisture - 0.65f) * 1.4f, 0f, 0.22f));
            baseColor = Color.Lerp(baseColor, new Color(142, 160, 93), MathHelper.Clamp((dryness - 0.8f) * 1.3f, 0f, 0.18f));

            // Parity keeps grid rhythm readable without making a checkerboard too obvious.
            float parity = (((x + y) & 1) == 0) ? 1.02f : 0.98f;
            baseColor = ScaleColor(baseColor, parity);
        }
        else if (type == TileType.DifficultTerrain)
        {
            float mudNoise = Hash01(x, y, z, 53);
            baseColor = ScaleColor(baseColor, 0.92f + mudNoise * 0.12f);
        }

        // Keep upper/lower floor separation, but avoid darkening the ground floor (z=0)
        // when changing view levels so the base floor remains readable.
        if (z < zLevel && z != 0) baseColor *= 0.3f;
        if (_showVisionOverlay && _playerCreature != null)
        {
            bool isVisible = _visionSystem.IsVisible(x, y, z);
            Color tint = _visionSystem.GetFogOfWarTint(x, y, z, isVisible, _playerCreature);
            baseColor = ApplyVisionTint(baseColor, tint);
        }
        return baseColor;
    }

    private void AddDifficultTerrainLines(List<VertexPositionColor> vertices, int x, int y, int z)
    {
        const float elevation = 0.015f;
        Color color = Color.Black * 0.5f;
        vertices.Add(new VertexPositionColor(new Vector3(x - 0.2f, y - 0.2f, z + elevation), color));
        vertices.Add(new VertexPositionColor(new Vector3(x + 0.2f, y + 0.2f, z + elevation), color));
        vertices.Add(new VertexPositionColor(new Vector3(x - 0.2f, y + 0.2f, z + elevation), color));
        vertices.Add(new VertexPositionColor(new Vector3(x + 0.2f, y - 0.2f, z + elevation), color));
    }

    private void AddBallBearingsOverlay(List<VertexPositionColor> vertices, int x, int y, int z)
    {
        const float elevation = 0.016f;
        const float r = 0.06f;
        Color color = Color.DimGray * 0.8f;
        // Draw 4 small cross markers representing scattered ball bearings
        float[] offsets = { -0.25f, 0.25f };
        foreach (float ox in offsets)
        {
            foreach (float oy in offsets)
            {
                vertices.Add(new VertexPositionColor(new Vector3(x + ox - r, y + oy, z + elevation), color));
                vertices.Add(new VertexPositionColor(new Vector3(x + ox + r, y + oy, z + elevation), color));
                vertices.Add(new VertexPositionColor(new Vector3(x + ox, y + oy - r, z + elevation), color));
                vertices.Add(new VertexPositionColor(new Vector3(x + ox, y + oy + r, z + elevation), color));
            }
        }
    }

    private void AddGridOutlineVertices(List<VertexPositionColor> vertices, int x, int y, int z, Color color)
    {
        const float halfTile = 0.5f;
        const float elevation = 0.01f;
        float zPos = z + elevation;

        Vector3 tl = new Vector3(x - halfTile, y - halfTile, zPos);
        Vector3 tr = new Vector3(x + halfTile, y - halfTile, zPos);
        Vector3 br = new Vector3(x + halfTile, y + halfTile, zPos);
        Vector3 bl = new Vector3(x - halfTile, y + halfTile, zPos);

        vertices.Add(new VertexPositionColor(tl, color)); vertices.Add(new VertexPositionColor(tr, color));
        vertices.Add(new VertexPositionColor(tr, color)); vertices.Add(new VertexPositionColor(br, color));
        vertices.Add(new VertexPositionColor(br, color)); vertices.Add(new VertexPositionColor(bl, color));
        vertices.Add(new VertexPositionColor(bl, color)); vertices.Add(new VertexPositionColor(tl, color));
    }

    private void AddTileVertices(List<VertexPositionNormalColor> vertices, int x, int y, int z, TileType type, Color baseColor)
    {
        const float half = 0.5f;
        Vector3 n = Vector3.Up;

        Color bottomLeft = baseColor;
        Color bottomRight = baseColor;
        Color topRight = baseColor;
        Color topLeft = baseColor;

        if (type == TileType.Grass || type == TileType.Floor || type == TileType.DifficultTerrain ||
            type == TileType.Sand || type == TileType.Snow || type == TileType.Rock ||
            type == TileType.Tree || type == TileType.Shrub)
        {
            // Per-corner variation creates a subtle faux texture and breaks up repeated flat color blocks.
            bottomLeft = ScaleColor(baseColor, 0.92f + Hash01(x, y, z, 101) * 0.16f);
            bottomRight = ScaleColor(baseColor, 0.92f + Hash01(x, y, z, 102) * 0.16f);
            topRight = ScaleColor(baseColor, 0.92f + Hash01(x, y, z, 103) * 0.16f);
            topLeft = ScaleColor(baseColor, 0.92f + Hash01(x, y, z, 104) * 0.16f);
        }

        // Triangle 1
        vertices.Add(new VertexPositionNormalColor(new Vector3(x - half, y - half, z), n, bottomLeft));
        vertices.Add(new VertexPositionNormalColor(new Vector3(x + half, y - half, z), n, bottomRight));
        vertices.Add(new VertexPositionNormalColor(new Vector3(x + half, y + half, z), n, topRight));

        // Triangle 2
        vertices.Add(new VertexPositionNormalColor(new Vector3(x - half, y - half, z), n, bottomLeft));
        vertices.Add(new VertexPositionNormalColor(new Vector3(x + half, y + half, z), n, topRight));
        vertices.Add(new VertexPositionNormalColor(new Vector3(x - half, y + half, z), n, topLeft));
    }

    private static Color ScaleColor(Color color, float factor)
    {
        factor = MathHelper.Clamp(factor, 0f, 2f);
        return new Color(
            (byte)MathHelper.Clamp(color.R * factor, 0f, 255f),
            (byte)MathHelper.Clamp(color.G * factor, 0f, 255f),
            (byte)MathHelper.Clamp(color.B * factor, 0f, 255f),
            color.A);
    }

    private static Color ApplyVisionTint(Color baseColor, Color tint)
    {
        if (tint == Color.Black) return Color.Transparent;

        if (tint.A == 254) // Magic flag for grayscale darkvision
        {
            float gray = baseColor.R * 0.299f + baseColor.G * 0.587f + baseColor.B * 0.114f;
            // Apply the tint (e.g. 128, 128, 128 for dim light) to the grayscale value
            byte finalGray = (byte)MathHelper.Clamp(gray * tint.R / 255f, 0, 255);
            return new Color(finalGray, finalGray, finalGray, baseColor.A);
        }

        return new Color(
            (byte)MathHelper.Clamp(baseColor.R * tint.R / 255f, 0, 255),
            (byte)MathHelper.Clamp(baseColor.G * tint.G / 255f, 0, 255),
            (byte)MathHelper.Clamp(baseColor.B * tint.B / 255f, 0, 255),
            (byte)MathHelper.Clamp(baseColor.A * tint.A / 255f, 0, 255));
    }

    private static float Hash01(int x, int y, int z, int seed)
    {
        unchecked
        {
            int h = x * 374761393 + y * 668265263 + z * 982451653 + seed * 1442695041;
            h = (h ^ (h >> 13)) * 1274126177;
            h ^= h >> 16;
            uint u = (uint)h;
            return (u & 0x00FFFFFF) / 16777215f;
        }
    }

    private bool IsWallLike(TileType type) => IsDungeonDoor(type);
    private bool IsDungeonDoor(TileType type) => type == TileType.DungeonDoorWooden || type == TileType.DungeonDoorStone || type == TileType.DungeonDoorIron || type == TileType.DungeonPortcullis || type == TileType.DungeonSecretDoor;

    /// <summary>
    /// Draw wall edge geometry for all edges of a given tile that have walls in the WallEdgeSystem.
    /// </summary>
    private void AddWallEdgeVertices(List<VertexPositionNormalColor> vertices, int x, int y, int z, int zLevel)
    {
        Color wallColor = new Color(90, 90, 95);
        if (z < zLevel && z != 0) wallColor *= 0.3f;
        if (_showVisionOverlay && _playerCreature != null)
        {
            bool isVisible = _visionSystem.IsVisible(x, y, z);
            Color tint = _visionSystem.GetFogOfWarTint(x, y, z, isVisible, _playerCreature);
            wallColor = ApplyVisionTint(wallColor, tint);
        }
        if (wallColor.A == 0) return;

        const float halfTile = 0.5f;
        const float wallHeight = 1.0f;
        const float capThickness = 0.05f;
        // Small offset to lift wall base above the ground tile plane, preventing Z-fighting.
        const float wallBaseOffset = 0.001f;

        // Only emit geometry for canonical edges owned by this tile (North, East).
        // South of (x,y) is the same edge as North of (x,y-1), and West of (x,y)
        // is the same edge as East of (x-1,y). Drawing both produces duplicate
        // overlapping triangles that cause Z-fighting.

        if (_wallEdges.HasWall(x, y, z, WallSide.North))
        {
            Vector3 start = new Vector3(x - halfTile, y + halfTile, z + wallBaseOffset);
            Vector3 end = new Vector3(x + halfTile, y + halfTile, z + wallBaseOffset);
            AddVerticalPlaneVertices(vertices, start, end, wallHeight, wallColor, Vector3.Backward);
            AddEdgeCapSegment(vertices, start, end, capThickness, wallHeight, wallColor);
        }
        if (_wallEdges.HasWall(x, y, z, WallSide.East))
        {
            Vector3 start = new Vector3(x + halfTile, y + halfTile, z + wallBaseOffset);
            Vector3 end = new Vector3(x + halfTile, y - halfTile, z + wallBaseOffset);
            AddVerticalPlaneVertices(vertices, start, end, wallHeight, wallColor, Vector3.Right);
            AddEdgeCapSegment(vertices, start, end, capThickness, wallHeight, wallColor);
        }
    }

    /// <summary>
    /// Draw a thin cap on top of a wall edge segment.
    /// </summary>
    private void AddEdgeCapSegment(List<VertexPositionNormalColor> vertices, Vector3 p1, Vector3 p2, float thickness, float wallHeight, Color color)
    {
        float topZ = p1.Z + wallHeight;
        Vector3 dir = Vector3.Normalize(p2 - p1);
        Vector3 side = Vector3.Cross(dir, Vector3.UnitZ) * thickness;

        Vector3 v1 = new Vector3(p1.X, p1.Y, topZ);
        Vector3 v2 = new Vector3(p2.X, p2.Y, topZ);
        Vector3 v3 = v1 - side;
        Vector3 v4 = v2 - side;

        vertices.Add(new VertexPositionNormalColor(v1, Vector3.Up, color));
        vertices.Add(new VertexPositionNormalColor(v3, Vector3.Up, color));
        vertices.Add(new VertexPositionNormalColor(v2, Vector3.Up, color));

        vertices.Add(new VertexPositionNormalColor(v2, Vector3.Up, color));
        vertices.Add(new VertexPositionNormalColor(v3, Vector3.Up, color));
        vertices.Add(new VertexPositionNormalColor(v4, Vector3.Up, color));

        // Also draw cap on the other side
        Vector3 v5 = v1 + side;
        Vector3 v6 = v2 + side;

        vertices.Add(new VertexPositionNormalColor(v1, Vector3.Up, color));
        vertices.Add(new VertexPositionNormalColor(v2, Vector3.Up, color));
        vertices.Add(new VertexPositionNormalColor(v5, Vector3.Up, color));

        vertices.Add(new VertexPositionNormalColor(v2, Vector3.Up, color));
        vertices.Add(new VertexPositionNormalColor(v6, Vector3.Up, color));
        vertices.Add(new VertexPositionNormalColor(v5, Vector3.Up, color));
    }

    private void AddThinWallVertices(List<VertexPositionNormalColor> vertices, int x, int y, int z, Color color)
    {
        // Used for dungeon doors which still occupy a tile.
        // Check neighbors to see which edges need a vertical plane
        bool north = !IsWallLike(_tacticalMap.Get(x, y + 1, z));
        bool south = !IsWallLike(_tacticalMap.Get(x, y - 1, z));
        bool east = !IsWallLike(_tacticalMap.Get(x + 1, y, z));
        bool west = !IsWallLike(_tacticalMap.Get(x - 1, y, z));

        const float halfTile = 0.5f;
        const float wallHeight = 1.0f;
        const float wallBaseOffset = 0.001f;
        float baseZ = z + wallBaseOffset;

        if (north) AddVerticalPlaneVertices(vertices, new Vector3(x - halfTile, y + halfTile, baseZ), new Vector3(x + halfTile, y + halfTile, baseZ), wallHeight, color, Vector3.Backward);
        if (south) AddVerticalPlaneVertices(vertices, new Vector3(x + halfTile, y - halfTile, baseZ), new Vector3(x - halfTile, y - halfTile, baseZ), wallHeight, color, Vector3.Forward);
        if (east) AddVerticalPlaneVertices(vertices, new Vector3(x + halfTile, y + halfTile, baseZ), new Vector3(x + halfTile, y - halfTile, baseZ), wallHeight, color, Vector3.Right);
        if (west) AddVerticalPlaneVertices(vertices, new Vector3(x - halfTile, y - halfTile, baseZ), new Vector3(x - halfTile, y + halfTile, baseZ), wallHeight, color, Vector3.Left);

        // Also draw a small "cap" on top of the wall edges to make them look solid from above
        // This helps the "separation" look
        AddHorizontalWallCap(vertices, x, y, z, color);
    }

    private void AddVerticalPlaneVertices(List<VertexPositionNormalColor> vertices, Vector3 start, Vector3 end, float height, Color color, Vector3 normal)
    {
        var v1 = new VertexPositionNormalColor(start, normal, color);
        var v2 = new VertexPositionNormalColor(end, normal, color);
        var v3 = new VertexPositionNormalColor(start + Vector3.UnitZ * height, normal, color);
        var v4 = new VertexPositionNormalColor(end + Vector3.UnitZ * height, normal, color);

        // Triangle 1
        vertices.Add(v1); vertices.Add(v3); vertices.Add(v2);
        // Triangle 2
        vertices.Add(v2); vertices.Add(v3); vertices.Add(v4);

        // Draw the other side of the plane too so it's visible from both directions
        var v1r = new VertexPositionNormalColor(start, -normal, color);
        var v2r = new VertexPositionNormalColor(end, -normal, color);
        var v3r = new VertexPositionNormalColor(start + Vector3.UnitZ * height, -normal, color);
        var v4r = new VertexPositionNormalColor(end + Vector3.UnitZ * height, -normal, color);

        vertices.Add(v1r); vertices.Add(v2r); vertices.Add(v3r);
        vertices.Add(v2r); vertices.Add(v4r); vertices.Add(v3r);
    }

    private void AddHorizontalWallCap(List<VertexPositionNormalColor> vertices, int x, int y, int z, Color color)
    {
        const float halfTile = 0.5f;
        const float wallHeight = 1.0f;
        const float thickness = 0.05f; // Thin cap

        float topZ = z + wallHeight;

        // We draw a thin border on top
        Vector3 tl = new Vector3(x - halfTile, y - halfTile, topZ);
        Vector3 tr = new Vector3(x + halfTile, y - halfTile, topZ);
        Vector3 br = new Vector3(x + halfTile, y + halfTile, topZ);
        Vector3 bl = new Vector3(x - halfTile, y + halfTile, topZ);

        // Check neighbors to avoid drawing caps where walls are continuous
        bool north = !IsWallLike(_tacticalMap.Get(x, y + 1, z));
        bool south = !IsWallLike(_tacticalMap.Get(x, y - 1, z));
        bool east = !IsWallLike(_tacticalMap.Get(x + 1, y, z));
        bool west = !IsWallLike(_tacticalMap.Get(x - 1, y, z));

        if (north) AddCapSegment(vertices, bl, br, thickness, color);
        if (south) AddCapSegment(vertices, tr, tl, thickness, color);
        if (east) AddCapSegment(vertices, br, tr, thickness, color);
        if (west) AddCapSegment(vertices, tl, bl, thickness, color);
    }

    private void AddCapSegment(List<VertexPositionNormalColor> vertices, Vector3 p1, Vector3 p2, float thickness, Color color)
    {
        Vector3 dir = Vector3.Normalize(p2 - p1);
        Vector3 side = Vector3.Cross(dir, Vector3.UnitZ) * thickness;

        Vector3 v1 = p1;
        Vector3 v2 = p2;
        Vector3 v3 = p1 - side;
        Vector3 v4 = p2 - side;

        vertices.Add(new VertexPositionNormalColor(v1, Vector3.Up, color));
        vertices.Add(new VertexPositionNormalColor(v3, Vector3.Up, color));
        vertices.Add(new VertexPositionNormalColor(v2, Vector3.Up, color));

        vertices.Add(new VertexPositionNormalColor(v2, Vector3.Up, color));
        vertices.Add(new VertexPositionNormalColor(v3, Vector3.Up, color));
        vertices.Add(new VertexPositionNormalColor(v4, Vector3.Up, color));
    }

    private void Draw3DGridOutlines(int zLevel)
    {
        Color gridOutlineColor = new Color(40, 50, 60);
        _reusableLineVertices.Clear();

        int viewDist = 40;
        int minX = (int)Math.Floor(_cameraTarget.X - viewDist);
        int maxX = (int)Math.Ceiling(_cameraTarget.X + viewDist);
        int minY = (int)Math.Floor(_cameraTarget.Y - viewDist);
        int maxY = (int)Math.Ceiling(_cameraTarget.Y + viewDist);

        for (int cz = Math.Max(0, zLevel - 1); cz <= zLevel; cz++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    TileType type = _tacticalMap.Get(x, y, cz);
                    if (type == TileType.Empty) continue;

                    Color color = gridOutlineColor;
                    if (cz < zLevel) color *= 0.5f;

                    if (_showVisionOverlay && _playerCreature != null)
                    {
                        bool isVisible = _visionSystem.IsVisible(x, y, cz);
                        Color tint = _visionSystem.GetFogOfWarTint(x, y, cz, isVisible, _playerCreature);
                        color = ApplyVisionTint(color, tint);
                        if (color == Color.Transparent) continue;
                    }

                    AddGridOutlineVertices(_reusableLineVertices, x, y, cz, color);
                }
            }
        }

        if (_reusableLineVertices.Count > 0)
        {
            _basicEffect.World = Matrix.Identity;
            _basicEffect.LightingEnabled = false;
            foreach (var pass in _basicEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                GraphicsDevice.DrawUserPrimitives(PrimitiveType.LineList, _reusableLineVertices.ToArray(), 0, _reusableLineVertices.Count / 2);
            }
            _basicEffect.LightingEnabled = true;
        }
    }

    private void Draw3DLine(Vector3 start, Vector3 end, Color color)
    {
        _basicEffect.World = Matrix.Identity;
        var vertices = new[] { new VertexPositionColor(start, color), new VertexPositionColor(end, color) };
        _basicEffect.LightingEnabled = false;
        foreach (var pass in _basicEffect.CurrentTechnique.Passes) { pass.Apply(); GraphicsDevice.DrawUserPrimitives(PrimitiveType.LineList, vertices, 0, 1); }
        _basicEffect.LightingEnabled = true;
    }

    private void Draw3DTile(int x, int y, int z, Color color)
    {
        Draw3DQuad(Matrix.CreateTranslation(x, y, z), color, true);
    }

    private void Draw3DQuad(Matrix world, Color color, bool lighting = false)
    {
        _basicEffect.World = world;
        _basicEffect.DiffuseColor = color.ToVector3();
        _basicEffect.Alpha = color.A / 255f;
        _basicEffect.LightingEnabled = lighting;
        GraphicsDevice.SetVertexBuffer(_tileVertexBuffer);
        GraphicsDevice.Indices = _tileIndexBuffer;
        foreach (var pass in _basicEffect.CurrentTechnique.Passes) { pass.Apply(); GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, 2); }
        _basicEffect.LightingEnabled = true;
    }

    private void Draw3DCreatures()
    {
        if (_combatManager.InCombat) foreach (var creature in _combatManager.Combatants) { if (creature.IsAlive()) Draw3DCreature(creature); }
        else if (_playerCreature != null) { Draw3DCreature(_playerCreature); foreach (var creature in _combatManager.Combatants.Where(c => !c.IsPlayer && c.IsAlive())) Draw3DCreature(creature); }
    }

    private void DrawCreatureTileOutlines()
    {
        IEnumerable<Creature> creatures = _combatManager.InCombat
            ? _combatManager.Combatants.Where(c => c.IsAlive())
            : (_playerCreature == null
                ? Enumerable.Empty<Creature>()
                : new[] { _playerCreature }.Concat(_combatManager.Combatants.Where(c => !c.IsPlayer && c.IsAlive())));

        foreach (var creature in creatures)
        {
            if (creature.Z > _currentViewLevel) continue;

            // Use the rounded visual position so the outline follows the creature during movement animation
            int outlineX = (int)MathF.Round(creature.VisualX);
            int outlineY = (int)MathF.Round(creature.VisualY);
            int outlineZ = (int)MathF.Round(creature.VisualZ);

            bool isVisible = _visionSystem.IsVisible(outlineX, outlineY, outlineZ);
            if (_combatManager.InCombat && _showVisionOverlay && _playerCreature != null)
            {
                Color fogTint = _visionSystem.GetFogOfWarTint(outlineX, outlineY, outlineZ, isVisible, _playerCreature);
                if (fogTint == Color.Black) continue;
            }

            Color outlineColor = GetCreatureFactionOutlineColor(creature);
            if (_showVisionOverlay && _playerCreature != null)
            {
                Color tint = _visionSystem.GetFogOfWarTint(outlineX, outlineY, outlineZ, true, _playerCreature);
                outlineColor = ApplyVisionTint(outlineColor, tint);
                if (outlineColor == Color.Transparent) continue;
            }

            var (w, h) = SizeHelper.GetSpaceInSquares(creature.Size);
            Draw3DTileOutline(outlineX, outlineY, outlineZ, outlineColor, w, h);
        }
    }

    private void DrawPlayerMovementPerimeter()
    {
        if (!_combatManager.InCombat)
            return;

        var currentCombatant = _combatManager.CurrentCombatant;
        if (currentCombatant == null || !currentCombatant.IsPlayer)
            return;

        var key = (currentCombatant.X, currentCombatant.Y, currentCombatant.Z, currentCombatant.MovementRemaining);
        if (_cachedReachableTiles == null || _cachedReachableKey != key || _cachedReachableViewLevel != _currentViewLevel)
        {
            _cachedReachableTiles = _combatManager.GetReachablePositions(currentCombatant)
                .Where(t => t.z == _currentViewLevel)
                .Select(t => (t.x, t.y))
                .ToHashSet();
            _cachedReachableKey = key;
            _cachedReachableViewLevel = _currentViewLevel;
        }

        var reachableTiles = _cachedReachableTiles;

        if (reachableTiles.Count == 0)
            return;

        Color perimeterColor = Color.LimeGreen;
        const float thickness = 0.07f;
        const float zOffset = 0.091f; // Slightly higher than grid to avoid z-fighting
        float z = _currentViewLevel + zOffset;

        foreach (var tile in reachableTiles)
        {
            int x = tile.x;
            int y = tile.y;

            if (!reachableTiles.Contains((x, y + 1)))
            {
                Draw3DQuad(Matrix.CreateScale(1.0f + thickness, thickness, 1.0f) * Matrix.CreateTranslation(x, y + 0.5f, z), perimeterColor);
            }

            if (!reachableTiles.Contains((x, y - 1)))
            {
                Draw3DQuad(Matrix.CreateScale(1.0f + thickness, thickness, 1.0f) * Matrix.CreateTranslation(x, y - 0.5f, z), perimeterColor);
            }

            if (!reachableTiles.Contains((x + 1, y)))
            {
                Draw3DQuad(Matrix.CreateScale(thickness, 1.0f + thickness, 1.0f) * Matrix.CreateTranslation(x + 0.5f, y, z), perimeterColor);
            }

            if (!reachableTiles.Contains((x - 1, y)))
            {
                Draw3DQuad(Matrix.CreateScale(thickness, 1.0f + thickness, 1.0f) * Matrix.CreateTranslation(x - 0.5f, y, z), perimeterColor);
            }
        }
    }

    private void DrawHoveredMovementPath((int x, int y, int z)? hoveredTile)
    {
        if (!hoveredTile.HasValue)
            return;

        if (_playerCreature == null || !_playerCreature.IsAlive())
            return;

        int targetX = hoveredTile.Value.x;
        int targetY = hoveredTile.Value.y;
        int targetZ = hoveredTile.Value.z;

        // Path trace for flight is tricky, only show if on same level as target for now
        // unless the creature is currently flying.
        if (!_playerCreature.IsFlying && _playerCreature.Z != targetZ)
            return;

        const float zOffset = 0.12f;
        var offset = SizeHelper.GetCenterOffset(_playerCreature.Size);
        Vector3 previousPoint = new Vector3(_playerCreature.VisualX + offset.X, _playerCreature.VisualY + offset.Y, _playerCreature.VisualZ + zOffset);

        Color activePathColor = Color.LimeGreen; // Green for current movement

        // 1. Draw through remaining waypoints (current movement in progress)
        var waypoints = _playerCreature.GetRemainingWaypoints();
        bool isMoving = waypoints.Count > 0 || _playerCreature.HasQueuedSteps();
        foreach (var wp in waypoints)
        {
            Vector3 nextPoint = new Vector3(wp.X + offset.X, wp.Y + offset.Y, wp.Z + zOffset);
            DrawMovementPathSegment(previousPoint, nextPoint, activePathColor);
            previousPoint = nextPoint;
        }

        // 2. Draw potential new path from current (or destination) position to hover target
        if (_combatManager.GetCreatureAt(targetX, targetY, targetZ) == null)
        {
            // When mid-movement, compute path from the movement destination
            // so the preview connects smoothly to the end of the current waypoints.
            int startX = isMoving ? _playerCreature.TargetX : _playerCreature.X;
            int startY = isMoving ? _playerCreature.TargetY : _playerCreature.Y;
            int startZ = isMoving ? _playerCreature.TargetZ : _playerCreature.Z;

            var path = _combatManager.GetPath(_playerCreature, startX, startY, startZ, targetX, targetY, targetZ);
            if (path != null && path.Count >= 2)
            {
                Color hoverPathColor = (_combatManager.InCombat && !_combatManager.CanMove(_playerCreature, targetX, targetY, targetZ))
                    ? Color.Orange // Orange if out of movement
                    : activePathColor;

                for (int i = 1; i < path.Count; i++)
                {
                    var to = path[i];
                    Vector3 nextPoint = new Vector3(to.x + offset.X, to.y + offset.Y, to.z + zOffset);
                    DrawMovementPathSegment(previousPoint, nextPoint, hoverPathColor);
                    previousPoint = nextPoint;
                }
            }
        }
    }

    private void DrawMovementPathSegment(Vector3 start, Vector3 end, Color color)
    {
        const float thickness = 0.07f;

        Vector2 planarDelta = new Vector2(end.X - start.X, end.Y - start.Y);
        float length = planarDelta.Length();
        if (length < 0.001f)
            return;

        float angle = MathF.Atan2(planarDelta.Y, planarDelta.X);
        float z = (start.Z + end.Z) * 0.5f;

        var world = Matrix.CreateScale(length + thickness, thickness, 1.0f)
            * Matrix.CreateRotationZ(angle)
            * Matrix.CreateTranslation((start.X + end.X) * 0.5f, (start.Y + end.Y) * 0.5f, z);

        Draw3DQuad(world, color);
    }

    private void DrawEnemySightLinesToPlayer()
    {
        if (_playerCreature == null || !_playerCreature.IsAlive())
            return;

        foreach (var enemy in _combatManager.Combatants.Where(c => !c.IsPlayer && c.IsAlive()))
        {
            if (!_visionSystem.CanSee(enemy, _playerCreature))
                continue;

            Vector3 enemyPos = new Vector3(enemy.X, enemy.Y, enemy.Z + 0.65f);
            Vector3 playerPos = new Vector3(_playerCreature.X, _playerCreature.Y, _playerCreature.Z + 0.65f);
            Draw3DLine(enemyPos, playerPos, Color.Red * 0.9f);
        }
    }

    private static Color GetCreatureFactionOutlineColor(Creature creature)
    {
        if (creature.IsPlayer)
            return Color.LimeGreen;

        // Any non-player unit is considered hostile from the player's perspective.
        return Color.Red;
    }

    private void Draw3DTileOutline(int x, int y, int z, Color color, int width = 1, int height = 1)
    {
        const float thickness = 0.07f;
        const float elevation = 0.091f;
        float zPos = z + elevation;

        float left = x - 0.5f;
        float right = left + width;
        float top = y - 0.5f;
        float bottom = top + height;
        float horizontalLength = width + thickness;
        float verticalLength = height + thickness;

        // Match movement perimeter outline thickness for consistent visual language.
        Draw3DQuad(Matrix.CreateScale(horizontalLength, thickness, 1.0f) * Matrix.CreateTranslation((left + right) * 0.5f, top, zPos), color);
        Draw3DQuad(Matrix.CreateScale(horizontalLength, thickness, 1.0f) * Matrix.CreateTranslation((left + right) * 0.5f, bottom, zPos), color);
        Draw3DQuad(Matrix.CreateScale(thickness, verticalLength, 1.0f) * Matrix.CreateTranslation(left, (top + bottom) * 0.5f, zPos), color);
        Draw3DQuad(Matrix.CreateScale(thickness, verticalLength, 1.0f) * Matrix.CreateTranslation(right, (top + bottom) * 0.5f, zPos), color);
    }

    private void Draw3DCreature(Creature creature)
    {
        if (creature.Z > _currentViewLevel) return;
        bool isVisible = _visionSystem.IsVisible(creature.X, creature.Y, creature.Z);
        Color color = creature.DisplayColor;
        if (_showVisionOverlay && _playerCreature != null)
        {
            Color tint = _visionSystem.GetFogOfWarTint(creature.X, creature.Y, creature.Z, isVisible, _playerCreature);
            color = ApplyVisionTint(color, tint);
            if (color == Color.Transparent) return;
        }
        var (capsuleRadius, capsuleHeight) = GetCreatureCapsuleDimensions(creature.Size);
        var offset = SizeHelper.GetCenterOffset(creature.Size);

        // Use visual position for smooth movement
        Draw3DCapsule(creature.VisualX + offset.X, creature.VisualY + offset.Y, creature.VisualZ, capsuleRadius, capsuleHeight, color);

        if (creature.VisualZ > 0)
        {
            // Draw shadow centered under creature
            var (w, h) = SizeHelper.GetSpaceInSquares(creature.Size);
            for (int dx = 0; dx < w; dx++)
                for (int dy = 0; dy < h; dy++)
                    Draw3DTile(creature.X + dx, creature.Y + dy, 0, Color.Black * 0.3f);

            Draw3DLine(new Vector3(creature.VisualX + offset.X, creature.VisualY + offset.Y, creature.VisualZ), new Vector3(creature.VisualX + offset.X, creature.VisualY + offset.Y, 0), Color.Gray * 0.3f);
        }
    }

    private static (float Radius, float Height) GetCreatureCapsuleDimensions(CreatureSize size)
    {
        float scale = size switch
        {
            CreatureSize.Tiny => 0.4f,
            CreatureSize.Small => 0.7f,
            CreatureSize.Large => 1.5f,
            CreatureSize.Huge => 2.0f,
            CreatureSize.Gargantuan => 2.5f,
            _ => 0.9f
        };

        return (scale * 0.45f, scale * 0.9f);
    }

    private static float GetCreatureVisualTopZ(Creature creature)
    {
        var (capsuleRadius, capsuleHeight) = GetCreatureCapsuleDimensions(creature.Size);
        return creature.Z + capsuleHeight + (capsuleRadius * 2f);
    }

    private void Draw3DCapsule(float x, float y, float z, float radius, float height, Color color)
    {
        // Capsule mesh is authored with radius 0.5 and total unit height 2.0 (body=1.0 + 2*hemispheres)
        float capsuleTotalHeight = height + (radius * 2f);
        Vector3 scale = new Vector3(radius / 0.5f, radius / 0.5f, capsuleTotalHeight / 2.0f);
        _basicEffect.World = Matrix.CreateScale(scale) * Matrix.CreateTranslation(x, y, z);
        _basicEffect.DiffuseColor = color.ToVector3();
        _basicEffect.Alpha = color.A / 255f;
        _basicEffect.LightingEnabled = true;
        GraphicsDevice.SetVertexBuffer(_capsuleVertexBuffer);
        GraphicsDevice.Indices = _capsuleIndexBuffer;
        foreach (var pass in _basicEffect.CurrentTechnique.Passes) { pass.Apply(); GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, _capsulePrimitiveCount); }
    }

    private void Draw3DCube(float x, float y, float z, float scale, Color color, Vector3? nonUniformScale = null)
    {
        // Place la base du cube sur z
        Matrix world = Matrix.Identity;
        if (nonUniformScale.HasValue)
            world = Matrix.CreateScale(nonUniformScale.Value * scale);
        else
            world = Matrix.CreateScale(scale);

        _basicEffect.World = world * Matrix.CreateTranslation(x, y, z);
        _basicEffect.DiffuseColor = color.ToVector3();
        _basicEffect.Alpha = color.A / 255f;
        _basicEffect.LightingEnabled = true;
        GraphicsDevice.SetVertexBuffer(_cubeVertexBuffer);
        GraphicsDevice.Indices = _cubeIndexBuffer;
        foreach (var pass in _basicEffect.CurrentTechnique.Passes) { pass.Apply(); GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, 12); }
    }

    private void UpdateHPTooltips()
    {
        // Track all alive creatures
        var allCreatures = _combatManager.Combatants.Where(c => c.IsAlive()).ToList();
        if (_playerCreature != null && _playerCreature.IsAlive() && !allCreatures.Contains(_playerCreature))
            allCreatures.Add(_playerCreature);

        foreach (var creature in allCreatures)
        {
            if (_creatureHPTracker.TryGetValue(creature, out int lastHP))
            {
                if (creature.CurrentHP > lastHP)
                {
                    int diff = creature.CurrentHP - lastHP;
                    AddTooltip(creature, Loc.Tr("+{0} HP", diff), Color.LimeGreen);
                }
            }
            _creatureHPTracker[creature] = creature.CurrentHP;
        }

        // Cleanup tracker for creatures no longer present or dead
        var toRemove = _creatureHPTracker.Keys.Where(k => !allCreatures.Contains(k)).ToList();
        foreach (var k in toRemove) _creatureHPTracker.Remove(k);
    }

    private void DrawFloatingTooltips()
    {
        if (_font == null) return;

        foreach (var tooltip in _activeTooltips)
        {
            if (tooltip.Owner.Z > _currentViewLevel) continue;

            bool isVisible = _visionSystem.IsVisible(tooltip.Owner.X, tooltip.Owner.Y, tooltip.Owner.Z);
            if (_combatManager.InCombat && _showVisionOverlay && _playerCreature != null)
            {
                Color fogTint = _visionSystem.GetFogOfWarTint(tooltip.Owner.X, tooltip.Owner.Y, tooltip.Owner.Z, isVisible, _playerCreature);
                if (fogTint == Color.Black) continue;
            }

            var (capsuleRadius, _) = GetCreatureCapsuleDimensions(tooltip.Owner.Size);
            float uiAnchorZ = GetCreatureVisualTopZ(tooltip.Owner) + MathF.Max(0.15f, capsuleRadius * 0.4f);
            var offset = SizeHelper.GetCenterOffset(tooltip.Owner.Size);

            // Project world position to screen
            Vector3 worldPos = new Vector3(tooltip.Owner.VisualX + offset.X, tooltip.Owner.VisualY + offset.Y, uiAnchorZ);
            Vector3 screenPos = GraphicsDevice.Viewport.Project(worldPos, _basicEffect.Projection, _basicEffect.View, Matrix.Identity);

            if (screenPos.Z < 0 || screenPos.Z > 1) continue;

            // Floating animation: progress from 0 to 1
            float progress = (tooltip.MaxTime - tooltip.RemainingTime) / tooltip.MaxTime;
            float floatingY = progress * 40f; // Move up by 40 pixels over lifetime

            // Vertical position: above health bar (-20) and name (-40).
            // We start at -65 and add stack offset and floating.
            Vector2 basePos = new Vector2(screenPos.X, screenPos.Y - 65 - (tooltip.StackIndex * 30) - floatingY);

            float alpha = tooltip.GetAlpha();
            float scale = 0.7f;
            Vector2 textSize = _font.MeasureString(tooltip.Text) * scale;

            Rectangle bubbleRect = new Rectangle(
                (int)(basePos.X - textSize.X / 2 - 10),
                (int)(basePos.Y - textSize.Y / 2 - 6),
                (int)(textSize.X + 20),
                (int)(textSize.Y + 12)
            );

            // Don't draw if too high (overlapping with combat top panel)
            if (_showCombatUI && _combatManager.InCombat && bubbleRect.Y <= _combatTopPanelHeight)
                continue;

            // Draw bubble
            _spriteBatch.Draw(_pixel, bubbleRect, Color.Black * 0.75f * alpha);
            DrawBorder(_spriteBatch, _pixel, bubbleRect, tooltip.Color * 0.8f * alpha, 2);

            // Draw text
            Vector2 textPos = new Vector2(basePos.X - textSize.X / 2, basePos.Y - textSize.Y / 2);
            _spriteBatch.DrawString(_font, tooltip.Text, textPos, tooltip.Color * alpha, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }
    }

    private void Draw3DCreatureUI(Creature creature)
    {
        if (creature.Z > _currentViewLevel) return;
        if (_font == null) return;

        bool isVisible = _visionSystem.IsVisible(creature.X, creature.Y, creature.Z);
        if (_combatManager.InCombat && _showVisionOverlay && _playerCreature != null)
        {
            Color fogTint = _visionSystem.GetFogOfWarTint(creature.X, creature.Y, creature.Z, isVisible, _playerCreature);
            if (fogTint == Color.Black) return;
        }
        var (capsuleRadius, _) = GetCreatureCapsuleDimensions(creature.Size);
        float uiAnchorZ = GetCreatureVisualTopZ(creature) + MathF.Max(0.15f, capsuleRadius * 0.4f);
        var offset = SizeHelper.GetCenterOffset(creature.Size);

        // Use visual position for UI anchoring
        Vector3 screenPos = GraphicsDevice.Viewport.Project(new Vector3(creature.VisualX + offset.X, creature.VisualY + offset.Y, uiAnchorZ), _basicEffect.Projection, _basicEffect.View, Matrix.Identity);
        if (screenPos.Z < 0 || screenPos.Z > 1) return;
        Vector2 pos = new Vector2(screenPos.X, screenPos.Y);

        // Avoid overlap between creature labels and the tactical top HUD.
        if (_showCombatUI && _combatManager.InCombat && pos.Y <= _combatTopPanelHeight + 12)
            return;

        _spriteBatch.Draw(_pixel, new Rectangle((int)pos.X - 30, (int)pos.Y - 20, 60, 6), Color.DarkRed);
        _spriteBatch.Draw(_pixel, new Rectangle((int)pos.X - 30, (int)pos.Y - 20, (int)(60 * (float)creature.CurrentHP / creature.MaxHP), 6), Color.Green);
        string name = $"{creature.Name} [Z{creature.Z}]" + (creature.IsFlying ? " [" + Loc.Tr("Fly") + "]" : "");
        Vector2 size = _font.MeasureString(name) * 0.6f;
        _spriteBatch.Draw(_pixel, new Rectangle((int)pos.X - (int)size.X / 2 - 4, (int)pos.Y - 40, (int)size.X + 8, (int)size.Y + 4), Color.Black * 0.6f);
        _spriteBatch.DrawString(_font, name, new Vector2(pos.X - size.X / 2, pos.Y - 38), Color.White, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);

        if (creature.CurrentDonDoffProcess != null)
        {
            var proc = creature.CurrentDonDoffProcess;
            string status = proc.IsDoffing ? Loc.Tr("Doffing {0}", proc.Item?.Name ?? "Armor") : Loc.Tr("Donning {0}", proc.Item?.Name ?? "Armor");
            string time = _combatManager.InCombat
                ? Loc.Tr("{0} rounds", proc.RoundsRemaining)
                : Loc.Tr("{0} min", (int)Math.Ceiling(proc.MinutesRemaining));
            string progressText = $"{status} ({time})";

            Vector2 progSize = _font.MeasureString(progressText) * 0.5f;
            _spriteBatch.Draw(_pixel, new Rectangle((int)pos.X - (int)progSize.X / 2 - 4, (int)pos.Y - 60, (int)progSize.X + 8, (int)progSize.Y + 4), Color.Black * 0.7f);
            _spriteBatch.DrawString(_font, progressText, new Vector2(pos.X - progSize.X / 2, pos.Y - 58), Color.Orange, 0f, Vector2.Zero, 0.5f, SpriteEffects.None, 0f);
        }
    }

    private (int x, int y, int z)? GetHoveredTile()
    {
        var mouse = Mouse.GetState();
        Vector3 near = GraphicsDevice.Viewport.Unproject(new Vector3(mouse.X, mouse.Y, 0f), _basicEffect.Projection, _basicEffect.View, Matrix.Identity);
        Vector3 far = GraphicsDevice.Viewport.Unproject(new Vector3(mouse.X, mouse.Y, 1f), _basicEffect.Projection, _basicEffect.View, Matrix.Identity);
        Vector3 dir = Vector3.Normalize(far - near);
        Ray ray = new Ray(near, dir);

        bool canFly = _playerCreature?.CanFly == true;

        // Iterate from the current view level down to ground (z=0).
        // This implements "piercing" through empty space for non-flying units.
        for (int hz = _currentViewLevel; hz >= 0; hz--)
        {
            Plane plane = new Plane(Vector3.UnitZ, -hz);
            float? d = ray.Intersects(plane);
            if (!d.HasValue) continue;

            // Don't hover tiles that are excessively far (prevents hovering "the void" in dungeons/cities)
            if (d.Value > 100f) continue;

            int hx = (int)Math.Round(near.X + dir.X * d.Value);
            int hy = (int)Math.Round(near.Y + dir.Y * d.Value);
            var tileType = _tacticalMap.Get(hx, hy, hz);

            // Found a solid tile (including walls and doors).
            if (tileType != TileType.Empty)
            {
                return (hx, hy, hz);
            }

            // It's an Empty tile.
            // If the unit can fly, they can select the "air" at the current view level.
            if (hz == _currentViewLevel && canFly)
            {
                return (hx, hy, hz);
            }

            // Otherwise, continue piercing through to lower levels.
        }

        return null;
    }

}

public struct VertexPositionNormalColor : IVertexType
{
    public Vector3 Position;
    public Vector3 Normal;
    public Color Color;
    public static readonly VertexDeclaration VertexDeclaration = new VertexDeclaration(
        new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
        new VertexElement(12, VertexElementFormat.Vector3, VertexElementUsage.Normal, 0),
        new VertexElement(24, VertexElementFormat.Color, VertexElementUsage.Color, 0)
    );
    VertexDeclaration IVertexType.VertexDeclaration => VertexDeclaration;
    public VertexPositionNormalColor(Vector3 position, Vector3 normal, Color color) { Position = position; Normal = normal; Color = color; }
}
