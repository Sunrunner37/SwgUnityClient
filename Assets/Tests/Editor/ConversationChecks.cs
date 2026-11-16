using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using NUnit.Framework;
using Swg.Client.Core;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Configuration;
using Swg.Client.Core.Networking.Game.Packets;
using Swg.Client.Core.Networking.Game.Packets.Conversation;
using Swg.Client.Core.Networking.Game.Packets.Data;
using Swg.Client.Core.World;
using Swg.Client.Core.World.SceneObjects;
using Swg.Client.Runtime.Assets.Tre;
using Swg.Client.Runtime.Localization;
using Swg.Client.Runtime.Networking;
using Swg.Client.Runtime.UI.Conversation;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Swg.Client.Tests.Editor
{
    public sealed class ConversationChecks
    {
        sealed class MainThread : SynchronizationContext
        {
            readonly Queue<Action> pending = new Queue<Action>();
            public override void Post(SendOrPostCallback callback, object state) => pending.Enqueue(() => callback(state));
            public void Drain() { while (pending.Count > 0) pending.Dequeue()(); }
        }
        static ObjectControllerPacket Frame(uint type, Action<BinaryWriter> payload)
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
            payload(writer);
            return new ObjectControllerPacket(0xb, type, 1, stream.ToArray());
        }
        static StartNpcConversationPacket Start(ulong id) => StartNpcConversationPacket.ReadFrom(Frame(0xdd, w =>
        {
            w.Write(id); w.Write((byte)0); PacketFieldCodec.Ascii(w, "artisanTrainerConvoTemplate"); w.Write(0u);
        }));
        static StringListPacket Options(params string[] options) => StringListPacket.ReadFrom(Frame(0xe0, w =>
        {
            w.Write((byte)options.Length); foreach (var option in options) PacketFieldCodec.Unicode(w, option);
        }));

        [Test]
        public void ConversationFollowsServerChoicesAndSendsTheirZeroBasedIndex()
        {
            var previous = SynchronizationContext.Current;
            var main = new MainThread();
            SynchronizationContext.SetSynchronizationContext(main);
            try
            {
                var sent = new List<(string Command, string Arguments, SwgId Target)>();
                using var conversation = new ClientConversation((name, args, target) => sent.Add((name, args, target)), new StringTableLocalizer(null), new ProseFormatter(), new WorldState(new SceneObjectRegistry()));
                conversation.Start(42);
                Assert.That(conversation.IsOpen, Is.False);
                Assert.That(sent[0], Is.EqualTo(("npcConversationStart", "", (SwgId)42)));
                conversation.Receive(Start(42));
                conversation.Receive(NpcConversationPacket.ReadFrom(Frame(0xdf, w => PacketFieldCodec.Unicode(w, "Can I help you?"))));
                conversation.Receive(Options("Learn a skill", "What can I learn next?"));
                Assert.That(conversation.IsOpen, Is.False, "Network callbacks must not change UI state directly.");
                main.Drain();
                Assert.That(conversation.IsOpen, Is.True);
                Assert.That(conversation.Message, Is.EqualTo("Can I help you?"));
                Assert.That(conversation.Options, Is.EqualTo(new[] { "Learn a skill", "What can I learn next?" }));
                conversation.Select(1);
                Assert.That(sent[1], Is.EqualTo(("npcConversationSelect", "1", (SwgId)42)));
                conversation.Select(1);
                Assert.That(sent.Count, Is.EqualTo(2), "Wait for the server's new options before accepting another click.");
                conversation.Receive(Options("Surveying IV"));
                main.Drain();
                Assert.That(conversation.Waiting, Is.False);
                conversation.Stop();
                Assert.That(sent[2], Is.EqualTo(("npcConversationStop", "", (SwgId)42)));
                Assert.That(conversation.IsOpen, Is.False);
                conversation.Receive(Options("Late reply")); main.Drain();
                Assert.That(conversation.Options, Is.Empty);
            }
            finally { SynchronizationContext.SetSynchronizationContext(previous); }
        }

        [Test]
        public void ServerStopMatchesNpcAndDisposedConversationsIgnorePostedMessages()
        {
            var previous = SynchronizationContext.Current;
            var main = new MainThread(); SynchronizationContext.SetSynchronizationContext(main);
            try
            {
                using var conversation = new ClientConversation((name, args, target) => { }, new StringTableLocalizer(null), new ProseFormatter(), new WorldState(new SceneObjectRegistry()));
                conversation.Receive(Start(42)); main.Drain();
                conversation.Receive(StopNpcConversationPacket.ReadFrom(Frame(0xde, w => w.Write(43ul)))); main.Drain();
                Assert.That(conversation.IsOpen, Is.True);
                conversation.Receive(StopNpcConversationPacket.ReadFrom(Frame(0xde, w => w.Write(42ul)))); main.Drain();
                Assert.That(conversation.IsOpen, Is.False);
                conversation.Receive(Start(43)); conversation.Dispose(); main.Drain();
                Assert.That(conversation.IsOpen, Is.False);
            }
            finally { SynchronizationContext.SetSynchronizationContext(previous); }
        }

        [Test, Category("NativeAssets")]
        public void TrainerSkillOptionsUseTheSharedNativeLocalizer()
        {
            var settings = ClientSettings.From(Path.Combine(Directory.GetParent(Application.dataPath).FullName, "client.ini"));
            using var assets = new TreRuntimeAssets(settings); assets.Ready.GetAwaiter().GetResult();
            var previous = SynchronizationContext.Current; var main = new MainThread(); SynchronizationContext.SetSynchronizationContext(main);
            try
            {
                using var conversation = new ClientConversation((name, args, target) => { }, new StringTableLocalizer(assets), new ProseFormatter(), new WorldState(new SceneObjectRegistry()));
                conversation.Receive(Start(42)); conversation.Receive(Options("@skl_n:crafting_artisan_survey_04")); main.Drain();
                Assert.That(conversation.Options[0], Does.Contain("Surveying IV").And.Not.StartWith("@"));
            }
            finally { SynchronizationContext.SetSynchronizationContext(previous); }
        }

        [Test]
        public void ConversableObjectsSupplyTheSameDefaultAndRadialAction()
        {
            var npc = new TangibleSceneObject { Options = 8 };
            Assert.That(ObjectMenuDefaults.TryGetDefaultAction(npc, (ObjectTypeId)0x400, out var action), Is.True);
            Assert.That(action, Is.SameAs(ObjectMenuAction.ConverseStart));
            Assert.That(ObjectMenuDefaults.Create(npc, (ObjectTypeId)0x400)[1].Callback, Is.EqualTo(1));
        }

        [Test]
        public void WrappedResponsesGrowToFitTheirText()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/Conversation/ConversationResponse.prefab");
            var row = UnityEngine.Object.Instantiate(prefab);
            try
            {
                var label = row.GetComponentInChildren<TMPro.TMP_Text>();
                var rectangle = (RectTransform)row.transform;
                label.text = "Yes";
                LayoutRebuilder.ForceRebuildLayoutImmediate(rectangle);
                float shortHeight = LayoutUtility.GetPreferredHeight(rectangle);
                label.text = "This is a long server-provided response that needs several lines in the conversation menu.";
                LayoutRebuilder.ForceRebuildLayoutImmediate(rectangle);
                Assert.That(LayoutUtility.GetPreferredHeight(rectangle), Is.GreaterThan(shortHeight + 10));
            }
            finally { UnityEngine.Object.DestroyImmediate(row); }
        }

        [Test]
        public void ConversationPrefabOwnsSpeechLayoutScrollingAndResponseTemplate()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/Conversation/Conversation.prefab");
            var view = prefab.GetComponent<ConversationWindow>();
            Assert.That(view.panel.gameObject.activeSelf, Is.False);
            Assert.That(view.bubble.rect.width, Is.EqualTo(240));
            Assert.That(view.responses.content.GetComponent<VerticalLayoutGroup>(), Is.Not.Null);
            Assert.That(view.responses.content.GetComponent<ContentSizeFitter>(), Is.Not.Null);
            Assert.That(view.optionPrefab.GetComponent<LayoutElement>().minHeight, Is.EqualTo(23));
            Assert.That(view.optionPrefab.GetComponent<VerticalLayoutGroup>(), Is.Not.Null);
            Assert.That(view.UiPrefabs, Does.Contain(view.optionPrefab.gameObject));
        }
    }
}
