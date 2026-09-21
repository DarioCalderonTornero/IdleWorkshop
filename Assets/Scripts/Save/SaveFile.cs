using System.IO;

/// <summary>
/// El archivo de guardado en disco, y nada más: no sabe qué hay dentro.
///
/// Su trabajo es que escribir no pueda destruir lo que ya había. Antes se
/// escribía directamente encima con File.WriteAllText, así que si el sistema
/// mataba el proceso a media escritura —cerrar la app en el móvil, quedarse
/// sin batería— el JSON quedaba cortado por la mitad y la partida se perdía
/// entera, sin copia de la que tirar.
///
/// Ahora cada guardado hace tres pasos, y morir en cualquiera de ellos deja
/// siempre un archivo bueno:
///
///   1. Se escribe entero en un .tmp aparte. El bueno sigue intacto.
///   2. El bueno pasa a ser la copia .bak.
///   3. El .tmp pasa a ser el bueno.
///
/// No usa nada de UnityEngine a propósito: el directorio se lo pasa quien lo
/// crea. Así esta lógica, que es la que protege la partida, se puede ejecutar
/// y comprobar fuera del Editor.
/// </summary>
public class SaveFile
{
    private readonly string _main;
    private readonly string _backup;
    private readonly string _temp;

    public SaveFile(string directory, string fileName = "savegame.json")
    {
        _main = Path.Combine(directory, fileName);
        _backup = _main + ".bak";
        _temp = _main + ".tmp";
    }

    public string MainPath => _main;
    public string BackupPath => _backup;

    /// <summary>Si hay algo que intentar leer, sea el bueno o la copia.</summary>
    public bool AnyExists => File.Exists(_main) || File.Exists(_backup);

    public bool TryReadMain(out string json) => TryRead(_main, out json);

    public bool TryReadBackup(out string json) => TryRead(_backup, out json);

    private static bool TryRead(string path, out string json)
    {
        json = null;

        if (!File.Exists(path)) return false;

        try
        {
            json = File.ReadAllText(path);
            return true;
        }
        catch
        {
            // Que el archivo esté ahí pero no se deje leer cuenta igual que no
            // tenerlo: quien llama pasará a la copia.
            return false;
        }
    }

    /// <summary>
    /// Guarda sin poder dejar el archivo a medias. Puede lanzar: quien llama
    /// decide qué hacer si el disco falla.
    /// </summary>
    public void Write(string json)
    {
        File.WriteAllText(_temp, json);

        if (File.Exists(_main))
        {
            if (File.Exists(_backup)) File.Delete(_backup);
            File.Move(_main, _backup);
        }

        File.Move(_temp, _main);
    }

    /// <summary>
    /// Sube la copia al puesto del bueno.
    ///
    /// Se llama justo después de recuperar una partida desde la copia. Sin
    /// esto, el archivo principal se quedaría siendo el dañado, y el primer
    /// guardado lo mandaría a .bak pisando la única copia buena que quedaba.
    /// </summary>
    public void PromoteBackup()
    {
        if (!File.Exists(_backup)) return;

        File.Copy(_backup, _main, overwrite: true);
    }

    /// <summary>Borra la partida entera, copia incluida.</summary>
    public void Delete()
    {
        foreach (string path in new[] { _main, _backup, _temp })
            if (File.Exists(path)) File.Delete(path);
    }
}
