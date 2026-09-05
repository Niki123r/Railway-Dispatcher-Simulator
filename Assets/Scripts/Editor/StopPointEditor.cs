using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Splines;

[CustomEditor(typeof(StopPoint), true)]
public class StopPointEditor : Editor
{
    [DrawGizmo(GizmoType.InSelectionHierarchy)]
    static void DrawHandles(StopPoint t, GizmoType gizmoType)
    {
        Spline spline = t.rail[t.splineIndex];
        NativeSpline native = new NativeSpline(spline);
        SplineUtility.GetNearestPoint(native, t.transform.position, out float3 nearest, out float pos, Constants.SPLINE_RESOLUTION);
        Vector3 axleCounterPos = native.GetPointAtLinearDistance(pos, t.axleCounterOffset, out float newPos);

        Vector3 nearestVec = (Vector3)nearest;

        Color color = Color.aliceBlue;
        Handles.color = color;

        Vector3 offset = new Vector3(0, 0.3f, 0);

        Handles.DrawLine(t.transform.position, nearestVec + offset);

        Vector3 forward = Vector3.Normalize(native.EvaluateTangent(pos));

        forward *= t.direction;

        float length = (t.transform.position - nearestVec + offset).magnitude;

        DrawArrow.ForDebug(nearestVec + offset, forward * 2f, color: color, arrowHeadLength: 0.6f);

        Handles.DrawLine(t.transform.position, axleCounterPos + offset);

        Signal exitSignal = t.exitSignal;

        Handles.color = Color.aquamarine;

        float distanceToSignal = Vector3.Distance(t.transform.position, exitSignal.transform.position);

        DrawArrow.ForDebug(t.transform.position, exitSignal.transform.position - t.transform.position, color: Color.aquamarine, arrowHeadLength: 1, arrowHeadAngle: 45);

        //Handles.DrawLine(t.transform.position, exitSignal.transform.position);
    }
}
