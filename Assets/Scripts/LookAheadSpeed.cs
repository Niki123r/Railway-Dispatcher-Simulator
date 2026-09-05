using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Splines;

public enum SpeedEntryType
{
    SpeedLimit,
    Signal,
    StopPosition,
}

public class SpeedEntry : IComparable<SpeedEntry>, IEquatable<SpeedEntry>
{
        public float distance, limit, position, endPosition;
        public int knotIndex;
        public Spline spline;
        public bool leadingCleared, trailingCleared;
        public SpeedEntryType type;
        public SpeedEntry(float distance, float limit, int knotIndex, Spline spline, float position, float endPosition)
        {
            this.distance = distance;
            this.limit = limit;
            this.knotIndex = knotIndex;
            this.spline = spline;
            this.position = position;
            this.endPosition = endPosition;
            leadingCleared = false;
            trailingCleared = false;
            type = SpeedEntryType.SpeedLimit;
        }

        public SpeedEntry(float distance, float limit, Spline spline, SpeedEntryType type)
        {
            this.distance = distance;
            this.limit = limit;
            this.spline = spline;
            leadingCleared = false;
            trailingCleared = false;
            this.type = type;
        }

        public int CompareTo(SpeedEntry other)
        {
            if(other == null) return 0;
            if(other.knotIndex == knotIndex && other.spline == spline)
            {
                return 0;
            }
            else
            {
                return 1;
            }
        }

        public bool Equals(SpeedEntry other)
        {
            if (this.CompareTo(other) == 0) return true;
            return false;
        }

        public override int GetHashCode()
        {
            int hash = 17;

            hash += 31 * knotIndex;
            hash += 31 * spline.GetHashCode();

            return hash;
        }

    override public string ToString()
        {
            return "(distance: " + distance + ", speed: " + limit + ", spline: " + spline.GetHashCode() + ", knot: " + this.knotIndex + ", position: " + position + ", endpos: " + endPosition + "; " + leadingCleared + ":" + trailingCleared + ")";
        }
}

public class KnotInfo
{
    public float distance;
    public int splineIndex;

    public KnotInfo(float distance, int splineIndex)
    {
        this.distance = distance;
        this.splineIndex = splineIndex;
    }

}

public class LookAheadSpeed
{
    private SplineContainer rail;
    private float accelForce, decelForce, trainLength;

    private List<SpeedEntry> speedLimits;

    private float maxLookAheadDistance;

    private HashSet<SpeedEntry> unclearedLimits = new();

    private AnimationCurve brakingIntensityCurve;

    private Service timetable;

    public LookAheadSpeed(SplineContainer rail, float accelForce, float decelForce, float trainLength, AnimationCurve brakingIntensityCurve, Service timetable) {
        this.rail = rail;
        this.accelForce = accelForce;
        this.decelForce = decelForce;
        this.trainLength = trainLength;
        this.brakingIntensityCurve = brakingIntensityCurve;
        this.timetable = timetable;
        speedLimits = new();
        maxLookAheadDistance = 3000f;
    }

