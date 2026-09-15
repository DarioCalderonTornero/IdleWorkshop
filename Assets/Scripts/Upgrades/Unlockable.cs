using System;
using UnityEngine;

/// <summary>
/// Algo del mapa que se compra una vez y a partir de ahí aparece: los asientos
/// del hall, por ejemplo.
///
/// Mientras está bloqueado, los objetos de <see cref="revealOnUnlock"/> están
/// apagados, así que no se ven ni funcionan.
/// </summary>
public class Unlockable : MonoBehaviour, IUnlockable
{
    [Header("Desbloqueo")]
    [SerializeField] private double unlockCost = 500;

    [Tooltip("Marcar para que empiece ya desbloqueado")]
    [SerializeField] private bool unlockedByDefault;

    [Tooltip("Lo que aparece al comprarlo. Se apaga mientras siga bloqueado")]
    [SerializeField] private GameObject[] revealOnUnlock;

    public bool IsUnlocked { get; private set; }
    public double UnlockCost => unlockCost;

    /// <summary>Se dispara al comprarlo, para que quien dependa de ello reaccione.</summary>
    public event Action<Unlockable> OnUnlocked;

    private void Awake()
    {
        IsUnlocked = unlockedByDefault;
        ApplyVisibility();
    }

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
}
