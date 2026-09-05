using UnityEditor;
using UnityEngine;
using UnityEngine.Splines;

[CustomEditor(typeof(Spawner), true)]
public class SpawnerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        Spawner script = (Spawner)target;

        if (GUILayout.Button("Snap to spline"))
        {
            script.SnapToSpline();
        }

        if (GUILayout.Button("Spawn train"))
        {
            script.SpawnTrain();
        }

        base.OnInspectorGUI();
    }
}
