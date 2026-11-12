using System;
using System.Collections.Generic;

namespace Swg.Client.Core.Commands
{
    public sealed class PlayerCommands
    {
        readonly List<string> abilities = new List<string>();
        public IReadOnlyList<string> Abilities => abilities;

        public event Action Changed;
        internal void Apply(Action<List<string>> update)
        {
            update(abilities);
            Changed?.Invoke();
        }
    }
}
