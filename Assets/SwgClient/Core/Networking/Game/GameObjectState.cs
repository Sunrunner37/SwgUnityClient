using UnityEngine;
using System;

namespace Swg.Client.Core.Networking.Game
{
    public sealed class GameObjectState
    {
        public ulong Id;
        public ulong ParentId;
        public uint TemplateCrc;
        public string TemplatePath;
        public Vector3 Position;
        public Quaternion Rotation = Quaternion.identity;
        public uint Arrangement;
        public uint MovementCounter;
        public uint BaselineType;
        public bool BaselinesComplete;
        public string Name = "";
        public byte[] Customization = Array.Empty<byte>();
        public GameEquipment[] Equipment = Array.Empty<GameEquipment>();
        public string AlternateAppearance = "";
        public float Height = 1;
        public byte Posture;
        public long Revision;
        public bool IsCreature => BaselineType == 0x4352454f;
        public string Kind => BaselineType == 0 ? "" : new string (new[] { (char)(BaselineType >> 24), (char)(BaselineType >> 16 & 255), (char)(BaselineType >> 8 & 255), (char)(BaselineType & 255) });

        internal GameObjectState Copy()
        {
            var copy = (GameObjectState)MemberwiseClone();
            copy.Customization = (byte[])Customization.Clone();
            copy.Equipment = new GameEquipment[Equipment.Length];
            for (int i = 0; i < Equipment.Length; i++)
            {
                var e = Equipment[i];
                copy.Equipment[i] = new GameEquipment((byte[])e.Customization.Clone(), e.Arrangement, e.ObjectId, e.TemplateCrc);
            }

            return copy;
        }
    }
}
