using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TrackCircuitUI : MonoBehaviour
{
    public TrackCircuit trackCircuit;
    public TextMeshProUGUI text;
    private RawImage[] colorElements;

    public SwitchTextureConfig switchTextures;

    private bool hasJunctionData = false;

    private SwitchUI[] switchesUI;

    public int straightPointIndex;
    private void OnValidate()
    {
        this.name = trackCircuit.name;

        foreach (var element in GetComponentsInChildren<SwitchUI>())
        {
            if(!element.image)
            {
                element.image = element.GetComponent<RawImage>();
            }
            if(element.image.mainTexture == switchTextures.Straight_Straight || element.image.mainTexture == switchTextures.Diverging_Straight)
            {
                element.type = SwitchTypeUI.StraightElement;
            }
            else
            {
                element.type = SwitchTypeUI.DivergingElement;
            }
        }
    }

    private void Start()
    {
        colorElements = GetComponentsInChildren<RawImage>();
        if(trackCircuit.pointIdentifier.knotIndex != 0 || trackCircuit.pointIdentifier.splineIndex != 0)
        {
            hasJunctionData=true;
            //straightPointIndex = trackCircuit.pointIdentifier.splineIndex;
        }

        foreach (var item in colorElements)
        {
            ColorElement(item);
        }
    }

    void Update()
    {
        int junctionState = -1;
        if(hasJunctionData)
        {
            junctionState = Helpers.GetJunction(trackCircuit.rail[trackCircuit.pointIdentifier.splineIndex], trackCircuit.pointIdentifier.knotIndex);
        }
        foreach (var colorElement in colorElements)
        {
            if(junctionState != -1)
            {
                if(colorElement.name == junctionState.ToString())
                {
                    ColorElement(colorElement);
                }
                if(junctionState != straightPointIndex)
                {
                    ActivateDiverging();
                }
                else
                {
                    ActivateStraight();
                }
            }
            else
            {
                ColorElement(colorElement);
            }
        }
        
    }

    void ColorElement(RawImage colorElement)
    {
        if (trackCircuit.state == TrackCircuitState.Occupied)
        {
            colorElement.color = Color.red;
        }
        else if(trackCircuit.state == TrackCircuitState.Reserved)
        {
            colorElement.color = Color.white;
        }
        else
        {
            colorElement.color = Color.grey;
        }
    }

    void ActivateDiverging()
    {
        foreach (var element in GetComponentsInChildren<SwitchUI>())
        {
            if(element.type == SwitchTypeUI.StraightElement)
            {
                element.image.texture = switchTextures.Diverging_Straight;
            }
            else
            {
                element.image.texture = switchTextures.Diverging_Diverging;
            }
        }
    }

    void ActivateStraight()
    {
        foreach (var element in GetComponentsInChildren<SwitchUI>())
        {
            if (element.type == SwitchTypeUI.StraightElement)
            {
                element.image.texture = switchTextures.Straight_Straight;
            }
            else
            {
                element.image.texture = switchTextures.Straight_Diverging;
            }
        }
    }
}
