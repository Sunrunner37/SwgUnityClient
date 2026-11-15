using System.Collections;
using System.Collections.Generic;
using Swg.Client.Runtime.World.SceneObjects;
using System.IO;
using Swg.Client.Core.Networking.Game.Packets.Data;
using Swg.Client.Core.World.SceneObjects;
using Swg.Client.Runtime.Localization;
using UnityEngine;
using UnityEngine.TestTools;
using Swg.Client.Runtime.UI.Testing;
using NUnit.Framework;
using Swg.Client.Core.Commands;
using Swg.Client.Runtime.Commands;
using Swg.Client.Core.World;
using Swg.Client.Runtime.Networking;
using Swg.Client.Runtime.Networking.Chat;
using System.Linq;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Networking;
using Swg.Client.Core.Networking.Game.Packets;
using Swg.Client.Core.Networking.Game.Packets.Chat;
using Swg.Client.Core.Networking.Game.Packets.Commands;

namespace Swg.Client.Tests.Editor
{
    public sealed class ChatChecks
    {
        static ByteReader Encode<T>(T packet) where T : struct, IPacket
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
            packet.WriteTo(writer);
            return new ByteReader(stream.ToArray());
        }

        [Test]
        public void ScreenMessagesKeepTheirOrderAndExpireIndependently()
        {
            var parent = new GameObject("Inactive screen-message test");
            parent.SetActive(false);
            try
            {
                var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/Hud/SystemMessageHud.prefab");
                var hud = UnityEngine.Object.Instantiate(prefab, parent.transform).GetComponent<Swg.Client.Runtime.UI.Hud.SystemMessageHud>();
                Assert.That(hud.label.richText, Is.True, "Per-line opacity uses TMP alpha tags.");
                Assert.That(hud.label.verticalAlignment, Is.EqualTo(TMPro.VerticalAlignmentOptions.Bottom));
                var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
                var messages = (System.Collections.IList)hud.GetType().GetField("messages", flags).GetValue(hud);
                messages.Add(("Practice complete", Time.unscaledTime + 10));
                messages.Add(("You gain 105 experience", Time.unscaledTime + 15));
                var display = (IEnumerator)hud.GetType().GetMethod("Display", flags).Invoke(hud, null);
                Assert.That(display.MoveNext(), Is.True);
                Assert.That(hud.label.text.IndexOf("Practice complete"), Is.LessThan(hud.label.text.IndexOf("You gain 105 experience")));
                messages[0] = ("Practice complete", Time.unscaledTime - 1);
                display.MoveNext();
                Assert.That(hud.label.text, Does.Not.Contain("Practice complete").And.Contain("You gain 105 experience"));
                messages[0] = ("You gain 105 experience", Time.unscaledTime - 1);
                display.MoveNext();
                Assert.That(display.MoveNext(), Is.False);
                Assert.That(hud.label.enabled, Is.False);
            }
            finally { UnityEngine.Object.DestroyImmediate(parent); }
        }
        [Test]
        public async System.Threading.Tasks.Task PracticeExperienceProseReachesChatAndScreen()
        {
            var settings = Swg.Client.Core.Configuration.ClientSettings.From(Path.Combine(Directory.GetParent(Application.dataPath).FullName, "client.ini"));
            using var assets = new Swg.Client.Runtime.Assets.Tre.TreRuntimeAssets(settings);
            await assets.Ready;
            var state = new WorldState(new SceneObjectRegistry());
            using var commands = new ClientCommands(_ => { }, _ => { }, state);
            var localizer = new StringTableLocalizer(assets);
            using var chat = new ClientChat(null, state, localizer, new ProseFormatter(), commands);
            var received = new System.Threading.Tasks.TaskCompletionSource<string>();
            chat.ScreenMessageReceived += message => received.TrySetResult(message);
            using var body = new MemoryStream();
            using var prose = new BinaryWriter(body);
            prose.Write((ushort)0); prose.Write((byte)1); prose.Write(uint.MaxValue);
            void StringId(string file, string key) { PacketFieldCodec.Ascii(prose, file); prose.Write(0u); PacketFieldCodec.Ascii(prose, key); }
            StringId("base_player", "prose_grant_xp");
            for (int i = 0; i < 3; i++)
            {
                prose.Write(0ul); StringId(i == 2 ? "exp_n" : "", i == 2 ? "crafting_general" : ""); PacketFieldCodec.Unicode(prose, "");
            }
            prose.Write(105); prose.Write(0f); prose.Write((byte)0);
            if ((body.Length & 1) != 0) { prose.Write((byte)0); body.Position = 0; prose.Write((ushort)1); }
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
            writer.Write((ushort)4); writer.Write(ChatSystemPacket.Opcode); writer.Write(ChatSystemPacket.DisplayChatAndScreen);
            PacketFieldCodec.Unicode(writer, ""); writer.Write((uint)(body.Length / 2)); writer.Write(body.ToArray());
            chat.Receive(ChatSystemPacket.ReadFrom(new ByteReader(stream.ToArray())));
            var completed = await System.Threading.Tasks.Task.WhenAny(received.Task, System.Threading.Tasks.Task.Delay(5000));
            Assert.That(completed, Is.SameAs(received.Task));
            string message = await received.Task;
            Assert.That(message, Does.Contain("105").And.Contain(localizer.Lookup("exp_n", "crafting_general")));
            Assert.That(message, Does.Not.Contain("%DI").And.Not.Contain("%TO"));
            Assert.That(chat.History.Single().Text, Is.EqualTo(message));
        }
        [UnityTest]
        public IEnumerator SystemMessagesRespectServerScreenDisplayFlag()
        {
            var state = new WorldState(new SceneObjectRegistry());
            using var commands = new ClientCommands(_ => { }, _ => { }, state);
            using var chat = new ClientChat(null, state, new StringTableLocalizer(null), new ProseFormatter(), commands);
            var messages = new List<string>();
            chat.ScreenMessageReceived += messages.Add;
            ChatSystemPacket Packet(byte display, string message)
            {
                using var stream = new MemoryStream();
                using var writer = new BinaryWriter(stream);
                writer.Write((ushort)4); writer.Write(ChatSystemPacket.Opcode); writer.Write(display);
                PacketFieldCodec.Unicode(writer, message); writer.Write(0u);
                return ChatSystemPacket.ReadFrom(new ByteReader(stream.ToArray()));
            }
            chat.Receive(Packet(ChatSystemPacket.DisplayChatAndScreen, "Generating vehicle in 15 seconds."));
            chat.Receive(Packet(ChatSystemPacket.DisplayChatOnly, "Chat only"));
            yield return null;
            Assert.That(messages, Is.EqualTo(new[] { "Generating vehicle in 15 seconds." }));
            Assert.That(chat.History.Select(message => message.Text), Is.EqualTo(new[] { "Generating vehicle in 15 seconds.", "Chat only" }));
        }

