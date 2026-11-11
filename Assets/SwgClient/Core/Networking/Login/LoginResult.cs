using System.Collections.Generic;
using System.Linq;
using Swg.Client.Core.Networking.Login.Packets;

namespace Swg.Client.Core.Networking.Login
{
    public readonly struct LoginResult
    {
        public readonly uint AccountId;
        public readonly uint StationId;
        public readonly string Username;
        public readonly byte[] Token;
        public readonly List<Galaxy> Galaxies;
        public readonly List<CharacterInfo> Characters;
        internal LoginResult(LoginClientTokenPacket token, LoginEnumClusterPacket clusters, LoginClusterStatusPacket status, EnumerateCharacterIdPacket characters)
        {
            AccountId = token.AccountId;
            StationId = token.StationId;
            Username = token.Username;
            Token = token.Token;
            var names = new Dictionary<uint, string>();
            foreach (var cluster in clusters.Clusters)
                names[cluster.Id] = cluster.Name;
            Galaxies = status.Clusters.Select(cluster => new Galaxy { Id = cluster.Id, Name = names.TryGetValue(cluster.Id, out var name) ? name : "Galaxy " + cluster.Id, Address = cluster.Address, Port = cluster.Port, Status = cluster.Status }).ToList();
            Characters = characters.Characters.Select(character => new CharacterInfo { Name = character.Name, Race = character.Race, SwgId = character.SwgId, Galaxy = character.GalaxyId }).ToList();
        }
    }
}
