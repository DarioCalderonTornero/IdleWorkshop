using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Worker : MonoBehaviour
{
    [Header("Posición idle")]
    [SerializeField] private Transform idlePosition;

    [Header("Objeto en cabeza")]
    [SerializeField] private Transform headAnchor;

    [Header("Movimiento")]
    [SerializeField] private float baseMoveSpeed = 4f;

    [Header("UI")]
    [SerializeField] private RepairProgressUI progressUI;

    private WorkStation _workStation;
    private float _currentMoveSpeed;

    private void Awake()
    {
        _currentMoveSpeed = baseMoveSpeed;
    }

    public void Init(WorkStation workStation)
    {
        _workStation = workStation;
        if (idlePosition != null)
            transform.position = idlePosition.position;
    }

    public void StartWork(GameObject itemGO, ItemDefinition itemDef)
    {
        StartCoroutine(WorkLoop(itemGO, itemDef));
    }

    public void ApplyMoveSpeedMultiplier(float multiplier)
    {
        _currentMoveSpeed = baseMoveSpeed * multiplier;
    }

    private IEnumerator WorkLoop(GameObject itemGO, ItemDefinition itemDef)
    {
        // 1. Ir a recepción
        yield return MoveTo(_workStation.ReceptionDesk.PlayerSlotPos);

        // 2. Coger objeto
        Transform anchor = headAnchor != null ? headAnchor : transform;
        itemGO.transform.SetParent(anchor);
        itemGO.transform.localPosition = Vector3.zero;

        // 3. Pasar por cada mesa desbloqueada en orden
        List<WorkTable> desks = _workStation.GetUnlockedDesks();

        if (desks.Count == 0)
        {
            Debug.LogError("[Worker] No hay mesas desbloqueadas.");
            yield break;
        }

        foreach (WorkTable desk in desks)
        {
            yield return MoveTo(desk.PlayerSlotPos);
            PutDown(itemGO, desk.ItemSlotPos);
            yield return ProcessRoutine(itemDef, desk);
            yield return PickUpAnim(itemGO);
        }

        // 4. Volver a recepción con el objeto
        yield return MoveTo(_workStation.ReceptionDesk.PlayerSlotPos);

        // 5. Dejar objeto para el cliente
        PutDown(itemGO, _workStation.ReceptionItemPoint.position);

        // 6. Avisar que terminó
        _workStation.OnWorkCompleted();

        // 7. Volver a idle
        yield return MoveTo(idlePosition.position);
    }

    private IEnumerator ProcessRoutine(ItemDefinition itemDef, WorkTable desk)
    {
        float processTime = Mathf.Min(desk.CurrentProcessTime, itemDef.baseRepairTime);
        float elapsed = 0f;

        progressUI?.Show(0f);

        while (elapsed < processTime)
        {
            elapsed += Time.deltaTime;
            progressUI?.SetFill(elapsed / processTime);
            yield return null;
        }

        progressUI?.SetFill(1f);
        progressUI?.Hide();
    }

    private void PutDown(GameObject itemGO, Vector3 worldPos)
    {
        itemGO.transform.SetParent(null);
        itemGO.transform.position = worldPos;
    }

    private IEnumerator PickUpAnim(GameObject itemGO)
    {
        Vector3 startPos = itemGO.transform.position;
        Transform anchor = headAnchor != null ? headAnchor : transform;
        Vector3 targetPos = anchor.position;

        float elapsed = 0f;
        float animTime = 0.35f;

        while (elapsed < animTime)
        {
            elapsed += Time.deltaTime;
            itemGO.transform.position = Vector3.Lerp(
                startPos, targetPos,
                Mathf.SmoothStep(0f, 1f, elapsed / animTime));
            yield return null;
        }

        itemGO.transform.SetParent(anchor);
        itemGO.transform.localPosition = Vector3.zero;
    }

    private IEnumerator MoveTo(Vector3 target)
    {
        while (Vector3.Distance(transform.position, target) > 0.05f)
        {
            transform.position = Vector3.MoveTowards(
                transform.position, target,
                _currentMoveSpeed * Time.deltaTime);
            yield return null;
        }
        transform.position = target;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (idlePosition != null)
        {
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(idlePosition.position, 0.12f);
        }
    }
#endif
}