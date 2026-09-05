using UnityEngine;
using UnityEngine.Splines;

public class Junction : MonoBehaviour
{
    private Collider m_Collider;
    [SerializeField] private SplineContainer rail;
    [SerializeField] private int splineIndex = 0;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter(Collider other)
    {
        TrainCar train = other.gameObject.GetComponentInParent<TrainCar>();
        if(!train)
        {
            return;
        }
        Debug.Log("Switching to path index: " + splineIndex);

        train.ChangeSplineIndex(splineIndex);
    }
}
