using System;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Swg.Client.Core.Networking.Game;
using Swg.Client.Core.Networking.Game.Packets.Chat;
using Swg.Client.Core.Networking.Game.Packets.Data;
using Swg.Client.Core.World;
using Swg.Client.Runtime.Localization;

namespace Swg.Client.Runtime.Networking.Chat
{
    public sealed class ClientChat : IDisposable
    {
        readonly GameConnection connection;
        readonly WorldState state;
        readonly ChatText text;
        readonly SynchronizationContext context;
        readonly Queue<(string Channel, string Text)> history = new Queue<(string, string)>();
        readonly Dictionary<uint, ChatRoomData> rooms = new Dictionary<uint, ChatRoomData>();
        readonly HashSet<uint> joined = new HashSet<uint>();
        readonly HashSet<uint> joining = new HashSet<uint>();
        readonly Dictionary<uint, Queue<string>> waiting = new Dictionary<uint, Queue<string>>();
        readonly Dictionary<uint, (string Recipient, string Text)> tells = new Dictionary<uint, (string, string)>();
        readonly ClientCommands commands;
        uint sequence;
        string replyTo;
        string retellTo;
        bool roomsRequested;
        bool disposed;
        public event Action<string, string> MessageReceived;
        public event Action<string> ScreenMessageReceived;
        public IEnumerable<(string Channel, string Text)> History => history;
        public IReadOnlyDictionary<uint, ChatRoomData> Rooms => rooms;

        public ClientChat(GameConnection connection, WorldState state, StringTableLocalizer localizer, ProseFormatter formatter, ClientCommands commands)
        {
            this.connection = connection;
            this.state = state;
            this.commands = commands;
            text = new ChatText(localizer, formatter, state);
            context = SynchronizationContext.Current ?? throw new InvalidOperationException("Create ClientChat on Unity's main thread.");
        }

        public void RequestRooms()
        {
            if (roomsRequested)
                return;
            roomsRequested = true;
            connection.Send(new ChatRequestRoomListPacket());
            JoinAuctionRoom();
        }

        public void SendSpatial(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return;
            commands.Enqueue("spatialChatInternal", "0 0 0 0 0 " + message, completed: result =>
            {
                if (result.ErrorType != 0)
                    Debug.LogWarning("Spatial chat failed (server error " + result.ErrorType + ":" + result.ErrorCode + ").");
            });
        }

        public void SendRoom(string channel, string message, out string error)
        {
            error = null;
            if (string.IsNullOrWhiteSpace(message))
                return;
            var room = rooms.Values.FirstOrDefault(r => Channel(r) == channel && (channel != "Planet" || CurrentPlanet(r)));
            if (room.Id == 0)
            {
                error = "This chat channel is not available.";
                return;
            }

            if (joined.Contains(room.Id))
                SendRoom(room.Id, message);
            else if (channel == "Planet" || channel == "Guildchat")
                error = "The server has not joined you to this channel.";
            else
            {
                if (!waiting.TryGetValue(room.Id, out var messages))
                    waiting.Add(room.Id, messages = new Queue<string>());
                messages.Enqueue(message);
                Join(room.Id);
            }
        }

        public void Tell(string recipient, string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return;
            uint id = ++sequence;
            tells.Add(id, (recipient, "To " + recipient + ": " + message));
            connection.Send(new ChatInstantMessageToCharacterPacket(connection.Admission.Galaxy.Name, recipient, message, id));
        }

        public void Reply(string message, out string error)
        {
            error = null;
            if (string.IsNullOrEmpty(replyTo))
                error = "No one has sent you a tell yet. Use /tell name message.";
            else
                Tell(replyTo, message);
        }

        public void Retell(string message, out string error)
        {
            error = null;
            if (string.IsNullOrEmpty(retellTo))
                error = "You have not sent a tell yet. Use /tell name message.";
            else
                Tell(retellTo, message);
        }

        void SendRoom(uint roomId, string message) => connection.Send(new ChatSendToRoomPacket(message, roomId, ++sequence));
        void Join(uint roomId)
        {
            if (!joined.Contains(roomId) && joining.Add(roomId))
                connection.Send(new ChatEnterRoomByIdPacket(++sequence, roomId));
        }

        bool CurrentPlanet(ChatRoomData room) => room.Path.EndsWith("." + Path.GetFileNameWithoutExtension(state.TerrainPath) + ".Planet", StringComparison.OrdinalIgnoreCase);
        void JoinAuctionRoom()
        {
            if (!roomsRequested)
                return;
            foreach (var room in rooms.Values)
                // Planet and guild membership are granted by Core3 during scene entry.
                if (Channel(room) == "Auction")
                    Join(room.Id);
        }

