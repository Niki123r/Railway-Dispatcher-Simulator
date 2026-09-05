using System.Collections.Generic;
using Codice.Client.Common.FsNodeReaders;
using NUnit.Framework;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Splines;

[CustomEditor(typeof(TrackCircuit), true)]
public class TrackCircuitEditor : Editor
{
    [DrawGizmo(GizmoType.InSelectionHierarchy)]
    static void DrawPath(TrackCircuit t, GizmoType gizmoType)
    {
        if(t.pointIdentifier == null || !t.rail)
        {
            return;
        }

        Spline spline = t.rail[t.pointIdentifier.splineIndex];
        NativeSpline native = new NativeSpline(spline);
        float distance = SplineUtility.GetNearestPoint(native, t.transform.position, out float3 nearest, out float pos, Constants.SPLINE_RESOLUTION);

        float distanceOffset = spline.ConvertIndexUnit(t.pointIdentifier.knotIndex, PathIndexUnit.Knot, PathIndexUnit.Distance);

        Vector3 to = native.GetPointAtLinearDistance(0, distanceOffset, out float newPos);

        Handles.color = Color.blue;
        Handles.DrawLine(t.transform.position, to);
    }

    [DrawGizmo(GizmoType.InSelectionHierarchy | GizmoType.NotInSelectionHierarchy)]
    static void DrawTrackCircuit(TrackCircuit t, GizmoType gizmoType)
    {
        Vector3 sum = Vector3.zero;
        Vector3 offset = new Vector3(0, 5f, 0);

        Handles.color = Color.greenYellow;

        foreach (var item in t.axleCounters)
        {
            sum += item.transform.position;
        }

        sum /= t.axleCounters.Length;

        foreach (var item in t.axleCounters)
        {
            Handles.DrawLine(sum + offset, item.transform.position, 3f);
            Handles.Label(sum + offset, t.name);
        }
    }
}
