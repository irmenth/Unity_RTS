using UnityEngine;
using UnityEngine.InputSystem;

public class InputActionsManager : MonoBehaviour
{
	public static InputAction RTSSetDestination { get; private set; }
	public static InputAction RTSGenerateUnit { get; private set; }

	private void Awake()
	{
		RTSSetDestination = InputSystem.actions.FindAction("RTS/SetDestination");
		RTSGenerateUnit = InputSystem.actions.FindAction("RTS/GenerateUnit");
	}
}
