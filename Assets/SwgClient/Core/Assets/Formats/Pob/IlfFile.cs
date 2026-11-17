using System.IO;
using System.Linq;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Assets.Formats.Iff;
using UnityEngine;

namespace Swg.Client.Core.Assets.Formats.Pob
{
    /// <summary>ILF (interior layout file) places client-only furnishings by template, named portal cell, and local transform.</summary>
    /// <seealso href="https://github.com/SWG-Source/client-tools/blob/master/src/engine/shared/library/sharedUtility/src/shared/InteriorLayoutReaderWriter.cpp"/>
    public sealed class IlfFile
    {
        public readonly struct Entry
        {
            public readonly string Template;
            public readonly string Cell;
            public readonly Matrix4x4 Transform;
            internal Entry(string template, string cell, Matrix4x4 transform) { Template = template; Cell = cell; Transform = transform; }
        }
        public Entry[] Entries { get; }
        IlfFile(Entry[] entries) => Entries = entries;
        public static IlfFile Parse(byte[] bytes)
        {
            var root = IffNode.Form(IffNode.Parse(bytes), "INLY") ?? throw new InvalidDataException("Expected INLY interior layout.");
            var version = root.Children.First(node => node.Tag == "FORM");
            if (version.Kind != "0000") throw new InvalidDataException("Unsupported interior layout " + version.Kind);
            return new IlfFile(version.Children.Where(node => node.Tag == "NODE").Select(node =>
            {
                var reader = new ByteReader(node.Data);
                string template = reader.CString();
                string cell = reader.CString();
                var transform = Matrix4x4.identity;
                for (int row = 0; row < 3; row++)
                    for (int column = 0; column < 4; column++) transform[row, column] = reader.Float();
                return new Entry(template, cell, transform);
            }).ToArray());
        }
    }
}
