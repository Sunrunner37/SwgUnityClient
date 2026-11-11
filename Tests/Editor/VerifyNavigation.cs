using Swg.Client.Core;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using Swg.Client.Core.Configuration;
using Swg.Client.Core.Networking.Login;
using Swg.Client.Runtime;
using Swg.Client.Runtime.UI.Screens;
using Swg.Client.Runtime.UI.Screens.CharacterCreation;
using Swg.Client.Runtime.UI.Screens.CharacterList;
using Swg.Client.Runtime.UI.Screens.Login;

namespace Swg.Client.Tests.Editor
{
    public static class VerifyNavigation
    {
        static async Task Until(Func<bool> check)
        {
            var deadline = DateTime.UtcNow.AddSeconds(15);
            while (!check())
            {
                if (DateTime.UtcNow > deadline)
                    throw new TimeoutException("Screen transition timed out");
                await Task.Delay(25);
            }
        }

        static void Check(bool value, string message)
        {
            if (!value)
                throw new Exception(message);
        }

        public static async Task<string> Run()
        {
            var login = UnityEngine.Object.FindAnyObjectByType<LoginScreen>();
            Check(login != null && login.IsReady, "Login must be ready");
            login.username.text = "";
            login.password.text = "";
            login.Submit();
            Check(login.status.text == "Enter your username and password.", "Empty form validation changed");
            var runtime = UnityEngine.Object.FindAnyObjectByType<ClientRuntime>();
            var music = runtime.Music.GetComponent<AudioSource>();
            await Until(() => music.isPlaying);
            var clip = music.clip;
            await Scenario(runtime, 1, true);
            await Scenario(runtime, 0, true);
            await Scenario(runtime, 1, false);
            Check(music.clip == clip && music.isPlaying, "Title music did not persist");
            return "PAGE_NAVIGATION_OK: populated/empty accounts always reach list; permissions gate Create New; creation Back preserves session; logout cleans up.";
        }

        static async Task Scenario(ClientRuntime runtime, int count, bool canCreate)
        {
            var login = UnityEngine.Object.FindAnyObjectByType<LoginScreen>();
            using (var local = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0)))
            {
                string ini = Path.GetTempFileName();
                ClientSettings settings;
                try
                {
                    File.WriteAllText(ini, "[Assets]\nTreDirectory=.\n[Network]\nLoginServerAddress=127.0.0.1\nLoginServerPort=" + ((IPEndPoint)local.Client.LocalEndPoint).Port + "\nClientVersion=20050408-18:00\n");
                    settings = ClientSettings.From(ini);
                }
                finally
                {
                    File.Delete(ini);
                }

                // Editor-only handoff fixture; full wire authentication is covered by the core checks.
                var connection = new LoginConnection(settings, "fixture", "fixture");
                var stop = (CancellationTokenSource)typeof(LoginConnection).GetField("stop", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(connection);
                try
                {
                    typeof(LoginScreen).GetField("connecting", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(login, connection);
                    var characters = new List<Swg.Client.Core.Networking.Login.CharacterInfo>();
                    for (int i = 0; i < count; i++)
                        characters.Add(new Swg.Client.Core.Networking.Login.CharacterInfo { Name = "Fixture " + i, Galaxy = 1, SwgId = (ulong)(100 + i) });
                    var admission = new Admission
                    {
                        AccountId = 42,
                        Galaxy = new Galaxy
                        {
                            Id = 1,
                            Name = "Screen handoff fixture"
                        },
                        Characters = characters,
                        CanLogin = true,
                        CanCreate = canCreate
                    };
                    connection.Events.Enqueue(new ClientEvent { Admission = admission });
                    await Until(() => UnityEngine.Object.FindAnyObjectByType<CharacterListScreen>()?.IsReady == true);
                    var list = UnityEngine.Object.FindAnyObjectByType<CharacterListScreen>();
                    Check(list.CharacterCount == count, "Character count differs from admission");
                    Check(UnityEngine.Object.FindAnyObjectByType<CreationScreen>() == null, "Login opened creation instead of the list");
                    Check(list.create.interactable == canCreate, "Creation permission was ignored");
                    Check(runtime.Session.Admission == admission && !stop.IsCancellationRequested, "Accepted session did not survive Login unload");
                    Check(UnityEngine.Object.FindObjectsByType<ClientScreen>().Length == 1, "Multiple screens loaded");
                    if (canCreate)
                    {
                        list.create.onClick.Invoke();
                        await Until(() => UnityEngine.Object.FindAnyObjectByType<CreationScreen>()?.IsReady == true);
                        var creation = UnityEngine.Object.FindAnyObjectByType<CreationScreen>();
                        Check(!creation.next.interactable, "Unimplemented submission became enabled");
                        creation.SelectSpecies(1);
                        creation.ToggleGender();
                        Check(creation.speciesName.text == "Rodian" && creation.preview.GetComponentsInChildren<MeshRenderer>().Length > 0, "Species preview failed");
                        creation.Back();
                        await Until(() => UnityEngine.Object.FindAnyObjectByType<CharacterListScreen>()?.IsReady == true);
                        Check(runtime.Session.Admission == admission && !stop.IsCancellationRequested, "Back from creation disconnected the session");
                    }
                    else
                    {
                        list.CreateNew();
                        await Task.Delay(100);
                        Check(UnityEngine.Object.FindAnyObjectByType<CharacterListScreen>() != null, "Denied creation permission bypassed");
                    }

                    UnityEngine.Object.FindAnyObjectByType<CharacterListScreen>().Back();
                    await Until(() => UnityEngine.Object.FindAnyObjectByType<LoginScreen>()?.IsReady == true);
                    Check(stop.IsCancellationRequested && runtime.Session.Admission == null, "Logout did not dispose the session");
                }
                finally
                {
                    connection.Dispose();
                }
            }
        }
    }
}
