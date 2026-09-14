// WorkTable.cs
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Una mesa del taller. Tiene dos zonas: la de espera, donde los objetos
/// aguardan su turno, y la parte contraria, donde quedan los ya terminados si
/// esta resulta ser la última mesa desbloqueada.
///
/// La mesa en sí es el contenedor de entrada (implementa IItemContainer): lo
/// que se le "entrega" cae en la zona de espera.
/// </summary>
public class WorkTable : MonoBehaviour, IItemContainer
{
    [Header("Puntos")]
    [Tooltip("Punto donde el trabajador se queda de pie mientras trabaja")]
    [SerializeField] private Transform playerSlot;
    [Tooltip("Punto encima de la mesa donde se procesa el objeto")]
    [SerializeField] private Transform itemSlot;
    [Tooltip("Punto de referencia de la mesa, usado si no hay zona de espera")]
    [SerializeField] private Transform boxPoint;

    [Header("Zonas de la mesa")]
    [Tooltip("Zona de espera: aquí se acumulan los objetos pendientes de procesar")]
    [SerializeField] private ItemStack inStack;

    [Tooltip("Parte contraria de la mesa: aquí quedan los terminados cuando esta " +
             "es la última mesa desbloqueada, a la espera de que pase el carrito")]
    [SerializeField] private ItemStack outStack;

    [Header("Worker propio de esta mesa")]
    [SerializeField] private Worker worker;

    [Header("Multiplicadores base")]
    [SerializeField] private float baseTimeMultiplier = 1f;
    [SerializeField] private float baseRewardMultiplier = 1f;

    // Multiplicador propio de nivel (viene de WorkDeskUpgradeable)
    private float _levelTimeMultiplier;
    private float _levelRewardMultiplier;

    // Bonus de zona, aportado por los DecorativeUpgradeable de la misma WorkStation
    private float _zoneTimeMultiplier = 1f;
    private float _zoneRewardMultiplier = 1f;

    private float _currentStarChance;

    /// <summary>Ha llegado trabajo a la zona de espera.</summary>
    public event Action OnItemEnqueued;

    void Awake()
    {
        _levelTimeMultiplier = baseTimeMultiplier;
        _levelRewardMultiplier = baseRewardMultiplier;
        _currentStarChance = 0f;
    }

    void Start()
    {
        if (worker != null)
            worker.Init(this, GetComponentInParent<WorkStation>());
    }

    public Vector3 PlayerSlotPos => playerSlot.position;
    public Vector3 ItemSlotPos => itemSlot.position;
    public Vector3 BoxPointPos => boxPoint != null ? boxPoint.position : transform.position;

    // Expuesto para que el Worker propio de esta mesa pueda usar este
    // mismo Transform como su posición idle (ver Worker.Init).
    public Transform PlayerSlotTransform => playerSlot;

    /// <summary>Parte contraria de la mesa. Null si esta mesa no tiene salida.</summary>
    public ItemStack OutStack => outStack;

    // ── IItemContainer: la mesa es su propia zona de espera ───────────
    public int Count => inStack != null ? inStack.Count : 0;
    public int Capacity => inStack != null ? inStack.Capacity : 0;
    public bool HasSpace => inStack == null || inStack.HasSpace;
    public bool IsFull => inStack != null && inStack.IsFull;
    public Vector3 AccessPointPos => inStack != null ? inStack.AccessPointPos : BoxPointPos;
    public Vector3 ContentsPos => inStack != null ? inStack.ContentsPos : BoxPointPos;

    public bool TryEnqueue(ItemOrder order)
    {
        if (order == null || inStack == null) return false;
        if (!inStack.TryEnqueue(order)) return false;

        OnItemEnqueued?.Invoke();
        return true;
    }

    public bool TryDequeue(out ItemOrder order)
    {
        if (inStack == null)
        {
            order = null;
            return false;
        }

        return inStack.TryDequeue(out order);
    }

    // ── Cálculo de tiempo/recompensa ──────────────────────────────────
    public float GetProcessTime(ItemDefinition item)
        => Mathf.Max(0.1f, item.baseRepairTime * _levelTimeMultiplier * _zoneTimeMultiplier);

    public int GetReward(ItemDefinition item)
        => Mathf.RoundToInt(item.rewardCoins * _levelRewardMultiplier * _zoneRewardMultiplier);

    // Llamado desde WorkDeskUpgradeable al mejorar (nivel propio de la mesa)
    public void ApplyMultipliers(float timeMultiplier, float rewardMultiplier)
    {
        _levelTimeMultiplier = timeMultiplier;
        _levelRewardMultiplier = rewardMultiplier;
    }

    // Llamado desde WorkStation al recalcular el bonus decorativo de la zona
    public void ApplyZoneMultipliers(float zoneTimeMultiplier, float zoneRewardMultiplier)
    {
        _zoneTimeMultiplier = zoneTimeMultiplier;
        _zoneRewardMultiplier = zoneRewardMultiplier;
    }

    public void ApplyUpgradeData(UpgradeData data, int level)
    {
        _currentStarChance = data.GetStarChanceForLevel(level);
    }

    public bool RollStar()
    {
        if (_currentStarChance <= 0f) return false;
        return UnityEngine.Random.Range(0f, 100f) < _currentStarChance;
    }

    public float CurrentStarChance => _currentStarChance;

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (playerSlot != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(playerSlot.position, 0.12f);
        }
        if (itemSlot != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(itemSlot.position, 0.12f);
        }
        if (boxPoint != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(boxPoint.position, 0.12f);
        }
    }
#endif
}
