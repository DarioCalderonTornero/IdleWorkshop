using System;
using System.IO;
using UnityEngine;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    private const string SaveFileName = "savegame.json";
    private const float autoSaveInterval = 15f;

    private string SaveFilePath => Path.Combine(Application.persistentDataPath, SaveFileName);

    private float _autoSaveTimer;

    private bool autoSave = true;

    [Header("Referencias")]
    [SerializeField] private ItemDatabase _itemDatabase;

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

    private void Start()
    {
        LoadGame();
        autoSave = true;
    }

    private void Update()
    {
        _autoSaveTimer += Time.deltaTime;

        if (_autoSaveTimer >= autoSaveInterval)
        {
            _autoSaveTimer = 0;
            SaveGame();
            //Debug.Log($"[SaveManager] Juego guardado en {SaveFilePath}");
        }
    }

    private void OnApplicationPause(bool pause)
    {
        if (pause) SaveGame();
    }

    private void OnApplicationQuit()
    {
        SaveGame();
    }

    public void SaveGame()
    {
        if (!autoSave) return;

        SaveData data = CollectSaveData();

        try
        {
            string json = JsonUtility.ToJson(data, prettyPrint: true);
            File.WriteAllText(SaveFilePath, json);
            Debug.Log($"[SaveManager] Juego guardado en {SaveFilePath}");
        }

        catch (Exception e)
        {
            Debug.LogError($"[SaveManager] Error al guardar: {e.Message}");
        }
    }

    public void LoadGame()
    {
        if (!File.Exists(SaveFilePath))
        {
            Debug.Log("[SaveManager] No hay archivo de guardado. Partida nueva.");
            return;
        }

        try
        {
            string json = File.ReadAllText(SaveFilePath);
            SaveData data = JsonUtility.FromJson<SaveData>(json);
            ApplySaveData(data);
            Debug.Log("[SaveManager] Juego cargado correctamente.");
        }

        catch (Exception e)
        {
            Debug.LogError($"[SaveManager] Error al cargar: {e.Message}");
        }
    }

    private SaveData CollectSaveData()
    {
        SaveData data = new SaveData();

        // Monedas
        data.coins = EconomyManager.Instance.CurrentCoins;

        // Timestamp para tiempo offline
        data.lastTimeSaved = DateTime.UtcNow.ToString("o");

        // Talleres
        data.workStations = WorkStationRegistry.Instance.GetAllSaveData();

        data.bestiaryItems = BestiaryManager.Instance.GetSaveData();

        return data;
    }

    private void ApplySaveData(SaveData data)
    {
        // Monedas
        EconomyManager.Instance.LoadCoins(data.coins);

        // Talleres
        WorkStationRegistry.Instance.LoadAllSaveData(data.workStations);

        BestiaryManager.Instance.LoadSaveData(data.bestiaryItems, _itemDatabase);  // nuevo
    }

    //---HELPERS---
    [ContextMenu("Borrar Save")]
    public void DeleteSave()
    {
        if (File.Exists(SaveFilePath))
        {
            File.Delete(SaveFilePath);
            Debug.Log("Save Deleted");
            autoSave = false;
        }
    }
}
