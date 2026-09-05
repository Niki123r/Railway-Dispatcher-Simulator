using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class MouseLook : MonoBehaviour
{
    private InputAction mouse, menu, click, cancelAction, rightClick;
    public float mouseSensitivity = 100f;
    public Transform playerBody;

    public GameObject cursorOverlay;

    private float xRotation = 0f;
    public TextMeshProUGUI rayText;
    LayerMask layerMask;

    private PlayerMovement parent;

    public bool checkRaycast = true;
    private Canvas canvas;
    public Camera canvasCamera, selfCamera;
    public Vector2 fovRange = new Vector2(30, 60);
    void Start()
    {
        mouse = InputSystem.actions.FindAction("Look");
        menu = InputSystem.actions.FindAction("UnlockMouse");
        click = InputSystem.actions.FindAction("Click");
        cancelAction = InputSystem.actions.FindAction("Cancel");
        rightClick = InputSystem.actions.FindAction("RightClick");
        Cursor.lockState = CursorLockMode.Locked;
        selfCamera = GetComponent<Camera>();

        parent = GetComponentInParent<PlayerMovement>();

        layerMask = LayerMask.GetMask("UI");

        if(!cursorOverlay)
        {
            PlayerMovement parent = GetComponentInParent<PlayerMovement>();
            cursorOverlay = parent.GetComponentInChildren<Canvas>().gameObject;
        }

        menu.started += ctx =>
        {
            Debug.Log("Menu pressed");
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                UnlockCursor();
            }
            else
            {
                LockCursor();
            }
        };
    }

    // Update is called once per frame
    void Update()
    {
        Vector2 mouseMove = mouse.ReadValue<Vector2>() * mouseSensitivity;

        if(Cursor.lockState == CursorLockMode.Locked) {
            playerBody.Rotate(Vector3.up * mouseMove.x);

            xRotation -= mouseMove.y;
            xRotation = Mathf.Clamp(xRotation, -90f, 90f);
            transform.localRotation = Quaternion.Euler(xRotation, 0, 0);
        }
    }

    void FixedUpdate() {
        float rightClicked = rightClick.ReadValue<float>();

        if(rightClicked > 0)
        {
            selfCamera.fieldOfView = Mathf.Lerp(selfCamera.fieldOfView, fovRange.x, 0.1f);
        }
        else
        {
            selfCamera.fieldOfView = Mathf.Lerp(selfCamera.fieldOfView, fovRange.y, 0.1f);
        }

        if(!checkRaycast)
        {
            return;
        }
        if(parent.state == PlayerState.Locked)
        {
            float cancel = cancelAction.ReadValue<float>();
            if(cancel > 0)
            {
                canvas.sortingOrder -= 1;
                canvasCamera.enabled = false;
                selfCamera.enabled = true;
                parent.UnlockMovement();
                LockCursor();
            }
            return;
        }

        RaycastHit hit;
        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());

        if(Physics.Raycast(ray, out hit, 10f, layerMask))
        {
            if(hit.collider.gameObject.tag == "SignalControlPanel" || hit.collider.gameObject.tag == "TimetablePanel")// && selfCamera.enabled)
            {
                float clicked = click.ReadValue<float>();

                if(clicked > 0)
                {
                    canvasCamera = hit.collider.GetComponentInChildren<Camera>();
                    canvas = hit.collider.GetComponent<Canvas>();
                    selfCamera.enabled = false;
                    canvasCamera.enabled = true;
                    rayText.gameObject.SetActive(false);
                    canvas.sortingOrder += 1;
                    UnlockCursor();
                    parent.LockMovement();
                }
                else
                {
                    Vector2 pos = Mouse.current.position.ReadValue();
                    if(rayText.gameObject.activeSelf == false)
                    {
                        rayText.gameObject.SetActive(true);
                        if(hit.collider.gameObject.tag == "SignalControlPanel")
                        {
                            rayText.text = "Upravljanje signalima";
                        }
                        else if(hit.collider.gameObject.tag == "TimetablePanel")
                        {
                            rayText.text = "Raspored vlakova";
                        }
                    }
                    rayText.gameObject.transform.position = new Vector3(pos.x, pos.y + 25, 0);   
                }
            }
        }
        else
        {
            if(rayText.gameObject.activeSelf == true)
            {
                rayText.gameObject.SetActive(false);   
            }
        }
    }

    void SetCursorLocked(bool locked)
    {
        if(parent.state == PlayerState.Locked)
        {
            return;
        }
        cursorOverlay.SetActive(locked);
        if(locked)
        {
            Cursor.lockState = CursorLockMode.Locked;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
        }
    }

    void UnlockCursor()
    {
        SetCursorLocked(false);
    }

    void LockCursor()
    {
        SetCursorLocked(true);
    }
}
