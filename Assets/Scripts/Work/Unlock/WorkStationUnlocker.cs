using UnityEngine;
using System.Collections.Generic;
using System;

public class WorkStationUnlocker : MonoBehaviour
{
    public static WorkStationUnlocker Instance { get; private set; }

    //Events
    public event Action OnWorkStationUnlocked;

    [SerializeField] private List <WorkStationData> workStationsData;
    private int nextIndex = 0;

    public bool HasNext => nextIndex < workStationsData.Count;
    public double NextCost => HasNext ? workStationsData[nextIndex].cost : 0;

    [SerializeField] private List<Transform> workStationSpawnPoints;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void UnlockNextWorkStation()
    {
        if (!HasNext || !EconomyManager.Instance.CanAfford(NextCost)) return;

        GameObject go = Instantiate(workStationsData[nextIndex].prefab, workStationSpawnPoints[nextIndex].position, Quaternion.identity);
        WorkStation workStation = go.GetComponent<WorkStation>();

        if (workStation == null)
        {
            Debug.LogError("[WorkStationUnlocker] El prefab no tiene componente WorkStation.");
            Destroy(go);
            return;
        }

        EconomyManager.Instance.SpendCoins(NextCost);
        CustomerManager.Instance.RegisterWorkStation(workStation);
        nextIndex++;
        OnWorkStationUnlocked?.Invoke();
    }

    /// <summary>
    /// Instancia un taller al cargar la partida guardada.
    /// No gasta monedas ni dispara OnWorkStationUnlocked.
    /// </summary>
    public void RestoreWorkStation(WorkStationSaveData data)
    {
        if (data.stationId >= workStationsData.Count)
        {
            Debug.LogError($"[WorkStationUnlocker] No hay WorkStationData para el ID {data.stationId}");
            return;
        }

        if (data.stationId >= workStationSpawnPoints.Count)
        {
            Debug.LogError($"[WorkStationUnlocker] No hay SpawnPoint para el ID {data.stationId}");
            return;
        }

        GameObject go = Instantiate(
            workStationsData[data.stationId].prefab,
            workStationSpawnPoints[data.stationId].position,
            Quaternion.identity);

        WorkStation workStation = go.GetComponent<WorkStation>();

        if (workStation == null)
        {
            Debug.LogError("[WorkStationUnlocker] El prefab no tiene componente WorkStation.");
            Destroy(go);
            return;
        }

        CustomerManager.Instance.RegisterWorkStation(workStation);
        workStation.LoadSaveData(data);

        if (data.stationId >= nextIndex)
            nextIndex = data.stationId + 1;
    }
}
