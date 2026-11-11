using Swg.Client.Core;
using Swg.Client.Core.World;
using System;

namespace Swg.Client.Core.World
{
    public readonly struct WorldEquipment
    {
        public readonly byte[] Customization;
        public readonly uint Arrangement;
        public readonly SwgId SwgId;
        public readonly uint TemplateCrc;
        public WorldEquipment(byte[] customization, uint arrangement, SwgId swgId, uint templateCrc)
        {
            Customization = customization;
            Arrangement = arrangement;
            SwgId = swgId;
            TemplateCrc = templateCrc;
        }
    }
}
