using UnityEditor;

namespace Editor
{
    [CustomEditor(typeof(PlanePartHitbox))]
    public class CustomPlaneHitboxInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            PlanePartHitbox script = (PlanePartHitbox)target;
            script.type = (PlanePartType)EditorGUILayout.EnumPopup("Part Type", script.type);
        }
    }
}