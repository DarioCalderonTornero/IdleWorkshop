using UnityEngine;

/// <summary>
/// La versión del formato de guardado y cómo se traen las partidas viejas.
///
/// Existe porque el formato va a cambiar —quedan por guardar las salas, las
/// decoraciones, los carritos y las recepciones— y las partidas que ya están
/// en los móviles tienen que sobrevivir a ese cambio. Sin un número de versión
/// no hay forma de saber qué tiene delante el juego al abrir un archivo.
/// </summary>
public static class SaveFormat
{
    /// <summary>La versión que escribe esta build.</summary>
    public const int Current = 2;

    /// <summary>
    /// Las partidas anteriores a que existiera el campo `version`.
    ///
    /// Se reconocen solas: JsonUtility deja los campos que no vienen en el
    /// JSON con el valor que tengan puesto en la clase, y ese valor es este.
    /// Por eso <see cref="SaveData.version"/> arranca en 0 y lo pone a
    /// <see cref="Current"/> el que guarda, no el que declara el campo.
    /// </summary>
    public const int Legacy = 0;

    /// <summary>
    /// Sube la partida al formato de esta build. False si no se ha podido, en
    /// cuyo caso no hay que guardar nada encima.
    ///
    /// Las conversiones se encadenan, una por salto de versión, para que una
    /// partida de hace tres formatos suba paso a paso sin casos especiales.
    /// </summary>
    public static bool TryMigrate(SaveData data)
    {
        if (data == null) return false;
        if (data.version == Current) return true;

        // De una build más nueva: no se toca. Lo gestiona quien llama, que es
        // quien puede bloquear el guardado.
        if (data.version > Current) return false;

        int from = data.version;

        // v0 -> v1: no hay nada que convertir. Lo único que cambia es que se
        // deja de escribir `workerLevel`, un campo que ya no leía nadie porque
        // el Worker que mejoraba dejó de existir. Los campos que sobran en el
        // JSON los ignora JsonUtility, así que las partidas viejas entran tal
        // cual.
        if (data.version < 1) data.version = 1;

        // v1 -> v2: aparecen `unlocks` y `upgrades`, las listas por id. Tampoco
        // hay nada que convertir: una partida de la v1 no las trae, así que
        // llega con las listas vacías y cada elemento se queda como de fábrica.
        // Es exactamente lo que pasaba antes, cuando no se guardaban.
        if (data.version < 2) data.version = 2;

        if (data.version != Current)
        {
            Debug.LogError(
                $"[SaveFormat] No sé subir una partida de la v{from} a la v{Current}. " +
                "Falta escribir el paso de migración.");
            return false;
        }

        Debug.Log($"[SaveFormat] Partida migrada de la v{from} a la v{Current}.");
        return true;
    }
}
