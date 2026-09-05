using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class UISignalManager : MonoBehaviour
{
    private SignalElementUI first;
    private IEnumerator coroutine;
    private SignalElementUI second;
    private GameObject blinkingElement;

    IEnumerator BlinkObject(GameObject obj)
    {
        if(!obj)
        {
            yield return null;
        }
        Color color = Color.black;
        obj.SetActive(true);
        RawImage image = obj.GetComponent<RawImage>();
        image.color = color;
        while(true)
        {
            if (color == Color.white)
            {
                color = Color.black;
            }
            else
            {
                color = Color.white;
            }
            image.color = color;
            yield return new WaitForSeconds(0.3f);
        }
    }

    public void SignalClicked(SignalElementUI signal)
    {
        if(signal.signal.isManual && first==null)
        {
            first = signal;
            blinkingElement = signal.selectHighlight;
            coroutine = BlinkObject(blinkingElement);
            StartCoroutine(coroutine);
            first.SetColor(Color.dimGray);
            SignalElementUI[] signals = GetComponentsInChildren<SignalElementUI>();

            /*foreach (var signalChild in signals)
            {
                foreach (var route in first.signal.routes)
                {
                    if(signalChild.signal == route.goal)
                    {
                        signalChild.SetColor(Color.blue);
                    }
                }
            }*/
        }
        else 
        {
            second = signal;

            SignalElementUI[] signals = GetComponentsInChildren<SignalElementUI>();

            foreach (var signalChild in signals)
            {
                if(!first)
                {
                    continue;
                }
                foreach (var route in first.signal.routes)
                {
                    if (signalChild.signal == route.goal)
                    {
                        signalChild.SetColor(Color.white);
                    }
                }
            }
        }

        if (first != null && second != null)
        {
            first.signal.ApplyJunctionData(second.signal);
            first.SetColor(Color.white);
            first = null;
            second = null;

            StopCoroutine(coroutine);

            blinkingElement.SetActive(false);
        }
    }
}
