using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Swg.Client.Core.Assets.Formats.Iff;
using Swg.Client.Core.Assets.Formats.Binary;
using UnityEngine;

namespace Swg.Client.Core.Assets.Formats.Pob
{
    /// <summary>POB (portalized object) cells, optional floor meshes and exterior entrance polygons, in building-local coordinates.</summary>
    /// <seealso href="https://github.com/swgemu/Core3/blob/unstable/MMOCoreORB/src/templates/appearance/PortalLayout.cpp"/>
    public sealed class PortalLayout
    {
        public sealed class Cell
        {
            public int Number;
            public string Name;
            public string AppearancePath;
            public Floor Floor;
            public readonly List<RoomLight> Lights = new List<RoomLight>();
        }

        public enum LightType : byte { Ambient, Directional, Point }
        public sealed class RoomLight
        {
            public LightType Type;
            public Color Diffuse;
            public Color Specular;
            public Matrix4x4 Transform;
            public Vector3 Attenuation;
        }

        public sealed class Door
        {
            public int PortalIndex;
            public int CellNumber;
            public int TargetCellNumber;
            public bool Passable;
            public string Style;
            public Matrix4x4 Transform;
        }

        public sealed class Floor
        {
            public Vector3[] Vertices;
            public int[] Triangles;
        }

        public readonly List<Cell> Cells = new List<Cell>();
        public readonly List<Door> Doors = new List<Door>();
        public readonly List<Vector3[]> Entrances = new List<Vector3[]>();

        public static PortalLayout Parse(IffNode root, Func<string, byte[]> read)
        {
            var result = new PortalLayout();
            var version = root.Children.First(node => node.Tag == "FORM");
            var geometry = version.Children.Single(node => node.Kind == "PRTS").Children;
            var portals = new List<Vector3[]>();
            foreach (var node in geometry)
            {
                var bytes = node.Tag == "PRTL" ? node.Data : node.Children.Single().Chunk("VERT");
                var reader = new ByteReader(bytes);
                int count = node.Tag == "PRTL" ? checked((int)reader.U32()) : bytes.Length / 12;
                var vertices = new Vector3[count];
                for (int i = 0; i < count; i++)
                    vertices[i] = new Vector3(reader.Float(), reader.Float(), reader.Float());
                portals.Add(vertices);
            }

            foreach (var node in version.Children.Single(node => node.Kind == "CELS").Children.Where(node => node.Kind == "CELL"))
            {
                var cell = new Cell { Number = result.Cells.Count };
                result.Cells.Add(cell);
                var cellVersion = node.Children.First(child => child.Tag == "FORM");
                if (cellVersion.Kind != "0004" && cellVersion.Kind != "0005")
                {
                    Debug.LogWarning("Unsupported portal cell version " + cellVersion.Kind);
                    continue;
                }
                var reader = new ByteReader(cellVersion.Chunk("DATA"));
                reader.U32(); // Portal count; portal records are separate chunks.
                reader.Byte();
                cell.Name = reader.CString();
                cell.AppearancePath = reader.CString();
                if (reader.Byte() != 0)
                {
                    string path = reader.CString();
                    try
                    {
                        cell.Floor = ReadFloor(read(path));
                    }
                    catch (Exception error) when (error is IOException || error is InvalidOperationException || error is ArgumentException || error is OverflowException)
                    {
                        Debug.LogWarning("Portal cell " + cell.Number + " floor " + path + ": " + error.Message + "; keeping its visual appearance.");
                    }
                }
                var lights = cellVersion.Children.FirstOrDefault(child => child.Tag == "LGHT");
                if (lights != null)
                {
                    var lightReader = new ByteReader(lights.Data);
                    uint count = lightReader.U32();
                    Color ReadColor()
                    {
                        float alpha = lightReader.Float();
                        return new Color(lightReader.Float(), lightReader.Float(), lightReader.Float(), alpha);
                    }
                    for (int i = 0; i < count; i++)
                    {
                        var light = new RoomLight { Type = (LightType)lightReader.Byte(), Diffuse = ReadColor(), Specular = ReadColor(), Transform = Matrix4x4.identity };
                        for (int row = 0; row < 3; row++)
                            for (int column = 0; column < 4; column++) light.Transform[row, column] = lightReader.Float();
                        light.Attenuation = new Vector3(lightReader.Float(), lightReader.Float(), lightReader.Float());
                        // Native PortalPropertyTemplate corrects the Maya exporter's light orientation.
                        light.Transform *= Matrix4x4.Rotate(Quaternion.Euler(0, 180, 0));
                        cell.Lights.Add(light);
                    }
                }
                foreach (var portal in cellVersion.Children.Where(child => child.Kind == "PRTL"))
                {
                    var data = portal.Children.First();
                    int portalVersion = int.Parse(data.Tag);
                    var entry = new ByteReader(data.Data);
                    bool disabled = portalVersion >= 5 && entry.Byte() != 0;
                    bool passable = portalVersion == 1 || entry.Byte() != 0;
                    int index = checked((int)entry.U32());
                    bool clockwise = entry.Byte() != 0;
                    int target = checked((int)entry.U32());
                    if (cell.Number == 0 && passable && !disabled) result.Entrances.Add(portals[index]);
                    if (portalVersion < 3) continue;
                    string style = entry.CString();
                    bool hasHardpoint = false;
                    var transform = Matrix4x4.identity;
                    if (portalVersion >= 4)
                    {
                        hasHardpoint = entry.Byte() != 0;
                        for (int row = 0; row < 3; row++)
                            for (int column = 0; column < 4; column++) transform[row, column] = entry.Float();
                    }
                    if (disabled || string.IsNullOrEmpty(style) || result.Doors.Any(door => door.PortalIndex == index)) continue;
                    if (!hasHardpoint)
                    {
                        var polygon = portals[index];
                        var bounds = new Bounds(polygon[0], Vector3.zero);
                        foreach (var vertex in polygon) bounds.Encapsulate(vertex);
                        var normal = Vector3.Cross(polygon[1] - polygon[0], polygon[2] - polygon[0]).normalized * (clockwise ? 1 : -1);
                        transform = Matrix4x4.TRS(new Vector3(bounds.center.x, bounds.min.y, bounds.center.z), Quaternion.LookRotation(normal, Vector3.up), Vector3.one);
                    }
                    result.Doors.Add(new Door { PortalIndex = index, CellNumber = cell.Number, TargetCellNumber = target,
                        Passable = passable, Style = style, Transform = transform });
                }
            }
            return result;
        }

