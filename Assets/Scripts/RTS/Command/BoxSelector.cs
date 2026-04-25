using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(RectTransform))]
public class BoxSelector : MonoBehaviour
{
    [SerializeField] private LayerMask groundLayerMask;

    private Vector2 startPos;
    private float2 startPosWS;
    private bool canStart;

    private void OnBoxSelect(InputAction.CallbackContext ctx)
    {
        Vector2 mousePos = Pointer.current.position.ReadValue();
        startPos = mousePos - screenSize / 2;
        Ray ray = Camera.main.ScreenPointToRay(mousePos);
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, groundLayerMask))
        {
            startPosWS = new(hit.point.x, hit.point.z);
        }
        canStart = true;

        rect.anchoredPosition = startPos;
        rect.sizeDelta = Vector2.zero;
    }

    private void OnBoxSelectEnd(InputAction.CallbackContext ctx)
    {
        canStart = false;

        rect.sizeDelta = Vector2.zero;
    }

    private RectTransform rect;
    private Vector2 screenSize;
    private Camera cMain;

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
        rect.sizeDelta = Vector2.zero;
        screenSize = new(Screen.width, Screen.height);
        cMain = Camera.main;

        InputActionsManager.RTSBoxSelect.started += OnBoxSelect;
        InputActionsManager.RTSBoxSelect.canceled += OnBoxSelectEnd;
    }

    private void Update()
    {
        if (canStart)
        {
            IndicatorBatchManager.instance.Clear();
            UnitRegister.instance.selectedList.Clear();

            Vector2 mousePos = Pointer.current.position.ReadValue();
            Vector2 curPos = mousePos - screenSize / 2;
            Vector2 size = curPos - startPos;
            Ray ray = cMain.ScreenPointToRay(mousePos);
            float2 curPosWS = startPosWS;
            if (Physics.Raycast(ray, out RaycastHit hit, 1000f, groundLayerMask))
            {
                curPosWS = new(hit.point.x, hit.point.z);
            }

            float2 ld = new(math.min(startPosWS.x, curPosWS.x), math.min(startPosWS.y, curPosWS.y));
            float2 ru = new(math.max(startPosWS.x, curPosWS.x), math.max(startPosWS.y, curPosWS.y));
            DragBoxJob job = new(
                UnitRegister.instance.selectedMap,
                UnitRegister.instance.positions,
                ld,
                ru
            );
            job.Schedule(UnitRegister.instance.indexer + 1, 64).Complete();
            GetSelectedListJob job2 = new(
                UnitRegister.instance.selectedMap,
                UnitRegister.instance.selectedList,
                UnitRegister.instance.indexer + 1
            );
            job2.Schedule().Complete();
            UnitRegister.instance.selectedList.Sort();

            rect.anchoredPosition = startPos + size / 2;
            rect.sizeDelta = new(Mathf.Abs(size.x), Mathf.Abs(size.y));
        }
    }

    private void OnDestroy()
    {
        InputActionsManager.RTSBoxSelect.started -= OnBoxSelect;
        InputActionsManager.RTSBoxSelect.canceled -= OnBoxSelectEnd;
    }
}
