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
    /// El saco de las bolsas y del carrito. Por encima de los actores, para que
    /// el que empuja el carrito no tape lo que lleva, pero por debajo del
    /// objeto: si empatan con él, Unity elige el orden por su cuenta y el
    /// objeto desaparece detrás del saco a ratos.
    /// </summary>
    public const int Sack = 35;

    /// <summary>
    /// El objeto que se está restaurando. Por encima de todo lo del taller a
    /// propósito: tiene que verse siempre, lo lleve quien lo lleve y esté
    /// dentro del saco que esté. Solo los textos y círculos lo tapan.
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
