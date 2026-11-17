using System;
using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Characters;

namespace Swg.Client.Core.Networking.Game.Replication
{
    // Core3 PlayerObjectMessage3 / PlayerObjectDeltaMessage3: character flags are four 32-bit words.
    internal static class PlayerView3
    {
        static uint ReadFlags(ByteReader reader)
        {
            if (reader.U32() != 4) throw new InvalidDataException("Expected four player flag words.");
            uint flags = reader.U32();
            reader.U32(); reader.U32(); reader.U32();
            return flags;
        }

        public static Action<CurrentPlayer> ReadBaseline(ByteReader reader)
        {
            IntangibleView3.ReadBaseline(reader);
            uint flags = ReadFlags(reader);
            ReadFlags(reader); // Profile settings.
            reader.Ascii(); // Skill title.
            for (int i = 0; i < 5; i++) reader.U32(); // Birth date, play time, and trailing constants.
            ReplicationReader.RequireEnd(reader);
            return player => player.IsAfk = (flags & (1u << 7)) != 0;
        }

        public static Action<CurrentPlayer> ReadDelta(ByteReader reader)
        {
            int count = reader.U16();
            uint? flags = null;
            var intangible = new IntangibleSceneObjectUpdate();
            for (int i = 0; i < count; i++)
            {
                ushort field = reader.U16();
                switch (field)
                {
                    case 5: flags = ReadFlags(reader); break;
                    case 6: ReadFlags(reader); break;
                    case 7: reader.Ascii(); break;
                    case 8: case 9: reader.U32(); break;
                    default:
                        if (!IntangibleView3.ReadDeltaField(field, reader, intangible))
                            throw new InvalidDataException("Unsupported PLAY3 field " + field);
                        break;
                }
            }
            ReplicationReader.RequireEnd(reader);
            return player => { if (flags.HasValue) player.IsAfk = (flags.Value & (1u << 7)) != 0; };
        }
    }
}
