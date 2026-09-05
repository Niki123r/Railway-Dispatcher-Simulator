using UnityEngine;

public class TrainSoundElement : MonoBehaviour
{
    public AnimationCurve speedVolume;

    public TrainController parent;

    public AudioSource sound;

    void Start()
    {
        if(!parent)
        {
            parent = GetComponentInParent<TrainController>();
        }
        if(!sound)
        {
            sound = GetComponent<AudioSource>();
            sound.loop = true;
            sound.spatialBlend = 1f;
        }
    }
    void Update()
    {
        float speed = parent.GetCurrentSpeedMS() * Constants.MStoKMH;
        sound.volume = speedVolume.Evaluate(speed);
    }
}
