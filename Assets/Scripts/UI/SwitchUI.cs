using System.Linq;
using UnityEngine;
using UnityEngine.UI;
public enum SwitchTypeUI
{
    StraightElement,
    DivergingElement
}

public class SwitchUI : MonoBehaviour
{
    public RawImage image;
    public SwitchTypeUI type;
    private void Start()
    {
        image = GetComponent<RawImage>();
    }
}
