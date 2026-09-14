using UnityEngine;

/// <summary>
/// Creación del GameObject que representa un objeto mientras viaja por el
/// taller. Centralizado aquí porque el cliente, el worker de mesa y la mesa de
/// recogida lo instancian exactamente igual.
/// </summary>
public static class ItemVisual
{
    public static GameObject Spawn(ItemDefinition itemDef)
    {
        if (itemDef == null || itemDef.itemPrefab == null) return null;

        GameObject go = Object.Instantiate(itemDef.itemPrefab);

        // El orden se fuerza aquí en vez de confiar en el del prefab: el objeto
        // tiene que verse por encima de quien lo transporte, esté donde esté.
        foreach (SpriteRenderer sr in go.GetComponentsInChildren<SpriteRenderer>(includeInactive: true))
            sr.sortingOrder = SortingOrders.Item;

        // El sprite del SO solo manda si hay uno puesto. Si está vacío se
        // respeta el del prefab, igual que con la escala: así el prefab define
        // el aspecto por defecto y el SO solo lo cambia cuando se quiere.
        if (itemDef.sprite != null && go.TryGetComponent<SpriteRenderer>(out var main))
        {
            main.sprite = itemDef.sprite;
        }

        // displayScale multiplica la escala del prefab en vez de sustituirla:
        // el tamaño lo manda el prefab y el SO solo lo retoca si hace falta.
        Vector3 prefabScale = go.transform.localScale;
        go.transform.localScale = new Vector3(
            prefabScale.x * itemDef.displayScale.x,
            prefabScale.y * itemDef.displayScale.y,
            prefabScale.z);

        return go;
    }
}
