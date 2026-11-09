using System.IO;

namespace Swg.Client.Core.Networking
{
    // Use constrained generic calls (not IPacket variables) to avoid boxing packet structs.
    public interface IPacket
    {
        void WriteTo(BinaryWriter writer);
    }
}
