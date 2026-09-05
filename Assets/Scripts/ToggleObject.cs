using UnityEngine;
using UnityEngine.InputSystem;

public class ToggleObject : MonoBehaviour
{
    private InputAction toggleAction;
    public GameObject toggleObject;
    void Start()
    {
        toggleAction = InputSystem.actions.FindAction("OpenSignalMenu");

        toggleAction.started += ctx =>
        {
            toggleObject.SetActive(!toggleObject.activeSelf);
        };
    }
}
