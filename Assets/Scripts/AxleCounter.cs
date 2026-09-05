using UnityEngine;

public class AxleCounter : MonoBehaviour
{
    public Collider counterCollider;

    public AxleCounterParent parent;

    public int splineIndex = 0;

    [SerializeField] private int counter = 0;
    public int Counter
    {
        get => counter; private set
        {
            
        }
    }

    private int? direction;
    public int? Direction
    {
        get => direction; 
        set
        {
            if (value == null)
            {
                direction = null;
            }
            else if(value == 1 || value == -1)
            {
                direction = value;
            }
            else
            {
                direction = 1;
            }
        }
    }
    void Start()
    {
        counterCollider = GetComponent<Collider>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.tag != "Bogie")
        {
            return;
        }
        TrainCar hit = other.GetComponent<TrainCar>();
        //Debug.Log("Hit bogie! " + other + ", with script: " + hit + ", direction: " + hit.Direction);
        if(hit == null)
        {
            return;
        }

        if(direction == null || hit.Direction == direction)
        {
            counter += 1;
            parent.CounterIncreased(this, other, counter);
        }
    }

    public void ChangeCounterBy(int amount)
    {
        counter += amount;
    }
}
