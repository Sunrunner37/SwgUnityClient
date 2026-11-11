namespace Swg.Client.Core.World
{
    public sealed class StaticMeshSource
    {
        public string AppearancePath;
        public string ShaderPath;
        public string DiffusePath;
        // Hint follows native effect naming; unsupported multi-pass effects are not recreated here.
        public bool AlphaCutoutHint;
        public byte[] VertexColors;
    }
}
