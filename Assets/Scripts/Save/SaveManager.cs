using System;
using UnityEngine;

/// <summary>
/// Guarda y carga la partida.
///
/// La regla de la casa es que guardar nunca puede empeorar lo que ya hay en
/// disco. De ahí las dos piezas: <see cref="SaveFile"/>, que escribe sin poder
/// dejar el archivo a medias y mantiene una copia, y el bloqueo de más abajo,
/// que corta el guardado en seco cuando escribir destruiría algo.
///
/// Ese bloqueo es la parte importante. Antes, una partida que no se podía leer
/// se registraba en consola y el juego seguía como partida nueva — con el
/// autoguardado activo, así que quince segundos después la machacaba con una
/// partida vacía. Un archivo dañado se convertía en un archivo borrado, en
/// silencio.
/// </summary>
[DefaultExecutionOrder(BootOrder.Load)]
public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    private const float AutoSaveInterval = 15f;

    [Header("Referencias")]
    [SerializeField] private ItemDatabase _itemDatabase;

    private SaveFile _file;
    private float _autoSaveTimer;

    /// <summary>
    /// Por qué no se está guardando, o null si todo va bien.
    ///
    /// Es un texto y no un bool para que el motivo salga en el aviso: si el
    /// juego deja de guardar, hay que poder saber por qué sin leer el código.
    /// </summary>
    private string _blockedReason;

    /// <summary>Si ahora mismo se puede escribir en disco.</summary>
    public bool CanSave => _blockedReason == null;

    /// <summary>
    /// La partida que se aplicó al arrancar, o null si era nueva.
    ///
    /// La lee el <see cref="GameBootstrap"/> para calcular el tiempo offline:
    /// necesita la marca del último guardado y la tasa de producción, y las
    /// necesita cuando ya está todo aplicado.
    /// </summary>
    public SaveData LoadedData { get; private set; }

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        _file = new SaveFile(Application.persistentDataPath);
    }

    private void Start()
    {
        LoadGame();

        // La escena ya está montada entera y la partida aplicada: a partir de
        // aquí, lo que aparezca o desaparezca es porque ha pasado algo de
        // verdad, y toca animarlo.
        BootPhase.End();
    }

    private void Update()
    {
        _autoSaveTimer += Time.deltaTime;

        if (_autoSaveTimer >= AutoSaveInterval)
        {
            _autoSaveTimer = 0f;
            SaveGame();
        }
    }

    // Salir al home en el móvil. Puede que el sistema no nos devuelva el
    // control nunca, así que es la última oportunidad de guardar.
    private void OnApplicationPause(bool pause)
    {
        if (pause) SaveGame();
    }

    private void OnApplicationQuit() => SaveGame();

    // ── Guardar ──────────────────────────────────────────────────────

    public void SaveGame()
    {
        if (!CanSave) return;

        try
        {
            SaveData data = CollectSaveData();
            _file.Write(JsonUtility.ToJson(data, prettyPrint: true));
        }
        catch (Exception e)
        {
            // No se bloquea el guardado: puede ser algo pasajero (sin espacio,
            // archivo ocupado) y el intento de dentro de quince segundos tiene
            // todo el derecho a salir bien. El .tmp que quede se sobrescribe.
            Debug.LogError($"[SaveManager] Error al guardar: {e.Message}");
        }
    }

    private SaveData CollectSaveData()
    {
        SaveData data = new SaveData
        {
            version = SaveFormat.Current,
            coins = EconomyManager.Instance.CurrentCoins,

            // Para el sistema de tiempo offline, que aún está por hacer.
            lastTimeSaved = DateTime.UtcNow.ToString("o"),

            coinsPerSecond = EconomyManager.Instance.CoinsPerSecond,

            workStations = WorkStationRegistry.Instance.GetAllSaveData(),
            bestiaryItems = BestiaryManager.Instance.GetSaveData()
        };

        // Todo lo que se guarda por id: salas, decoraciones, carritos,
        // mostradores, trabajadores de sala. Aquí no hay que añadir nada al
        // meter un elemento nuevo — se apunta él solo.
        SaveRegistry.Capture(data);

        return data;
    }

    // ── Cargar ───────────────────────────────────────────────────────

    public void LoadGame()
    {
        if (!_file.AnyExists)
        {
            Debug.Log("[SaveManager] No hay partida guardada. Partida nueva.");
            return;
        }

        if (TryLoad(_file.TryReadMain, "archivo principal", out SaveData data))
        {
            ApplySaveData(data);
            Debug.Log("[SaveManager] Partida cargada.");
            return;
        }

        if (TryLoad(_file.TryReadBackup, "copia de seguridad", out data))
        {
            // El principal está dañado y la copia no. Se sube la copia a su
            // sitio antes de seguir: si no, el próximo guardado mandaría el
            // archivo dañado a .bak y se llevaría por delante la única copia
            // buena que quedaba.
            _file.PromoteBackup();

            Debug.LogWarning(
                "[SaveManager] El archivo principal no se pudo leer. " +
                "Partida recuperada desde la copia de seguridad.");

            ApplySaveData(data);
            return;
        }

        Block("hay una partida guardada en disco que no se ha podido leer, " +
              "ni ella ni su copia");
    }

    private delegate bool Reader(out string json);

    /// <summary>
    /// Intenta sacar una partida de uno de los dos archivos. False si no está,
    /// no se lee, no es una partida o no se puede migrar — y entonces quien
    /// llama pasa al siguiente.
    /// </summary>
    private bool TryLoad(Reader read, string label, out SaveData data)
    {
        data = null;

        if (!read(out string json)) return false;

        try
        {
            data = JsonUtility.FromJson<SaveData>(json);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[SaveManager] El {label} está dañado y se descarta: {e.Message}");
            return false;
        }

        if (data == null)
        {
            Debug.LogWarning($"[SaveManager] El {label} no contiene ninguna partida.");
            return false;
        }

        if (data.version > SaveFormat.Current)
        {
            // Guardar encima la convertiría al formato viejo y se perdería
            // todo lo que esta build no sabe leer.
            Block($"el {label} es de una versión más nueva del juego " +
                  $"(v{data.version}; esta build entiende hasta la v{SaveFormat.Current})");

            data = null;
            return false;
        }

        if (!SaveFormat.TryMigrate(data))
        {
            Debug.LogWarning($"[SaveManager] No se ha podido migrar el {label}.");
            data = null;
            return false;
        }

        return true;
    }

    private void ApplySaveData(SaveData data)
    {
        LoadedData = data;

        EconomyManager.Instance.LoadCoins(data.coins);
        EconomyManager.Instance.LoadRate(data.coinsPerSecond);
        WorkStationRegistry.Instance.LoadAllSaveData(data.workStations);
        BestiaryManager.Instance.LoadSaveData(data.bestiaryItems, _itemDatabase);
        SaveRegistry.Restore(data);
    }

    // ── Bloqueo ──────────────────────────────────────────────────────

    /// <summary>
    /// Corta el guardado porque escribir destruiría algo. Se queda con el
    /// primer motivo, que es el que explica de verdad lo que pasó.
    /// </summary>
    private void Block(string reason)
    {
        if (!CanSave) return;

        _blockedReason = reason;

        Debug.LogError(
            $"[SaveManager] GUARDADO DESACTIVADO: {reason}. " +
            "No se escribirá nada para no perder lo que hubiera. " +
            $"Archivo: {_file.MainPath}");
    }

    // ── Helpers de desarrollo ────────────────────────────────────────

    [ContextMenu("Borrar partida guardada")]
    public void DeleteSave()
    {
        _file.Delete();

        // Sin esto, al cerrar el juego se volvería a escribir el archivo que
        // se acaba de borrar.
        _blockedReason = "se ha borrado la partida a mano";
        Debug.Log("[SaveManager] Partida borrada. No se guardará más en esta sesión.");
    }
}
