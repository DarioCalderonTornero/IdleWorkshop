using UnityEngine;
using System.Collections.Generic;

public class WorkStationRegistry : MonoBehaviour
{
    public static WorkStationRegistry Instance { get; private set; }

    private List<WorkStation> workStations = new();

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

    public void Register (WorkStation workStation)
    {
        if (!workStations.Contains(workStation))
        {
            workStations.Add(workStation);
            Debug.Log($"[WorkStationRegistry] Taller registrado. ID: {workStation.StationId}");
        }
    }

    public void UnRegister(WorkStation workStation)
    {
        workStations.Remove(workStation);
    }

    public List<WorkStationSaveData> GetAllSaveData()
    {
        List<WorkStationSaveData> result = new();

        foreach(WorkStation ws in workStations)
        {
            result.Add(ws.GetSaveData());
        }

        return result;
    }

    public void LoadAllSaveData(List<WorkStationSaveData> saveDataList)
    {
        foreach (WorkStationSaveData data in saveDataList)
        {
            WorkStation ws = GetById(data.stationId);

            if (ws != null)
                ws.LoadSaveData(data);
            else
                WorkStationUnlocker.Instance.RestoreWorkStation(data);
        }
    }

    private WorkStation GetById(int stationId)
    {
        foreach (WorkStation ws in workStations)
            if (ws.StationId == stationId) return ws;
        return null;
    }
}
