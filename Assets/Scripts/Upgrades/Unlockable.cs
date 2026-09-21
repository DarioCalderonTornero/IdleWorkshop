using System;
using UnityEngine;

/// <summary>
/// Algo del mapa que se compra una vez y a partir de ahí aparece: los asientos
/// del hall, el almacén, la habitación.
///
/// Mientras está bloqueado, los objetos de <see cref="revealOnUnlock"/> están
/// apagados, así que no se ven ni funcionan.
///
/// Se guarda solo: le basta con existir en la escena con su id puesto. Antes no
/// se guardaba nada de esto, así que el jugador pagaba miles de monedas por el
/// almacén, cerraba el juego, y al volver no tenía ni el dinero ni el almacén —
/// y el panel se lo volvía a ofrecer a precio completo.
/// </summary>
public class Unlockable : MonoBehaviour, IUnlockable, ISaveableUnlock
{
    [Header("Desbloqueo")]
    [SerializeField] private double unlockCost = 500;

    [Tooltip("Marcar para que empiece ya desbloqueado")]
    [SerializeField] private bool unlockedByDefault;

    [Tooltip("Lo que aparece al comprarlo. Se apaga mientras siga bloqueado")]
    [SerializeField] private GameObject[] revealOnUnlock;

    [Header("Guardado")]
    [Tooltip("Identidad de este elemento en la partida guardada. Se asigna sola: " +
             "no la escribas a mano ni la copies de otro objeto. Si dos elementos " +
             "acaban con el mismo id, 'Taller > Revisar guardado' lo detecta y lo arregla")]
    [SerializeField] private string saveId;

    public bool IsUnlocked { get; private set; }
    public double UnlockCost => unlockCost;

    // ── ISaveableUnlock ──────────────────────────────────────────────
    public string SaveId => saveId;
    public bool SavesItself => true;

    /// <summary>
    /// Lo devuelve a comprado al cargar la partida. Es lo mismo que comprarlo,
    /// porque comprar no cobra aquí: el precio lo cobra el panel antes de
    /// llamar a <see cref="Unlock"/>.
    /// </summary>
    public void RestoreUnlocked() => Unlock();

    /// <summary>Se dispara al comprarlo, para que quien dependa de ello reaccione.</summary>
    public event Action<Unlockable> OnUnlocked;

    private void Awake()
    {
        IsUnlocked = unlockedByDefault;
        ApplyVisibility();

        SaveRegistry.Register(this);
    }

    private void OnDestroy() => SaveRegistry.Unregister(this);

    /// <summary>No tiene requisitos previos: basta con poder pagarlo.</summary>
    public bool MeetsRequirements() => true;

    public string GetMissingRequirementsText() => string.Empty;

    public void Unlock()
    {
        if (IsUnlocked) return;

        IsUnlocked = true;
        ApplyVisibility();
        OnUnlocked?.Invoke(this);
    }

    private void ApplyVisibility()
    {
        if (revealOnUnlock == null) return;

        foreach (GameObject go in revealOnUnlock)
            if (go != null) go.SetActive(IsUnlocked);
    }

#if UNITY_EDITOR
    private void OnValidate() => SaveIdentity.EnsureAssigned(this, ref saveId);
#endif
}
