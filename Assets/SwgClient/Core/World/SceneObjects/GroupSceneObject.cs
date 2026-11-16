using System;
using System.Collections.Generic;
using Swg.Client.Core.Networking.Game.Replication;

namespace Swg.Client.Core.World.SceneObjects
{
    public readonly struct GroupMember
    {
        public SwgId SwgId { get; }
        public string Name { get; }
        public GroupMember(SwgId swgId, string name) { SwgId = swgId; Name = name; }
    }

    /// <summary>Server group roster. Core3 keeps the leader at member index zero, including after leadership changes.</summary>
    public sealed class GroupSceneObject : SceneObject
    {
        List<GroupMember> members = new List<GroupMember>();
        public IReadOnlyList<GroupMember> Members => members;
        public SwgId LeaderSwgId => members.Count > 0 ? members[0].SwgId : default;
        public bool Contains(SwgId swgId) => members.Exists(member => member.SwgId == swgId);
        internal void Apply(GroupSceneObjectUpdate update) { base.Apply(update); update.Members?.Invoke(members); }
        internal override SceneObject Copy()
        {
            var copy = (GroupSceneObject)base.Copy();
            copy.members = new List<GroupMember>(members);
            return copy;
        }
        internal override void CopyTo(SceneObject target)
        {
            base.CopyTo(target);
            if (target is GroupSceneObject group) group.members = new List<GroupMember>(members);
        }
    }
}
