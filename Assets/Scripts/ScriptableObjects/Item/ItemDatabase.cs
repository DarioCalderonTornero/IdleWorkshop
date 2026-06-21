using UnityEngine;

/// <summary>
/// Base de datos central con todos los objetos restaurables del juego.
/// Crea UNA sola instancia desde: Assets > Create > Restaurador > Item Database
/// Luego asígnala al CustomerManager.
/// </summary>
[CreateAssetMenu(fileName = "ItemDatabase", menuName = "Restaurador/Item Database")]
public class ItemDatabase : ScriptableObject
{
    [SerializeField] private ItemDefinition[] items;

    /// <summary>Devuelve un ItemDefinition aleatorio de la base de datos.</summary>
    public ItemDefinition GetRandom()
    {
        if (items == null || items.Length == 0)
        {
            Debug.LogWarning("[ItemDatabase] No hay ítems definidos en la base de datos.");
            return null;
        }
        return items[Random.Range(0, items.Length)];
    }

    /// <summary>Devuelve todos los ítems (por si necesitas listarlos en UI).</summary>
    public ItemDefinition[] GetAll() => items;
}