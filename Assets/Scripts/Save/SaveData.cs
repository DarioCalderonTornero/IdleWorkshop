using System;
using System.Collections.Generic;

[Serializable]
public class SaveData
{
    /// <summary>
    /// Formato con el que se escribió esta partida.
    ///
    /// Arranca en Legacy (0) a propósito: JsonUtility deja los campos que no
    /// vienen en el JSON con el valor declarado aquí, así que una partida
    /// anterior a este campo se lee como v0 y la migración la reconoce. Quien
    /// guarda es el que lo pone a SaveFormat.Current.
    /// </summary>
    public int version = SaveFormat.Legacy;

    public double coins;
    public string lastTimeSaved;

    /// <summary>
    /// Monedas por segundo que producía el montaje del jugador al guardar.
    ///
    /// Es lo que paga el tiempo offline: se mide mientras juega en vez de
    /// simular el taller, así que una fuente de ingresos nueva entra sola sin
    /// tocar nada. Una partida anterior a este campo lo lee como 0 y
    /// sencillamente no cobra offline la primera vez.
    /// </summary>
    public double coinsPerSecond;
    public List<WorkStationSaveData> workStations = new();
    public List<BestiaryItemSaveData> bestiaryItems = new();

    /// <summary>
    /// Lo comprado y el nivel de cada elemento, por id.
    ///
    /// Son listas planas y no un campo por cosa a propósito: así añadir una
    /// sala, una decoración o un carrito nuevo no toca este archivo. El
    /// elemento se apunta solo en el SaveRegistry y aparece aquí.
    /// </summary>
    public List<UnlockSaveData> unlocks = new();
    public List<UpgradeSaveData> upgrades = new();
}

/// <summary>Algo que el jugador compró y se queda comprado.</summary>
[Serializable]
public class UnlockSaveData
{
    public string id;
    public bool unlocked;
}

/// <summary>El nivel al que el jugador subió algo.</summary>
[Serializable]
public class UpgradeSaveData
{
    public string id;
    public int level;
}

[Serializable]
public class WorkStationSaveData
{
    public int stationId;
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
