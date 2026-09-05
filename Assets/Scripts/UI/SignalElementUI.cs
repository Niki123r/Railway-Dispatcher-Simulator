using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SignalElementUI : MonoBehaviour
{
    public Signal signal;
    public TextMeshProUGUI text;

    public RawImage signalLight;
    public GameObject selectHighlight;

    private Color[] signalColors = { Color.red, Color.yellow, Color.green };

    private void OnValidate()
    {
        if (signal)
        {
            this.name = signal.name + " UI";
        }

        Button button = GetComponentInChildren<Button>();

        UISignalManager uiManager = GetComponentInParent<UISignalManager>();

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => uiManager.SignalClicked(this));

        SetColor(Color.white);

        if(text)
        {
            text.text = this.name;   
        }
    }

    public void SetColor(Color color)
    {
        if(signalLight)
        {
            signalLight.color = color;
        } else
        {
            Image[] children = GetComponentsInChildren<Image>();
            foreach (var item in children)
            {
                if(item.transform == this.transform)
                {
                    continue;
                }
                item.color = color;
            }
        }
    }

    private void Update()
    {
        UpdateState();
    }

    public void UpdateState()
    {
        SetColor(signalColors[(int)signal.SignalState]);
    }
}
