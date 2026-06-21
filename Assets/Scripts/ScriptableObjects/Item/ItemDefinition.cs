using UnityEngine;

/// <summary>
/// Define un tipo de objeto que los clientes pueden traer a restaurar.
/// Crea instancias desde el menú: Assets > Create > Restaurador > Item Definition
/// </summary>
[CreateAssetMenu(fileName = "Item_", menuName = "Restaurador/Item Definition")]
public class ItemDefinition : ScriptableObject
{
    [Header("Identidad")]
    public string itemName = "Objeto sin nombre";

    [TextArea(2, 4)]
    public string description = "";

    [Header("Visual")]
    [Tooltip("Prefab del objeto: permite configurar layer, tag, colliders, etc.")]
    public GameObject itemPrefab;

    [Tooltip("Sprite que se asignará al SpriteRenderer del prefab en tiempo de ejecución")]
    public Sprite sprite;

    [Tooltip("Escala del sprite cuando se muestra encima de la cabeza del cliente")]
    public Vector2 displayScale = Vector2.one;

    [Header("Restauración")]
    [Tooltip("Tiempo base en segundos que tarda en restaurarse")]
    public float baseRepairTime = 5f;

    [Tooltip("Monedas que da al completarse")]
    public int rewardCoins = 10;
}