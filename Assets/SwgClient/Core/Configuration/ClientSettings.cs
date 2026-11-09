using System;
using System.Collections.Generic;
using System.IO;

namespace Swg.Client.Core.Configuration
{
    public sealed class ClientSettings
    {
        public string GameDirectory
        {
            get; private set;
        }
        public string TreeConfig
        {
            get; private set;
        }
        public string LoginHost
        {
            get; private set;
        }
        public string ClientVersion
        {
            get; private set;
        }
        public string ZoneHost
        {
            get; private set;
        }
        public ushort LoginPort
        {
            get; private set;
        }
        public uint GalaxyId
        {
            get; private set;
        }
        public bool SkipIntro
        {
            get; private set;
        }

        public string LoginEndpoint
        {
            get
            {
                return LoginHost + ":" + LoginPort;
            }
        }

        public static ClientSettings From(string filename)
        {
            var values = Parse(File.ReadAllText(filename));
            var s = new ClientSettings();
            string Get(string group, string key, string fallback)
            {
                return values.TryGetValue(group + "/" + key, out var value) && value.Length > 0 ? value : fallback;
            }

            s.GameDirectory = Get("assets", "directory", s.GameDirectory);
            if (!Path.IsPathRooted(s.GameDirectory))
                s.GameDirectory = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(Path.GetFullPath(filename)), s.GameDirectory));
            s.TreeConfig = Get("assets", "config", s.TreeConfig);
            s.LoginHost = Get("network", "loginserveraddress", s.LoginHost);
            s.LoginPort = ushort.Parse(Get("network", "loginserverport", "44453"));
            if (s.LoginPort == 0)
                throw new InvalidDataException("LoginServerPort must be 1..65535");
            s.ClientVersion = Get("network", "clientversion", s.ClientVersion);
            s.GalaxyId = uint.Parse(Get("network", "galaxyid", "0"));
            s.ZoneHost = Get("network", "zonehost", "");
            string skip = Get("startup", "skipintro", "false").ToLowerInvariant();
            if (skip != "true" && skip != "false" && skip != "1" && skip != "0")
                throw new InvalidDataException("SkipIntro must be true or false");
            s.SkipIntro = skip == "true" || skip == "1";
            return s;
        }

        internal static Dictionary<string, string> Parse(string text)
        {
            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            string section = "";
            foreach (string raw in text.TrimStart('\ufeff').Split('\n'))
            {
                string line = raw.Trim();
                if (line.Length == 0 || line[0] == ';' || line[0] == '#')
                {
                    continue;
                }

                if (line.StartsWith("[") && line.EndsWith("]"))
                {
                    section = line.Substring(1, line.Length - 2).Trim().ToLowerInvariant();
                    continue;
                }

                int eq = line.IndexOf('=');
                if (eq < 1)
                    throw new InvalidDataException("Invalid INI line: " + line);
                values[section + "/" + line.Substring(0, eq).Trim().ToLowerInvariant()] = line.Substring(eq + 1).Trim().Trim('"');
            }

            return values;
        }
    }
}
