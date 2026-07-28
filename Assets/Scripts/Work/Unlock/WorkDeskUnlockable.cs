using UnityEngine;

public class WorkDeskUnlockable : MonoBehaviour
{
    [System.Serializable]
    public class LevelRequirement
    {
        [Tooltip("Mesa que debe tener el nivel mínimo")]
        public WorkDeskUpgradeable desk;
        [Tooltip("Nivel mínimo requerido")]
        public int minLevel;
    }

    [Header("Desbloqueo")]
    [SerializeField] private double unlockCost = 500;

    [Header("Requisitos de nivel (opcional)")]
    [Tooltip("Mesas que deben estar en un nivel mínimo para poder desbloquear esta")]
    [SerializeField] private LevelRequirement[] levelRequirements;

    [Header("Visual")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Color lockedColor = new Color(0.3f, 0.3f, 0.3f, 1f);

    [Header("Estado inicial")]
    [SerializeField] private bool unlockedByDefault = false;

    public bool IsUnlocked { get; private set; } = false;
    public double UnlockCost => unlockCost;

    public System.Action<WorkDeskUnlockable> OnUnlocked;

    [Header("Material")]
    [SerializeField] private ItemMaterial material = ItemMaterial.Cloth;
    public ItemMaterial Material => material;

    void Awake()
    {
        if (unlockedByDefault)
        {
            IsUnlocked = true;
            SetVisual(true);
        }
        else
        {
            SetVisual(false);
        }
    }

    // Comprueba si se cumplen todos los requisitos para desbloquear
    public bool MeetsRequirements()
    {
        if (levelRequirements == null) return true;

        foreach (var req in levelRequirements)
        {
            if (req.desk == null) continue;
            if (req.desk.CurrentLevel < req.minLevel)
                return false;
        }
        return true;
    }

    // Devuelve una descripción de qué requisitos faltan (para mostrar en UI)
    public string GetMissingRequirementsText()
    {
        if (levelRequirements == null) return "";

        System.Text.StringBuilder sb = new();
        foreach (var req in levelRequirements)
        {
            if (req.desk == null) continue;
            if (req.desk.CurrentLevel < req.minLevel)
            {
                sb.AppendLine($"• {req.desk.UpgradeData?.elementName ?? req.desk.gameObject.name} " +
                              $"nivel {req.minLevel} (actual: {req.desk.CurrentLevel})");
            }
        }
        return sb.ToString();
    }

    public void Unlock()
    {
        if (IsUnlocked) return;
        IsUnlocked = true;
        SetVisual(true);
        OnUnlocked?.Invoke(this);
    }

    void SetVisual(bool unlocked)
    {
        if (spriteRenderer != null)
            spriteRenderer.color = unlocked ? Color.white : lockedColor;
    }

}