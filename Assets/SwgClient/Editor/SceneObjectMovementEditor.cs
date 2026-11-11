using Swg.Client.Runtime.World;
using UnityEditor;

namespace Swg.Client.Editor
{
    [CustomEditor(typeof(SceneObjectMovement))]
    [CanEditMultipleObjects]
    public sealed class SceneObjectMovementEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            bool mixed = false;
            var swgId = ((SceneObjectMovement)target).SwgId;
            foreach (SceneObjectMovement movement in targets)
                mixed |= movement.SwgId != swgId;
            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.TextField("Swg Id", mixed ? "—" : swgId.ToString());
        }
    }
}
