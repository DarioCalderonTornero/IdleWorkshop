using UnityEngine;
using System.Collections.Generic;

public class BestiaryManager : MonoBehaviour
{
    public static BestiaryManager Instance { get; private set; }

    private HashSet<ItemDefinition> _discovered = new();

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // Llamado desde Worker al terminar de procesar en una mesa
    public void RegisterItem(ItemDefinition item)
    {
        if (item == null) return;
        _discovered.Add(item);
    }

    public bool IsDiscovered(ItemDefinition item) => _discovered.Contains(item);
}