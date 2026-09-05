using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;

public enum SignalState
{
    Red,
    Yellow,
    Green
}

public enum SpeedRestrictionState
{
    None,
    
    Restricted
}

[System.Serializable]
public class JunctionData
{
    public int splineIndex;
    public int knotIndex;
    public int splineToJumpTo;
}

[System.Serializable]
public class Rule
{
    public TrackCircuit[] freeTrackCircuits;
}

[System.Serializable]
public class Route
{
    public Signal goal;
    public JunctionData[] junctionData;
    public Rule rule;
    public bool isSpeedRestricted;
}

public class Signal : MonoBehaviour, AxleCounterParent
{
    public SplineContainer rail;
    public int splineIndex;
    public int direction = 1;

    [SerializeField] private SignalState signalState = SignalState.Green;
    public SpeedRestrictionState speedRestrictionState = SpeedRestrictionState.None;
    public SpeedRestrictionState expectRestrictionState = SpeedRestrictionState.None;
    public SignalState SignalState
    {
        get => signalState;
        set {
            signalState = value;
            if(value == SignalState.Red)
            {
                speedRestrictionState = SpeedRestrictionState.None;
                expectRestrictionState = SpeedRestrictionState.None;
            }
            if(value == SignalState.Yellow)
            {
                expectRestrictionState = SpeedRestrictionState.None;
            }
            if(lightSystem)
            {
                lightSystem.SwitchToState(value, speedRestrictionState, expectRestrictionState);
            }
        }
    }

    private int counter = 0;

    public bool isManual = false;

    public GameObject[] previousSignals;

    public GameObject axleCounterPrefab;

    [SerializeField]
    public Route[] routes = new Route[0];

    public SignalLightSystem lightSystem;

    void Start()
    {
        Vector3 railPosition = Helpers.GetNearestSplinePosition(rail[splineIndex], transform.position, out float normalized);

        AddSplineData(normalized);

        GameObject axleCounterObject = Instantiate(axleCounterPrefab, transform);
        axleCounterObject.transform.position = railPosition;
        AxleCounter axleCounter = axleCounterObject.GetComponent<AxleCounter>();
        axleCounter.parent = this;
        axleCounter.Direction = direction;

        lightSystem = GetComponentInChildren<SignalLightSystem>();

        if(lightSystem)
        {
            lightSystem.SwitchToState(signalState, speedRestrictionState, expectRestrictionState);
        }
    }

    public void CounterIncreased(AxleCounter _, int newCounter)
    {
        counter = newCounter;
        SignalState = SignalState.Red;
        for(int i = 0; i < previousSignals.Length; i++)
        {
            Signal previous = previousSignals[i].GetComponent<Signal>();
            previous.IncreaseState(counter, 1);
        }
    }

    public void CounterIncreased(AxleCounter axle, Collider other, int newCounter)
    {
        CounterIncreased(axle, newCounter);
    }

    public void IncreaseState(int nextCounter, int depth)
    {
        if(isManual)
        {
            return;
        }
        if(signalState == SignalState.Green)
        {
            return;
        }
        if(counter != nextCounter)
        {
            return;
        }
        if(depth > 2)
        {
            return;
        }
        SignalState += 1;

        for(int i = 0; i < previousSignals.Length; i++)
        {
            Signal previous = previousSignals[i].GetComponent<Signal>();

            previous.IncreaseState(nextCounter, depth + 1);
        }
    }

    public void IncreasePreviousState(int nextCounter, SpeedRestrictionState expectRestriction)
    {
        for(int i = 0; i < previousSignals.Length; i++)
        {
            Signal previous = previousSignals[i].GetComponent<Signal>();
            previous.expectRestrictionState = expectRestriction;
            if(previous.SignalState != SignalState.Green && previous.SignalState != SignalState.Red)
            {
                previous.SignalState += 1;
            }
            previous.UpdateLightSystem();

            previous.IncreaseState(nextCounter, 1);
        }
    }

    private void AddSplineData(float normalizedPosition)
    {
        Helpers.AddSplineData(normalizedPosition, rail[splineIndex], this);
        /*
        Spline spline = rail[splineIndex];
        float index = SplineUtility.ConvertIndexUnit(spline, normalizedPosition, PathIndexUnit.Normalized, PathIndexUnit.Knot);

        DataPoint<UnityEngine.Object> newPoint = new DataPoint<UnityEngine.Object>(index, this);

        if(spline.TryGetObjectData("signal", out SplineData<UnityEngine.Object> data))
        {
            data.Add(newPoint);
        } else
        {
            SplineData<UnityEngine.Object> newData = new SplineData<UnityEngine.Object>();

            newData.Add(newPoint);

            spline.SetObjectData("signal", newData);
        }
        */
    }

    public void ApplyJunctionData(int dataIndex)
    {
        Route? data = routes[dataIndex];

        ApplyJunctionData(data);
    }

    public void ApplyJunctionData(Signal signal)
    {
        Route? data = null;

        foreach (var item in routes)
        {
            if(item.goal == signal)
            {
                data = item;
            }
        }

        if (data == null)
        {
            return;
        }

        ApplyJunctionData(data);
    }

    public void ApplyJunctionData(Route data)
    {
        if (TryReserveRoute(data) == false)
        {
            return;
        }

        foreach (var item in data.junctionData)
        {
            int splineIndex = item.splineIndex;
            int knot = item.knotIndex;
            int jumpTo = item.splineToJumpTo;

            if (rail[splineIndex].TryGetIntData("junctions", out var junctions))
            {
                for (int i = 0; i < junctions.Count; i++)
                {
                    if (junctions[i].Index == knot)
                    {
                        junctions.SetDataPoint(i, new DataPoint<int>(junctions[i].Index, jumpTo));
                    }
                }
            }
        }

        if(data.isSpeedRestricted)
        {
            speedRestrictionState = SpeedRestrictionState.Restricted;
        }

        if(data.goal.signalState == SignalState.Red)
        {
            SignalState = SignalState.Yellow;
        }
        else
        {
            SignalState = SignalState.Green;
        }

        IncreasePreviousState(this.counter, speedRestrictionState);
    }

    private bool TryReserveRoute(Route route)
    {
        bool success = true;
        foreach (var item in route.rule.freeTrackCircuits)
        {
            if(item.state != TrackCircuitState.Free)
            {
                return false;
            }
        }

        foreach (var item in route.rule.freeTrackCircuits)
        {
            item.state = TrackCircuitState.Reserved;
        }

        return success;
    }

    public void UpdateLightSystem()
    {
        if(lightSystem)
        {
            lightSystem.SwitchToState(signalState, speedRestrictionState, expectRestrictionState);
        }
    }
}
