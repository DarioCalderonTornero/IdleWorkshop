using UnityEngine;
using System.Collections.Generic;

public class WorkerRegistry : MonoBehaviour
{
    public static WorkerRegistry Instance { get; private set; }

    private readonly List<WorkerBase> _activeWorkers = new();

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void Register(WorkerBase worker)
    {
        if (!_activeWorkers.Contains(worker))
            _activeWorkers.Add(worker);
    }

    public void Unregister(WorkerBase worker)
    {
        _activeWorkers.Remove(worker);
    }

    public void ApplyTapBoostToAll(float seconds)
    {
        foreach (WorkerBase w in _activeWorkers)
            w.ApplyTapBoost(seconds);
    }
}