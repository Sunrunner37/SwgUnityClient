using System;
using System.Collections.Generic;
using System.IO;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.World;
using Swg.Client.Core.World.SceneObjects;

namespace Swg.Client.Core.Networking.Game.Replication
{
    internal sealed class GroupSceneObjectUpdate : SceneObjectUpdate
    {
        public Action<List<GroupMember>> Members;
        internal override Type RequiredType => typeof(GroupSceneObject);
        public override SceneObjectChange Changes => base.Changes | (Members != null ? SceneObjectChange.Group : SceneObjectChange.None);
        internal override void ApplyTo(SceneObject sceneObject) => ((GroupSceneObject)sceneObject).Apply(this);

        public static GroupSceneObjectUpdate ReadBaseline3(ByteReader reader)
        {
            reader.Float(); reader.Ascii(); reader.U32(); reader.Ascii(); reader.Unicode(); reader.U32(); reader.Float();
            return new GroupSceneObjectUpdate();
        }
        static GroupMember ReadMember(ByteReader reader) => new GroupMember(reader.U64(), reader.Ascii());
        public static GroupSceneObjectUpdate ReadBaseline6(ByteReader reader)
        {
            reader.U32();
            int count = reader.Count();
            reader.U32();
            var members = new GroupMember[count];
            for (int i = 0; i < count; i++) members[i] = ReadMember(reader);
            ReplicationReader.SkipVector(reader, 12); // Member ship ID and index; Core3 currently sends an empty vector.
            reader.Ascii(); reader.U16(); reader.U32(); reader.U64(); reader.U32();
            return new GroupSceneObjectUpdate { Members = list => { list.Clear(); list.AddRange(members); } };
        }
        public static bool ReadDeltaField(ushort field, ByteReader reader, GroupSceneObjectUpdate update)
        {
            switch (field)
            {
                case 0: reader.U32(); break;
                case 1:
                    int count = reader.Count();
                    reader.U32();
                    var changes = new List<(byte Operation, ushort Index, GroupMember Member)>();
                    for (int i = 0; i < count; i++)
                    {
                        byte operation = reader.Byte();
                        ushort index = operation == 4 ? (ushort)0 : reader.U16();
                        if (operation != 0 && operation != 1 && operation != 2 && operation != 4)
                            throw new InvalidDataException("Unsupported group member operation " + operation);
                        changes.Add((operation, index, operation == 1 || operation == 2 ? ReadMember(reader) : default));
                    }
                    update.Members += list =>
                    {
                        foreach (var change in changes)
                            switch (change.Operation)
                            {
                                case 0: list.RemoveAt(change.Index); break;
                                case 1: list.Insert(change.Index, change.Member); break;
                                case 2: list[change.Index] = change.Member; break;
                                case 4: list.Clear(); break;
                            }
                    };
                    break;
                case 2: ReplicationReader.SkipVectorDelta(reader, 12); break;
                case 3: reader.Ascii(); break;
                case 4: reader.U16(); break;
                case 5: reader.U32(); break;
                case 6: reader.U64(); break;
                case 7: reader.U32(); break;
                default: return false;
            }
            return true;
        }
    }
}