    public void UpdateSpeedTable(Vector3 position, Spline startSpline, int direction, float trainSpeed, ref AnimationCurve speedCurve, ref List<SpeedEntry> unclearedLimits)
    {
        if(direction != 1 && direction != -1)
        {
            return;
        }
        Func<float, int> roundToInt;

        if(direction == 1)
        {
            roundToInt = Mathf.FloorToInt;
        }
        else
        {
            roundToInt = Mathf.CeilToInt;
        }

            speedLimits.Clear();

        Spline currentSpline = startSpline;
        NativeSpline native = new NativeSpline(currentSpline);
        float distance = SplineUtility.GetNearestPoint(native, position, out float3 nearest, out float t, Constants.SPLINE_RESOLUTION);
        float index = currentSpline.ConvertIndexUnit(t, PathIndexUnit.Normalized, PathIndexUnit.Knot);
        float currentDistance = currentSpline.ConvertIndexUnit(t, PathIndexUnit.Normalized, PathIndexUnit.Distance);
        int currentKnotIndex = roundToInt(index);
        int startIndex = currentKnotIndex;

        int currentSplineIndex = 0;

        float cumulativeDistance = 0f;
        float previousDistance = currentDistance;

        float currentLimit;

        bool junctionTaken = false;

        for(int i = 0; i < 10; i++)
        {
            int newIndex = GetJunction(currentSpline, currentKnotIndex);

            if(newIndex != -1 && (currentKnotIndex != startIndex || junctionTaken))
            {
                junctionTaken = true;
                float3 knotPosition = currentSpline.Knots.ToArray()[currentKnotIndex].Position;

                currentSpline = rail.Splines[newIndex];
                currentSplineIndex = newIndex;

                native = new NativeSpline(currentSpline);
                distance = SplineUtility.GetNearestPoint(native, knotPosition, out _, out float newPos, Constants.SPLINE_RESOLUTION);
                index = currentSpline.ConvertIndexUnit(newPos, PathIndexUnit.Normalized, PathIndexUnit.Knot);

                currentKnotIndex = Mathf.RoundToInt(index);

                currentLimit = GetSpeed(currentSpline, currentKnotIndex, direction);

                cumulativeDistance += (currentDistance - previousDistance) * direction;
                currentDistance = currentSpline.ConvertIndexUnit(newPos, PathIndexUnit.Normalized, PathIndexUnit.Distance);

                float endDistance = currentSpline.ConvertIndexUnit(currentKnotIndex + direction, PathIndexUnit.Knot, PathIndexUnit.Distance);
                SpeedEntry newSpeed = new SpeedEntry(cumulativeDistance, currentLimit, currentKnotIndex, currentSpline, currentDistance, endDistance - direction * Mathf.Abs(currentDistance - endDistance) * 0.05f);
                speedLimits.Add(newSpeed);
                if(unclearedLimits.Contains(newSpeed) == false)
                {
                    unclearedLimits.Add(newSpeed);
                }

                SpeedEntry signal = GetSignals(currentSpline, currentKnotIndex, direction, currentDistance);

                if(signal != null)
                {
                    speedLimits.Add(new SpeedEntry(signal.distance * direction - currentDistance * direction + cumulativeDistance, 0, currentKnotIndex, currentSpline, currentDistance, endDistance - direction * Mathf.Abs(currentDistance - endDistance) * 0.05f));
                    Debug.Log(speedLimits.Last());
                }

                previousDistance = currentDistance;
                //Debug.Log(index + ", " + currentKnotIndex + ", " + previousDistance + ", " + knotPosition);

                currentKnotIndex += direction;
            }
            else {
                currentLimit = GetSpeed(currentSpline, currentKnotIndex, direction);
                SpeedEntry signal = GetSignals(currentSpline, currentKnotIndex, direction, currentDistance);

                cumulativeDistance += (currentDistance - previousDistance) * direction;

                float endDistance = currentSpline.ConvertIndexUnit(currentKnotIndex + direction, PathIndexUnit.Knot, PathIndexUnit.Distance);
                SpeedEntry newSpeed = new SpeedEntry(cumulativeDistance, currentLimit, currentKnotIndex, currentSpline, currentDistance, endDistance - direction * Mathf.Abs(currentDistance - endDistance) * 0.05f);
                speedLimits.Add(newSpeed);

                if(signal != null)
                {
                    speedLimits.Add(new SpeedEntry(signal.distance*direction - currentDistance*direction + cumulativeDistance, 0, currentKnotIndex, currentSpline, currentDistance, endDistance - direction * Mathf.Abs(currentDistance - endDistance) * 0.05f));
                    Debug.Log(speedLimits.Last());
                }

                if (unclearedLimits.Contains(newSpeed) == false)
                {
                    unclearedLimits.Add(newSpeed);
                }

                //Debug.Log(currentKnotIndex + ", " + cumulativeDistance + ", " + currentDistance + ", " + previousDistance);

                currentKnotIndex += direction;
            }

            if(currentKnotIndex > currentSpline.Knots.Count() - 1 || currentKnotIndex < 0)
            {
                break;
            }

            if(Mathf.Abs(cumulativeDistance) > maxLookAheadDistance)
            {
                break;
            }

            previousDistance = currentDistance;
            currentDistance = currentSpline.ConvertIndexUnit(currentKnotIndex, PathIndexUnit.Knot, PathIndexUnit.Distance);
        }

        speedCurve.ClearKeys();

        Helpers.PrintList(speedLimits);

        float currentSpeed = 0f;

        for(int i = 0; i < speedLimits.Count; i++)
        {
            if(i == 0)
            {
                currentSpeed = speedLimits[i].limit;
                speedCurve.AddKey(new Keyframe(speedLimits[i].distance - 1000f, currentSpeed));
            }
            if(i > 0)
            {
                if(speedLimits[i].limit < currentSpeed)
                {
                    float currentSpeedMS = currentSpeed * Constants.KMHtoMS;
                    float speedLimitMS = speedLimits[i].limit * Constants.KMHtoMS;
                    float dist = (currentSpeedMS*currentSpeedMS - speedLimitMS*speedLimitMS) / (2 * 0.7f);
                    speedCurve.AddKey(new Keyframe(speedLimits[i].distance - dist, currentSpeed));
                    speedCurve.AddKey(new Keyframe(speedLimits[i].distance, speedLimits[i].limit));

                    currentSpeed = speedLimits[i].limit;
                }
                else if (speedLimits[i].limit > currentSpeed)
                {
                    speedCurve.AddKey(new Keyframe(speedLimits[i].distance + trainLength + 1, speedLimits[i].limit));
                    speedCurve.AddKey(new Keyframe(speedLimits[i].distance + trainLength, currentSpeed));

                    currentSpeed = speedLimits[i].limit;
                }

                if(speedLimits[i].limit == 0)
                {
                    break;
                }
            }
        }

        Keyframe[] keys = new Keyframe[speedCurve.keys.Length];

        for (int i = 0; i < speedCurve.keys.Length; i++)
        {
            Keyframe k = speedCurve.keys[i];
            k.time -= 30f;
            keys[i]= k;
        }

        for (int i = 0; i < keys.Length; i++)
        {
            if(i > 0)
            {
                KeyOut result = Helpers.CalculateKeyframeSlopes(keys[i - 1], keys[i]);

                keys[i].inTangent = result.slope * brakingIntensityCurve.Evaluate(trainSpeed);
                keys[i - 1].outTangent = -result.outTan;
            }
        }

        List<Keyframe> keyFrameList = new();

        keyFrameList.Add(keys[0]);

        SqrtTable sqrtTable = new();
        sqrtTable.calculateSqrt();

        for (int i = 0; i < keys.Length; i++)
        {
            if(i > 0)
            {
                if(keys[i - 1].value > keys[i].value && keys[i].value == 0)
                {
                    Keyframe keyframeA = keys[i - 1];
                    Keyframe keyframeB = keys[i];
                    float keyDistance = keyframeB.time - keyframeA.time;
                    float speedB = keyframeB.value;
                    float speedA = keyframeA.value;
                    float slope = (speedB - speedA) / (keyDistance);

                    speedA *= Constants.KMHtoMS;
                    speedB *= Constants.KMHtoMS;

                    float decel = 0.7f;

                    float coeff = -2*decel*(speedA - speedB) / (speedA + speedB);
                    for(float j = keys[i - 1].time; j < keys[i].time; j+=50)
                    {
                        float sqrtVal = coeff * -(j - keys[i - 1].time);
                        float value = (sqrtTable.Get((int)sqrtVal) + speedB) * Constants.MStoKMH;
                        //float value = (Mathf.Sqrt(coeff * -(j - keys[i - 1].time)) + speedB) * Constants.MStoKMH;

                        keyFrameList.Add(new Keyframe(keys[i].time - j + keys[i - 1].time, value));
                    }
                }
                else
                {
                    keyFrameList.Add(keys[i]);
                }
            }
        }


        speedCurve = new AnimationCurve(keyFrameList.ToArray());
    }

