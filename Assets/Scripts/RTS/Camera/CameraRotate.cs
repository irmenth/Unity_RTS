using UnityEngine;
using UnityEngine.InputSystem;

public class CameraRotate : MonoBehaviour
{
    [SerializeField][Range(0, 100)] private float rotationSpeed;

    private float startX;
    private bool shouldRotate = false;

    private void RotateCamera()
    {
        bool shiftHeld = Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed;
        if (shiftHeld) return;

        float curX = Pointer.current.position.ReadValue().x;
        float delta = curX - startX;
        tr.Rotate(Vector3.up, delta * rotationSpeed * Time.deltaTime, Space.World);
        startX = curX;
    }

    private void ShouldRotate(InputAction.CallbackContext ctx)
    {
        startX = Pointer.current.position.ReadValue().x;
        shouldRotate = true;
    }

    private void ShouldNotRotate(InputAction.CallbackContext ctx) => shouldRotate = false;

    private Transform tr;

    private void Awake()
    {
        tr = transform;
    }

    private void OnEnable()
    {
        InputActionsManager.RTSCameraRotate.performed += ShouldRotate;
        InputActionsManager.RTSCameraRotate.canceled += ShouldNotRotate;
    }

    private void Update()
    {
        if (!shouldRotate) return;
        RotateCamera();
    }

    private void OnDisable()
    {
        InputActionsManager.RTSCameraRotate.performed -= ShouldRotate;
        InputActionsManager.RTSCameraRotate.canceled -= ShouldNotRotate;
    }
}
