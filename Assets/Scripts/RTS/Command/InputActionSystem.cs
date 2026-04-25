using UnityEngine;
using UnityEngine.InputSystem;

public class InputActionSystem : MonoBehaviour
{
    public static InputActionSystem instance;

    [SerializeField] private LayerMask groundLayerMask;

    private void GenerateOrangeUnit(InputAction.CallbackContext ctx)
    {
        Vector2 mousePos = Pointer.current.position.ReadValue();
        Ray ray = Camera.main.ScreenPointToRay(mousePos);
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, groundLayerMask))
        {
            Client.instance.SendInput(new GenerateCommand(UnitType.OrangeSmall, 10, new(hit.point.x, hit.point.z)));
        }
    }

    private void GenerateBlueUnit(InputAction.CallbackContext ctx)
    {
        Vector2 mousePos = Pointer.current.position.ReadValue();
        Ray ray = Camera.main.ScreenPointToRay(mousePos);
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, groundLayerMask))
        {
            Client.instance.SendInput(new GenerateCommand(UnitType.BlueSmall, 10, new(hit.point.x, hit.point.z)));
        }
    }

    private void GeneratePinkUnit(InputAction.CallbackContext ctx)
    {
        Vector2 mousePos = Pointer.current.position.ReadValue();
        Ray ray = Camera.main.ScreenPointToRay(mousePos);
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, groundLayerMask))
        {
            Client.instance.SendInput(new GenerateCommand(UnitType.White, 1, new(hit.point.x, hit.point.z)));
        }
    }

    private void SetDestination(InputAction.CallbackContext ctx)
    {
        Vector2 mousePos = Pointer.current.position.ReadValue();
        Ray ray = Camera.main.ScreenPointToRay(mousePos);
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, groundLayerMask))
        {
            Client.instance.SendInput(new MoveCommand(new(hit.point.x, hit.point.z)));
        }
    }

    private void Delete(InputAction.CallbackContext ctx)
    {
        Client.instance.SendInput(new DeleteCommand());
    }

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        InputActionsManager.RTSGenerateOrangeUnit.started += GenerateOrangeUnit;
        InputActionsManager.RTSGenerateBlueUnit.started += GenerateBlueUnit;
        InputActionsManager.RTSGeneratePinkUnit.started += GeneratePinkUnit;
        InputActionsManager.RTSSetDestination.started += SetDestination;
        InputActionsManager.RTSDelete.started += Delete;
    }

    private void OnDestroy()
    {
        InputActionsManager.RTSGenerateOrangeUnit.started -= GenerateOrangeUnit;
        InputActionsManager.RTSGenerateBlueUnit.started -= GenerateBlueUnit;
        InputActionsManager.RTSGeneratePinkUnit.started -= GeneratePinkUnit;
        InputActionsManager.RTSSetDestination.started -= SetDestination;
        InputActionsManager.RTSDelete.started -= Delete;

        instance = null;
    }
}
