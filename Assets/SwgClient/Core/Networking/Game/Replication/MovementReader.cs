using System;
using UnityEngine;
using Swg.Client.Core.Assets.Formats.Binary;

namespace Swg.Client.Core.Networking.Game.Replication
{
    internal static class MovementReader
    {
        public static MovementUpdate ReadControllerTransform(ByteReader reader, bool withParent)
        {
            uint movement = reader.U32();
            ulong parent = withParent ? reader.U64() : 0;
            var rotation = new Quaternion(reader.Float(), reader.Float(), reader.Float(), reader.Float());
            var position = new Vector3(reader.Float(), reader.Float(), reader.Float());
            reader.Float();
            return new MovementUpdate(position, rotation, parent, movement);
        }

        // Compressed game transforms encode a full turn as 100 heading units.
        public static Quaternion Heading(byte hundredths)
        {
            double half = hundredths * Math.PI / 100.0;
            return new Quaternion(0, (float)Math.Sin(half), 0, (float)Math.Cos(half));
        }
    }
}
