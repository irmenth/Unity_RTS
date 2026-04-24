using UnityEngine;
using UnityEngine.InputSystem;

public class InputActionsManager : MonoBehaviour
{
	public static InputAction RTSSetDestination { get; private set; }
	public static InputAction RTSGenerateOrangeUnit { get; private set; }
	public static InputAction RTSGenerateBlueUnit { get; private set; }
	public static InputAction RTSCameraMove { get; private set; }
	public static InputAction RTSCameraRotate { get; private set; }
	public static InputAction RTSBoxSelect { get; private set; }
	public static InputAction RTSDelete { get; private set; }

	private void Awake()
	{
		RTSSetDestination = InputSystem.actions.FindAction("RTS/SetDestination");
		RTSGenerateOrangeUnit = InputSystem.actions.FindAction("RTS/GenerateOrangeUnit");
		RTSGenerateBlueUnit = InputSystem.actions.FindAction("RTS/GenerateBlueUnit");
		RTSCameraMove = InputSystem.actions.FindAction("RTS/CameraMove");
		RTSCameraRotate = InputSystem.actions.FindAction("RTS/CameraRotate");
		RTSBoxSelect = InputSystem.actions.FindAction("RTS/BoxSelect");
		RTSDelete = InputSystem.actions.FindAction("RTS/Delete");
	}
}
