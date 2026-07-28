using UnityEngine;

public enum ItemRarity
{
    Common,
    Uncommon,
    Rare,
    Epic,
    Legendary
}

public enum ItemMaterial
{
    Cloth,
    Wood
}

[CreateAssetMenu(fileName = "Item_", menuName = "Restaurador/Item Definition")]
public class ItemDefinition : ScriptableObject
{
    [Header("Identidad")]
    public string itemName = "Objeto sin nombre";

    [TextArea(2, 4)]
    public string description = "";

    [Header("Visual")]
    public GameObject itemPrefab;
    public Sprite sprite;
    public Vector2 displayScale = Vector2.one;

    [Header("Rareza")]
    public ItemRarity rarity = ItemRarity.Common;

    [Header("Material")]
    public ItemMaterial material = ItemMaterial.Cloth;

    [Header("Restauración")]
    public float baseRepairTime = 5f;
    public int rewardCoins = 10;
}