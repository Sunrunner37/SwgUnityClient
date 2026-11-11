using Swg.Client.Core;
using UnityEngine;

namespace Swg.Client.Core.World
{
    public sealed class SnapshotEntry
    {
        public SwgId SwgId { get; internal set; }
        public SwgId ParentSwgId { get; internal set; }
        public uint CellSwgId { get; internal set; }
        public string TemplatePath { get; internal set; }
        public Vector3 Position { get; internal set; }
        public Quaternion Rotation { get; internal set; }
        public Vector3 WorldPosition { get; internal set; }
        public Quaternion WorldRotation { get; internal set; }
        // Preserved native fields: Core3 labels the first gameObjectType; do not treat it as a mesh radius.
        public float NativeValue { get; internal set; }
        public uint Reserved { get; internal set; }
    }
}
