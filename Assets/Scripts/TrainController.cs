using UnityEngine;
using UnityEngine.Splines;
using TMPro;
using Unity.Mathematics;
using System;
using Unity.VisualScripting.Antlr3.Runtime;
using Unity.VisualScripting;
using System.Collections.Generic;
using System.Linq;

public class TrainController : MonoBehaviour
{
    public float proportionalGain = 1f, derivativeGain = 1f, integralGain = 1f;
    public TextMeshProUGUI text;
    private Spline currentSpline;
    [SerializeField] public SplineContainer rail;

    public bool manualSpeed = false;
    public int direction = 1;

    [SerializeField]
    private float targetSpeed = 0f;

    private PIDController controller;

    private TrainCar[] bogies;

    public TrainCar leadingBogie, trailingBogie;
    public Rigidbody leadingBogieRB, trailingBogieRB;

    [SerializeField]
    private float totalAccel = 0f, totalDecel = 0f, trainLength, totalMass = 0f;

    public AnimationCurve speedCurve;

    public AnimationCurve brakingIntensityCurve;

    private LookAheadSpeed lookAhead;
    private List<SpeedEntry> unclearedLimits = new();

    private Vector3 previousPos;
    private float distanceTraveled = 0f;

    public AudioSource engine;

    public Service timetable;

    public int startSplineIndex = 0;

    public void UpdateChildBogieSplineContainer()
    {
        bogies = GetComponentsInChildren<TrainCar>();
        for(int i = 0; i < bogies.Length; i++)
        {
            bogies[i].rail = rail;
            bogies[i].startSplineIndex = startSplineIndex;
        }
    }

    void Start()
    {
        controller = new PIDController(proportionalGain, derivativeGain, integralGain);
        targetSpeed = 0;

        bogies = GetComponentsInChildren<TrainCar>();

        UpdateChildBogieSplineContainer();

        for(int i = 0; i < bogies.Length; i++)
        {
            totalAccel += bogies[i].maxAcceleration;
            totalDecel += bogies[i].maxBrake;
        }

        Rigidbody[] rigidBodies = GetComponentsInChildren<Rigidbody>();

        for(int i = 0; i < rigidBodies.Length; i++)
        {
            totalMass += rigidBodies[i].mass;
        }

        if(!leadingBogie)
        {
            leadingBogie = bogies[0];
            leadingBogieRB = leadingBogie.GetComponent<Rigidbody>();
        }
        if (!trailingBogie)
        {
            trailingBogie = bogies[^1];
            trailingBogieRB = trailingBogie.GetComponent<Rigidbody>();
        }

        if(!leadingBogieRB)
        {
            leadingBogieRB = leadingBogie.GetComponent<Rigidbody>();
        }
        if(!trailingBogieRB)
        {
            trailingBogieRB = trailingBogie.GetComponent<Rigidbody>();
        }

        speedCurve = new AnimationCurve();

        trainLength = (leadingBogieRB.position - trailingBogieRB.position).magnitude;

        lookAhead = new LookAheadSpeed(rail, totalAccel, totalDecel, trainLength, brakingIntensityCurve, timetable);

        UpdateSpeedCurve();
    }

    float GetSpeed(Spline spline, Vector3 position)
    {
        float speed = 0;

        NativeSpline native = new NativeSpline(spline);

        float distance = SplineUtility.GetNearestPoint(native, position, out float3 nearest, out float t, Constants.SPLINE_RESOLUTION);

        if (spline.TryGetFloatData("speed", out SplineData<float> speedData) && !manualSpeed)
        {
            float index = spline.ConvertIndexUnit(t, PathIndexUnit.Normalized, PathIndexUnit.Knot);

            speed = speedData.Evaluate(spline, index, PathIndexUnit.Knot, InterpolatorUtility.LerpFloat);
        }

        return speed;
    }

    private float time = 0f;
    private float period = 5f;
    private float previousLever = 0f, previousPitch = 1f;