    public float GetSpeed(Spline spline, int knotIndex, int direction)
    {
        float speed = 0;

        if(direction == -1)
        {
            knotIndex--;
        }
        if (knotIndex < 0)
        {
            knotIndex = 0;
        }

        if (spline.TryGetFloatData("speed", out SplineData<float> speedData))
        {
            for (int i = 0; i < speedData.Count; i++)
            {
                if (speedData[i].Index <= knotIndex)
                {
                    speed = speedData[i].Value;
                }
            }
        }

        return speed;
    }

    IEnumerator WaitForDeparture(ScheduleEntry order, StopPoint stopPoint)
    {
        float minimumStopTime = 40f;
        float timer = minimumStopTime;

        int departureTime = TimeManager.ParseTimeString(order.departureTime);
        int now = stopPoint.timeManager.clock;

        int diff = departureTime - now;

        if(diff > minimumStopTime)
        {
            timer = diff;
        }

        while(timer > 0 || stopPoint.exitSignal.SignalState == SignalState.Red)
        {
            timer -= 1f;

            yield return new WaitForSeconds(1f);
        }

        yield return null;
    }

    public SpeedEntry GetSignals(Spline spline, int knotIndex, int direction, float distance)
    {
        SpeedEntry speedEntry = null;
        if(direction == -1)
        {
            knotIndex--;
        }
        if (knotIndex < 0)
        {
            knotIndex = 0;
        }

        if(spline.TryGetObjectData("signal", out SplineData<UnityEngine.Object> data))
        {
            int start = (direction == -1) ? data.Count - 1 : 0;
            for (int i = start; i < data.Count && i >= 0; i += direction)
            {
                float index = data[i].Index;

                //Debug.Log("index: " + index + ", direction: " + signal.direction + ", signalState: " + SignalState.Red);

                if(Math.Floor(index) != knotIndex)
                {
                    continue;
                }
                if(data[i].Value.GetType() == typeof(Signal))
                {
                    Signal signal = data[i].Value as Signal;
                    if(signal.direction != direction)
                    {
                        continue;
                    }
                    if(signal.SignalState != SignalState.Red)
                    {
                        continue;
                    }
                }
                else if(data[i].Value.GetType() == typeof(StopPoint))
                {
                    StopPoint stopPoint = data[i].Value as StopPoint;

                    if(stopPoint.direction != direction)
                    {
                        continue;
                    }

                    bool found = false;

                    foreach(var item in timetable.schedule)
                    {
                        if(item.station == stopPoint.station.stationName && item.departed == false)
                        {
                            found = true;
                            break;
                        }
                    }

                    if(found == false)
                    {
                        continue;
                    }
                }
                
                float signalDistance = SplineUtility.ConvertIndexUnit(spline, index, PathIndexUnit.Knot, PathIndexUnit.Distance);
                if(distance.CompareTo(signalDistance) == direction)
                {
                    continue;
                }
                speedEntry = new SpeedEntry(signalDistance, 0, spline, SpeedEntryType.Signal);
                break;
            }
        }

        return speedEntry;
    }

    public int GetJunction(Spline spline, int knotIndex)
    {
        if(spline.TryGetIntData("junctions", out SplineData<int> junctions))
        {
            for(int i = 0; i < junctions.Count; i++)
            {
                if(junctions[i].Index == knotIndex)
                {
                    return junctions[i].Value;
                }
            }
        }
        return -1;
    }
}
