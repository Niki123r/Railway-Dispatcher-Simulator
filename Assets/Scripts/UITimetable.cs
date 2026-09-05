using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class UITimetable : MonoBehaviour
{
    public TimetableManager timetableManager;
    public GameObject rowPrefab;
    public Station station;
    void Start()
    {
        if(timetableManager == null)
        {
            timetableManager = FindAnyObjectByType<TimetableManager>();
        }
        if (station == null)
        {
            station = FindAnyObjectByType<Station>();
        }
        foreach (var service in timetableManager.services)
        {
            GameObject child = Instantiate(rowPrefab, this.transform);
            var text = child.GetComponentsInChildren<TextMeshProUGUI>();
            text[0].text = service.name;
            child.name = "Timetable " + service.name;
            string fromStation, toStation, departureTime = "", arrivalTime = "", nonStop = "", track = "";
            fromStation = service.schedule[0].station;
            toStation = service.schedule[service.schedule.Length - 1].station;
            for (int i = 0; i < service.schedule.Length; i++)
            {
                if(service.schedule[i].station == station.stationName)
                {
                    departureTime = service.schedule[i].departureTime;
                    arrivalTime = service.schedule[i].arrivalTime;
                    nonStop = (service.schedule[i].nonStop) ? "Da" : "Ne";
                    track = service.schedule[i].plannedTrack.ToString();
                    break;
                }
            }
            text[1].text = arrivalTime;
            text[2].text = departureTime;
            text[3].text = track;
            text[4].text = nonStop;
            text[5].text = fromStation;
            text[6].text = toStation;
            text[7].text = service.delay.ToString();
        }
    }
}
