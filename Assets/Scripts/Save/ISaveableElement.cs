/// <summary>
/// Algo que se guarda por su cuenta, con identidad propia.
///
/// Existe porque el guardado estaba atado a la jerarquía del taller: una mesa
/// se guardaba como "la mesa nº 2 del taller nº 0". Todo lo que no vivía dentro
/// de un taller —las salas, los asientos, las decoraciones, los carritos, los
/// mostradores— no tenía forma de nombrarse, así que sencillamente no se
/// guardaba, y el jugador perdía cada compra al cerrar.
///
/// Con esto, un elemento nuevo se guarda por el mero hecho de existir: se
/// apunta solo en el <see cref="SaveRegistry"/> al despertar. No hay que tocar
/// <see cref="SaveData"/> ni el SaveManager nunca más.
/// </summary>
public interface ISaveableElement
{
    /// <summary>
    /// Identidad estable en la partida guardada. Se asigna sola en el Editor y
    /// no cambia nunca: es la que une lo que hay en el JSON con lo que hay en
    /// la escena.
    /// </summary>
    string SaveId { get; }

    /// <summary>
    /// Si se guarda por id, o lo guarda otro por él.
    ///
    /// Las mesas dicen que no: las guarda su taller por posición, porque los
    /// talleres 2 en adelante se instancian desde un prefab y un id puesto en
    /// el prefab se repetiría en cada copia.
    /// </summary>
    bool SavesItself { get; }
}

/// <summary>Algo que se compra una vez y se queda comprado.</summary>
public interface ISaveableUnlock : ISaveableElement
{
    bool IsUnlocked { get; }

    /// <summary>
    /// Lo devuelve a desbloqueado al cargar. No cobra: el precio ya se pagó en
    /// la partida que se está restaurando.
    /// </summary>
    void RestoreUnlocked();
}

/// <summary>Algo que sube de nivel.</summary>
public interface ISaveableUpgrade : ISaveableElement
{
    int CurrentLevel { get; }

    void LoadLevel(int level);
}
