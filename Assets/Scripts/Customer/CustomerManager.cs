using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawner y gestor de la cola de clientes.
/// </summary>
public class CustomerManager : MonoBehaviour
{
    [Header("Camino compartido")]
    [Tooltip("Transforms en orden: el último es el pie de la columna de espera")]
    [SerializeField] private Transform[] pathWaypoints;

    [Header("Cola (frente a la mesa de recepción)")]
    [Tooltip("Punto donde el primer cliente SE PARA a esperar")]
    [SerializeField] private Transform deskSlot;

    [Tooltip("Punto encima de la mesa donde el objeto cae y queda para el jugador")]
    [SerializeField] private Transform itemDropTarget;

    [Tooltip("Separación en Y entre clientes (negativo = cola hacia abajo)")]
    [SerializeField] private float slotSpacingY = -1.2f;

    [Header("Spawn")]
    [SerializeField] private GameObject customerPrefab;
    [SerializeField] private ItemDatabase itemDatabase;
    [SerializeField] private float spawnInterval = 4f;
    [SerializeField] private int maxCustomers = 5;

    [Header("Referencias")]
    [SerializeField] private PlayerController player;

    private readonly List<Customer> _queue = new();

    // ── Unity ───────────────────────────────────────────────────────
    private void Start()
    {
        if (player == null)
            player = FindAnyObjectByType<PlayerController>();

        StartCoroutine(SpawnLoop());
    }

    // ── Spawn ────────────────────────────────────────────────────────
    private IEnumerator SpawnLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnInterval);
            if (_queue.Count < maxCustomers)
                SpawnCustomer();
        }
    }

    private void SpawnCustomer()
    {
        if (pathWaypoints == null || pathWaypoints.Length == 0)
        {
            Debug.LogWarning("[CustomerManager] No hay waypoints definidos.");
            return;
        }

        ItemDefinition itemDef = itemDatabase != null ? itemDatabase.GetRandom() : null;
        if (itemDef == null)
        {
            Debug.LogWarning("[CustomerManager] No se pudo obtener un ItemDefinition.");
            return;
        }

        GameObject go = Instantiate(customerPrefab, pathWaypoints[0].position, Quaternion.identity);
        Customer customer = go.GetComponent<Customer>();

        int slotIndex = _queue.Count;
        Vector3 slotPos = GetSlotPosition(slotIndex);

        customer.Init(this, pathWaypoints, slotPos, slotIndex, itemDef, itemDropTarget.position);
        _queue.Add(customer);
    }

    // ── Posiciones de cola ───────────────────────────────────────────
    public Vector3 GetSlotPosition(int index)
    {
        return deskSlot.position + Vector3.up * slotSpacingY * index;
    }

    // ── Callbacks desde Customer ─────────────────────────────────────

    /// <summary>El cliente ha dejado el objeto. Avisar al jugador.</summary>
    public void OnItemPlacedOnDesk(ItemDefinition itemDef, GameObject itemGO)
    {
        player?.OnItemAvailable(itemGO);
    }

    /// <summary>
    /// El cliente ha recogido su objeto y va a salir.
    /// Avanzar la cola AHORA, sin esperar a que cruce el borde.
    /// </summary>
    public void OnCustomerLeaving(Customer customer)
    {
        _queue.Remove(customer);

        for (int i = 0; i < _queue.Count; i++)
            _queue[i].MoveToSlot(i, GetSlotPosition(i));
    }

    /// <summary>El cliente ha cruzado el borde. Destruir el GameObject.</summary>
    public void OnCustomerDone(Customer customer)
    {
        Destroy(customer.gameObject);
    }

    /// <summary>Llamar cuando el objeto reparado está de vuelta en la mesa.</summary>
    public void ServeFirstCustomer()
    {
        if (_queue.Count == 0) return;
        _queue[0].BeServed();
    }

    // ── Gizmos ───────────────────────────────────────────────────────
#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (pathWaypoints != null)
        {
            Gizmos.color = Color.yellow;
            for (int i = 0; i < pathWaypoints.Length; i++)
            {
                if (pathWaypoints[i] == null) continue;
                Gizmos.DrawSphere(pathWaypoints[i].position, 0.1f);
                if (i > 0 && pathWaypoints[i - 1] != null)
                    Gizmos.DrawLine(pathWaypoints[i - 1].position, pathWaypoints[i].position);
            }
        }

        if (deskSlot != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(deskSlot.position, 0.15f);
            Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
            for (int i = 0; i < maxCustomers; i++)
                Gizmos.DrawWireSphere(GetSlotPosition(i), 0.1f);
        }

        if (itemDropTarget != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(itemDropTarget.position, 0.15f);
        }
    }
#endif
}