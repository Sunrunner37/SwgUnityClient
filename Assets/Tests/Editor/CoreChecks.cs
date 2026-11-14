using Swg.Client.Core.Assets.Appearances;
using Swg.Client.Core.Networking.Game;
using Swg.Client.Core.Networking.Game.Packets.Session;
using Swg.Client.Core.Networking.Game.Packets.Scene;
using Swg.Client.Core.Assets.Audio;
using Swg.Client.Core.Assets;
using Swg.Client.Core.Assets.Formats.Customization;
using Application = UnityEngine.Application;
using System;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Swg.Client.Core.Assets.Formats.Binary;
using Swg.Client.Core.Assets.Formats.Compression;
using Swg.Client.Core.Assets.Formats.Dds;
using Swg.Client.Core.Assets.Formats.Iff;
using Swg.Client.Core.Assets.Formats.Mp3;
using Swg.Client.Core.Assets.Formats.Stf;
using Swg.Client.Core.Assets.Formats.Ui;
using Swg.Client.Core.Assets.Tre;
using Swg.Client.Core.Audio;
using Swg.Client.Core.Characters.Appearance;
using Swg.Client.Core.Characters.Creation;
using Swg.Client.Core.Configuration;
using Swg.Client.Core.Networking;
using Swg.Client.Core.Networking.Login;
using Swg.Client.Core.Networking.Soe;
using Swg.Client.Core.Networking.Login.Packets;

namespace Swg.Client.Tests.Editor
{
    [TestFixture]
    public sealed class CoreChecks
    {
        static void Check(bool test, string reason)
        {
            if (!test)
                throw new Exception(reason);
        }

        static void Throws(Action action)
        {
            try
            {
                action();
            }
            catch
            {
                return;
            }

            throw new Exception("Expected an exception");
        }

        static TestCaseData Case(string name, Action check, bool nativeAssets = false)
        {
            return new TestCaseData(check).SetName(name).SetCategory(nativeAssets ? "NativeAssets" : "Core");
        }

