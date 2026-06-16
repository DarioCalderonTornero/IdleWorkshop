using System.Collections;
using UnityEngine;

/// <summary>
/// Controlador automático del jugador (idle).
///
/// LOOP:
///  1. Espera 0.5s tras recibir el aviso.
///  2. Va a recepción → coge el objeto.
///  3. Va a la mesa de trabajo → deja el objeto.
///  4. Círculo de progreso 0→1 en repairDuration segundos.
///  5. Recoge el objeto reparado.
///  6. Va a recepción → deja el objeto en receptionItemPoint.
///  7. Avisa a CustomerManager → el cliente recoge el objeto y se va.
///  8. Vuelve a idle.
/// </summary>
public class PlayerController : MonoBehaviour
{
    [Header("Posiciones")]
    [Tooltip("Posición de espera sin trabajo")]
    [SerializeField] private Transform idlePosition;

    [Tooltip("Donde el jugador se para en la mesa de recepción")]
    [SerializeField] private Transform receptionSlot;

    [Tooltip("Punto encima de la mesa de recepción donde está/deja el objeto")]
    [SerializeField] private Transform receptionItemPoint;

    [Tooltip("Mesa de trabajo del taller")]
    [SerializeField] private WorkTable workTable;

    [Header("Objeto en cabeza")]
    [Tooltip("Transform hijo encima de la cabeza del jugador")]
    [SerializeField] private Transform headAnchor;

    [Header("Reparación")]
    [SerializeField] private float repairDuration = 4f;

    [Header("Movimiento")]
    [SerializeField] private float moveSpeed = 4f;

    [Header("UI")]
    [SerializeField] private RepairProgressUI progressUI;

    // ── Estado ─────────────────────────────────────────────────────
    private bool _busy;
    private CustomerManager _customerManager;

    // ── Init ───────────────────────────────────────────────────────
    private void Start()
    {
        _customerManager = FindAnyObjectByType<CustomerManager>();

        if (idlePosition != null)
            transform.position = idlePosition.position;
    }

    // ── API pública ────────────────────────────────────────────────
    public void OnItemAvailable(GameObject itemGO)
    {
        if (_busy) return;
        StartCoroutine(RepairLoop(itemGO));
    }

    // ── Loop principal ─────────────────────────────────────────────
    private IEnumerator RepairLoop(GameObject itemGO)
    {
        _busy = true;

        // 1. Pequeña pausa (el cliente acaba de soltar el objeto)
        yield return new WaitForSeconds(0.5f);

        // 2. Ir a recepción y coger el objeto
        yield return MoveTo(receptionSlot.position);
        PickUp(itemGO);

        // 3. Ir a la mesa de trabajo y dejar el objeto
        yield return MoveTo(workTable.PlayerSlotPos);
        PutDown(itemGO, workTable.ItemSlotPos);

        // 4. Reparar (círculo de progreso)
        yield return RepairRoutine();

        // 5. Recoger el objeto reparado
        PickUp(itemGO);

        // 6. Volver a recepción y dejar el objeto para el cliente
        yield return MoveTo(receptionSlot.position);
        PutDown(itemGO, receptionItemPoint.position);

        // 7. Avisar al manager: el cliente puede recoger el objeto y marcharse
        _customerManager.ServeFirstCustomer();

        // 8. Volver a idle
        yield return MoveTo(idlePosition.position);

        _busy = false;
    }

    // ── Acciones ───────────────────────────────────────────────────
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

    // ── Reparación ─────────────────────────────────────────────────
    private IEnumerator RepairRoutine()
    {
        float elapsed = 0f;
        progressUI?.Show(0f);

        while (elapsed < repairDuration)
        {
            elapsed += Time.deltaTime;
            progressUI?.SetFill(elapsed / repairDuration);
            yield return null;
        }

        progressUI?.SetFill(1f);
        progressUI?.Hide();
    }

    // ── Movimiento ─────────────────────────────────────────────────
    private IEnumerator MoveTo(Vector3 target)
    {
        while (Vector3.Distance(transform.position, target) > 0.05f)
        {
            transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);
            yield return null;
        }
        transform.position = target;
    }

    // ── Gizmos ─────────────────────────────────────────────────────
#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (idlePosition != null)
        {
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(idlePosition.position, 0.12f);
        }
        if (receptionSlot != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(receptionSlot.position, 0.12f);
        }
        if (receptionItemPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(receptionItemPoint.position, 0.12f);
        }
    }
#endif
}