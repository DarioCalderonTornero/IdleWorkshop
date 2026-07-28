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

        WorkerRegistry.Instance?.Register(this);
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

        // 3. Copia la lista
        List<WorkTable> desks = new List<WorkTable>(_workStation.GetUnlockedDesks(itemDef.material));
        if (desks.Count == 0)
        {
            Debug.LogError("[Worker] No hay mesas desbloqueadas.");
            yield break;
        }

        // Contador de estrellas de esta ronda
        int starsThisRound = 0;

        foreach (WorkTable desk in desks)
        {
            yield return MoveTo(desk.PlayerSlotPos);
            PutDown(itemGO, desk.ItemSlotPos);

            // Procesa y recoge las estrellas de esta mesa
            bool gotStar = false;
            yield return ProcessRoutine(itemDef, desk, result => gotStar = result);

            if (gotStar) starsThisRound++;

            yield return PickUpAnim(itemGO);
        }

        // Registra el objeto con las estrellas de esta ronda
        BestiaryManager.Instance?.RegisterItem(itemDef, starsThisRound);

        // 4. Volver a recepción
        yield return MoveTo(_workStation.ReceptionDesk.PlayerSlotPos);

        // 5. Dejar objeto para el cliente
        PutDown(itemGO, _workStation.ReceptionItemPoint.position);

        // Registra como vendido al terminar todas las mesas
        BestiaryManager.Instance?.RegisterSold(itemDef);

        // 6. Avisar que terminó
        _workStation.OnWorkCompleted();

        // 7. Volver a idle
        yield return MoveTo(idlePosition.position);
    }

    private IEnumerator ProcessRoutine(ItemDefinition itemDef, WorkTable desk,
    System.Action<bool> onComplete)
    {
        float processTime = desk.GetProcessTime(itemDef);
        float elapsed = 0f;

        progressUI?.Show(0f);

        while (elapsed < processTime)
        {
            // Consume el boost acumulado por taps
            if (_tapBoostAccumulated > 0f)
            {
                elapsed += _tapBoostAccumulated;
                _tapBoostAccumulated = 0f;
            }

            elapsed += Time.deltaTime;
            elapsed = Mathf.Min(elapsed, processTime); // no sobrepasa el límite
            progressUI?.SetFill(elapsed / processTime);
            yield return null;
        }

        progressUI?.SetFill(1f);
        progressUI?.Hide();

        // Roll de estrella
        bool gotStar = desk.RollStar();
        if (gotStar)
        {
            StarPopupSpawner.Instance?.Spawn(desk.ItemSlotPos);
            Debug.Log($"[Worker] ¡Estrella en {desk.gameObject.name}!");
        }

        // Paga al terminar esta mesa
        int reward = desk.GetReward(itemDef);
        EconomyManager.Instance?.AddCoins(reward);

        // Registra en bestiario
        BestiaryManager.Instance?.RegisterItem(itemDef, 0);

        onComplete?.Invoke(gotStar);
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

    // Variable de boost acumulado
    private float _tapBoostAccumulated = 0f;
    private readonly object _boostLock = new object();

    // Llamado desde TapHandler
    public void ApplyTapBoost(float seconds)
    {
        _tapBoostAccumulated += seconds;
    }

    void OnDestroy()
    {
        WorkerRegistry.Instance?.Unregister(this);
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