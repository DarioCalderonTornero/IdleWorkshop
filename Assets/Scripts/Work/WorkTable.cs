// WorkTable.cs
using System;
using System.Collections.Generic;
using UnityEngine;

public class WorkTable : MonoBehaviour
{
    [Tooltip("Punto donde el jugador se para mientras trabaja")]
    [SerializeField] private Transform playerSlot;
    [Tooltip("Punto encima de la mesa donde se deposita el objeto")]
    [SerializeField] private Transform itemSlot;
    [Tooltip("Punto donde se acumulan los objetos pendientes (caja de entrada)")]
    [SerializeField] private Transform boxPoint;

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

    // ── Cola de ítems pendientes ────────────────────────────────────
    private readonly Queue<ItemDefinition> _pendingItems = new();
    public event Action OnItemEnqueued;

    [Header("Visual de la caja (opcional)")]
    [SerializeField] private SpriteRenderer boxRenderer;
    [SerializeField] private Sprite boxEmptySprite;
    [SerializeField] private Sprite boxPartialSprite;
    [SerializeField] private Sprite boxFullSprite;
    [Tooltip("A partir de cuántos ítems acumulados se considera 'llena'")]
    [SerializeField] private int boxFullThreshold = 5;

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

        UpdateBoxVisual();
    }

    public Vector3 PlayerSlotPos => playerSlot.position;
    public Vector3 ItemSlotPos => itemSlot.position;
    public Vector3 BoxPointPos => boxPoint.position;

    // Expuesto para que el Worker propio de esta mesa pueda usar este
    // mismo Transform como su posición idle (ver Worker.Init).
    public Transform PlayerSlotTransform => playerSlot;

    // ── Cola ─────────────────────────────────────────────────────────
    public void EnqueueItem(ItemDefinition item)
    {
        _pendingItems.Enqueue(item);
        UpdateBoxVisual();
        OnItemEnqueued?.Invoke();
    }

    public bool TryDequeueItem(out ItemDefinition item)
    {
        bool success = _pendingItems.TryDequeue(out item);
        if (success) UpdateBoxVisual();
        return success;
    }

    private void UpdateBoxVisual()
    {
        if (boxRenderer == null) return;

        int count = _pendingItems.Count;

        if (count <= 0)
            boxRenderer.sprite = boxEmptySprite;
        else if (count >= boxFullThreshold)
            boxRenderer.sprite = boxFullSprite;
        else
            boxRenderer.sprite = boxPartialSprite;
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