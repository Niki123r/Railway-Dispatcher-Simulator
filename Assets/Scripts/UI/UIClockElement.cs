using System.Collections;
using TMPro;
using UnityEngine;

public class UIClockElement : MonoBehaviour
{
    public TextMeshProUGUI text;
    public TimeManager timeManager;
    IEnumerator AdvanceClock()
    {
        while(true)
        {
            text.text = timeManager.time;
            yield return new WaitForSeconds(1f);
        }
    }
    void Start()
    {
        text = GetComponent<TextMeshProUGUI>();
        timeManager = FindAnyObjectByType<TimeManager>();
        StartCoroutine(AdvanceClock());
    }
}
