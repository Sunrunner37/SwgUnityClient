using System;
using UnityEngine;
using System.Collections.Generic;
using Swg.Client.Core.Commands;
using Swg.Client.Runtime.Networking;
using Swg.Client.Runtime.Networking.Chat;

namespace Swg.Client.Runtime.Commands
{
    public sealed class CommandDispatcher : ICommandDispatcher
    {
        readonly CommandCatalog catalog;
        readonly ClientChat chat;
        readonly ClientCommands commands;
        static readonly Dictionary<string, string> aliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["t"] = "tell",
            ["r"] = "reply",
            ["rtel"] = "reply",
            ["retell"] = "rtell",
            ["guild"] = "guildchat",
            ["gu"] = "guildchat",
            ["g"] = "guildchat"
        };

        public static string CanonicalName(string name) => aliases.TryGetValue(name, out var canonical) ? canonical : name;

        public CommandDispatcher(CommandCatalog catalog, ClientChat chat, ClientCommands commands)
        {
            this.catalog = catalog;
            this.chat = chat;
            this.commands = commands;
        }

        public void Submit(string channel, string text, out string error)
        {
            error = null;
            var input = SlashCommandParser.Parse(text);
            if (input.IsCommand)
                Execute(input.Name, input.Arguments, out error);
            else if (input.Arguments.Length > 0)
            {
                if (channel == "Spatial")
                    chat.SendSpatial(input.Arguments);
                else if (channel == "Instant Messages")
                    chat.Reply(input.Arguments, out error);
                else
                    chat.SendRoom(channel, input.Arguments, out error);
            }
        }

        public void Execute(string name, string arguments, out string error)
        {
            error = null;
            name = CanonicalName(name);
            arguments = (arguments ?? "").Trim();
            switch (name.ToLowerInvariant())
            {
                case "say": chat.SendSpatial(arguments); return;
                case "planet": chat.SendRoom("Planet", arguments, out error); return;
                case "auction": chat.SendRoom("Auction", arguments, out error); return;
                case "guildchat": chat.SendRoom("Guildchat", arguments, out error); return;
                case "tell":
                    int separator = 0;
                    while (separator < arguments.Length && !char.IsWhiteSpace(arguments[separator]))
                        separator++;
                    string message = arguments.Substring(separator).Trim();
                    if (separator == 0 || message.Length == 0)
                        error = "Usage: /tell name message";
                    else
                        chat.Tell(arguments.Substring(0, separator), message);
                    return;
                case "reply": chat.Reply(arguments, out error); return;
                case "rtell": chat.Retell(arguments, out error); return;
            }
            if (!catalog.TryResolve(name, arguments, out var command))
            {
                error = "Unknown or unavailable command: /" + name;
                return;
            }
            // These posture hooks use the corresponding Core3 queue commands.
            // Other client hooks need their local behavior implemented before dispatch.
            switch (command.ClientHook)
            {
                case "":
                case "stand":
                case "kneel":
                case "prone":
                    break;
                default:
                    error = "Command is not supported yet: /" + command.Name;
                    return;
            }
            commands.Enqueue(command.Name, arguments, completed: result =>
            {
                if (result.ErrorType != 0)
                    Debug.LogWarning(command.Label + " failed (server error " + result.ErrorType + ":" + result.ErrorCode + ").");
            });
        }
    }
}

