using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TrainController))]
public class TrainControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        TrainController train = (TrainController)target;

        if(GUILayout.Button("Update child bogie spline containers"))
        {
            train.UpdateChildBogieSplineContainer();
        }

        base.OnInspectorGUI();
    }
}