        [TestCase(" hello ", null, "hello")]
        [TestCase("/t Misty   hello ", "t", "Misty   hello")]
        [TestCase("/R hello", "R", "hello")]
        [TestCase("/attack", "attack", "")]
        [TestCase("/flourish\t1", "flourish", "1")]
        [TestCase("  ", null, "")]
        [TestCase("/", "", "")]
        public void SlashInputOnlySeparatesNameAndArguments(string text, string name, string arguments)
        {
            var input = SlashCommandParser.Parse(text);
            Assert.That(input.Name, Is.EqualTo(name));
            Assert.That(input.Arguments, Is.EqualTo(arguments));
        }

        [TestCase("r", "reply")]
        [TestCase("rtel", "reply")]
        [TestCase("reply", "reply")]
        [TestCase("R", "reply")]
        [TestCase("retell", "rtell")]
        [TestCase("rtell", "rtell")]
        [TestCase("t", "tell")]
        [TestCase("gu", "guildchat")]
        public void AliasesUseTheSameCanonicalCommand(string alias, string name)
            => Assert.That(CommandDispatcher.CanonicalName(alias), Is.EqualTo(name));

        [Test]
        public void DispatchResolutionUsesCatalogGrantsAndVariantArguments()
        {
            var player = new PlayerCommands();
            player.Apply(list => list.Add("flourish+1"));
            using var catalog = new CommandCatalog(new[]
            {
                new CommandCatalog.Command { Name = "flourish", Arguments = "", Label = "Flourish", Ability = "flourish" }
            }, player);
            Assert.That(catalog.TryResolve("FLOURISH", "1", out var resolved), Is.True);
            Assert.That(resolved.Key, Is.EqualTo("flourish+1"));
            Assert.That(catalog.TryResolve("flourish", "10", out _), Is.False);
            Assert.That(catalog.TryResolve("flourish", "", out _), Is.False);
            Assert.That(catalog.TryResolve("unknown", "", out _), Is.False);
            player.Apply(list => list.Clear());
            Assert.That(catalog.TryResolve("flourish", "1", out _), Is.False);
        }

