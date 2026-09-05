using UnityEngine;
using UnityEngine.Splines;

public class Spawner : MonoBehaviour
{
    public SplineContainer rail;
    public int splineIndex;
    public int direction = 1;

    public int bogieCountInCollider = 0;

    public GameObject trainToSpawn;

    public void SnapToSpline()
    {
        transform.position = Helpers.GetNearestSplinePosition(rail[splineIndex], transform.position);
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.tag != "Bogie")
        {
            return;
        }

        bogieCountInCollider += 1;
    }

    private void OnTriggerExit(Collider other)
    {
        if(other.tag != "Bogie")
        {
            return;
        }

        bogieCountInCollider -= 1;
    }
    
    public bool SpawnTrain()
    {
        return SpawnTrain(trainToSpawn, null, null);
    }

    public bool SpawnTrain(GameObject trainPrefab, string name, Service timetable)
    {
        if(bogieCountInCollider != 0)
        {
            return false;
        }

        GameObject trainObj = Instantiate(trainPrefab, transform.position, transform.rotation);
        TrainController train = trainObj.GetComponent<TrainController>();
        train.rail = rail;
        train.direction = direction;
        train.startSplineIndex = splineIndex;

        if(name != null)
        {
            train.name = name;
        }

        if(timetable != null)
        {
            train.timetable = timetable;
        }

        return true;
    }
}
