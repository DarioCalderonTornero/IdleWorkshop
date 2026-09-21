using UnityEngine;
using System.Collections.Generic;

[DefaultExecutionOrder(BootOrder.Manager)]
public class BestiaryManager : MonoBehaviour
{
    public static BestiaryManager Instance { get; private set; }

    private HashSet<ItemDefinition> _discovered = new();
    private Dictionary<ItemDefinition, int> _maxStars = new();
    private Dictionary<ItemDefinition, int> _totalSold = new();   // nuevo

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ── API de juego ─────────────────────────────────────────────────

    public void RegisterItem(ItemDefinition item, int starsThisRound)
    {
        if (item == null) return;

        _discovered.Add(item);

        if (!_maxStars.ContainsKey(item))
            _maxStars[item] = starsThisRound;
        else if (starsThisRound > _maxStars[item])
            _maxStars[item] = starsThisRound;
    }

    // Llamado cuando el objeto completa todas las mesas y se devuelve al cliente
    public void RegisterSold(ItemDefinition item)
    {
        if (item == null) return;

        if (!_totalSold.ContainsKey(item))
            _totalSold[item] = 1;
        else
            _totalSold[item]++;
    }

    public bool IsDiscovered(ItemDefinition item) => _discovered.Contains(item);
    public int GetMaxStars(ItemDefinition item)
        => _maxStars.TryGetValue(item, out int stars) ? stars : 0;
    public int GetTotalSold(ItemDefinition item)
        => _totalSold.TryGetValue(item, out int sold) ? sold : 0;

    // ── Guardado ─────────────────────────────────────────────────────

    public List<BestiaryItemSaveData> GetSaveData()
    {
        List<BestiaryItemSaveData> result = new();

        foreach (ItemDefinition item in _discovered)
        {
            result.Add(new BestiaryItemSaveData
            {
                itemName = item.itemName,
                discovered = true,
                maxStars = _maxStars.TryGetValue(item, out int stars) ? stars : 0,
                totalSold = _totalSold.TryGetValue(item, out int sold) ? sold : 0
            });
        }

        return result;
    }

    // ── Carga ─────────────────────────────────────────────────────────

    public void LoadSaveData(List<BestiaryItemSaveData> saveData, ItemDatabase database)
    {
        if (saveData == null || database == null) return;

        Dictionary<string, ItemDefinition> lookup = new();
        foreach (ItemDefinition item in database.GetAll())
            if (item != null && !lookup.ContainsKey(item.itemName))
                lookup[item.itemName] = item;

        foreach (BestiaryItemSaveData entry in saveData)
        {
            if (!lookup.TryGetValue(entry.itemName, out ItemDefinition item)) continue;

            if (entry.discovered) _discovered.Add(item);
            if (entry.maxStars > 0) _maxStars[item] = entry.maxStars;
            if (entry.totalSold > 0) _totalSold[item] = entry.totalSold;
        }
    }
}