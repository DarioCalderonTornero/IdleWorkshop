using UnityEditor;
using UnityEngine;

/// <summary>
/// Crea las capas que el taller necesita, si no existen ya.
///
/// Las capas son ajustes de proyecto, no de escena, así que no se pueden dejar
/// al builder sin más: si alguien clona el repo y construye el layout, el
/// collider de un mueble acabaría en Default y le robaría el toque a la zona
/// que hay debajo. Esto se asegura de que la capa exista antes.
/// </summary>
public static class LayerSetup
{
    /// <summary>
    /// Todo lo que un personaje no puede atravesar: muebles, mostradores y
    /// decoraciones. Va en su propia capa para que el raycast del toque la
    /// ignore y siga abriendo el panel de la sala.
    /// </summary>
    public const string SolidLayer = "Solido";

    public static int SolidLayerIndex => EnsureLayer(SolidLayer);

    /// <summary>
    /// Devuelve el índice de la capa, creándola si hace falta. -1 si no queda
    /// ningún hueco libre.
    /// </summary>
    public static int EnsureLayer(string layerName)
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
        if (assets == null || assets.Length == 0)
        {
            Debug.LogError("[LayerSetup] No encuentro TagManager.asset.");
            return -1;
        }

        SerializedObject tagManager = new(assets[0]);
        SerializedProperty layers = tagManager.FindProperty("layers");

        for (int i = 0; i < layers.arraySize; i++)
            if (layers.GetArrayElementAtIndex(i).stringValue == layerName) return i;

        // De la 0 a la 5 son de Unity. La 3, la 6 y la 7 suelen estar libres,
        // y de la 8 en adelante son todas del usuario.
        for (int i = 6; i < layers.arraySize; i++)
        {
            SerializedProperty layer = layers.GetArrayElementAtIndex(i);
            if (!string.IsNullOrEmpty(layer.stringValue)) continue;

            layer.stringValue = layerName;
            tagManager.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();

            Debug.Log($"[LayerSetup] Creada la capa '{layerName}' en el hueco {i}.");
            return i;
        }

        Debug.LogError($"[LayerSetup] No queda ningún hueco libre para la capa '{layerName}'.");
        return -1;
    }
}
