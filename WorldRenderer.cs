using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace _4DND;

public class WorldRenderer
{
    private GraphicsDevice _graphicsDevice;
    private BasicEffect _basicEffect;

    private VertexBuffer _cubeVertexBuffer;
    private IndexBuffer _cubeIndexBuffer;
    private VertexBuffer _capsuleVertexBuffer;
    private IndexBuffer _capsuleIndexBuffer;
    private int _capsulePrimitiveCount;
    private VertexBuffer _tileVertexBuffer;
    private IndexBuffer _tileIndexBuffer;

    public WorldRenderer(GraphicsDevice graphicsDevice)
    {
        _graphicsDevice = graphicsDevice;
        Initialize3DRendering();
    }

    public void UpdateCamera(Vector3 target, float yaw, float pitch, float distance)
    {
        float aspectRatio = _graphicsDevice.Viewport.AspectRatio;
        _basicEffect.Projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(45f), aspectRatio, 0.1f, 1000f);
        Vector3 direction = new Vector3(0, distance, 0);
        Matrix rotation = Matrix.CreateRotationX(pitch) * Matrix.CreateRotationZ(yaw);
        _basicEffect.View = Matrix.CreateLookAt(target + Vector3.Transform(direction, rotation), target, Vector3.UnitZ);
    }

    private void Initialize3DRendering()
    {
        _basicEffect = new BasicEffect(_graphicsDevice) { VertexColorEnabled = true, LightingEnabled = true };
        _basicEffect.EnableDefaultLighting();

        var cubeVertices = new VertexPositionNormalColor[] {
            new(new(-0.5f,-0.5f,0), Vector3.Down, Color.White), new(new(0.5f,-0.5f,0), Vector3.Down, Color.White), new(new(0.5f,0.5f,0), Vector3.Down, Color.White), new(new(-0.5f,0.5f,0), Vector3.Down, Color.White),
            new(new(-0.5f,-0.5f,1), Vector3.Up, Color.White), new(new(0.5f,-0.5f,1), Vector3.Up, Color.White), new(new(0.5f,0.5f,1), Vector3.Up, Color.White), new(new(-0.5f,0.5f,1), Vector3.Up, Color.White),
            new(new(-0.5f,-0.5f,0), Vector3.Left, Color.White), new(new(-0.5f,0.5f,0), Vector3.Left, Color.White), new(new(-0.5f,0.5f,1), Vector3.Left, Color.White), new(new(-0.5f,-0.5f,1), Vector3.Left, Color.White),
            new(new(0.5f,-0.5f,0), Vector3.Right, Color.White), new(new(0.5f,-0.5f,1), Vector3.Right, Color.White), new(new(0.5f,0.5f,1), Vector3.Right, Color.White), new(new(0.5f,0.5f,0), Vector3.Right, Color.White),
            new(new(-0.5f,-0.5f,0), Vector3.Forward, Color.White), new(new(0.5f,-0.5f,0), Vector3.Forward, Color.White), new(new(0.5f,-0.5f,1), Vector3.Forward, Color.White), new(new(-0.5f,-0.5f,1), Vector3.Forward, Color.White),
            new(new(-0.5f,0.5f,0), Vector3.Backward, Color.White), new(new(-0.5f,0.5f,1), Vector3.Backward, Color.White), new(new(0.5f,0.5f,1), Vector3.Backward, Color.White), new(new(0.5f,0.5f,0), Vector3.Backward, Color.White)
        };
        _cubeVertexBuffer = new VertexBuffer(_graphicsDevice, typeof(VertexPositionNormalColor), cubeVertices.Length, BufferUsage.WriteOnly);
        _cubeVertexBuffer.SetData(cubeVertices);
        var cubeIndices = new short[] { 0,1,2,0,2,3, 4,6,5,4,7,6, 8,9,10,8,10,11, 12,13,14,12,14,15, 16,17,18,16,18,19, 20,21,22,20,22,23 };
        _cubeIndexBuffer = new IndexBuffer(_graphicsDevice, IndexElementSize.SixteenBits, cubeIndices.Length, BufferUsage.WriteOnly);
        _cubeIndexBuffer.SetData(cubeIndices);

        var tileVertices = new VertexPositionNormalColor[] {
            new(new(-0.5f,-0.5f,0), Vector3.Up, Color.White), new(new(0.5f,-0.5f,0), Vector3.Up, Color.White), new(new(0.5f,0.5f,0), Vector3.Up, Color.White), new(new(-0.5f,0.5f,0), Vector3.Up, Color.White)
        };
        _tileVertexBuffer = new VertexBuffer(_graphicsDevice, typeof(VertexPositionNormalColor), tileVertices.Length, BufferUsage.WriteOnly);
        _tileVertexBuffer.SetData(tileVertices);
        var tileIndices = new short[] { 0, 1, 2, 0, 2, 3 };
        _tileIndexBuffer = new IndexBuffer(_graphicsDevice, IndexElementSize.SixteenBits, tileIndices.Length, BufferUsage.WriteOnly);
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

        _capsuleVertexBuffer = new VertexBuffer(_graphicsDevice, typeof(VertexPositionNormalColor), capsuleVertices.Count, BufferUsage.WriteOnly);
        _capsuleVertexBuffer.SetData(capsuleVertices.ToArray());
        _capsuleIndexBuffer = new IndexBuffer(_graphicsDevice, IndexElementSize.SixteenBits, capsuleIndices.Count, BufferUsage.WriteOnly);
        _capsuleIndexBuffer.SetData(capsuleIndices.ToArray());
        _capsulePrimitiveCount = capsuleIndices.Count / 3;
    }

    public void Draw3DGrid(InfiniteGrid3D<TileType> grid, int zLevel, bool showVisionOverlay, VisionSystem visionSystem, Creature? playerCreature)
    {
        _basicEffect.World = Matrix.Identity;
        _basicEffect.LightingEnabled = true;
        foreach (var cell in grid.EnumerateNonEmpty())
        {
            int cx = cell.Key.x, cy = cell.Key.y, cz = cell.Key.z;
            if (cz > zLevel || cell.Value == TileType.Empty) continue;

            if (cell.Value == TileType.Grass && (cz != 0 || zLevel > 0)) continue;

            Color baseColor = cell.Value switch
            {
                TileType.Floor => Color.ForestGreen,
                TileType.Grass => new Color(80, 180, 80),
                TileType.DifficultTerrain => new Color(139, 69, 19),
                TileType.Wall => Color.Gray,
                TileType.Water => Color.CornflowerBlue,
                _ => Color.ForestGreen
            };

            if (cz < zLevel) baseColor *= 0.3f;
            if (showVisionOverlay && playerCreature != null)
            {
                bool isVisible = visionSystem.IsVisible(cx, cy, cz);
                Color tint = visionSystem.GetFogOfWarTint(cx, cy, cz, isVisible, playerCreature);
                if (tint == Color.Black) continue;
                baseColor = new Color((byte)(baseColor.R * tint.R / 255), (byte)(baseColor.G * tint.G / 255), (byte)(baseColor.B * tint.B / 255), (byte)(baseColor.A * tint.A / 255));
            }

            if (cell.Value == TileType.Wall)
            {
                Draw3DCube(cx, cy, cz, 1.0f, baseColor);
            }
            else
            {
                Draw3DTile(cx, cy, cz, baseColor);
                if (cell.Value == TileType.DifficultTerrain)
                {
                    Draw3DLine(new Vector3(cx - 0.2f, cy - 0.2f, cz + 0.01f), new Vector3(cx + 0.2f, cy + 0.2f, cz + 0.01f), Color.Black * 0.5f);
                    Draw3DLine(new Vector3(cx - 0.2f, cy + 0.2f, cz + 0.01f), new Vector3(cx + 0.2f, cy - 0.2f, cz + 0.01f), Color.Black * 0.5f);
                }
            }
        }
    }

    public void Draw3DLine(Vector3 start, Vector3 end, Color color)
    {
        var vertices = new[] { new VertexPositionColor(start, color), new VertexPositionColor(end, color) };
        foreach (var pass in _basicEffect.CurrentTechnique.Passes) { pass.Apply(); _graphicsDevice.DrawUserPrimitives(PrimitiveType.LineList, vertices, 0, 1); }
    }

    public void Draw3DTile(int x, int y, int z, Color color)
    {
        _basicEffect.World = Matrix.CreateTranslation(x, y, z);
        _basicEffect.DiffuseColor = color.ToVector3();
        _basicEffect.Alpha = color.A / 255f;
        _graphicsDevice.SetVertexBuffer(_tileVertexBuffer);
        _graphicsDevice.Indices = _tileIndexBuffer;
        foreach (var pass in _basicEffect.CurrentTechnique.Passes) { pass.Apply(); _graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, 2); }
    }

    public void Draw3DCreature(Creature creature, int currentViewLevel, bool inCombat, bool showVisionOverlay, VisionSystem visionSystem, Creature? playerCreature)
    {
        if (creature.Z > currentViewLevel) return;
        if (inCombat && showVisionOverlay && !visionSystem.IsVisible(creature.X, creature.Y, creature.Z)) return;
        Color color = creature.DisplayColor;
        if (showVisionOverlay && playerCreature != null) { Color tint = visionSystem.GetFogOfWarTint(creature.X, creature.Y, creature.Z, true, playerCreature); color = new Color((byte)(color.R * tint.R / 255), (byte)(color.G * tint.G / 255), (byte)(color.B * tint.B / 255)); }
        var (capsuleRadius, capsuleHeight) = GetCreatureCapsuleDimensions(creature.Size);
        Draw3DCapsule(creature.VisualX, creature.VisualY, creature.VisualZ, capsuleRadius, capsuleHeight, color);
        if (creature.VisualZ > 0) { Draw3DTile(creature.X, creature.Y, 0, Color.Black * 0.3f); Draw3DLine(new Vector3(creature.VisualX, creature.VisualY, creature.VisualZ), new Vector3(creature.VisualX, creature.VisualY, 0), Color.Gray * 0.3f); }
    }

    public void Draw3DTileOutline(int x, int y, int z, Color color)
    {
        const float halfTile = 0.5f;
        const float elevation = 0.03f;
        float zPos = z + elevation;

        Vector3 topLeft = new Vector3(x - halfTile, y - halfTile, zPos);
        Vector3 topRight = new Vector3(x + halfTile, y - halfTile, zPos);
        Vector3 bottomRight = new Vector3(x + halfTile, y + halfTile, zPos);
        Vector3 bottomLeft = new Vector3(x - halfTile, y + halfTile, zPos);

        Draw3DLine(topLeft, topRight, color);
        Draw3DLine(topRight, bottomRight, color);
        Draw3DLine(bottomRight, bottomLeft, color);
        Draw3DLine(bottomLeft, topLeft, color);
    }

    private void Draw3DCapsule(float x, float y, float z, float radius, float height, Color color)
    {
        float capsuleTotalHeight = height + (radius * 2f);
        Vector3 scale = new Vector3(radius / 0.5f, radius / 0.5f, capsuleTotalHeight / 2.0f);
        _basicEffect.World = Matrix.CreateScale(scale) * Matrix.CreateTranslation(x, y, z);
        _basicEffect.DiffuseColor = color.ToVector3();
        _basicEffect.Alpha = color.A / 255f;
        _basicEffect.LightingEnabled = true;
        _graphicsDevice.SetVertexBuffer(_capsuleVertexBuffer);
        _graphicsDevice.Indices = _capsuleIndexBuffer;
        foreach (var pass in _basicEffect.CurrentTechnique.Passes) { pass.Apply(); _graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, _capsulePrimitiveCount); }
    }

    public void Draw3DCube(float x, float y, float z, float scale, Color color)
    {
        _basicEffect.World = Matrix.CreateScale(scale) * Matrix.CreateTranslation(x, y, z);
        _basicEffect.DiffuseColor = color.ToVector3();
        _basicEffect.Alpha = 1.0f;
        _basicEffect.LightingEnabled = true;
        _graphicsDevice.SetVertexBuffer(_cubeVertexBuffer);
        _graphicsDevice.Indices = _cubeIndexBuffer;
        foreach (var pass in _basicEffect.CurrentTechnique.Passes) { pass.Apply(); _graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, 12); }
    }

    public static (float Radius, float Height) GetCreatureCapsuleDimensions(CreatureSize size)
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

    public static float GetCreatureVisualTopZ(Creature creature)
    {
        var (capsuleRadius, capsuleHeight) = GetCreatureCapsuleDimensions(creature.Size);
        return creature.Z + capsuleHeight + (capsuleRadius * 2f);
    }

    public BasicEffect Effect => _basicEffect;
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
