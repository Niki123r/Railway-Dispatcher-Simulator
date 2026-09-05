using System;
using System.Collections;
using System.Globalization;
using UnityEngine;

public class TimeManager : MonoBehaviour
{
    public int clock = 7*60*60;
    public float Clock
    {
        get => clock;
        private set {}
    }

    public string time;

    IEnumerator AdvanceSecond() {
        while(true)
        {
            clock += 1;

            if(clock >= 24*60*60)
            {
                clock = 0;
            }

            time = DigitalClock();

            yield return new WaitForSeconds(1f);
        }
    }

    void Start()
    {
        StartCoroutine(AdvanceSecond());
    }

    void OnValidate()
    {
        try
        {
            DateTime d = DateTime.ParseExact(time, "HH:mm:ss", CultureInfo.InvariantCulture);
            clock = d.Second + d.Minute * 60 + d.Hour * 60 * 60;
        } catch
        {
            
        }
    }

    string DigitalClock()
    {
        int hours = clock / 60 / 60;
        int minutes = clock / 60 % 60;
        int seconds = clock % 60;

        return string.Format("{0:00}:{1:00}:{2:00}", hours, minutes, seconds);
    }

    public static int ParseTimeString(string timeString)
    {
        DateTime d = DateTime.ParseExact(timeString, "HH:mm:ss", CultureInfo.InvariantCulture);
        return d.Second + d.Minute * 60 + d.Hour * 60 * 60;
    }
}
