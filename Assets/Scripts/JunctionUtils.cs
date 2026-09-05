using System;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

public class JunctionUtils : MonoBehaviour
{
    public void UpdateJunctionParamaters()
    {
        SplineContainer rail = GetComponent<SplineContainer>();

        if(!rail) return;

        foreach(Spline spline in rail.Splines)
        {
            spline.RemoveIntData("junctionDirections");
            if (spline.TryGetIntData("junctions", out SplineData<int> junctions))
            {
                foreach(var junction in junctions)
                {
                    int direction = 0;

                    if(junction.Index == 0)
                    {
                        direction = 1;
                    }
                    else if(junction.Index == spline.Count - 1)
                    {
                        direction = -1;
                    }
                    else
                    {
                        int currentKnotIndex = (int)junction.Index;
                        int containerIndex = junction.Value;

                        Spline other = FindNearestKnotSpline(rail, spline.Knots.ToArray()[currentKnotIndex], spline);
                        NativeSpline native = new NativeSpline(other);

                        float3 currentPos = spline.Knots.ToArray()[currentKnotIndex].Position;

                        float distance = SplineUtility.GetNearestPoint(native, currentPos, out _, out float newPos, Constants.SPLINE_RESOLUTION);
                        int index = Mathf.RoundToInt(other.ConvertIndexUnit(newPos, PathIndexUnit.Normalized, PathIndexUnit.Knot));

                        if(index == 0)
                        {
                            direction = 1;
                        }
                        else if (index == other.Count - 1)
                        {
                            direction = -1;
                        }
                    }

                    DataPoint<int> newPoint = new DataPoint<int>(junction.Index, direction);
                    if (spline.TryGetIntData("junctionDirections", out SplineData<int> data))
                    {
                        data.Add(newPoint);
                    }
                    else
                    {
                        SplineData<int> newData = new SplineData<int>();
                        newData.Add(newPoint);
                        spline.SetIntData("junctionDirections", newData);
                    }
                }
            }
        }
    }

    public Spline? FindNearestKnotSpline(SplineContainer container, BezierKnot other, Spline otherSpline)
    {
        float distance = float.PositiveInfinity;

        Spline? nearest = null;

        foreach (Spline spline in container.Splines)
        {
            if(otherSpline == spline)
            {
                continue;
            }
            foreach(BezierKnot knot in spline.Knots)
            {
                float newDistance = Vector3.Distance(knot.Position, other.Position);

                if(newDistance < distance)
                {
                    nearest = spline;
                    distance = newDistance;
                }
            }
        }
        return nearest;
    }
}
