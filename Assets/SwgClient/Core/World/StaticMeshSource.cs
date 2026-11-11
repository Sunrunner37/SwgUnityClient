namespace Swg.Client.Core.World
{
    public sealed class StaticMeshSource
    {
        public string AppearancePath;
        public string ShaderPath;
        public string DiffusePath;
        // Taken from the native effect base pass; additional passes are not recreated here.
        public bool AlphaCutoutHint;
        public byte[] VertexColors;
    }
}
