using UnityEditor;
using Swg.Client.Runtime.Assets.Tre;

namespace Swg.Client.Editor
{
    internal sealed class TreVisualImport : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            foreach (string path in importedAssets)
            {
                var type = AssetDatabase.GetMainAssetTypeAtPath(path);
                if (type != null && typeof(TreVisual).IsAssignableFrom(type))
                {
                    TreEditorAssets.QueueRefresh();
                    return;
                }
            }
        }
    }
}
