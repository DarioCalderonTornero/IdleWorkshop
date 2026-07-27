using System;
using System.Collections.Generic;

[Serializable]
public class SaveData
{
    public double coins;
    public string lastTimeSaved;
    public List<WorkStationSaveData> workStations = new();
    public List<BestiaryItemSaveData> bestiaryItems = new();
}

[Serializable]
public class WorkStationSaveData
{
    public int stationId;
    public int workerLevel;
    public List<DeskSaveData> desks = new();
}

[Serializable]
public class DeskSaveData
{
    public int deskIndex;
    public int level;
    public bool isUnlocked;
}

[Serializable]
public class BestiaryItemSaveData
{
    public string itemName;   
    public bool discovered;
    public int maxStars;
    public int totalSold;
}