    public float throttleAccelSpeed = 5f;
    void Update()
    {
        UpdateSpeedKeyframes();
        time += Time.deltaTime;
        if(time > period)
        {
            time = 0f;
            UpdateSpeedCurve();
        }

        List<SpeedEntry> orderedSpeed = unclearedLimits.ToList();
        orderedSpeed.Sort((el1, el2) => el1.limit.CompareTo(el2.limit));
        SpeedEntry? localLimit = orderedSpeed.Find((el) => el.leadingCleared == true && el.trailingCleared == false);
        float trailingSpeed = (localLimit == null) ? float.PositiveInfinity : localLimit.limit;

        Vector3 position = leadingBogieRB.position;

        distanceTraveled += (previousPos - position).magnitude;

        CheckLimits();

        if (!manualSpeed)
        {
            float leadingSpeed = speedCurve.Evaluate(distanceTraveled);
            targetSpeed = Mathf.Min(leadingSpeed, trailingSpeed);
        }

        float lever = GetLeverPosition();

        if(engine)
        {
            float pitchGoal = (lever < 0) ? 0 : lever;

            engine.pitch = Mathf.Lerp(previousPitch, pitchGoal + 1, Time.deltaTime / throttleAccelSpeed);

            if(engine.pitch < 1)
            {
                engine.pitch = 1;
            }
            previousPitch = engine.pitch;
        }

        if(lever > 0)
        {
            lever = Mathf.Lerp(previousLever, lever, Time.deltaTime / throttleAccelSpeed);
        }

        for (int i = 0; i < bogies.Length; i++)
        {
            bogies[i].targetSpeed = targetSpeed;
            bogies[i].SetLeverPosition(lever);
            bogies[i].Direction = direction;
        }
        if(text)
        {
            text.text = String.Format("Speed: {0:0.00} km/h\nTarget: {1:0.00} km/h\nThrottle: {2:0}%", leadingBogieRB.linearVelocity.magnitude * Constants.MStoKMH, targetSpeed, lever * 100);   
        }

        previousPos = leadingBogieRB.position;
        previousLever = lever;
    }

    void CheckLimits()
    {
        if(unclearedLimits.Count == 0)
        {
            return;
        }
        SpeedEntry current = unclearedLimits.First();
        Tuple<float, Spline> leadingStatus = leadingBogie.GetPosition();
        Tuple<float, Spline> trailingStatus = trailingBogie.GetPosition();

        for (int i = 0; i < unclearedLimits.Count; i++)
        {
            if(leadingStatus.Item1.CompareTo(unclearedLimits[i].position) == direction && leadingStatus.Item2 == unclearedLimits[i].spline)
            {
                unclearedLimits[i].leadingCleared = true;
            }
            if (trailingStatus.Item1.CompareTo(unclearedLimits[i].endPosition) == direction && trailingStatus.Item2 == unclearedLimits[i].spline)
            {
                unclearedLimits[i].trailingCleared = true;
            }
        }
    }

    void UpdateSpeedCurve()
    {
        lookAhead.UpdateSpeedTable(leadingBogieRB.position, leadingBogie.currentSpline, direction, leadingBogieRB.linearVelocity.magnitude, ref speedCurve, ref unclearedLimits);
        distanceTraveled = 0f;

        Helpers.PrintList<SpeedEntry>(unclearedLimits);
    }

    void UpdateSpeedKeyframes()
    {
        float currentSpeed = leadingBogieRB.linearVelocity.magnitude;
        Keyframe[] keys = new Keyframe[speedCurve.keys.Length];

        for (int i = 0; i < speedCurve.keys.Length; i++)
        {
            Keyframe k = speedCurve.keys[i];
            keys[i]= k;
        }

        for (int i = 0; i < keys.Length; i++)
        {
            if(i > 0)
            {
                KeyOut result = Helpers.CalculateKeyframeSlopes(keys[i - 1], keys[i]);
                //float slope = (keys[i].value - keys[i - 1].value) / (keys[i].time - keys[i - 1].time);
                //keys[i].inTangent = result.slope * brakingIntensityCurve.Evaluate(keys[i - 1].value);
                //keys[i - 1].outTangent = -result.outTan;
            }
        }

        speedCurve = new AnimationCurve(keys);
    }

    float GetLeverPosition() 
    {
        if(targetSpeed < 0.01f)
        {
            return -0.7f;
        }
        controller.proportionalGain = proportionalGain;
        controller.derivativeGain = derivativeGain;
        controller.integralGain = integralGain;

        float lever = controller.Update(Time.fixedDeltaTime, leadingBogieRB.linearVelocity.magnitude, targetSpeed * Constants.KMHtoMS);
        lever = Mathf.Clamp(lever, -1f, 1f);

        return lever;
    }

    public float GetCurrentSpeedMS()
    {
        return leadingBogieRB.linearVelocity.magnitude;
    }

    public void UpdateBogieStartSplineIndeces(int newIndex)
    {
        foreach (var item in bogies)
        {
            item.startSplineIndex = newIndex;
        }
    }
}
