using UnityEngine;
using System;

public class UITrackCircuitManager : MonoBehaviour
{
    public SwitchTextureConfig switchTextures;
    void OnValidate()
    {
        foreach (var item in GetComponentsInChildren<TrackCircuitUI>())
        {
            item.switchTextures = switchTextures;
        }
    }
}
