using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Assets.Formats.DataTable;
using Swg.Client.Core.Configuration;
using Swg.Client.Core.Networking.Game.Packets.Commands;
using Swg.Client.Core.Networking.Game.Packets.Data;
using Swg.Client.Core.Networking.Game.Packets.Player;
using Swg.Client.Core.World.Travel;
using Swg.Client.Runtime.Assets.Tre;
using Swg.Client.Runtime.Networking;
using Swg.Client.Runtime.UI.Travel;

namespace Swg.Client.Tests.Editor
{
    public sealed class TravelChecks
    {
        static EnterTicketPurchaseModePacket Enter(string planet, string point)
        {
            using var stream = new MemoryStream();
            using var w = new BinaryWriter(stream);
            w.Write((ushort)4);
            w.Write(EnterTicketPurchaseModePacket.Opcode);
            PacketFieldCodec.Ascii(w, planet);
            PacketFieldCodec.Ascii(w, point);
            w.Write((byte)0);
            return EnterTicketPurchaseModePacket.ReadFrom(new ByteReader(stream.ToArray()));
        }
        static PlanetTravelPointListResponsePacket Points(string planet, string[] names, byte[] starports, uint[] tax = null)
        {
            using var stream = new MemoryStream();
            using var w = new BinaryWriter(stream);
            w.Write((ushort)6);
            w.Write(PlanetTravelPointListResponsePacket.Opcode);
            PacketFieldCodec.Ascii(w, planet);
            w.Write((uint)names.Length);
            foreach (var name in names)
                PacketFieldCodec.Ascii(w, name);
            w.Write((uint)names.Length);
            for (int i = 0; i < names.Length; i++)
                PacketFieldCodec.Position(w, new Vector3(i * 100, 0, i * 200));
            w.Write((uint)names.Length);
            for (int i = 0; i < names.Length; i++)
                w.Write(tax == null ? 0u : tax[i]);
            w.Write((uint)starports.Length);
            foreach (var starport in starports)
                w.Write(starport);
            return PlanetTravelPointListResponsePacket.ReadFrom(new ByteReader(stream.ToArray()));
        }
        static SuiPageData PurchaseConfirmation(string prompt)
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
            writer.Write(42u);
            PacketFieldCodec.Ascii(writer, "Script.messageBox");
            writer.Write(1u);
            writer.Write((byte)3);
            writer.Write(1u);
            PacketFieldCodec.Unicode(writer, prompt);
            writer.Write(2u);
            PacketFieldCodec.Ascii(writer, "Prompt.lblPrompt");
            PacketFieldCodec.Ascii(writer, "Text");
            writer.Write(0UL);
            writer.Write(0f);
            writer.Write(0UL);
            return SuiPageData.ReadFrom(new ByteReader(stream.ToArray()));
        }
        [Test]
        public void RequestUsesCore3OpcodePlayerAndPlanet()
        {
            using var stream = new MemoryStream();
            using var w = new BinaryWriter(stream);
            new PlanetTravelPointListRequestPacket(42, "talus").WriteTo(w);
            var r = new ByteReader(stream.ToArray());
            Assert.That(r.U16(), Is.EqualTo(3));
            Assert.That(r.U32(), Is.EqualTo(0x96405d4d));
            Assert.That(r.U64(), Is.EqualTo(42));
            Assert.That(r.Ascii(), Is.EqualTo("talus"));
            Assert.That(r.Remaining, Is.Zero);
        }
        [Test]
        public void FaresMirrorCore3UpperTriangleRatherThanTheConflictingLowerTriangle()
        {
            using var assets = new TreRuntimeAssets(ClientSettings.From(Path.Combine(Application.dataPath, "../client.ini")));
            var routes = new TravelRoutes(DataTableFile.Parse(assets.Loader.Read("datatables/travel/travel.iff")));
            Assert.That(routes.Fare("dantooine", "corellia"), Is.EqualTo(1000));
            Assert.That(routes.Fare("corellia", "talus"), Is.EqualTo(300));
            Assert.That(routes.Fare("corellia", "corellia"), Is.EqualTo(100));
            Assert.That(routes.Fare("corellia", "rori"), Is.Zero);
        }
        [TestCase(false)]
        [TestCase(true)]
        public async Task OriginStarportAndServerListsDetermineReachableDestinations(bool interplanetary)
        {
            using var assets = new TreRuntimeAssets(ClientSettings.From(Path.Combine(Application.dataPath, "../client.ini")));
            GameServicesForTests.CreateState(out var player);
            player.BeginScene(42);
            var requests = new List<PlanetTravelPointListRequestPacket>();
            var purchases = new List<CommandQueueEnqueuePacket>();
            using var commands = new ClientCommands(purchases.Add, _ => { }, player);
            using var travel = new ClientTravel(assets, player, commands, requests.Add, _ => { });
            await travel.Ready;
            travel.Receive(Enter("corellia", "Coronet Starport"));
            await Task.Delay(30);
            Assert.That(requests.Select(p => p.Planet), Is.EqualTo(new[] { "corellia" }));
            travel.Receive(Points("corellia", new[] { "Coronet Starport", "Coronet Shuttle A" }, new[] { interplanetary ? (byte)1 : (byte)0, (byte)0 }, new[] { 25u, 0u }));
            await Task.Delay(30);
            Assert.That(travel.Destinations.Select(p => p.Name), Is.EqualTo(new[] { "Coronet Shuttle A" }));
            Assert.That(requests.Any(p => p.Planet == "rori"), Is.False);
            if (!interplanetary)
            {
                Assert.That(requests.Count, Is.EqualTo(1));
                travel.SelectPlanet("talus");
                Assert.That(travel.SelectedPlanet, Is.EqualTo("corellia"));
                return;
            }
            Assert.That(requests.Any(p => p.Planet == "talus"), Is.True);
            travel.Receive(Points("talus", new[] { "Talus Imperial Outpost", "Local Shuttle" }, new byte[] { 1, 0 }));
            travel.Receive(Points("yavin4", new[] { "" }, new byte[] { 0 }));
            await Task.Delay(30);
            Assert.That(travel.Planets.Contains("yavin4"), Is.False);
            travel.SelectPlanet("talus");
            Assert.That(travel.Destinations.Select(p => p.Name), Is.EqualTo(new[] { "Talus Imperial Outpost" }));
            Assert.That(travel.Cost, Is.EqualTo(325));
            travel.SetRoundTrip(true);
            Assert.That(travel.Cost, Is.EqualTo(650));
            travel.SetRoundTrip(false);
            travel.Purchase();
            travel.Purchase();
            Assert.That(purchases.Count, Is.EqualTo(1));
            Assert.That(purchases.Single().Arguments, Is.EqualTo("corellia Coronet_Starport talus Talus_Imperial_Outpost single"));
            Assert.That(travel.TryReceive(PurchaseConfirmation("@other:message")), Is.False);
            Assert.That(travel.TryReceive(PurchaseConfirmation("@travel:ticket_purchase_complete")), Is.True);
            await Task.Delay(30);
            Assert.That(travel.Purchasing, Is.False);
            Assert.That(travel.PurchaseComplete, Is.True);
            Assert.That(purchases.Count, Is.EqualTo(1));
            travel.AcknowledgePurchase();
            Assert.That(travel.PurchaseComplete, Is.False);
        }
        [Test]
        public void MismatchedDestinationColumnsAreRejectedAtThePacketBoundary()
        {
            Assert.Throws<InvalidDataException>(() => Points("talus", new[] { "Port" }, new byte[0]));
        }
        [Test]
        public void DestinationMapUsesServerGroundPlaneRatherThanElevation()
        {
            var map = TravelWindow.MapPosition(new Vector3(6644.27f, -5922.52f, 330));
            Assert.That(map.x, Is.EqualTo(.5f + 6644.27f / 16384).Within(.00001f));
            Assert.That(map.y, Is.EqualTo(.5f - 5922.52f / 16384).Within(.00001f));
            Assert.That(TravelWindow.MapPosition(new Vector3(6644.27f, -5922.52f, 999)), Is.EqualTo(map));
        }
        [Test]
        public async Task SelectionChangesReuseDropdownOptionsAndDestinationMarkers()
        {
            using var assets = new TreRuntimeAssets(ClientSettings.From(Path.Combine(Application.dataPath, "../client.ini")));
            GameServicesForTests.CreateState(out var player);
            using var commands = new ClientCommands(_ => { }, _ => { }, player);
            using var travel = new ClientTravel(assets, player, commands, _ => { }, _ => { });
            await travel.Ready;
            travel.Receive(Enter("corellia", "Coronet Starport"));
            await Task.Delay(30);
            travel.Receive(Points("corellia", new[] { "Coronet Starport", "A", "B" }, new byte[] { 1, 0, 0 }));
            await Task.Delay(30);
            var scene = UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
            var root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/Travel/Travel.prefab"));
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
            var view = root.GetComponent<TravelWindow>();
            var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            typeof(TravelWindow).GetField("travel", flags).SetValue(view, travel);
            void Refresh()
            {
                typeof(TravelWindow).GetMethod("RefreshDestinations", flags).Invoke(view, null);
                typeof(TravelWindow).GetMethod("RefreshSelection", flags).Invoke(view, null);
            }
            try
            {
                Refresh();
                var option = view.locationSelector.options[0];
                var planet = view.planetSelector.options[0];
                var markers = view.markers.GetComponentsInChildren<UnityEngine.UI.Button>(true);
                Assert.That(markers.Length, Is.EqualTo(2));
                travel.SelectPoint("B");
                travel.SetRoundTrip(true);
                Refresh();
                Assert.That(view.locationSelector.options[0], Is.SameAs(option));
                Assert.That(view.planetSelector.options[0], Is.SameAs(planet));
                Assert.That(view.markers.GetComponentsInChildren<UnityEngine.UI.Button>(true), Is.EqualTo(markers));
                Assert.That(view.locationSelector.value, Is.EqualTo(1));
                Assert.That(markers[1].GetComponentInChildren<TMPro.TMP_Text>(true).color, Is.EqualTo(Color.white));
            }
            finally { UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene); }
        }

        [Test]
        public void PrefabHasBothViewsSelectorsAndNativeMaps()
        {
            var view = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/Travel/Travel.prefab").GetComponent<TravelWindow>();
            Assert.That(view.panel.activeSelf, Is.False);
            Assert.That(view.planets.Length, Is.EqualTo(10));
            Assert.That(view.planetSelector, Is.Not.Null);
            Assert.That(view.locationSelector, Is.Not.Null);
            Assert.That(view.planets.All(p => p.button != null && p.map != null && !string.IsNullOrEmpty(p.appearance)), Is.True);
            Assert.That(view.confirmation, Is.Not.Null);
            Assert.That(view.confirmationPrompt, Is.Not.Null);
            Assert.That(view.markerPrefab, Is.Not.Null);
            Assert.That(view.previews, Is.Not.Null);
        }
    }
}

