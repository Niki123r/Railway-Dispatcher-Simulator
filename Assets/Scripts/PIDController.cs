using UnityEngine;

public class PIDController
{
    public float proportionalGain, derivativeGain, integralGain;

    private float errorLast = 0f, integrationStored;

    public PIDController(float proportionalGain, float derivativeGain, float integralGain)
    {
        this.proportionalGain = proportionalGain;
        this.derivativeGain = derivativeGain;
        this.integralGain = integralGain;
    }

 
    public float Update(float dt, float currentValue, float targetValue)
    {
        float error = targetValue - currentValue;

        float P = proportionalGain * error;

        float errorRateOfChange = (error - errorLast) / dt;
        errorLast = error;

        float D = derivativeGain * errorRateOfChange;

        integrationStored = integrationStored + (error * dt);
        float I = integralGain * integrationStored;

        return P + I + D;
    }
}
