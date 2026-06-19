using System.Collections;
using UnityEngine;

/// <summary>
/// Worker genérico que ejecuta el loop de trabajo dentro de su WorkStation.
/// No tiene referencias directas al CustomerManager ni a otros sistemas.
/// Toda comunicación hacia fuera pasa por WorkStation.
/// 
/// LOOP:
///  1. Ir a mesa de recepción → animar recogida → objeto parented al worker.
///  2. Ir a mesa de trabajo LLEVANDO el objeto.
///  3. Dejar objeto en mesa de trabajo al llegar.
///  4. Procesar (barra de progreso).
///  5. Animar recogida → objeto parented al worker.
///  6. Volver a mesa de recepción LLEVANDO el objeto.
///  7. Dejar objeto para el cliente.
///  8. Avisar a WorkStation → trabajo completado.
///  9. Volver a posición idle.
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
        // 1. Ir a mesa de recepción
        yield return MoveTo(_workStation.ReceptionDesk.PlayerSlotPos);

        // 2. Parentear objeto al headAnchor del worker
        itemGO.transform.SetParent(headAnchor != null ? headAnchor : transform);
        itemGO.transform.localPosition = Vector3.zero;

        // 3. Ir a WorkDesk LLEVANDO el objeto en la cabeza
        yield return MoveTo(_workStation.WorkDesk.PlayerSlotPos);

        // 4. Desparentear al llegar
        PutDown(itemGO, _workStation.WorkDesk.ItemSlotPos);

        // 5. Procesar
        yield return ProcessRoutine(itemDef);

        // 6. Animar recogida: objeto va de la mesa de trabajo al headAnchor y queda parented
        yield return PickUpAnim(itemGO);

        // 7. Volver a recepción LLEVANDO el objeto (parented al headAnchor)
        yield return MoveTo(_workStation.ReceptionDesk.PlayerSlotPos);

        // 8. Desparentear y dejar el objeto para el cliente
        PutDown(itemGO, _workStation.ReceptionItemPoint.position);

        // 9. Avisar a WorkStation que el trabajo está completo
        _workStation.OnWorkCompleted();

        // 10. Volver a idle
        yield return MoveTo(idlePosition.position);
    }

    // ── Acciones ────────────────────────────────────────────────────
    private void PutDown(GameObject itemGO, Vector3 worldPos)
    {
        itemGO.transform.SetParent(null);
        itemGO.transform.position = worldPos;
    }

    // ── Animación de recogida ────────────────────────────────────────
    private IEnumerator PickUpAnim(GameObject itemGO)
    {
        Vector3 startPos = itemGO.transform.position;
        Transform anchor = headAnchor != null ? headAnchor : transform;

        // Capturamos la posición del anchor AHORA, cuando el worker está parado
        Vector3 targetPos = anchor.position;

        float elapsed = 0f;
        float animTime = 0.35f;

        while (elapsed < animTime)
        {
            elapsed += Time.deltaTime;
            itemGO.transform.position = Vector3.Lerp(
                startPos,
                targetPos,
                Mathf.SmoothStep(0f, 1f, elapsed / animTime));
            yield return null;
        }

        // Al terminar la animación, parentear al anchor
        // A partir de aquí el objeto se mueve con el worker automáticamente
        itemGO.transform.SetParent(anchor);
        itemGO.transform.localPosition = Vector3.zero;
    }

    // ── Procesado ───────────────────────────────────────────────────
    private IEnumerator ProcessRoutine(ItemDefinition itemDef)
    {
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