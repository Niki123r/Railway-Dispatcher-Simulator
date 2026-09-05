#if UNITY_EDITOR
using System;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;
using UnityEditor;

public class SplineEditor : MonoBehaviour
{
    public SplineContainer rail;

    public void GetSplineContainer()
    {
        rail = GetComponent<SplineContainer>();
    }
}

[CustomEditor(typeof(SplineEditor))]
public class SplineGizmos : Editor
{

    private SplineContainer rail;
    public void OnSceneGUI()
    {
        var t = target as SplineEditor;
        t.GetSplineContainer();
        rail = t.rail;

        foreach (var spline in rail.Splines)
        {
            BezierKnot[] knots = spline.Knots.ToArray();
            for (int i = 0; i < knots.Length; i++)
            {
                int newIndex = GetJunction(spline, i);

                if(newIndex == -1)
                {
                    continue;
                }
                float3 pos1 = knots[i].Position;
                int otherKnot = GetIndex(pos1, newIndex);

                otherKnot = (otherKnot == 0) ? otherKnot + 1 : otherKnot - 1;
                float3 pos2 = rail.Splines[newIndex].Knots.ToArray()[otherKnot].Position;

                float3 offset = new float3(0, 2, 0);
                Handles.color = Color.yellow;
                Handles.DrawLine(pos1 + offset, pos2 + offset);
            }
        }
    }

    int GetIndex(float3 position, int splineIndex)
    {
        Spline currentSpline = rail.Splines[splineIndex];
        NativeSpline native = new NativeSpline(currentSpline);

        float distance = SplineUtility.GetNearestPoint(native, position, out _, out float newPos, Constants.SPLINE_RESOLUTION);
        float index = currentSpline.ConvertIndexUnit(newPos, PathIndexUnit.Normalized, PathIndexUnit.Knot);

        return Mathf.RoundToInt(index);
    }

    public int GetJunction(Spline spline, int knotIndex)
    {
        if (spline.TryGetIntData("junctions", out SplineData<int> junctions))
        {
            for (int i = 0; i < junctions.Count; i++)
            {
                if (junctions[i].Index == knotIndex)
                {
                    return junctions[i].Value;
                }
            }
        }
        return -1;
    }
}
#endif