using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class EchoVRLocomotion : MonoBehaviour
{
    [Header("References")]
    public Transform playerRig;
    public Transform head;

    [Header("Movement")]
    public float moveSpeed = 2.5f;
    public float verticalSpeed = 2.5f;
    public float deadzone = 0.15f;
    public bool useCharacterController = true;

    CharacterController characterController;

    void Start()
    {
        if (playerRig == null)
        {
            GameObject rigObject = GameObject.Find("OVRCameraRig");

            if (rigObject != null)
            {
                playerRig = rigObject.transform;
            }
        }

        if (head == null)
        {
            GameObject centerEye = GameObject.Find("CenterEyeAnchor");

            if (centerEye != null)
            {
                head = centerEye.transform;
            }
        }

        if (playerRig == null || head == null)
        {
            Debug.LogWarning("EchoVRLocomotion: missing OVRCameraRig or CenterEyeAnchor.");
            enabled = false;
            return;
        }

        if (useCharacterController)
        {
            characterController = playerRig.GetComponent<CharacterController>();

            if (characterController == null)
            {
                characterController = playerRig.gameObject.AddComponent<CharacterController>();
            }

            characterController.height = 1.6f;
            characterController.radius = 0.3f;
            characterController.center = new Vector3(0f, 0.8f, 0f);
            characterController.slopeLimit = 45f;
            characterController.stepOffset = 0.25f;
        }
    }

    void Update()
    {
        if (playerRig == null || head == null)
        {
            return;
        }

        Vector2 horizontalInput = ReadHorizontalInput();
        float verticalInput = ReadVerticalInput();

        bool hasHorizontal = horizontalInput.sqrMagnitude >= deadzone * deadzone;
        bool hasVertical = Mathf.Abs(verticalInput) >= deadzone;

        if (!hasHorizontal && !hasVertical)
        {
            return;
        }

        Vector3 move = Vector3.zero;

        if (hasHorizontal)
        {
            Vector3 forward = head.forward;
            forward.y = 0f;

            if (forward.sqrMagnitude < 0.0001f)
            {
                forward = playerRig.forward;
            }

            forward.Normalize();

            Vector3 right = head.right;
            right.y = 0f;
            right.Normalize();

            move += (forward * horizontalInput.y + right * horizontalInput.x) * moveSpeed;
        }

        if (hasVertical)
        {
            move += Vector3.up * (verticalInput * verticalSpeed);
        }

        move *= Time.deltaTime;

        if (useCharacterController && characterController != null)
        {
            characterController.Move(move);
        }
        else
        {
            playerRig.position += move;
        }
    }

    Vector2 ReadHorizontalInput()
    {
        Vector2 stick = GetRightThumbstick();

        if (stick.sqrMagnitude >= deadzone * deadzone)
        {
            return stick;
        }

        return GetKeyboardHorizontal();
    }

    float ReadVerticalInput()
    {
        float stick = GetLeftThumbstickVertical();

        if (Mathf.Abs(stick) >= deadzone)
        {
            return stick;
        }

        return GetKeyboardVertical();
    }

    static Vector2 GetRightThumbstick()
    {
        try
        {
            Vector2 stick = OVRInput.Get(
                OVRInput.Axis2D.PrimaryThumbstick,
                OVRInput.Controller.RTouch
            );

            if (stick.sqrMagnitude > 0.0001f)
            {
                return stick;
            }

            return OVRInput.Get(OVRInput.RawAxis2D.RThumbstick);
        }
        catch
        {
            return Vector2.zero;
        }
    }

    static float GetLeftThumbstickVertical()
    {
        try
        {
            Vector2 stick = OVRInput.Get(
                OVRInput.Axis2D.PrimaryThumbstick,
                OVRInput.Controller.LTouch
            );

            if (Mathf.Abs(stick.y) > 0.0001f)
            {
                return stick.y;
            }

            return OVRInput.Get(OVRInput.RawAxis2D.LThumbstick).y;
        }
        catch
        {
            return 0f;
        }
    }

    static Vector2 GetKeyboardHorizontal()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current == null)
        {
            return Vector2.zero;
        }

        float x = 0f;
        float y = 0f;

        if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
        {
            x -= 1f;
        }

        if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
        {
            x += 1f;
        }

        if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed)
        {
            y += 1f;
        }

        if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed)
        {
            y -= 1f;
        }

        return new Vector2(x, y).normalized;
#else
        return new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
#endif
    }

    static float GetKeyboardVertical()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current == null)
        {
            return 0f;
        }

        float vertical = 0f;

        if (Keyboard.current.eKey.isPressed || Keyboard.current.pageUpKey.isPressed)
        {
            vertical += 1f;
        }

        if (Keyboard.current.qKey.isPressed || Keyboard.current.pageDownKey.isPressed)
        {
            vertical -= 1f;
        }

        return vertical;
#else
        return 0f;
#endif
    }
}
