using UnityEngine;
using System.Collections.Generic;
using System;

public class WorkStationUnlocker : MonoBehaviour
{
    public static WorkStationUnlocker Instance { get; private set; }

    public event Action OnWorkStationUnlocked;

    [SerializeField] private List<WorkStationData> workStationsData;
    private int nextIndex = 0;

    // El taller inicial ocupa el stationId 0 fuera de estas listas,
    // así que el stationId N corresponde al índice (N - 1) de las listas.
    private int ListIndex(int stationId) => stationId - 1;

    public bool HasNext => ListIndex(nextIndex) < workStationsData.Count;
    public double NextCost => HasNext ? workStationsData[ListIndex(nextIndex)].cost : 0;

    [SerializeField] private List<Transform> workStationSpawnPoints;

    [Header("Taller inicial (ya presente en la escena)")]
    [SerializeField] private WorkStation initialWorkStation;

    private WorkStation currentWorkStation;

    public bool CanUnlockNext =>
        HasNext &&
        EconomyManager.Instance.CanAfford(NextCost) &&
        (currentWorkStation == null || currentWorkStation.AllDesksUnlocked());

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (workStationsData.Count != workStationSpawnPoints.Count)
        {
            Debug.LogError($"[WorkStationUnlocker] Desincronización: {workStationsData.Count} WorkStationData vs {workStationSpawnPoints.Count} SpawnPoints.");
        }

        if (initialWorkStation != null)
        {
            initialWorkStation.Init(0);
            nextIndex = 1;
            currentWorkStation = initialWorkStation;
        }
        else
        {
            Debug.LogError("[WorkStationUnlocker] No se ha asignado el taller inicial en el Inspector.");
        }
    }

    public void UnlockNextWorkStation()
    {
        if (!CanUnlockNext) return;

        int listIndex = ListIndex(nextIndex);

        GameObject go = Instantiate(workStationsData[listIndex].prefab, workStationSpawnPoints[listIndex].position, Quaternion.identity);
        WorkStation workStation = go.GetComponent<WorkStation>();
        if (workStation == null)
        {
            Debug.LogError("[WorkStationUnlocker] El prefab no tiene componente WorkStation.");
            Destroy(go);
            return;
        }

        workStation.Init(nextIndex);
        EconomyManager.Instance.SpendCoins(NextCost);
        CustomerManager.Instance.RegisterWorkStation(workStation);
        currentWorkStation = workStation;
        nextIndex++;
        OnWorkStationUnlocked?.Invoke();
    }

    public void RestoreWorkStation(WorkStationSaveData data)
    {
        int listIndex = ListIndex(data.stationId);

        if (listIndex >= workStationsData.Count)
        {
            Debug.LogError($"[WorkStationUnlocker] No hay WorkStationData para el ID {data.stationId}");
            return;
        }
        if (listIndex >= workStationSpawnPoints.Count)
        {
            Debug.LogError($"[WorkStationUnlocker] No hay SpawnPoint para el ID {data.stationId}");
            return;
        }

        GameObject go = Instantiate(
            workStationsData[listIndex].prefab,
            workStationSpawnPoints[listIndex].position,
            Quaternion.identity);
        WorkStation workStation = go.GetComponent<WorkStation>();
        if (workStation == null)
        {
            Debug.LogError("[WorkStationUnlocker] El prefab no tiene componente WorkStation.");
            Destroy(go);
            return;
        }

        workStation.Init(data.stationId);
        CustomerManager.Instance.RegisterWorkStation(workStation);
        workStation.LoadSaveData(data);

        if (data.stationId >= nextIndex)
        {
            nextIndex = data.stationId + 1;
            currentWorkStation = workStation;
        }
    }
}