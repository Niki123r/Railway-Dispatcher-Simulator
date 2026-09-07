using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class SignalLightBehaviour
{
    public GameObject light;
    public bool blinking;
    public AnimationCurve blinkingCurve;
    public Color baseColor;
    public float currentIntensity = 0f;
}

[System.Serializable]
public class SignalStateBehaviour
{
    public SignalState state;
    public SpeedRestrictionState SpeedRestrictionState;
    public SpeedRestrictionState ExpectRestrictionState;
    public List<SignalLightBehaviour> lightsToEnable;
}

public class SignalLightSystem : MonoBehaviour
{
    public SignalLightBehaviour[] lights = new SignalLightBehaviour[0];
    public List<SignalStateBehaviour> states;

    public float lightIntensity = 5f;

    public int stateIndex = 0;

    private bool start = true;

    private List<IEnumerator> blinkingLights = new();

    void OnValidate()
    {
        if(lights.Length == 0)
        {
            lights = new SignalLightBehaviour[transform.childCount];

            for(int i = 0; i < transform.childCount; i++)
            {
                lights[i] = new()
                {
                    light = transform.GetChild(i).gameObject,
                    currentIntensity = 0f
                };
            }
        }
    }

    IEnumerator BlinkLight(SignalLightBehaviour light)
    {
        float time = 0f;

        const float incrementDuration = 0.05f;

        Renderer renderer = light.light.GetComponent<Renderer>();
        Material material = renderer.material;
        Color color = light.baseColor;
        light.blinkingCurve.postWrapMode = WrapMode.Loop;

        while(true)
        {
            float intensity = light.blinkingCurve.Evaluate(time);

            material.SetColor("_EmissionColor", color * intensity);
            light.currentIntensity = intensity;

            time += incrementDuration * 1.5f;

            yield return new WaitForSeconds(incrementDuration);   
        }
    }

    IEnumerator FadeLight(SignalLightBehaviour light, float increment, float waitTime, float goal)
    {
        Renderer renderer = light.light.GetComponent<Renderer>();
        Material material = renderer.material;

        bool done = false;

        while(true)
        {
            Color color = light.baseColor;

            float intensity = light.currentIntensity;

            intensity += increment;

            if((increment > 0 && intensity > goal) || (increment < 0 && intensity < goal))
            {
                intensity = goal;
                done = true;
            }

            material.SetColor("_EmissionColor", color * intensity);
            light.currentIntensity = intensity;

            if(done) break;

            yield return new WaitForSeconds(waitTime);
        }
        yield return null;
    }

    void SetLightColor(SignalLightBehaviour light, Color color, float intensity)
    {
        Renderer renderer = light.light.GetComponent<Renderer>();
        Material material = renderer.material;
        material.SetColor("_EmissionColor", color * intensity);
        light.currentIntensity = intensity;
    }

    IEnumerator SwitchStates(int newStateIndex)
    {
        if (start)
        {
            foreach(var light in states[newStateIndex].lightsToEnable)
            {
                SetLightColor(light, light.baseColor, lightIntensity);
            }
            start = false;
            yield return null;
        }

        foreach(var item in blinkingLights)
        {
            StopCoroutine(item);
        }
        var oldLights = states[stateIndex].lightsToEnable;
        var newLights = states[newStateIndex].lightsToEnable;

        List<IEnumerator> transitions = new();

        foreach(var light in oldLights)
        {
            if(newLights.Find((el) => el.Equals(light)) != null)
            {
                continue;
            }
            transitions.Add(FadeLight(light, -0.2f, 0.01f, 0f));
            StartCoroutine(transitions.Last());
        }
        foreach(var light in newLights)
        {
            if(oldLights.Find((el) => el.Equals(light)) != null)
            {
                continue;
            }
            transitions.Add(FadeLight(light, 0.2f, 0.01f, lightIntensity));
            StartCoroutine(transitions.Last());
        }
        stateIndex = newStateIndex;

        foreach(var item in transitions)
        {
            yield return item;
        }

        foreach(var light in newLights)
        {
            if(light.blinking)
            {
                IEnumerator routine = BlinkLight(light);
                blinkingLights.Add(routine);
                StartCoroutine(routine);
            }
        }
        yield return null;
        
    }

    private void SwitchToState(int newStateIndex)
    {
        StartCoroutine(SwitchStates(newStateIndex));
    }

    public void SwitchToState(SignalState state, SpeedRestrictionState speedRestrictionState, SpeedRestrictionState expect)
    {
        for(int i = 0; i < states.Count; i++)
        {
            if(states[i].state == state && states[i].SpeedRestrictionState == speedRestrictionState && states[i].ExpectRestrictionState == expect)
            {
                SwitchToState(i);
                break;
            }
        }
    }
}
