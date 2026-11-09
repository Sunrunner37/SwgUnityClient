using System;
using System.IO;
using UnityEngine;
using Swg.Client.Core.Configuration;
using Swg.Client.Runtime.Assets.Tre;
using Swg.Client.Runtime.Assets.Audio;
using Swg.Client.Core.Networking;

namespace Swg.Client.Runtime
{
    // Shared service lifetime only. It has no screen references or navigation logic.
    public sealed class ClientRuntime : MonoBehaviour
    {
        static ClientRuntime instance;
        public ClientSettings Settings
        {
            get; private set;
        }
        public TreRuntimeAssets Assets
        {
            get; private set;
        }
        public ClientSession Session
        {
            get; private set;
        }
        public ClientMusic Music
        {
            get; private set;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() => instance = null;
        public static ClientRuntime Ensure()
        {
            if (instance != null)
                return instance;
            string ini = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "client.ini");
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i + 1 < args.Length; i++)
                if (args[i] == "--settings")
                    ini = args[i + 1];
            var settings = ClientSettings.From(ini);
            var root = new GameObject("Client services");
            instance = root.AddComponent<ClientRuntime>();
            instance.Settings = settings;
            instance.Assets = new TreRuntimeAssets(settings);
            instance.Session = new ClientSession();
            instance.Music = root.AddComponent<ClientMusic>();
            root.GetComponent<AudioSource>().playOnAwake = false;
            DontDestroyOnLoad(root);
            Application.runInBackground = true;
            return instance;
        }

        void Update() => Session?.Poll();
        void OnDestroy()
        {
            Session?.Dispose();
            Assets?.Dispose();
            if (instance == this)
                instance = null;
        }
    }
}
