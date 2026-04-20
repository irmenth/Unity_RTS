using UnityEngine;
using UnityEngine.InputSystem;

public class InputActionsManager : MonoBehaviour
{
	public static InputAction RTSSetOrangeDestination { get; private set; }
	public static InputAction RTSSetBlueDestination { get; private set; }
	public static InputAction RTSGenerateOrangeUnit { get; private set; }
	public static InputAction RTSGenerateBlueUnit { get; private set; }
	public static InputAction RTSCameraMove { get; private set; }
	public static InputAction RTSCameraRotate { get; private set; }

	private void Awake()
	{
		RTSSetOrangeDestination = InputSystem.actions.FindAction("RTS/SetOrangeDestination");
		RTSSetBlueDestination = InputSystem.actions.FindAction("RTS/SetBlueDestination");
		RTSGenerateOrangeUnit = InputSystem.actions.FindAction("RTS/GenerateOrangeUnit");
		RTSGenerateBlueUnit = InputSystem.actions.FindAction("RTS/GenerateBlueUnit");
		RTSCameraMove = InputSystem.actions.FindAction("RTS/CameraMove");
		RTSCameraRotate = InputSystem.actions.FindAction("RTS/CameraRotate");
	}
}
