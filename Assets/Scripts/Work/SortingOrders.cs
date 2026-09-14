using UnityEngine;

/// <summary>
/// Orden de dibujado de todo lo que hay en el taller. El proyecto tiene una
/// única Sorting Layer ("Default"), así que el orden lo decide enteramente el
/// sortingOrder — y por eso conviene que esté en un solo sitio en vez de
/// repartido en números sueltos por prefabs y scripts.
///
/// Se dejan huecos de 10 para poder colar cosas entre medias sin renumerar.
/// </summary>
public static class SortingOrders
{
    /// <summary>Los suelos de cada zona.</summary>
    public const int Floor = 0;

    /// <summary>Mesas, mostradores, escritorios, carriles y asientos.</summary>
    public const int Furniture = 10;

    /// <summary>Bandejas de los montones y la caja del carrito.</summary>
    public const int Container = 20;

    /// <summary>Trabajadores y clientes.</summary>
    public const int Actor = 30;

    /// <summary>
    /// El objeto que se está restaurando. Va por encima de los actores a
    /// propósito: tiene que verse siempre, lo lleve quien lo lleve.
    /// </summary>
    public const int Item = 40;

    /// <summary>Textos flotantes y círculos de progreso.</summary>
    public const int Popup = 50;

    /// <summary>
    /// Fija el orden en todos los SpriteRenderer de un objeto y sus hijos.
    /// Útil para prefabs que se instancian en runtime y traen su propio orden.
    /// </summary>
    public static void Apply(GameObject root, int order)
    {
        if (root == null) return;

        foreach (SpriteRenderer sr in root.GetComponentsInChildren<SpriteRenderer>(includeInactive: true))
            sr.sortingOrder = order;
    }
}
