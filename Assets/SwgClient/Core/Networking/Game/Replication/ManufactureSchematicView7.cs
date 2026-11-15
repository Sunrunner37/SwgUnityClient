using System;
using System.Collections.Generic;
using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.World.SceneObjects;
namespace Swg.Client.Core.Networking.Game.Replication
{
    /// <summary>Core3 MSCO7 contains ingredient vectors and server-calculated experimentation percentages.</summary>
    internal static class ManufactureSchematicView7
    {
        static string StringId(ByteReader reader) { string file = reader.Ascii(); reader.U32(); return "@" + file + ":" + reader.Ascii(); }
        static List<T> Vector<T>(ByteReader reader, Func<ByteReader, T> read)
        {
            int count = reader.Count();
            var result = new List<T>(count);
            for (int i = 0; i < count; i++) result.Add(read(reader));
            return result;
        }
        static Action<List<T>> List<T>(ByteReader reader, Func<ByteReader, T> read, bool baseline)
        {
            int count = reader.Count();
            reader.U32();
            var changes = new List<Action<List<T>>>();
            if (baseline) changes.Add(list => list.Clear());
            for (int i = 0; i < count; i++)
            {
                if (baseline) { var value = read(reader); changes.Add(list => list.Add(value)); continue; }
                byte operation = reader.Byte();
                if (operation == 3 || operation == 4) { changes.Add(list => list.Clear()); continue; }
                int index = reader.U16();
                if (operation == 0) { changes.Add(list => list.RemoveAt(index)); continue; }
                var item = read(reader);
                if (operation == 1) changes.Add(list => list.Insert(index, item));
                else if (operation == 2) changes.Add(list => list[index] = item);
                else throw new InvalidDataException("Unknown MSCO vector operation " + operation);
            }
            return list => { foreach (var change in changes) change(list); };
        }
        public static ManufactureSchematicSceneObjectUpdate ReadDeltaPacket(ByteReader reader)
        {
            // Core3 sendDelta7 writes four field IDs with insertShort rather than startUpdate.
            // Its declared count is therefore 1 although five fields follow. Parse this view's
            // known fields to the packet boundary so slot contents and its counter apply together.
            reader.U16();
            var update = new ManufactureSchematicSceneObjectUpdate();
            while (reader.Remaining > 0)
            {
                ushort field = reader.U16();
                if (!ReadDelta(field, reader, update)) throw new InvalidDataException("Unsupported MSCO7 field " + field);
            }
            return update;
        }
        public static ManufactureSchematicSceneObjectUpdate ReadBaseline(ByteReader reader)
        {
            var update = new ManufactureSchematicSceneObjectUpdate();
            for (ushort field = 0; field <= 20; field++) Read(field, reader, update, true);
            return update;
        }
        public static bool ReadDelta(ushort field, ByteReader reader, ManufactureSchematicSceneObjectUpdate update) => Read(field, reader, update, false);
        static bool Read(ushort field, ByteReader r, ManufactureSchematicSceneObjectUpdate update, bool baseline)
        {
            Action<ManufactureSchematicSceneObject> apply;
            switch (field)
            {
                case 0: List(r, StringId, baseline); return true;
                case 1: List(r, item => item.U32(), baseline); return true;
                case 2:
                    var ids = List(r, item => Vector(item, child => new SwgId(child.U64())), baseline);
                    apply = item => ids(item.IngredientSwgIds); break;
                case 3:
                    var quantities = List(r, item => Vector(item, child => (int)child.U32()), baseline);
                    apply = item => quantities(item.IngredientQuantities); break;
                case 4: List(r, item => item.Float(), baseline); return true;
                case 5: case 6: List(r, item => item.U32(), baseline); return true;
                case 7: r.Byte(); return true;
                case 8:
                    var names = List(r, StringId, baseline); apply = item => names(item.ExperimentNames); break;
                case 9:
                    var percentages = List(r, item => item.Float(), baseline); apply = item => percentages(item.ExperimentValues); break;
                case 10: case 11: List(r, item => item.Float(), baseline); return true;
                case 12:
                    var maxima = List(r, item => item.Float(), baseline); apply = item => maxima(item.ExperimentMaximums); break;
                case 13: case 19: List(r, item => item.Ascii(), baseline); return true;
                case 14: case 15: case 16: List(r, item => item.U32(), baseline); return true;
                case 17: case 20: r.Byte(); return true;
                case 18: float risk = r.Float(); apply = item => item.ExperimentRisk = risk; break;
                default: return false;
            }
            update.Crafting.Add(apply);
            return true;
        }
    }
}
