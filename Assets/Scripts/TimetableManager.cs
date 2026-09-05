using System.Collections;
using UnityEngine;

[System.Serializable]
public class ScheduleEntry
{
    public string station;
    public string departureTime;
    public string arrivalTime;
    public int plannedTrack;
    public bool nonStop = false;
    public bool departed = false;
}

[System.Serializable]
public class Service
{
    public string name;
    public GameObject trainPrefab;
    public string spawnTime;
    public Spawner spawnPoint;
    public Vector2 delayRange;
    public int delay;
    public ScheduleEntry[] schedule;
    
    public bool spawned = false;
}

public class TimetableManager : MonoBehaviour
{
    public Service[] services;

    public TimeManager timeManager;

    IEnumerator SpawnServices()
    {
        yield return new WaitForSeconds(5f);
        while(true)
        {
            int time = timeManager.clock;
            foreach(var service in services)
            {
                if(service.spawned)
                {
                    continue;
                }
                int spawnTime = TimeManager.ParseTimeString(service.spawnTime);
                if(time > spawnTime + service.delay * 60)
                {
                    service.spawned = service.spawnPoint.SpawnTrain(service.trainPrefab, service.name, service);
                    break;
                }
            }
            yield return new WaitForSeconds(5f);
        }
    }

    void Awake()
    {
        foreach (var service in services)
        {
            foreach (var item in service.schedule)
            {
                if(item.nonStop)
                {
                    item.departed = true;
                }
            }
            service.delay = (int)Random.Range(service.delayRange.x, service.delayRange.y);
        }
        timeManager = FindAnyObjectByType<TimeManager>();

        StartCoroutine(SpawnServices());
    }
}
