using System.Collections.Generic;

namespace Swg.Client.Core.Networking.Login
{
    public sealed class Admission
    {
        public uint AccountId;
        public Galaxy Galaxy;
        public List<CharacterInfo> Characters;
        public bool CanLogin;
        public bool CanCreate;
    }
}
