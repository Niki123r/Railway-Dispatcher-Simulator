using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Mathematics;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;
using UnityEngine.Splines;

public class TrainCar : MonoBehaviour
{
    [SerializeField] public SplineContainer rail;

    public Spline currentSpline;

    private int currentSplineIndex;
    private bool passedJunction = false;
    private Vector3 passedJunctionPosition;

    public bool manualSpeed = false;

    [SerializeField]
    private float junctionDistanceReset = 10f;

    private Rigidbody rb;

    [Range(0f, 200f)]
    public float targetSpeed = 20f;
    public AnimationCurve accelerationTE;
    public AnimationCurve brakeTE;
    public float maxAcceleration = 100f;
    public float maxBrake = 150f;

    [SerializeField]
    private float3 offset;

    [Range(0f, 0.1f)]
    public float step = 1f;
    public float amplitude;

    public float proportionalGain = 1f, derivativeGain = 1f, integralGain = 1f;

    public TextMeshProUGUI text;

    private const float KMHtoMS = 1 / 3.6f;
    private const float MStoKMH = 3.6f;

    private bool increasing = true;

    private PIDController controller;
    private float leverPosition = 0f;

    private int direction = 1;

    public int? startSplineIndex = null;
    public int Direction
    {
        get => direction; 
        set
        {
            if(value == 1 || value == -1)
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
        rb = GetComponent<Rigidbody>();

        currentSpline = rail.Splines[startSplineIndex ?? 0];

        controller = new PIDController(proportionalGain, derivativeGain, integralGain);

        targetSpeed = 0f;
    }


    void FixedUpdate()
    {
        NativeSpline native = new NativeSpline(currentSpline);

        float distance = SplineUtility.GetNearestPoint(native, transform.position, out float3 nearest, out float t, Constants.SPLINE_RESOLUTION);

        /*
        if (currentSpline.TryGetFloatData("speed", out SplineData<float> speedData) && !manualSpeed)
        {
            float index = currentSpline.ConvertIndexUnit(t, PathIndexUnit.Normalized, PathIndexUnit.Knot);

            IInterpolator<float> interpolator = InterpolatorUtility.LerpFloat;

            float speed = speedData.Evaluate(currentSpline, index, PathIndexUnit.Knot, InterpolatorUtility.LerpFloat);

            targetSpeed = speed;
        }
        */

        transform.position = nearest + offset;

        Vector3 forward = Vector3.Normalize(native.EvaluateTangent(t));
        Vector3 up = native.EvaluateUpVector(t);

        var remappedForward = new Vector3(0, 0, 1);
        var remappedUp = new Vector3(0, 1, 0);
        var axisRemapRotation = Quaternion.Inverse(Quaternion.LookRotation(remappedForward, remappedUp));

        transform.rotation = Quaternion.LookRotation(forward, up) * axisRemapRotation;

        float force = leverPosition;
        float forcePercent;

        if (force > 0f)
        {
            force *= maxAcceleration * accelerationTE.Evaluate(rb.linearVelocity.magnitude * MStoKMH);
            forcePercent = force / maxAcceleration * 100;
        }
        else
        {
            force *= maxBrake * brakeTE.Evaluate(rb.linearVelocity.magnitude * MStoKMH);
            forcePercent = force / maxBrake * 100;
        }

        Vector3 engineForward = transform.forward * direction;

        /*if (Vector3.Dot(rb.linearVelocity, transform.forward) < 0)
        {
            engineForward *= -1;
        }*/

        rb.AddForce(force * engineForward);

        rb.linearVelocity = rb.linearVelocity.magnitude * engineForward;

        //Debug.Log("Current speed: " + rb.linearVelocity.magnitude * MStoKMH + ", force applied: " + force + ", engine forward:" + engineForward);

        //text.text = String.Format("Speed: {0:0.00} km/h\nTarget: {1:0.00} km/h\nThrottle: {2:0}%\nForce: {3:0}%", rb.linearVelocity.magnitude * MStoKMH, targetSpeed, lever * 100, forcePercent);

        if(passedJunction)
        {
            if(Vector3.Distance(passedJunctionPosition, transform.position) > junctionDistanceReset)
            {
                passedJunction = false;
            }
        }

        float knotPosition = currentSpline.ConvertIndexUnit(t, PathIndexUnit.Normalized, PathIndexUnit.Knot);
        float trainCarDistance = currentSpline.ConvertIndexUnit(knotPosition, PathIndexUnit.Knot, PathIndexUnit.Distance);

        if (currentSpline.TryGetIntData("junctions", out SplineData<int> junctions) && passedJunction == false)
        {
            for (int i = 0; i < junctions.Count; i++)
            {
                float knotDistance = currentSpline.ConvertIndexUnit(junctions[i].Index, PathIndexUnit.Knot, PathIndexUnit.Distance);
                if (Math.Abs(knotDistance - trainCarDistance) <= (rb.linearVelocity.magnitude / 60.0f * 2))
                {
                    Debug.Log("Switch to path index: " + junctions[i].Value);
                    currentSplineIndex = junctions[i].Value;
                    currentSpline = rail.Splines[currentSplineIndex];
                    passedJunction = true;
                    passedJunctionPosition = transform.position;
                }
            }
        }
    }

    public void ChangeSplineIndex(int newSplineIndex) { 
        currentSplineIndex = newSplineIndex;
        currentSpline = rail.Splines[currentSplineIndex];
    }

    public Tuple<float, Spline> GetPosition()
    {
        NativeSpline native = new NativeSpline(currentSpline);

        float distance = SplineUtility.GetNearestPoint(native, transform.position, out float3 nearest, out float t, Constants.SPLINE_RESOLUTION);

        return new Tuple<float, Spline>(currentSpline.ConvertIndexUnit(t, PathIndexUnit.Normalized, PathIndexUnit.Distance), currentSpline);
    }

    public void SetLeverPosition(float newPosition) { 
        leverPosition = Mathf.Clamp(newPosition, -1f, 1f);
    }
}