        [TestCaseSource(nameof(Cases))]
        public void Passes(Action check) => check();
        public static IEnumerable<TestCaseData> Cases()
        {
            yield return Case("Attribute-based data-table mapping", DataTableMappingChecks.Run);
            yield return Case("DTII typed schemas and malformed input", DataTableChecks.Run);
            yield return Case("Original mixed-schema data tables", DataTableChecks.OriginalAssets, nativeAssets: true);
            yield return Case("Rigid pose composition and inverse", RigidTransformChecks.Run);
            yield return Case("Typed login packet wire contracts", LoginPacketChecks.Run);
            yield return Case("Game packet contracts and world replication", GamePacketChecks.Run);
            yield return Case("Original world snapshots, buildings, and terrain", WorldAssetChecks.Run, nativeAssets: true);
            yield return Case("Native loading artwork and localized content", LoadingContentChecks.Run, nativeAssets: true);
            yield return Case("Required login packet collection and handoff", LoginPacketCollectorChecks.Run);
            yield return Case("Native appearance cache and customization validation", AppearanceChecks);
            yield return Case("Core3 credential wire format", () =>
            {
                var actual = Encode(new LoginCredentialsPacket("a", "b", "20050408-18:00"));
                var expected = new byte[]
                {
                    4,
                    0,
                    0x96,
                    0x1f,
                    0x13,
                    0x41,
                    1,
                    0,
                    97,
                    1,
                    0,
                    98,
                    14,
                    0
                }.Concat(Encoding.ASCII.GetBytes("20050408-18:00")).ToArray();
                Check(actual.SequenceEqual(expected), "Credential fields differ");
                Throws(() => Encode(new LoginCredentialsPacket("", "x", "v")));
                Throws(() => Encode(new LoginCredentialsPacket("x", "é", "v")));
            });
            yield return Case("CRC and packet encoding", () =>
            {
                var plain = SoeCodec.Data(7, Encode(new LoginCredentialsPacket("test", "password", "20050408-18:00")));
                var encoded = SoeCodec.Encode(plain, 0x12345678);
                Check(SoeCodec.Decode(encoded, 0x12345678).SequenceEqual(plain), "Codec round trip");
                encoded[5] ^= 1;
                Throws(() => SoeCodec.Decode(encoded, 0x12345678));
            });
            yield return Case("Zlib validation and expansion limits", () =>
            {
                var input = Encoding.UTF8.GetBytes("compressed original asset");
                byte[] packed = Pack(input);
                Check(Zlib.Inflate(packed, 100).SequenceEqual(input), "Zlib content");
                Throws(() => Zlib.Inflate(packed, 2));
                packed[packed.Length - 1] ^= 1;
                Throws(() => Zlib.Inflate(packed, 100));
            });
            yield return Case("Fragment ordering and deduplication", () =>
            {
                var channel = new OrderedChannel();
                var application = Encode(new LoginCredentialsPacket("a", "b", "20050408-18:00"));
                var first = Net32((uint)application.Length).Concat(application.Take(9)).ToArray();
                var last = SoeCodec.Data(1, application.Skip(9).ToArray(), 13);
                Check(channel.Accept(last).Count == 0, "Dispatched out of order fragment");
                Check(channel.Accept(SoeCodec.Data(0, first, 13)).Single().SequenceEqual(application), "Fragment content");
                Check(channel.Accept(last).Count == 0, "Duplicate dispatched");
                Check(channel.LastAck == 1, "Wrong acknowledgement");
            });
            yield return Case("Variable-length tokens and list bounds", () =>
            {
                var reply = new LoginPacketCollector();
                reply.Accept(Encode(new LoginClientTokenPacket(Enumerable.Repeat((byte)'x', 64).ToArray(), 42, 42, "test")));
                Check(!reply.Finished, "Partial login reply returned a result");
                Throws(() => reply.Accept(new byte[] { 2, 0, 0x74, 0x45, 0xea, 0x65, 255, 255, 255, 255 }));
            });
            yield return Case("TRE-only precedence and loose-file isolation", TreChecks);
            yield return Case("DDS masked pixels and DXT1", DdsChecks);
            yield return Case("Malformed original formats", () =>
            {
                Throws(() => TextureData.DecodeDds(new byte[128]));
                Throws(() => IffNode.Parse(Encoding.ASCII.GetBytes("FORM\0\0\0\u0010SKMG")));
                Throws(() => StfStringTable.Parse(new byte[] { 0xcd, 0xab, 0, 0, 1 }));
                Throws(() => UiNode.Parse("<Namespace>"));
            });
            yield return Case("INI and path validation", () =>
            {
                var ini = ClientSettings.Parse("[Network]\nLoginServerAddress=localhost\nLoginServerPort=12345");
                Check(ini["network/loginserveraddress"] == "localhost", "INI section");
                Throws(() => ClientSettings.Parse("bad line"));
                Throws(() => TreAssetLoader.Normalize("../outside"));
                Throws(() => TreAssetLoader.Normalize("C:/outside"));
                Check(TreAssetLoader.Normalize("TEXTURE\\intro.dds") == "texture/intro.dds", "Path normalization");
            });
            yield return Case("UDP login + zone admission with packet loss, reordering and bundles", () => Handshake(false, false));
            yield return Case("Rejected password never admits account", () => Handshake(true, false));
            yield return Case("Creation permission does not block character-list login", () => Handshake(false, true));
            yield return Case("Denied galaxy login never admits account", () => Handshake(false, false, true));
            yield return Case("Disconnect event is delivered on the owning context", () => DisconnectEventCheck(false, false));
            yield return Case("Unsubscribing suppresses a posted disconnect", () => DisconnectEventCheck(true, false));
            yield return Case("Intentional close suppresses a posted disconnect", () => DisconnectEventCheck(false, true));
            yield return Case("Original audio and twenty character models", OriginalAssets, nativeAssets: true);
            yield return Case("Client-owned appearance cache and skeletal idle pose", OriginalCachedAppearance, nativeAssets: true);
        }

