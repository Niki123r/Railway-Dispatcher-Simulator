using UnityEngine;

public class Despawner : MonoBehaviour
{
    public Signal resetSignal;
    private void OnTriggerEnter(Collider other)
    {
        if(other.tag != "Bogie")
        {
            return;
        }

        TrainController train = other.GetComponentInParent<TrainController>();

        if(!train)
        {
            return;
        }

        Destroy(train.gameObject);

        resetSignal.SignalState = SignalState.Green;
    }
}
