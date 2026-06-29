using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class BestiaryUI : MonoBehaviour
{
    [Header("Panel principal")]
    [SerializeField] private GameObject bestiaryPanel;
    [SerializeField] private Transform gridContainer;       // contenedor de la cuadrícula
    [SerializeField] private GameObject itemCellPrefab;     // prefab de cada celda

    [Header("Panel de detalle")]
    [SerializeField] private GameObject detailPanel;
    [SerializeField] private Image detailIcon;
    [SerializeField] private TextMeshProUGUI detailName;
    [SerializeField] private TextMeshProUGUI detailDescription;
    [SerializeField] private TextMeshProUGUI detailRarity;
    [SerializeField] private Button detailCloseButton;

    [Header("Base de datos")]
    [SerializeField] private ItemDatabase itemDatabase;

    [Header("Visual bloqueado")]
    [SerializeField] private Sprite lockedSprite;   // silueta negra
    [SerializeField] private Color lockedColor = Color.black;

    [Header("Colores de rareza")]
    [SerializeField] private Color colorCommon = Color.white;
    [SerializeField] private Color colorUncommon = Color.green;
    [SerializeField] private Color colorRare = Color.blue;
    [SerializeField] private Color colorEpic = new Color(0.6f, 0f, 1f);
    [SerializeField] private Color colorLegendary = Color.yellow;

    [Header("Estrellas en detalle")]
    [SerializeField] private Image[] starImages;        // 5 imágenes de estrella
    [SerializeField] private Sprite starFilled;         // estrella iluminada
    [SerializeField] private Sprite starEmpty;          // estrella vacía/apagada

    public static bool IsOpen { get; private set; } = false;

    void Start()
    {
        bestiaryPanel.SetActive(false);
        detailPanel.SetActive(false);
        detailCloseButton.onClick.AddListener(() => detailPanel.SetActive(false));
    }

    public void OpenBestiary()
    {
        IsOpen = true;
        bestiaryPanel.SetActive(true);
        PopulateGrid();
    }

    public void CloseBestiary()
    {
        IsOpen = false;
        bestiaryPanel.SetActive(false);
        detailPanel.SetActive(false);
    }

    void PopulateGrid()
    {
        // Limpia celdas anteriores
        foreach (Transform child in gridContainer)
            Destroy(child.gameObject);

        ItemDefinition[] allItems = itemDatabase.GetAll();

        foreach (ItemDefinition item in allItems)
        {
            bool discovered = BestiaryManager.Instance.IsDiscovered(item);
            GameObject cell = Instantiate(itemCellPrefab, gridContainer);
            SetupCell(cell, item, discovered);
        }
    }

    void SetupCell(GameObject cell, ItemDefinition item, bool discovered)
    {
        Image icon = cell.transform.Find("Icon").GetComponent<Image>();
        TextMeshProUGUI nameText = cell.transform.Find("Name").GetComponent<TextMeshProUGUI>();
        Button button = cell.GetComponent<Button>();

        // Siempre usa el mismo sprite
        icon.sprite = item.sprite;

        if (discovered)
        {
            icon.color = Color.white;       // se ve normal
            nameText.text = item.itemName;
            button.onClick.AddListener(() => OpenDetail(item));
        }
        else
        {
            icon.color = Color.black;       // silueta negra, mismo sprite
            nameText.text = "???";
            button.onClick.RemoveAllListeners();
        }
    }

    void OpenDetail(ItemDefinition item)
    {
        detailPanel.SetActive(true);
        detailIcon.sprite = item.sprite;
        detailName.text = item.itemName;
        detailDescription.text = item.description;
        detailRarity.text = GetRarityText(item.rarity);
        detailRarity.color = GetRarityColor(item.rarity);

        // Actualiza las estrellas
        int maxStars = BestiaryManager.Instance.GetMaxStars(item);
        for (int i = 0; i < starImages.Length; i++)
        {
            starImages[i].sprite = i < maxStars ? starFilled : starEmpty;
        }
    }

    string GetRarityText(ItemRarity rarity) => rarity switch
    {
        ItemRarity.Common => "Común",
        ItemRarity.Uncommon => "Poco común",
        ItemRarity.Rare => "Raro",
        ItemRarity.Epic => "Épico",
        ItemRarity.Legendary => "Legendario",
        _ => ""
    };

    Color GetRarityColor(ItemRarity rarity) => rarity switch
    {
        ItemRarity.Common => colorCommon,
        ItemRarity.Uncommon => colorUncommon,
        ItemRarity.Rare => colorRare,
        ItemRarity.Epic => colorEpic,
        ItemRarity.Legendary => colorLegendary,
        _ => Color.white
    };
}