        static void AppearanceChecks()
        {
            var encoded = new byte[]
            {
                1,
                3,
                1,
                255,
                1,
                2,
                255,
                2,
                0x83,
                0xd1,
                255,
                2,
                255,
                3
            };
            var custom = PackedCustomization.Decode(encoded);
            Check(custom.Values[1] == 0 && custom.Values[2] == 255 && custom.Values[3] == -47, "Escaped/signed customization values");
            Throws(() => PackedCustomization.Decode(encoded.Take(encoded.Length - 1).ToArray()));
            Throws(() => PackedCustomization.Decode(new byte[] { 1, 1, 1, 255, 4, 255, 3 }));
            Throws(() => PackedCustomization.Decode(new byte[] { 2, 0, 255, 3 }));
            Throws(() => CharacterAppearanceCache.Parse(new byte[20]));
            Check(ObjectTemplateNames.TemplateCrc("object/creature/player/human_female.iff") == 3567725168, "Native race CRC");
            Throws(() => CharacterAppearanceCache.SaveProfile(Path.GetTempPath(), "server", "../outside", "Core3", 1, null));
        }

        static void OriginalCachedAppearance()
        {
            var settings = ClientSettings.From(FindSettings());
            using var loader = new TreAssetLoader(settings.TreDirectory, settings.TreArchivePriorities);
            string cacheRoot = Path.Combine(Path.GetTempPath(), "swg-appearance-" + Guid.NewGuid());
            try
            {
                var source = new CharacterAppearance
                {
                    TemplatePath = "object/creature/player/shared_human_female.iff",
                    Customization = PackedCustomization.Decode(new byte[] { 1, 0, 255, 3 }),
                    Wearables = new[]
                    {
                        new WearableAppearance
                        {
                            TemplatePath = "object/tangible/hair/human/shared_hair_human_female_s31.iff",
                            Customization = PackedCustomization.Decode(Array.Empty<byte>()),
                            ArrangementHint = 4
                        }
                    }
                };
                CharacterAppearanceCache.SaveProfile(cacheRoot, "test-server:44453", "test-user", "Core3", 42, source, "corellia");
                Check(CharacterAppearanceCache.TryReadProfile(cacheRoot, "test-server:44453", "test-user", "Core3", 42, out var appearance), "Saved appearance cache could not be read");
                Check(appearance.Planet == "corellia" && appearance.FromCache, "Client-owned planet/cache source");
                Check(appearance.SourcePath.StartsWith(cacheRoot, StringComparison.OrdinalIgnoreCase), "Cache escaped client directory");
                Check(!CharacterAppearanceCache.TryReadProfile(cacheRoot, "other-server:44453", "test-user", "Core3", 42, out _), "Cache leaked between servers");
                Check(appearance.Appearance.Wearables.Count == 1 && appearance.Appearance.Wearables[0].ArrangementHint == 4, "Cached wearable record");
                var model = SkeletalAppearanceLoader.LoadDetailed(loader, appearance.Appearance);
                Check(model.Parts.Count > 0, "Cached character has no mesh");
                var positions = model.Parts.SelectMany(p => p.Vertices).ToArray();
                Check(positions.All(p => !float.IsNaN(p.x) && !float.IsInfinity(p.x) && !float.IsNaN(p.y) && !float.IsInfinity(p.y) && !float.IsNaN(p.z) && !float.IsInfinity(p.z)), "Nonfinite cached character");
                Check(positions.Max(p => p.x) - positions.Min(p => p.x) < .8f, "Character remained in bind pose");
                var cache = File.ReadAllBytes(appearance.SourcePath);
                Throws(() => CharacterAppearanceCache.Parse(cache.Take(cache.Length - 1).ToArray()));
                var fallback = CharacterAppearanceCache.LoadOrFallback(loader, cacheRoot, "test-server:44453", "missing-user", "Core3", new CharacterInfo { Race = 3567725168, SwgId = 1 });
                Check(!fallback.FromCache && fallback.Appearance.TemplatePath == appearance.Appearance.TemplatePath, "Missing-cache TRE template default");
            }
            finally
            {
                if (Directory.Exists(cacheRoot))
                    Directory.Delete(cacheRoot, true);
            }
        }

