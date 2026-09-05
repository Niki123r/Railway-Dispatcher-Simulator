#if UNITY_EDITOR
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Splines;

public static class DrawArrow
{
    public static void ForGizmo(Vector3 pos, Vector3 direction, float arrowHeadLength = 0.25f, float arrowHeadAngle = 20.0f)
    {
        Gizmos.DrawRay(pos, direction);

        Vector3 right = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 + arrowHeadAngle, 0) * new Vector3(0, 0, 1);
        Vector3 left = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 - arrowHeadAngle, 0) * new Vector3(0, 0, 1);
        Gizmos.DrawRay(pos + direction, right * arrowHeadLength);
        Gizmos.DrawRay(pos + direction, left * arrowHeadLength);
    }

    public static void ForGizmo(Vector3 pos, Vector3 direction, Color color, float arrowHeadLength = 0.25f, float arrowHeadAngle = 20.0f)
    {
        Gizmos.color = color;
        Gizmos.DrawRay(pos, direction);

        Vector3 right = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 + arrowHeadAngle, 0) * new Vector3(0, 0, 1);
        Vector3 left = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 - arrowHeadAngle, 0) * new Vector3(0, 0, 1);
        Gizmos.DrawRay(pos + direction, right * arrowHeadLength);
        Gizmos.DrawRay(pos + direction, left * arrowHeadLength);
    }

    public static void ForDebug(Vector3 pos, Vector3 direction, float arrowHeadLength = 0.25f, float arrowHeadAngle = 20.0f)
    {
        Debug.DrawRay(pos, direction);

        Vector3 right = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 + arrowHeadAngle, 0) * new Vector3(0, 0, 1);
        Vector3 left = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 - arrowHeadAngle, 0) * new Vector3(0, 0, 1);
        Debug.DrawRay(pos + direction, right * arrowHeadLength);
        Debug.DrawRay(pos + direction, left * arrowHeadLength);
    }
    public static void ForDebug(Vector3 pos, Vector3 direction, Color color, float arrowHeadLength = 0.25f, float arrowHeadAngle = 20.0f)
    {
        Debug.DrawRay(pos, direction, color);

        Vector3 right = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 + arrowHeadAngle, 0) * new Vector3(0, 0, 1);
        Vector3 left = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 - arrowHeadAngle, 0) * new Vector3(0, 0, 1);
        Debug.DrawRay(pos + direction, right * arrowHeadLength, color);
        Debug.DrawRay(pos + direction, left * arrowHeadLength, color);
    }
}

