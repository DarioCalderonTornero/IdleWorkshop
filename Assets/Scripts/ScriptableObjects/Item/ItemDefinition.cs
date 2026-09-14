using UnityEngine;

public enum ItemRarity
{
    Common,
    Uncommon,
    Rare,
    Epic,
    Legendary
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

    [Tooltip("Opcional. Si se deja vacío se usa el sprite que tenga el propio prefab; " +
             "solo hay que rellenarlo para que este objeto concreto se vea distinto.")]
    public Sprite sprite;

    [Tooltip("Multiplicador sobre la escala del prefab, no un tamaño absoluto. " +
             "Dejar en 1,1 para que el objeto salga exactamente como esté el prefab.")]
    public Vector2 displayScale = Vector2.one;

    [Header("Rareza")]
    public ItemRarity rarity = ItemRarity.Common;

    [Header("Restauraci�n")]
    public float baseRepairTime = 5f;
    public int rewardCoins = 10;
}