        [Test]
        public void HiddenCommandsRemainExecutableOnlyWhenGranted()
        {
            var player = new PlayerCommands();
            player.Apply(list => list.AddRange(new[] { "hiddenAbility", "visibleAbility", "flourish+1" }));
            using var catalog = new CommandCatalog(new[]
            {
                new CommandCatalog.Command { Name = "hidden", Label = "Hidden", Ability = "hiddenAbility", BrowserVisible = false },
                new CommandCatalog.Command { Name = "visible", Label = "Visible", Ability = "visibleAbility", BrowserVisible = true },
                new CommandCatalog.Command { Name = "ungranted", Label = "Ungranted", Ability = "missingAbility", BrowserVisible = true },
                new CommandCatalog.Command { Name = "flourish", Label = "Flourish", Ability = "flourish", BrowserVisible = false }
            }, player);
            Assert.That(catalog.TryResolve("hidden", "", out var hidden), Is.True);
            Assert.That(hidden.BrowserVisible, Is.False);
            Assert.That(catalog.TryResolve("flourish", "1", out var variant), Is.True);
            Assert.That(variant.BrowserVisible, Is.False);
            Assert.That(catalog.TryResolve("ungranted", "", out _), Is.False);
            player.Apply(list => list.Clear());
            Assert.That(catalog.TryResolve("hidden", "", out _), Is.False);
        }

        [Test]
        public void ProductionDispatcherReturnsSyntaxAndAvailabilityErrors()
        {
            var state = new WorldState(new SceneObjectRegistry());
            state.Commands.Apply(list => list.Add("flourish+1"));
            using var catalog = new CommandCatalog(new[]
            {
                new CommandCatalog.Command { Name = "flourish", Label = "Flourish", Ability = "flourish" }
            }, state.Commands);
            using var commands = new ClientCommands(_ => { }, _ => { }, state);
            using var chat = new ClientChat(null, state, new StringTableLocalizer(null), new ProseFormatter(), commands);
            var dispatcher = new CommandDispatcher(catalog, chat, commands, new ClientTargeting(state, _ => { }), new ClientCombatQueue(commands, catalog));
            dispatcher.Submit("Spatial", "/tell Misty", out string error);
            Assert.That(error, Is.EqualTo("Usage: /tell name message"));
            dispatcher.Submit("Spatial", "/t Misty", out error);
            Assert.That(error, Is.EqualTo("Usage: /tell name message"));
            dispatcher.Submit("Spatial", "/unknown", out error);
            Assert.That(error, Does.Contain("Unknown or unavailable"));
            dispatcher.Execute("flourish", "10", out error);
            Assert.That(error, Does.Contain("Unknown or unavailable"));
            dispatcher.Submit("Spatial", "", out error);
            Assert.That(error, Is.Null);
            dispatcher.Submit("Spatial", "/RTEL hello", out error);
            Assert.That(error, Does.Contain("No one has sent you a tell yet"));
            Assert.That(chat.History, Is.Empty);
        }

        [Test]
        public void OfflineDispatcherLogsRequestsWithoutAliasesOrValidation()
        {
            ICommandDispatcher dispatcher = new OfflineCommandDispatcher();
            LogAssert.Expect(LogType.Log, "Offline command: /unknown arguments");
            dispatcher.Execute("unknown", "arguments", out string error);
            Assert.That(error, Is.Null);
            LogAssert.Expect(LogType.Log, "Offline command: /t Misty");
            dispatcher.Submit("Spatial", "/t Misty", out error);
            Assert.That(error, Is.Null);
        }

