using UnityEngine;

public class SqrtTable
{
    public static float[] sqrtTable;
    public static bool calculatedSqrt = false;

    public void calculateSqrt()
    {
        if(calculatedSqrt)
        {
            return;
        }
        sqrtTable = new float[3000];
        for(int i = 0; i < 3000; i++)
        {
            sqrtTable[i] = Mathf.Sqrt(i);
        }
        calculatedSqrt = true;
    }

    public float Get(int value)
    {
        return sqrtTable[value];
    }
}
