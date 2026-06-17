using System.Collections;
using UnityEngine;

/// <summary>
/// Worker genérico que ejecuta el loop de trabajo dentro de su WorkStation.
/// No tiene referencias directas al CustomerManager ni a otros sistemas.
/// Toda comunicación hacia fuera pasa por WorkStation.
/// 
/// LOOP:
///  1. Ir a mesa de recepción → coger objeto.
///  2. Ir a mesa de trabajo → dejar objeto.
///  3. Procesar (barra de progreso).
///  4. Recoger objeto procesado.
///  5. Volver a mesa de recepción → dejar objeto para el cliente.
///  6. Avisar a WorkStation → trabajo completado.
///  7. Volver a posición idle.
/// </summary>
public class Worker : MonoBehaviour
{
    [Header("Posición idle")]
    [Tooltip("Posición de espera cuando no hay trabajo")]
    [SerializeField] private Transform idlePosition;

    [Header("Objeto en cabeza")]
    [Tooltip("Transform hijo encima de la cabeza del worker")]
    [SerializeField] private Transform headAnchor;

    [Header("Procesado")]
    [Tooltip("Tiempo base en segundos para procesar un objeto (se modificará con mejoras)")]
    [SerializeField] private float baseProcessTime = 4f;

    [Header("Movimiento")]
    [Tooltip("Velocidad base de movimiento (se modificará con mejoras)")]
    [SerializeField] private float baseMoveSpeed = 4f;

    [Header("UI")]
    [SerializeField] private RepairProgressUI progressUI;

    // ── Referencias ─────────────────────────────────────────────────
    private WorkStation _workStation;

    // ── Estado ──────────────────────────────────────────────────────
    private float _currentProcessTime;
    private float _currentMoveSpeed;

    // ── Unity ───────────────────────────────────────────────────────
    private void Awake()
    {
        _currentProcessTime = baseProcessTime;
        _currentMoveSpeed = baseMoveSpeed;
    }

    // ── Init ────────────────────────────────────────────────────────

    /// <summary>WorkStation llama a este método en su Awake para enlazarse.</summary>
    public void Init(WorkStation workStation)
    {
        _workStation = workStation;

        if (idlePosition != null)
            transform.position = idlePosition.position;
    }

    // ── API pública ─────────────────────────────────────────────────

    /// <summary>WorkStation llama a este método para arrancar el loop de trabajo.</summary>
    public void StartWork(GameObject itemGO, ItemDefinition itemDef)
    {
        StartCoroutine(WorkLoop(itemGO, itemDef));
    }

    /// <summary>
    /// Modifica la velocidad de procesado (llamado desde sistema de mejoras).
    /// Ejemplo: ApplyProcessTimeMultiplier(0.8f) → 20% más rápido.
    /// </summary>
    public void ApplyProcessTimeMultiplier(float multiplier)
    {
        _currentProcessTime = baseProcessTime * multiplier;
    }

    /// <summary>
    /// Modifica la velocidad de movimiento (llamado desde sistema de mejoras).
    /// </summary>
    public void ApplyMoveSpeedMultiplier(float multiplier)
    {
        _currentMoveSpeed = baseMoveSpeed * multiplier;
    }

    // ── Loop principal ──────────────────────────────────────────────
    private IEnumerator WorkLoop(GameObject itemGO, ItemDefinition itemDef)
    {
        // 1. Ir a mesa de recepción y coger objeto
        yield return MoveTo(_workStation.ReceptionDesk.PlayerSlotPos);
        PickUp(itemGO);

        // 2. Ir a mesa de trabajo y dejar objeto
        yield return MoveTo(_workStation.WorkDesk.PlayerSlotPos);
        PutDown(itemGO, _workStation.WorkDesk.ItemSlotPos);

        // 3. Procesar
        yield return ProcessRoutine(itemDef);

        // 4. Recoger objeto procesado
        PickUp(itemGO);

        // 5. Volver a mesa de recepción y dejar objeto para el cliente
        yield return MoveTo(_workStation.ReceptionDesk.PlayerSlotPos);
        PutDown(itemGO, _workStation.ReceptionItemPoint.position);

        // 6. Avisar a WorkStation que el trabajo está completo
        _workStation.OnWorkCompleted();

        // 7. Volver a idle
        yield return MoveTo(idlePosition.position);
    }

    // ── Acciones ────────────────────────────────────────────────────
    private void PickUp(GameObject itemGO)
    {
        itemGO.transform.SetParent(headAnchor != null ? headAnchor : transform);
        itemGO.transform.localPosition = Vector3.zero;
    }

    private void PutDown(GameObject itemGO, Vector3 worldPos)
    {
        itemGO.transform.SetParent(null);
        itemGO.transform.position = worldPos;
    }

    // ── Procesado ───────────────────────────────────────────────────
    private IEnumerator ProcessRoutine(ItemDefinition itemDef)
    {
        // El tiempo de proceso puede venir del itemDef o del worker, 
        // usamos el menor para respetar tanto el item como las mejoras del worker.
        float processTime = Mathf.Min(_currentProcessTime, itemDef.baseRepairTime);

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

    // ── Movimiento ──────────────────────────────────────────────────
    private IEnumerator MoveTo(Vector3 target)
    {
        while (Vector3.Distance(transform.position, target) > 0.05f)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                target,
                _currentMoveSpeed * Time.deltaTime);
            yield return null;
        }
        transform.position = target;
    }

    // ── Gizmos ──────────────────────────────────────────────────────
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