        static Floor ReadFloor(byte[] bytes)
        {
            var root = IffNode.Form(IffNode.Parse(bytes), "FLOR") ?? throw new InvalidDataException("Expected FLOR floor mesh");
            var version = root.Children.First(node => node.Tag == "FORM");
            if (version.Kind != "0003" && version.Kind != "0005" && version.Kind != "0006")
                throw new InvalidDataException("Unsupported floor version " + version.Kind);
            var reader = new ByteReader(version.Chunk("VERT"));
            int count = version.Kind == "0006" ? checked((int)reader.U32()) : reader.Remaining / 12;
            var vertices = new Vector3[count];
            for (int i = 0; i < count; i++)
                vertices[i] = new Vector3(reader.Float(), reader.Float(), reader.Float());
            reader = new ByteReader(version.Chunk("TRIS"));
            int triangleCount = version.Kind == "0006" ? checked((int)reader.U32()) : reader.Remaining / 60;
            var triangles = new List<int>();
            for (int i = 0; i < triangleCount; i++)
            {
                int a = checked((int)reader.U32());
                int b = checked((int)reader.U32());
                int c = checked((int)reader.U32());
                reader.Skip(31); // Triangle ID, neighbors, normal and edge flags.
                bool nonSolid = reader.Byte() != 0;
                reader.Skip(16); // Tag and portal IDs.
                if (!nonSolid)
                {
                    triangles.Add(a);
                    triangles.Add(b);
                    triangles.Add(c);
                }
            }
            return new Floor { Vertices = vertices, Triangles = triangles.ToArray() };
        }
    }
}
