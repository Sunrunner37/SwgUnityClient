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
        }

        public sealed class Floor
        {
            public Vector3[] Vertices;
            public int[] Triangles;
        }

        public readonly List<Cell> Cells = new List<Cell>();
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
                if (cell.Number != 0)
                    continue;
                foreach (var portal in cellVersion.Children.Where(child => child.Kind == "PRTL"))
                {
                    var data = portal.Children.First();
                    var entry = new ByteReader(data.Data);
                    if (data.Tag == "0005") entry.Byte();
                    bool solid = entry.Byte() != 0;
                    int index = checked((int)entry.U32());
                    if (solid) result.Entrances.Add(portals[index]);
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
