using UnityEngine;

[CreateAssetMenu(fileName = "New Switch Texutre Config", menuName = "Switch Texture Config")]
public class SwitchTextureConfig : ScriptableObject
{
    public Texture Straight_Straight;
    public Texture Straight_Diverging;
    public Texture Diverging_Straight;
    public Texture Diverging_Diverging;
}
