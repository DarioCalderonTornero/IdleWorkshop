using System;
using System.Collections.Generic;

[Serializable]
public class SaveData
{
    public double coins;
    public string lastTimeSaved;
    public List<WorkStationSaveData> workStations = new();
}

[Serializable]
public class WorkStationSaveData
{
    public int stationId;
    public int workLevel;
    public List<DeskSaveData> desks = new();
}

[Serializable]
public class DeskSaveData
{
    public int deskIndex;
    public int level;
    public bool isUnlocked;
}

