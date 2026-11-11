using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;

namespace Swg.Client.Core.Networking.Game.Packets.Data
{
    public readonly struct CombatDefender
    {
        public readonly ushort Index;
        public readonly ulong SwgId;
        public readonly byte Posture;
        public readonly byte Hit;
        public readonly byte Reserved;
        public readonly byte HitLocation;
        public readonly byte InitialDamage;
        internal CombatDefender(ByteReader reader, bool extended)
        {
            this = default;
            if (extended)
                Index = reader.U16();
            SwgId = reader.U64();
            Posture = reader.Byte();
            Hit = reader.Byte();
            Reserved = reader.Byte();
            if (extended)
            {
                HitLocation = reader.Byte();
                InitialDamage = reader.Byte();
            }
        }

        internal void WriteTo(BinaryWriter writer, bool extended)
        {
            if (extended)
                writer.Write(Index);
            writer.Write(SwgId);
            writer.Write(Posture);
            writer.Write(Hit);
            writer.Write(Reserved);
            if (extended)
            {
                writer.Write(HitLocation);
                writer.Write(InitialDamage);
            }
        }
    }
}
