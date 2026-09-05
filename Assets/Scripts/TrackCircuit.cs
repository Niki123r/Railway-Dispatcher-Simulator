using UnityEngine;
using UnityEngine.Splines;

public enum TrackCircuitState
{
    Occupied,
    Reserved,
    Free
}

public enum ReservationState
{
    Reserved,
    Free
}

[System.Serializable]
public class PointIdentifier
{
    public int splineIndex;
    public int knotIndex;
}

public class TrackCircuit : MonoBehaviour, AxleCounterParent
{
    public SplineContainer rail;
    public int splineIndex;

    public AxleCounter[] axleCounters;

    public TrackCircuitState state;

    private AxleCounter entry = null;

    public PointIdentifier? pointIdentifier;

    void Start()
    {
        SaveChildAxleCounters();
    }

    private void OnValidate()
    {
        SaveChildAxleCounters();
    }

    void SaveChildAxleCounters()
    {
        axleCounters = GetComponentsInChildren<AxleCounter>();

        foreach (var axleCounter in axleCounters)
        {
            axleCounter.Direction = null;
            Vector3 railPosition = Helpers.GetNearestSplinePosition(rail[axleCounter.splineIndex], axleCounter.transform.position, out float normalized);
            axleCounter.parent = this;
            axleCounter.transform.position = railPosition;
        }
    }

    public void CounterIncreased(AxleCounter axleCounter, Collider other, int newCounter)
    {
        CounterIncreased(axleCounter, newCounter);
    }

    public void CounterIncreased(AxleCounter axleCounter, int newCounter)
    {
        if(state == TrackCircuitState.Free || state == TrackCircuitState.Reserved)
        {
            entry = axleCounter;
            state = TrackCircuitState.Occupied;
        }
        else if(state == TrackCircuitState.Occupied)
        {
            bool cleared = false;
            foreach (var item in axleCounters)
            {
                if (item == axleCounter) continue;

                if(newCounter == item.Counter)
                {
                    cleared = true;
                }
            }

            if(cleared)
            {
                state = TrackCircuitState.Free;
                int amount = entry.Counter;
                foreach (var item in axleCounters)
                {
                    if (item.Counter == amount)
                    {
                        item.ChangeCounterBy(-amount);
                    }
                }
            }
        }
    }
}
