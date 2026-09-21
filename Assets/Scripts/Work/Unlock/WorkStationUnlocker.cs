using UnityEngine;
using System.Collections.Generic;
using System;

[DefaultExecutionOrder(BootOrder.Manager)]
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

    /// <summary>
    /// Instancia el prefab correspondiente a listIndex, valida que tenga
    /// WorkStation, lo inicializa con stationId y lo registra en CustomerManager.
    /// Devuelve null (y destruye lo instanciado) si algo falla.
    /// </summary>
    private WorkStation InstantiateStation(int stationId, int listIndex)
    {
        GameObject go = Instantiate(
            workStationsData[listIndex].prefab,
            workStationSpawnPoints[listIndex].position,
            Quaternion.identity);

        WorkStation workStation = go.GetComponent<WorkStation>();
        if (workStation == null)
        {
            Debug.LogError("[WorkStationUnlocker] El prefab no tiene componente WorkStation.");
            Destroy(go);
            return null;
        }

        workStation.Init(stationId);
        CustomerManager.Instance.RegisterWorkStation(workStation);
        return workStation;
    }

    public void UnlockNextWorkStation()
    {
        if (!CanUnlockNext) return;

        int listIndex = ListIndex(nextIndex);
        WorkStation workStation = InstantiateStation(nextIndex, listIndex);
        if (workStation == null) return;

        EconomyManager.Instance.SpendCoins(NextCost);
        currentWorkStation = workStation;
        nextIndex++;
        OnWorkStationUnlocked?.Invoke();
    }

    public void RestoreWorkStation(WorkStationSaveData data)
    {
        // Salvaguarda: si ya existe una WorkStation registrada con este id
        // (por ejemplo, el taller inicial, inicializado en Awake), no la
        // dupliques — simplemente aplícale los datos guardados.
        WorkStation existing = WorkStationRegistry.Instance.GetById(data.stationId);
        if (existing != null)
        {
            existing.LoadSaveData(data);

            if (data.stationId >= nextIndex)
            {
                nextIndex = data.stationId + 1;
                currentWorkStation = existing;
            }
            return;
        }

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

        WorkStation workStation = InstantiateStation(data.stationId, listIndex);
        if (workStation == null) return;

        workStation.LoadSaveData(data);

        if (data.stationId >= nextIndex)
        {
            nextIndex = data.stationId + 1;
            currentWorkStation = workStation;
        }
    }
}