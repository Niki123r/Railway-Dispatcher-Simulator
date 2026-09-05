using System.Collections.Generic;
using System;
using UnityEngine;
using System.Linq;
using UnityEngine.Splines;
using Unity.Mathematics;

public class KeyOut
{
    public float slope;
    public float outTan;
}

public static class Helpers
{
    public static void PrintList<T>(IList<T> list)
    {
        T[] arr = list.ToArray();

        String data = "";

        for (int i = 0; i < arr.Length; i++)
        {
            if (i > 0)
            {
                data += "\n";
            }
            data = data + arr[i];
        }
        Debug.Log(arr.Length + " entries: " + data);
    }

    public static Vector3 GetNearestSplinePosition(Spline spline, Vector3 position)
    {
        NativeSpline native = new NativeSpline(spline);

        Vector3 offset = new Vector3(0, 0.3f, 0);

        float distance = SplineUtility.GetNearestPoint(native, position, out float3 nearest, out float pos, Constants.SPLINE_RESOLUTION);

        return (Vector3)nearest;
    }

    public static Vector3 GetNearestSplinePosition(Spline spline, Vector3 position, out float normalized)
    {
        NativeSpline native = new NativeSpline(spline);

        Vector3 offset = new Vector3(0, 0.3f, 0);

        float distance = SplineUtility.GetNearestPoint(native, position, out float3 nearest, out float pos, Constants.SPLINE_RESOLUTION);

        normalized = pos;

        return (Vector3)nearest;
    }

    public static int GetJunction(Spline spline, int knotIndex)
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

    public static int? GetJunctionDirection(Spline spline, int knotIndex)
    {
        if (spline.TryGetIntData("junctionDirections", out SplineData<int> junctions))
        {
            for (int i = 0; i < junctions.Count; i++)
            {
                if (junctions[i].Index == knotIndex)
                {
                    return junctions[i].Value;
                }
            }
        }
        return null;
    }

    public static KeyOut CalculateKeyframeSlopes(Keyframe keyframeA, Keyframe keyframeB)
    {
        KeyOut result = new KeyOut();

        float keyDistance = keyframeB.time - keyframeA.time;
        float speedB = keyframeB.value;
        float speedA = keyframeA.value;
        float slope = (speedB - speedA) / (keyDistance);

        speedA *= Constants.KMHtoMS;
        speedB *= Constants.KMHtoMS;

        float coeff = -(speedB - speedA) / (speedA + speedB);

        float outTan = 0;
        if(speedB < speedA)
        {
            outTan = (speedA - Mathf.Sqrt(coeff * keyDistance / 2)) / (keyDistance / 2);
        }

        result.slope = slope;
        result.outTan = outTan;

        return result;
    }

    public static void AddSplineData(float normalizedPosition, Spline spline, UnityEngine.Object gameObject)
    {
        float index = SplineUtility.ConvertIndexUnit(spline, normalizedPosition, PathIndexUnit.Normalized, PathIndexUnit.Knot);

        DataPoint<UnityEngine.Object> newPoint = new DataPoint<UnityEngine.Object>(index, gameObject);

        if(spline.TryGetObjectData("signal", out SplineData<UnityEngine.Object> data))
        {
            data.Add(newPoint);
        } else
        {
            SplineData<UnityEngine.Object> newData = new SplineData<UnityEngine.Object>();

            newData.Add(newPoint);

            spline.SetObjectData("signal", newData);
        }
    }
}
