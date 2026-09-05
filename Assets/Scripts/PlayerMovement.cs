using UnityEngine;
using UnityEngine.InputSystem;

public enum PlayerState
{
    Moving,
    Locked
}

public class PlayerMovement : MonoBehaviour
{
    private InputAction moveAction, jumpAction, sprintAction;
    public CharacterController characterController;
    public float walkingSpeed = 5f;
    public float runningSpeed = 10f;
    private float currentSpeed;
    public float gravity = -9.81f;
    public float jumpHeight = 3f;

    public PlayerState state = PlayerState.Moving;

    Vector3 velocity;

    void Start()
    {
        moveAction = InputSystem.actions.FindAction("Move");
        jumpAction = InputSystem.actions.FindAction("Jump");
        sprintAction = InputSystem.actions.FindAction("Sprint");
        characterController = GetComponent<CharacterController>();

        currentSpeed = walkingSpeed;

        jumpAction.started += ctx =>
        {
            if(characterController.isGrounded == true && state == PlayerState.Moving)
            {
                velocity.y = Mathf.Sqrt(jumpHeight * -2 * gravity);
            }
        };
    }

    // Update is called once per frame
    void Update()
    {
        if(state == PlayerState.Locked)
        {
            return;
        }
        var sprintInput = sprintAction.ReadValue<float>();
        if(sprintInput > 0f)
        {
            currentSpeed = runningSpeed;
        }
        else
        {
            currentSpeed = walkingSpeed;
        }

        Vector2 moveDirection = moveAction.ReadValue<Vector2>();

        Vector3 move = transform.right * moveDirection.x + transform.forward * moveDirection.y;

        characterController.Move(move * currentSpeed * Time.deltaTime);

        velocity.y += gravity * Time.deltaTime;

        if(characterController.isGrounded == true)
        {
            velocity.y = -1f;
        }

        characterController.Move(velocity * Time.deltaTime);
    }

    public void LockMovement()
    {
        state = PlayerState.Locked;
    }

    public void UnlockMovement()
    {
        state = PlayerState.Moving;
    }
}
