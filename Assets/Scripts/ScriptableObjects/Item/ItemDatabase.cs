using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[CreateAssetMenu(fileName = "ItemDatabase", menuName = "Restaurador/Item Database")]
public class ItemDatabase : ScriptableObject
{
    [SerializeField] private ItemDefinition[] items;

    [Header("Probabilidad de rareza (deben sumar 100)")]
    [SerializeField] private float chanceCommon = 80f;
    [SerializeField] private float chanceUncommon = 10f;
    [SerializeField] private float chanceRare = 7f;
    [SerializeField] private float chanceEpic = 2.5f;
    [SerializeField] private float chanceLegendary = 0.5f;

    public ItemDefinition[] GetAll() => items;

    public ItemDefinition GetRandom(ItemMaterial material)
    {
        if (items == null || items.Length == 0)
        {
            Debug.LogWarning("[ItemDatabase] No hay ítems definidos.");
            return null;
        }

        ItemRarity rarity = RollRarity();

        // Filtra por rareza Y material
        ItemDefinition[] pool = items
            .Where(i => i.rarity == rarity && i.material == material)
            .ToArray();

        // Si no hay ítems de esa rareza para ese material, cae a cualquier rareza de ese material
        if (pool.Length == 0)
        {
            pool = items.Where(i => i.material == material).ToArray();
        }

        if (pool.Length == 0)
        {
            Debug.LogWarning($"[ItemDatabase] No hay ítems del material {material}.");
            return null;
        }

        return pool[Random.Range(0, pool.Length)];
    }

    private ItemRarity RollRarity()
    {
        float roll = Random.Range(0f, 100f);

        if (roll < chanceCommon) return ItemRarity.Common;
        roll -= chanceCommon;

        if (roll < chanceUncommon) return ItemRarity.Uncommon;
        roll -= chanceUncommon;

        if (roll < chanceRare) return ItemRarity.Rare;
        roll -= chanceRare;

        if (roll < chanceEpic) return ItemRarity.Epic;

        return ItemRarity.Legendary;
    }
}