        internal static string Channel(ChatRoomData room)
        {
            string path = room.Path ?? "";
            if (path.EndsWith(".Planet", StringComparison.OrdinalIgnoreCase))
                return "Planet";
            if (path.EndsWith(".Auction", StringComparison.OrdinalIgnoreCase))
                return "Auction";
            if (path.IndexOf(".guild.", StringComparison.OrdinalIgnoreCase) >= 0)
                return "Guildchat";
            return "Spatial";
        }

        bool IsSelf(string name) => connection.Admission.Characters.Any(c => c.SwgId == state.PlayerSwgId && string.Equals(c.Name.Split(' ')[0], name, StringComparison.OrdinalIgnoreCase));
        void Post(Action apply) => context.Post(_ =>
        {
            if (!disposed)
                apply();
        }, null);
        void Add(string channel, string message)
        {
            history.Enqueue((channel, message));
            while (history.Count > 500)
                history.Dequeue();
            MessageReceived?.Invoke(channel, message);
        }

        public void Receive(SpatialChatPacket packet) => Post(() => Add("Spatial", text.Name(packet.SenderSwgId) + ": " + text.Format(packet.Message, packet.Parameters)));
        public void Receive(ChatSystemPacket packet) => Post(() =>
        {
            string message = text.Format(packet.Message, packet.Parameters);
            Add("Spatial", message);
            if (packet.ShowOnScreen) ScreenMessageReceived?.Invoke(message);
        });
        public void Receive(ChatRoomPacket packet) => Post(() =>
        {
            string channel = rooms.TryGetValue(packet.ChannelId, out var room) ? Channel(room) : "Spatial";
            Add(channel, "[" + channel + "] " + packet.Sender.Name + ": " + text.Format(packet.Message, default));
        });
        public void Receive(ChatInstantMessageToClientPacket packet) => Post(() =>
        {
            replyTo = packet.Sender.Name;
            Add("Instant Messages", "From " + replyTo + ": " + text.Format(packet.Message, default));
        });
        public void Receive(ChatRoomListPacket packet) => Post(() =>
        {
            foreach (var room in packet.Rooms)
                rooms[room.Id] = room;
            JoinAuctionRoom();
        });
        public void Receive(ChatOnCreateRoomPacket packet) => Post(() =>
        {
            if (packet.Error == 0)
                rooms[packet.Room.Id] = packet.Room;
        });
        public void Receive(ChatQueryRoomResultsPacket packet) => Post(() => rooms[packet.Room.Id] = packet.Room);
        public void Receive(ChatOnEnteredRoomPacket packet) => Post(() =>
        {
            if (!IsSelf(packet.Player.Name))
                return;
            joining.Remove(packet.RoomId);
            string channel = rooms.TryGetValue(packet.RoomId, out var room) ? Channel(room) : "Spatial";
            if (packet.Error != 0)
            {
                waiting.Remove(packet.RoomId);
                Add(channel, "Could not join channel (server error " + packet.Error + ").");
                return;
            }

            if (joined.Add(packet.RoomId))
                Add(channel, "You have joined " + (room.Path ?? channel) + ".");
            if (waiting.TryGetValue(packet.RoomId, out var messages))
            {
                waiting.Remove(packet.RoomId);
                while (messages.Count > 0)
                    SendRoom(packet.RoomId, messages.Dequeue());
            }
        });
        public void Receive(ChatOnLeaveRoomPacket packet) => Post(() =>
        {
            if (packet.Error == 0 && IsSelf(packet.Player.Name))
                joined.Remove(packet.RoomId);
        });
        public void Receive(ChatOnDestroyRoomPacket packet) => Post(() =>
        {
            if (packet.Error != 0)
                return;
            rooms.Remove(packet.RoomId);
            joined.Remove(packet.RoomId);
            joining.Remove(packet.RoomId);
            waiting.Remove(packet.RoomId);
        });
        public void Receive(ChatOnSendInstantPacket packet) => Post(() =>
        {
            if (!tells.TryGetValue(packet.Sequence, out var sent))
                return;
            tells.Remove(packet.Sequence);
            if (packet.Result == 0)
                retellTo = sent.Recipient;
            Add("Instant Messages", packet.Result == 0 ? sent.Text : "Tell was not delivered (server error " + packet.Result + ").");
        });
        public void Receive(ChatOnSendRoomPacket packet) => Post(() =>
        {
            if (packet.Result != 0)
                Add("Spatial", "Channel message was not delivered (server error " + packet.Result + ").");
        });
        public void Dispose() => disposed = true;
    }
}
