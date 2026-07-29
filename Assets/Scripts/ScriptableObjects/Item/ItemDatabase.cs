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

    public ItemDefinition GetRandom()
    {
        if (items == null || items.Length == 0)
        {
            Debug.LogWarning("[ItemDatabase] No hay ítems definidos.");
            return null;
        }

        ItemRarity rarity = RollRarity();

        // Filtra los items de esa rareza
        ItemDefinition[] pool = items
            .Where(i => i.rarity == rarity)
            .ToArray();

        // Si no hay items de esa rareza, coge uno aleatorio de cualquier rareza
        if (pool.Length == 0)
        {
            Debug.LogWarning($"[ItemDatabase] No hay ítems de rareza {rarity}, usando aleatorio.");
            return items[Random.Range(0, items.Length)];
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