using UnityEngine;
using System.Collections.Generic;

public class WorkDeskManager : MonoBehaviour
{
    [Header("Mesas en orden (1 a 5)")]
    [SerializeField] private List<WorkTable> allDesks;
    [SerializeField] private List<WorkDeskUnlockable> allUnlockables;

    // Lista de mesas desbloqueadas en orden
    private List<WorkTable> _unlockedDesks = new();

    void Awake()
    {
        // Registra las ya desbloqueadas por defecto
        for (int i = 0; i < allUnlockables.Count; i++)
        {
            allUnlockables[i].OnUnlocked += OnDeskUnlocked;

            if (allUnlockables[i].IsUnlocked)
                _unlockedDesks.Add(allDesks[i]);
        }
    }

    void OnDeskUnlocked(WorkDeskUnlockable unlockable)
    {
        int index = allUnlockables.IndexOf(unlockable);
        if (index >= 0 && index < allDesks.Count)
        {
            if (!_unlockedDesks.Contains(allDesks[index]))
                _unlockedDesks.Add(allDesks[index]);
        }
    }

    // El Worker llama a esto para saber por qué mesas pasar
    public List<WorkTable> GetUnlockedDesks() => _unlockedDesks;

    // Devuelve la siguiente mesa bloqueada (para el panel de desbloqueo)
    public WorkDeskUnlockable GetNextLocked()
    {
        foreach (var u in allUnlockables)
        {
            if (!u.IsUnlocked) return u;
        }
        return null;
    }

    public bool AllUnlocked()
    {
        foreach (var u in allUnlockables)
            if (!u.IsUnlocked) return false;
        return true;
    }
}