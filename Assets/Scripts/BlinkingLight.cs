using UnityEngine;

public class BlinkingLight : MonoBehaviour
{
    public Material material;
    public AnimationCurve animationCurve;
    public Color materialColor;
    [SerializeField] private float intensity;

    private float time = 0f;
    void OnValidate()
    {
        Renderer renderer = GetComponent<Renderer>();
        material = renderer.material;
        if(!material)
        {
            material = GetComponent<Material>();
        }
    }

    void Start()
    {
        animationCurve.postWrapMode = WrapMode.Loop;
    } 

    // Update is called once per frame
    void Update()
    {
        material.SetColor("_EmissionColor", animationCurve.Evaluate(time) * materialColor);
        time += Time.deltaTime;
    }
}