[CustomEditor(typeof(Signal), true)]
[InitializeOnLoad]
public class SignalEditor : Editor
{
    [DrawGizmo(GizmoType.InSelectionHierarchy | GizmoType.NotInSelectionHierarchy)]
    static void DrawHandles(Signal t, GizmoType gizmoType)
    { 
        if(t.rail == null)
        {
            return;
        }

        Color[] colors = {Color.red, Color.yellow, Color.green};

        Spline spline = t.rail[t.splineIndex];

        NativeSpline native = new NativeSpline(spline);

        Vector3 offset = new Vector3(0, 0.3f, 0);

        float distance = SplineUtility.GetNearestPoint(native, t.transform.position, out float3 nearest, out float pos, Constants.SPLINE_RESOLUTION);

        Vector3 nearestVec = (Vector3)nearest;

        Color color = colors[(int)t.SignalState];

        Handles.color = color;
        Handles.DrawLine(t.transform.position, nearestVec + offset);

        Vector3 forward = Vector3.Normalize(native.EvaluateTangent(pos));

        forward *= t.direction;

        float length = (t.transform.position - nearestVec + offset).magnitude;

        DrawArrow.ForDebug(nearestVec + offset, forward * 2f, color: color);
    }
    [DrawGizmo(GizmoType.InSelectionHierarchy)]
    static void DrawPath(Signal t, GizmoType gizmoType)
    {
        Vector3 offset = new Vector3(0, 0.3f, 0);
        Color[] colors = { Color.red, Color.yellow, Color.green };
        Color color = colors[(int)t.SignalState];
        if (t.rail == null)
        {
            return;
        }

        Signal[] signals = GameObject.FindObjectsByType<Signal>(FindObjectsSortMode.None);

        Spline spline = t.rail[t.splineIndex];
        NativeSpline native = new NativeSpline(spline);
        float distance = SplineUtility.GetNearestPoint(native, t.transform.position, out float3 nearest, out float pos, Constants.SPLINE_RESOLUTION);

        int oldSplineIndex = t.splineIndex;

        float distanceOffset = 0;

        float previousKnot = spline.ConvertIndexUnit(pos, PathIndexUnit.Normalized, PathIndexUnit.Knot);

        for(int i = 0; i < 3000; i += 10)
        {
            float index = spline.ConvertIndexUnit(pos, PathIndexUnit.Normalized, PathIndexUnit.Distance);

            float knot = spline.ConvertIndexUnit(index + distanceOffset, PathIndexUnit.Distance, PathIndexUnit.Knot);

            Vector3 forward = Vector3.Normalize(native.EvaluateTangent(spline.ConvertIndexUnit(index + distanceOffset, PathIndexUnit.Distance, PathIndexUnit.Normalized)));

            Vector3 position = native.GetPointAtLinearDistance(pos, distanceOffset, out float newPos);

            foreach (var item in signals)
            {
                bool previousFound = false;
                foreach (var previous in item.previousSignals)
                {
                    if(previous == t.gameObject)
                    {
                        previousFound = true;
                        break;
                    }
                }
                if (item != t && item.direction == t.direction && previousFound && Vector3.Distance(item.transform.position, position) < 10)
                {
                    return;
                }
            }

            if (newPos == 1 || newPos == 0 || Mathf.FloorToInt(previousKnot) != Mathf.FloorToInt(knot))
            {
                int knotIndex = Mathf.RoundToInt(spline.ConvertIndexUnit(newPos, PathIndexUnit.Normalized, PathIndexUnit.Knot));

                int newSplineIndex = Helpers.GetJunction(spline, knotIndex);
                int? junctionDirection = Helpers.GetJunctionDirection(spline, knotIndex);

                if(newSplineIndex != oldSplineIndex && junctionDirection.HasValue && junctionDirection != t.direction && newPos != 0 && newPos != 1)
                {
                    return;
                }

                if(newSplineIndex != -1)
                {
                    spline = t.rail[newSplineIndex];
                    native = new NativeSpline(spline);

                    distance = SplineUtility.GetNearestPoint(native, position, out nearest, out pos, Constants.SPLINE_RESOLUTION);

                    float checkPos = spline.ConvertIndexUnit(pos, PathIndexUnit.Normalized, PathIndexUnit.Knot);
                    int checkIndex = Mathf.RoundToInt(checkPos);

                    if (junctionDirection.HasValue && junctionDirection != t.direction)
                    {
                        int splineJump = Helpers.GetJunction(spline, checkIndex);
                        if(splineJump != oldSplineIndex)
                        {
                            return;
                        }
                    }

                    DrawArrow.ForDebug(position + offset, forward * 5f * t.direction, color: color, arrowHeadLength: 2, arrowHeadAngle: 45);

                    distanceOffset = 10 * t.direction;

                    float checkPosDist = spline.ConvertIndexUnit(checkPos, PathIndexUnit.Knot, PathIndexUnit.Distance);
                    previousKnot = spline.ConvertIndexUnit(checkPosDist + distanceOffset, PathIndexUnit.Distance, PathIndexUnit.Knot);

                    oldSplineIndex = newSplineIndex;
                    continue;
                }
            }

            previousKnot = knot;

            distanceOffset += 10 * t.direction;

            DrawArrow.ForDebug(position + offset, forward * 5f * t.direction, color: color, arrowHeadLength: 2, arrowHeadAngle: 45);
        }
    }

    public override void OnInspectorGUI()
    {
        Signal signal = (Signal)target;

        if (GUILayout.Button("Change points"))
        {
            signal.ApplyJunctionData(0);
        }

        base.OnInspectorGUI();
    }
}
#endif