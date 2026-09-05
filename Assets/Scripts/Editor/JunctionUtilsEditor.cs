using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(JunctionUtils))]
public class JunctionUtilsEditor : Editor
{
    public override void OnInspectorGUI()
    {
        JunctionUtils script = (JunctionUtils)target;

        if (GUILayout.Button("Update junctions"))
        {
            script.UpdateJunctionParamaters();
        }

        base.OnInspectorGUI();
    }
}
