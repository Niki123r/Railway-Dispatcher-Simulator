using System.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

public enum StopPointState
{
    Free,
    TrainWaiting
}

public class StopPoint : MonoBehaviour, AxleCounterParent
{
    public SplineContainer rail;
    public int splineIndex = 0;
    public int direction = 1;
    public GameObject signalPrefab;
    public GameObject axleCounterPrefab;
    public Signal exitSignal;
    public Station station;

    public float axleCounterOffset = -50f;

    public Signal signal;
    public AxleCounter axleCounter;
    public StopPointState state = StopPointState.Free;

    public float minimumStopTime = 40f;

    public float timer = 0f;

    public TimeManager timeManager;

    TrainController trainWaiting;
    void Start()
    {
        Spline spline = rail[splineIndex];
        NativeSpline native = new NativeSpline(spline);

        SplineUtility.GetNearestPoint(native, transform.position, out float3 nearest, out float pos, Constants.SPLINE_RESOLUTION);

        Helpers.AddSplineData(pos, spline, this);

        /*GameObject signalObj = Instantiate(signalPrefab, this.transform);
        signalObj.transform.position = transform.position;

        signal = signalObj.GetComponent<Signal>();
        signal.rail = rail;
        signal.direction = direction;
        signal.splineIndex = splineIndex;
        signal.SignalState = SignalState.Red;*/


        Vector3 axleCounterPos = native.GetPointAtLinearDistance(pos, axleCounterOffset, out float newPos);
        GameObject axleCounterObj = Instantiate(axleCounterPrefab, this.transform);
        axleCounterObj.transform.position = axleCounterPos;

        axleCounter = axleCounterObj.GetComponent<AxleCounter>();
        axleCounter.Direction = direction;
        axleCounter.parent = this;

        timeManager = FindAnyObjectByType<TimeManager>();
    }

    IEnumerator WaitForDeparture()
    {
        timer = minimumStopTime;

        ScheduleEntry order = null;

        if(trainWaiting && trainWaiting.timetable != null)
        {
            foreach(var item in trainWaiting.timetable.schedule)
            {
                if(item.station == station.stationName && item.departureTime != null && item.departureTime.Length != 0)
                {
                    int departureTime = TimeManager.ParseTimeString(item.departureTime);
                    int now = timeManager.clock;

                    int diff = departureTime - now;

                    if(diff > minimumStopTime)
                    {
                        timer = diff;
                    }

                    order = item;

                    break;
                }
            }
        }

        if(order == null)
        {
            yield return null;
        }

        while(timer > 0 || exitSignal.SignalState == SignalState.Red)
        {
            timer -= 1f;

            yield return new WaitForSeconds(1f);
        }

        //signal.SignalState = SignalState.Yellow;
        if(order != null) {
            order.departed = true;
        }

        yield return null;
    }

    public void CounterIncreased(AxleCounter axleCounter, Collider other, int newCounter)
    {
        TrainCar trainCar = other.GetComponent<TrainCar>();
        if(trainWaiting != null && trainWaiting.trailingBogie == trainCar)
        {    
            trainWaiting = null;
            state = StopPointState.Free;
            return;
        }
        if(trainWaiting || state == StopPointState.TrainWaiting)
        {
            return;
        }
        trainWaiting = other.GetComponentInParent<TrainController>();

        state = StopPointState.TrainWaiting;
        StartCoroutine(WaitForDeparture());
    }

    public void CounterIncreased(AxleCounter axleCounter, int newCounter) {}
}
