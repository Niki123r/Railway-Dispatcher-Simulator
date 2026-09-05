using UnityEngine;

public interface AxleCounterParent
{
    public void CounterIncreased(AxleCounter axleCounter, int newCounter);
    public void CounterIncreased(AxleCounter axleCounter, Collider other, int newCounter);
}