        static byte[] Net32(uint value)
        {
            return new[]
            {
                (byte)(value >> 24),
                (byte)(value >> 16),
                (byte)(value >> 8),
                (byte)value
            };
        }

        static byte[] Pack(byte[] bytes)
        {
            using (var memory = new MemoryStream())
            {
                memory.WriteByte(0x78);
                memory.WriteByte(0x9c);
                using (var z = new DeflateStream(memory, CompressionLevel.Optimal, true))
                    z.Write(bytes, 0, bytes.Length);
                uint a = 1;
                uint b = 0;
                foreach (byte value in bytes)
                {
                    a = (a + value) % 65521;
                    b = (b + a) % 65521;
                }

                byte[] sum = Net32((b << 16) | a);
                memory.Write(sum, 0, 4);
                return memory.ToArray();
            }
        }

        static byte[] Tre(string name, byte[] data, bool compressed)
        {
            uint method = compressed ? 2u : 0;
            byte[] payload = compressed ? Pack(data) : data;
            byte[] record;
            using (var m = new MemoryStream())
            using (var w = new BinaryWriter(m))
            {
                foreach (uint value in new[]
                {
                    0u,
                    (uint)data.Length,
                    36u,
                    method,
                    (uint)payload.Length,
                    0u
                }

                )
                    w.Write(value);
                record = m.ToArray();
            }

            byte[] names = Encoding.ASCII.GetBytes(name + "\0");
            byte[] packedNames = compressed ? Pack(names) : names;
            if (compressed)
                record = Pack(record);
            using (var m = new MemoryStream())
            using (var w = new BinaryWriter(m))
            {
                w.Write(Encoding.ASCII.GetBytes("EERT5000"));
                foreach (uint value in new[]
                {
                    1u,
                    36u + (uint)payload.Length,
                    method,
                    (uint)record.Length,
                    method,
                    (uint)packedNames.Length,
                    (uint)names.Length
                }

                )
                    w.Write(value);
                w.Write(payload);
                w.Write(record);
                w.Write(packedNames);
                return m.ToArray();
            }
        }

        static void TreChecks()
        {
            string temp = Path.Combine(Path.GetTempPath(), "swg-csharp-check-" + Guid.NewGuid());
            Directory.CreateDirectory(temp);
            try
            {
                File.WriteAllBytes(Path.Combine(temp, "base.tre"), Tre("test.txt", Encoding.ASCII.GetBytes("base"), false));
                File.WriteAllBytes(Path.Combine(temp, "patch.tre"), Tre("test.txt", Encoding.ASCII.GetBytes("patch"), true));
                using (var loader = new TreAssetLoader(temp, new Dictionary<string, int> { ["patch.tre"] = 20, ["base.tre"] = 10 }))
                {
                    Check(loader.ArchiveCount == 2, "Discovered archive count");
                    Check(Encoding.ASCII.GetString(loader.Read("TEST.TXT")) == "patch", "Explicit custom archive priority");
                    File.WriteAllText(Path.Combine(temp, "test.txt"), "loose poison");
                    Check(loader.Text("test.txt") == "patch", "Loose files must not override TRE assets");
                    Check(loader.Sources.Values.All(path => path.EndsWith(".tre", StringComparison.OrdinalIgnoreCase)), "Non-TRE source opened");
                    Throws(() => loader.Read("../outside"));
                }

                Throws(() => new TreAssetLoader(temp, new Dictionary<string, int> { ["../outside.tre"] = 30 }));
                Throws(() => new TreAssetLoader(temp, new Dictionary<string, int> { ["not-an-archive.cfg"] = 30 }));
                Throws(() => new TreAssetLoader(temp));
            }
            finally
            {
                Directory.Delete(temp, true);
            }
        }

