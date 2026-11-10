using System;

namespace Swg.Client.Core.Networking.Game
{
    public readonly struct GameEquipment
    {
        public readonly byte[] Customization;
        public readonly uint Arrangement;
        public readonly ulong ObjectId;
        public readonly uint TemplateCrc;
        public GameEquipment(byte[] customization, uint arrangement, ulong objectId, uint templateCrc)
        {
            Customization = customization;
            Arrangement = arrangement;
            ObjectId = objectId;
            TemplateCrc = templateCrc;
        }
    }
}
