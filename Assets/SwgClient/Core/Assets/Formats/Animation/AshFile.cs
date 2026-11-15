using System.Collections.Generic;
using System.Linq;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Assets.Formats.Iff;

namespace Swg.Client.Core.Assets.Formats.Animation
{
    /// <summary>ASH (Animation State Hierarchy) maps actions and idle loops to logical animations, inheriting mappings from parent states.</summary>
    public sealed class AshFile
    {
        public sealed class State
        {
            public string Name;
            public string Loop;
            public readonly Dictionary<string, string> Actions = new Dictionary<string, string>();
            public readonly List<State> Children = new List<State>();
            internal State(IffNode node)
            {
                var info = new ByteReader(node.Chunk("INFO"));
                Name = info.CString();
                Loop = info.CString();
                void ReadActions(IffNode entry)
                {
                    if (entry.Tag == "ACTN")
                    {
                        var reader = new ByteReader(entry.Data);
                        string action = reader.CString();
                        Actions[action] = reader.CString();
                    }
                    foreach (var child in entry.Children) ReadActions(child);
                }
                foreach (var actions in node.Children.Where(n => n.Kind == "ACTS")) ReadActions(actions);
                foreach (var child in node.Children.Where(n => n.Kind == "CHLD").SelectMany(n => n.Children).Where(n => n.Kind == "STAT"))
                    Children.Add(new State(child));
            }
        }
        public State Root { get; }
        AshFile(State root) => Root = root;
        public static AshFile Parse(byte[] bytes)
        {
            var root = IffNode.Form(IffNode.Parse(bytes), "ASHT");
            return new AshFile(new State(root.Children.Single(n => n.Tag == "FORM").Children.Single(n => n.Kind == "STAT")));
        }
        public string Resolve(string action, params string[] path)
        {
            var state = Root;
            string result = action == null ? state.Loop : state.Actions.TryGetValue(action, out var mapped) ? mapped : action;
            foreach (string part in path)
            {
                var next = state.Children.FirstOrDefault(n => n.Name == part);
                if (next == null) continue;
                state = next;
                if (action == null && state.Loop.Length > 0) result = state.Loop;
                else if (action != null && state.Actions.TryGetValue(action, out mapped)) result = mapped;
            }
            return result;
        }
    }
}