        [Test]
        public void SharedDispatcherHandlesCommandsWithoutAnyHud()
        {
            var state = new WorldState(new SceneObjectRegistry());
            using var catalog = new CommandCatalog(System.Array.Empty<CommandCatalog.Command>(), state.Commands);
            using var commands = new ClientCommands(_ => { }, _ => { }, state);
            using var chat = new ClientChat(null, state, new StringTableLocalizer(null), new ProseFormatter(), commands);
            var dispatcher = new CommandDispatcher(catalog, chat, commands, new ClientTargeting(state, _ => { }), new ClientCombatQueue(commands, catalog));
            dispatcher.Submit("Spatial", "/unknown", out string error);
            Assert.That(error, Does.Contain("Unknown or unavailable"));
            dispatcher.Submit("Spatial", "/tell Misty", out error);
            Assert.That(error, Is.EqualTo("Usage: /tell name message"));
            dispatcher.Execute("r", "hello", out error);
            Assert.That(error, Does.Contain("No one has sent you a tell yet"));
            Assert.That(chat.History, Is.Empty);
        }

        [TestCase("follow")]
        [TestCase("consider")]
        public void UnimplementedClientHooksReturnUnsupportedWithoutEnqueueing(string hook)
        {
            var state = new WorldState(new SceneObjectRegistry());
            state.Commands.Apply(list => list.Add("local+1"));
            using var catalog = new CommandCatalog(new[]
            {
                new CommandCatalog.Command { Name = "local", Label = "Local", ClientHook = hook }
            }, state.Commands);
            using var commands = new ClientCommands(_ => { }, _ => { }, state);
            using var chat = new ClientChat(null, state, new StringTableLocalizer(null), new ProseFormatter(), commands);
            var dispatcher = new CommandDispatcher(catalog, chat, commands, new ClientTargeting(state, _ => { }), new ClientCombatQueue(commands, catalog));
            Assert.That(catalog.Find("local+1").ClientHook, Is.EqualTo(hook));
            dispatcher.Execute("local", "1", out string error);
            Assert.That(error, Does.Contain("not supported yet"));
            Assert.That(chat.History, Is.Empty);
        }

        [Test]
        public void ChatAdapterResolvesPacketTokensBeforeFormatting()
        {
            using var bytes = new MemoryStream();
            using var writer = new BinaryWriter(bytes);
            void WriteStringId()
            {
                PacketFieldCodec.Ascii(writer, "");
                writer.Write(0u);
                PacketFieldCodec.Ascii(writer, "");
            }
            WriteStringId();
            foreach (string text in new[] { "Misty", "Trixie", "" })
            {
                writer.Write(text.Length == 0 ? 42ul : 0ul);
                WriteStringId();
                PacketFieldCodec.Unicode(writer, text);
            }
            writer.Write(7);
            writer.Write(1.5f);
            writer.Write((byte)0);
            var state = new WorldState(new SceneObjectRegistry());
            state.SceneObjects.Add(new SceneObject { SwgId = 42, Name = "item" });
            var adapter = new ChatText(new StringTableLocalizer(null), new ProseFormatter(), state);
            var values = adapter.ResolveParameters(LocalizedChatString.ReadFrom(new ByteReader(bytes.ToArray())));
            Assert.That(values.User, Is.EqualTo("Misty"));
            Assert.That(values.Target, Is.EqualTo("Trixie"));
            Assert.That(values.Object, Is.EqualTo("item"));
            Assert.That(values.Integer, Is.EqualTo(7));
            Assert.That(values.Float, Is.EqualTo(1.5f));
            Assert.That(adapter.Format(@"a \#ff0000b\#. c", default), Is.EqualTo("a b c"));
        }

