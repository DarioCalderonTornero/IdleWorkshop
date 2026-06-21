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
        if (HasNext && EconomyManager.Instance.CanAfford(NextCost))
        {
            EconomyManager.Instance.SpendCoins(NextCost);
            GameObject go = Instantiate(workStationsData[nextIndex].prefab, workStationSpawnPoints[nextIndex].position, Quaternion.identity);
            WorkStation workStation = go.GetComponent<WorkStation>();

            if(workStation == null)
            {
                Debug.LogError("[WorkStationUnlocker] El prefab no tiene componente WorkStation.");
                return;
            }

            CustomerManager.Instance.RegisterWorkStation(workStation);
            nextIndex++;
            OnWorkStationUnlocked?.Invoke();
        }
    }
}