        static void DdsChecks()
        {
            var b = new byte[132];
            Buffer.BlockCopy(Encoding.ASCII.GetBytes("DDS "), 0, b, 0, 4);
            foreach (var item in new[]
            {
                (4, 124u),
                (8, 8u),
                (12, 1u),
                (16, 1u),
                (20, 4u),
                (76, 32u),
                (80, 65u),
                (88, 32u),
                (92, 0xff0000u),
                (96, 0xff00u),
                (100, 0xffu),
                (104, 0xff000000u)
            }

            )
                Buffer.BlockCopy(BitConverter.GetBytes(item.Item2), 0, b, item.Item1, 4);
            b[128] = 3;
            b[129] = 2;
            b[130] = 1;
            b[131] = 128;
            Check(TextureData.DecodeDds(b).Pixels.SequenceEqual(new byte[] { 1, 2, 3, 128 }), "ARGB masks");
            Throws(() => TextureData.DecodeDds(b.Take(131).ToArray()));
            var dxt = new byte[136];
            Buffer.BlockCopy(b, 0, dxt, 0, 128);
            Buffer.BlockCopy(BitConverter.GetBytes(4u), 0, dxt, 12, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(4u), 0, dxt, 16, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(4u), 0, dxt, 80, 4);
            Buffer.BlockCopy(Encoding.ASCII.GetBytes("DXT1"), 0, dxt, 84, 4);
            dxt[129] = 0xf8;
            Check(TextureData.DecodeDds(dxt).Pixels.Take(4).SequenceEqual(new byte[] { 255, 0, 0, 255 }), "DXT1 red block");
        }