        [TestCase("Spatial", "/reply hello", "No one has sent you a tell yet")]
        [TestCase("Instant Messages", "hello", "No one has sent you a tell yet")]
        [TestCase("Spatial", "/rtell hello", "You have not sent a tell yet")]
        [TestCase("Spatial", "/auction hello", "This chat channel is not available")]
        [TestCase("Planet", "hello", "This chat channel is not available")]
        public void ChatValidationReturnsToTheCallerWithoutPublishingHistory(string channel, string message, string expected)
        {
            var state = new WorldState(new SceneObjectRegistry());
            using var catalog = new CommandCatalog(System.Array.Empty<CommandCatalog.Command>(), state.Commands);
            using var commands = new ClientCommands(_ => { }, _ => { }, state);
            using var chat = new ClientChat(null, state, new StringTableLocalizer(null), new ProseFormatter(), commands);
            int notifications = 0;
            chat.MessageReceived += (receivedChannel, text) => notifications++;
            var dispatcher = new CommandDispatcher(catalog, chat, commands, new ClientTargeting(state, _ => { }), new ClientCombatQueue(commands, catalog));
            dispatcher.Submit(channel, message, out string error);
            Assert.That(error, Does.Contain(expected));
            Assert.That(chat.History, Is.Empty);
            Assert.That(notifications, Is.Zero);
        }

        [Test]
        public void DisposingCatalogUnsubscribesFromAbilityUpdates()
        {
            var player = new PlayerCommands();
            var catalog = new CommandCatalog(System.Array.Empty<CommandCatalog.Command>(), player);
            int changes = 0;
            catalog.Changed += () => changes++;
            catalog.Dispose();
            player.Apply(list => list.Add("attack"));
            Assert.That(changes, Is.Zero);
        }

        [Test]
        public void SpatialCommandMatchesCore3EnqueueCallback()
        {
            var packet = ObjectControllerPacket.ReadFrom(Encode(new CommandQueueEnqueuePacket(42, 7, 0x12345678, "0 0 0 0 0 Hello ðŸŒŒ")));
            Assert.That(packet.Flags, Is.EqualTo(0x23));
            Assert.That(packet.ControllerType, Is.EqualTo(0x116));
            Assert.That(packet.SwgId, Is.EqualTo(42));
            Assert.That(packet.Reserved, Is.Zero);
            var r = new ByteReader(packet.Payload);
            Assert.That(r.U32(), Is.EqualTo(7));
            Assert.That(r.U32(), Is.EqualTo(0x12345678));
            Assert.That(r.U64(), Is.Zero);
            Assert.That(r.Unicode(), Is.EqualTo("0 0 0 0 0 Hello ðŸŒŒ"));
            Assert.That(r.Remaining, Is.Zero);
        }

        [Test]
        public void RoomSendMatchesCore3ChatSendToRoomCallback()
        {
            var r = Encode(new ChatSendToRoomPacket("Hello ðŸŒŒ", 44, 8));
            r.U16();
            Assert.That(r.U32(), Is.EqualTo(0x20e4dbe3));
            Assert.That(r.Unicode(), Is.EqualTo("Hello ðŸŒŒ"));
            Assert.That(r.U32(), Is.Zero);
            Assert.That(r.U32(), Is.EqualTo(44));
            Assert.That(r.U32(), Is.EqualTo(8));
            Assert.That(r.Remaining, Is.Zero);
        }

        [Test]
        public void TellMatchesCore3InstantMessageCallback()
        {
            var r = Encode(new ChatInstantMessageToCharacterPacket("Test", "Misty", "hÃ©llo", 9));
            r.U16();
            Assert.That(r.U32(), Is.EqualTo(0x84bb21f7));
            Assert.That(r.Ascii(), Is.EqualTo("SWG"));
            Assert.That(r.Ascii(), Is.EqualTo("Test"));
            Assert.That(r.Ascii(), Is.EqualTo("Misty"));
            Assert.That(r.Unicode(), Is.EqualTo("hÃ©llo"));
            Assert.That(r.U32(), Is.Zero);
            Assert.That(r.U32(), Is.EqualTo(9));
            Assert.That(r.Remaining, Is.Zero);
        }

        [Test]
        public void RoomJoinMatchesCore3EnterRoomCallback()
        {
            var r = Encode(new ChatEnterRoomByIdPacket(12, 44));
            r.U16();
            Assert.That(r.U32(), Is.EqualTo(0xbc6bddf2));
            Assert.That(r.U32(), Is.EqualTo(12));
            Assert.That(r.U32(), Is.EqualTo(44));
            Assert.That(r.Remaining, Is.Zero);
        }
    }
}

