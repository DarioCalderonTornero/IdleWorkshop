using UnityEngine;

/// <summary>
/// De dónde salen los ids del guardado.
///
/// Se asignan solos en el Editor, no a mano: un id escrito a dedo se puede
/// repetir sin que nadie se entere, y dos elementos con el mismo id se pisan
/// el progreso al cargar. Si algo sale mal, `Taller > Revisar guardado` lo
/// detecta y lo arregla.
///
/// El id es opaco a propósito. Podría llevar el nombre del objeto, que se
/// leería mucho mejor en el JSON, pero entonces renombrar una lámpara en la
/// escena borraría su progreso. La legibilidad la pone la herramienta de
/// revisión, que imprime la tabla de id a ruta en la jerarquía.
/// </summary>
public static class SaveIdentity
{
    /// <summary>Un id nuevo. Corto, porque hay uno por elemento y se leen en el JSON.</summary>
    public static string NewId() => System.Guid.NewGuid().ToString("N").Substring(0, 12);

#if UNITY_EDITOR
    /// <summary>
    /// Le pone id al elemento si todavía no tiene.
    ///
    /// Se llama desde OnValidate, así que ocurre solo con abrir la escena o al
    /// añadir el componente: no hay ningún paso manual que se pueda olvidar.
    /// Marca la escena como sucia para que Unity pida guardarla — hasta que no
    /// se guarde, el id no es permanente.
    /// </summary>
    public static void EnsureAssigned(Object target, ref string saveId)
    {
        // En Play los ids ya tienen que estar puestos. Generar uno aquí daría
        // un elemento que se guarda distinto cada partida.
        if (Application.isPlaying) return;

        if (!string.IsNullOrEmpty(saveId)) return;

        // Nunca dentro de un prefab. El id quedaría guardado en el prefab y
        // todas sus copias lo compartirían, así que en vez de identificar a
        // cada una las confundiría entre sí — que es lo que pasó con el
        // trabajador de las mesas, tres copias con el mismo id.
        //
        // Lo que vive en un prefab lo tiene que guardar su dueño (ver
        // WorkerUpgradeable y WorkDeskUpgradeable, que se declaran fuera del
        // registro con SavesItself). Quedarse sin id aquí es lo correcto: si
        // además intentara guardarse, el registro lo dirá al arrancar.
        if (UnityEditor.PrefabUtility.IsPartOfPrefabAsset(target)) return;

        saveId = NewId();
        UnityEditor.EditorUtility.SetDirty(target);
    }
#endif
}