        static UdpClient Socket()
        {
            var s = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0));
            s.Client.ReceiveTimeout = 5000;
            return s;
        }

        static uint Session(UdpClient socket)
        {
            IPEndPoint peer = null;
            byte[] request = socket.Receive(ref peer);
            Check(request.Length == 14 && request.Take(6).SequenceEqual(new byte[] { 0, 1, 0, 0, 0, 2 }), "Session request");
            socket.Connect(peer);
            uint seed = 0x11223344;
            var response = new byte[]
            {
                0,
                2
            }.Concat(request.Skip(6).Take(4)).Concat(Net32(seed)).Concat(new byte[] { 2, 1, 4, 0, 0, 1, 0xf0 }).ToArray();
            socket.Send(response, response.Length);
            return seed;
        }

        static byte[] ReceiveData(UdpClient socket, uint seed)
        {
            while (true)
            {
                IPEndPoint peer = null;
                var wire = socket.Receive(ref peer);
                byte[] b;
                try
                {
                    b = SoeCodec.Decode(wire, seed);
                }
                catch (InvalidDataException)
                {
                    continue;
                }

                if (b[0] == 0 && b[1] == 9)
                    return b.Skip(4).ToArray();
            }
        }

        static void Send(UdpClient s, uint seed, byte[] b)
        {
            b = SoeCodec.Encode(b, seed);
            s.Send(b, b.Length);
        }

        sealed class DisconnectContext : SynchronizationContext, IDisposable
        {
            public readonly ManualResetEventSlim Posted = new ManualResetEventSlim();
            SendOrPostCallback callback;
            object state;
            public override void Post(SendOrPostCallback callback, object state)
            {
                this.callback = callback;
                this.state = state;
                Posted.Set();
            }

            public void Deliver() => callback(state);
            public void Dispose() => Posted.Dispose();
        }

        static void DisconnectEventCheck(bool unsubscribe, bool dispose)
        {
            using var context = new DisconnectContext();
            int ownerThread = Thread.CurrentThread.ManagedThreadId;
            Handshake(false, false, notificationContext: context, afterReady: (game, disconnectServer) =>
            {
                int calls = 0;
                int notificationThread = 0;
                string reason = null;
                Action<string> handler = message =>
                {
                    calls++;
                    notificationThread = Thread.CurrentThread.ManagedThreadId;
                    reason = message;
                };
                game.Disconnected += handler;
                try
                {
                    disconnectServer();
                    Check(context.Posted.Wait(3000), "Network failure was not posted to the owning context");
                    Check(calls == 0, "Disconnect ran directly on the network thread");
                    if (unsubscribe)
                        game.Disconnected -= handler;
                    if (dispose)
                        game.Dispose();
                    context.Deliver();
                    Check(calls == (unsubscribe || dispose ? 0 : 1), "Unexpected disconnect notification count");
                    if (calls == 1)
                    {
                        Check(notificationThread == ownerThread, "Disconnect was not delivered on the owning thread");
                        Check(reason == "Server disconnected the session", "Disconnect reason was lost");
                    }
                }
                finally
                {
                    game.Disconnected -= handler;
                }
            });
        }

        static void Handshake(bool rejectPassword, bool denyCreation, bool denyLogin = false, SynchronizationContext notificationContext = null, Action<GameConnection, Action> afterReady = null)
        {
            using (var login = Socket())
            using (var zone = Socket())
            using (var done = new ManualResetEventSlim())
            using (var delivered = new ManualResetEventSlim())
            using (var disconnectRequested = new ManualResetEventSlim())
            {
                ushort zonePort = (ushort)((IPEndPoint)zone.Client.LocalEndPoint).Port;
                string fixtureIni = Path.GetTempFileName();
                ClientSettings config;
                try
                {
                    File.WriteAllText(fixtureIni, "[Assets]\nTreDirectory=.\n[Network]\nLoginServerAddress=127.0.0.1\nLoginServerPort=" + ((IPEndPoint)login.Client.LocalEndPoint).Port + "\nClientVersion=20050408-18:00\n");
                    config = ClientSettings.From(fixtureIni);
                }
                finally
                {
                    File.Delete(fixtureIni);
                }

                var server = Task.Run(() =>
                {
                    uint seed = Session(login);
                    var first = ReceiveData(login, seed);
                    Check(first.SequenceEqual(Encode(new LoginCredentialsPacket("test", "password", config.ClientVersion))), "Login payload");
                    if (rejectPassword)
                    {
                        Send(login, seed, SoeCodec.Data(0, Encode(new ErrorPacket("Wrong Password", "The password you entered was incorrect."))));
                        done.Wait(5000);
                        return;
                    }

                    Check(ReceiveData(login, seed).SequenceEqual(first), "Reliable retry");
                    Send(login, seed, new byte[] { 0, 21, 0, 0 });
                    var token = Encode(new LoginClientTokenPacket(Enumerable.Repeat((byte)'x', 64).ToArray(), 42, 42, "test"));
                    var names = Encode(new LoginEnumClusterPacket(new[] { new ClusterNameEntry(7, "Test Galaxy") }));
                    var status = Encode(new LoginClusterStatusPacket(new[] { new ClusterStatusEntry(7, "127.0.0.1", zonePort, 44462, population: 0, reserved1: 0, reserved2: 0, reserved3: 0) }));
                    var characters = Encode(new EnumerateCharacterIdPacket(new[] { new CharacterEntry("Tést", 0x1234u, 0x100000001ul, 7) }));
                    var bundle = new List<byte>
                    {
                        0,
                        25
                    };
                    foreach (var b in new[]
                    {
                        token,
                        names,
                        status,
                        characters
                    }

                    )
                    {
                        bundle.Add((byte)b.Length);
                        bundle.AddRange(b);
                    }

                    var firstFragment = Net32((uint)bundle.Count).Concat(bundle.Take(40)).ToArray();
                    Send(login, seed, SoeCodec.Data(1, bundle.Skip(40).ToArray(), 13));
                    Send(login, seed, SoeCodec.Data(0, firstFragment, 13));
                    uint zoneSeed = Session(zone);
                    var id = new ByteReader(ReceiveData(zone, zoneSeed));
                    Check(id.U16() == 3 && id.U32() == 0xd5899226 && id.U32() == 0 && id.U32() == 68, "Zone ID header");
                    Check(id.Take(64).All(c => c == 'x') && id.U32() == 42 && id.Ascii() == config.ClientVersion, "Zone token/version");
                    Send(zone, zoneSeed, new byte[] { 0, 21, 0, 0 });
                    Send(zone, zoneSeed, SoeCodec.Data(0, Encode(new ClientPermissionsPacket(!denyLogin, !denyCreation))));
                    if (!denyLogin)
                    {
                        Check(ReceiveData(zone, zoneSeed).SequenceEqual(Encode(new SelectCharacterPacket(0x100000001ul))), "Queued character selection after login handoff");
                        Check(ReceiveData(zone, zoneSeed).SequenceEqual(Encode(new CmdSceneReadyPacket())), "Generic queue packet ordering");
                        delivered.Set();
                        if (afterReady != null)
                        {
                            Check(disconnectRequested.Wait(3000), "Remote disconnect was not requested");
                            Send(zone, zoneSeed, new byte[] { 0, 5 });
                        }
                    }

                    done.Wait(5000);
                });
                try
                {
                    using (var connection = new LoginConnection(config, "test", "password"))
                    {
                        DateTime deadline = DateTime.UtcNow.AddSeconds(6);
                        bool result = false;
                        while (DateTime.UtcNow < deadline && !result)
                        {
                            if (connection.Events.TryDequeue(out var e))
                            {
                                if (e.Failed)
                                {
                                    Check(rejectPassword || denyLogin, e.Status);
                                    if (rejectPassword)
                                        Check(e.Status.Contains("Wrong Password"), e.Status);
                                    result = true;
                                }
                                else if (e.Admission != null)
                                {
                                    Check(!rejectPassword && !denyLogin, "Authenticated a rejected account");
                                    Check(e.Admission.CanLogin && e.Admission.CanCreate == !denyCreation, "Login and creation permissions were conflated");
                                    Check(e.Admission.Username == "test" && e.Admission.AccountId == 42 && e.Admission.Galaxy.Name == "Test Galaxy" && e.Admission.Characters[0].Name == "Tést" && e.Admission.Characters[0].SwgId == 0x100000001ul, "Admission fields");
                                    using (var game = connection.TakeGameConnection())
                                    {
                                        connection.Dispose(); // The admitted zone connection now has its own lifetime.
                                        game.Start(notificationContext ?? SynchronizationContext.Current ?? new SynchronizationContext());
                                        game.Send(new SelectCharacterPacket(0x100000001ul));
                                        game.Send(new CmdSceneReadyPacket());
                                        Check(delivered.Wait(3000), "Game connection did not send queued packets after login disposed");
                                        afterReady?.Invoke(game, () => disconnectRequested.Set());
                                    }

                                    result = true;
                                }
                            }
                            else
                                Thread.Sleep(5);
                        }

                        Check(result, "Handshake timed out");
                    }
                }
                finally
                {
                    done.Set();
                }

                server.GetAwaiter().GetResult();
            }
        }

        static string FindSettings()
        {
            var candidates = new[]
            {
                Path.Combine(Directory.GetParent(Application.dataPath).FullName, "client.ini"),
                Path.Combine(Environment.CurrentDirectory, "client.ini"),
                Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../client.ini")),
                Path.GetFullPath("../client.ini")
            };
            return candidates.First(File.Exists);
        }

        static void OriginalAssets()
        {
            var settings = ClientSettings.From(FindSettings());
            using (var assets = new TreAssetLoader(settings.TreDirectory, settings.TreArchivePriorities))
            {
                Check(assets.ArchiveCount > 0, "No archives");
                var data = new CharacterCreationData(assets);
                Check(data.Species.Count == 10, "Species count");
                foreach (var species in data.Species)
                    foreach (string path in new[]
                    {
                        species.Male,
                        species.Female
                    }

                    )
                    {
                        var parts = SkeletalAppearanceLoader.Load(assets, path);
                        Check(parts.Count > 0 && parts.Sum(p => p.Triangles.Length) > 300, "Empty original model");
                    }

                double intro = MusicLoader.Load(assets, "music/intro.mp3").Duration;
                double title = MusicLoader.Load(assets, "sound/music_main_title.snd").Duration;
                Check(intro > 90 && intro < 92 && title > 115 && title < 117, "Audio decode duration");
                Console.WriteLine("  " + assets.ArchiveCount + " archives; 20 native mesh combinations; intro " + intro + "s; title " + title + "s");
            }
        }

        static byte[] Encode<T>(in T packet)
            where T : struct, IPacket
        {
            using (var writer = new PacketWriter())
                return writer.Encode(in packet);
        }
    }
}
