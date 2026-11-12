using UnityEngine;
using Swg.Client.Core.Commands;
using Swg.Client.Runtime.Commands;

namespace Swg.Client.Runtime.UI.Testing
{
    public sealed class OfflineCommandDispatcher : ICommandDispatcher
    {
        public void Submit(string channel, string text, out string error)
        {
            error = null;
            var input = SlashCommandParser.Parse(text);
            if (input.IsCommand)
                Execute(input.Name, input.Arguments, out error);
        }

        public void Execute(string name, string arguments, out string error)
        {
            error = null;
            Debug.Log("Offline command: /" + name + " " + arguments);
        }
    }
}
