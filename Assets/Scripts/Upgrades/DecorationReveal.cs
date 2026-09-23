using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Una decoración que no aparece de golpe sino a trozos: al comprarla sale la
/// primera pieza, y las demás van saliendo según sube su nivel.
///
/// Los umbrales son los tramos de evolución del propio UpgradeData —un tramo
/// por pieza—, que son los mismos que mueven la barra del panel, así que lo que
/// se ve y lo que anuncia la barra no pueden descuadrarse.
///
/// Sirve para cualquier decoración: los asientos del hall, las estanterías del
/// taller o las plantas. Quien necesite además saber cuántas piezas hay
/// disponibles (la sala de espera, para sentar clientes) pregunta por
/// <see cref="VisibleCount"/> en vez de volver a calcularlo por su cuenta.
/// </summary>
public class DecorationReveal : MonoBehaviour, IUpgradePreview, IHiddenUntilBought
{
    /// <summary>Todas sus piezas: en un taller recién abierto no sale ninguna.</summary>
    public IEnumerable<Transform> HiddenParts
    {
        get
        {
            if (pieces == null) yield break;

            foreach (GameObject piece in pieces)
                if (piece != null) yield return piece.transform;
        }
    }

    [Tooltip("Las piezas, en el orden en que van apareciendo")]
    [SerializeField] private GameObject[] pieces;

    [Tooltip("Efecto de aparición de cada pieza, en el mismo orden que 'pieces'. " +
             "Puede quedar vacío: entonces aparecen sin animación")]
    [SerializeField] private Poof[] piecePoofs;

    [Tooltip("Hasta comprarla no se ve nada. Si es null, está disponible desde el principio")]
    [SerializeField] private Unlockable unlockable;

    [Tooltip("La mejora de la que sale el nivel. Sus tramos de evolución deciden " +
             "cuántas piezas se ven")]
    [SerializeField] private UpgradeableBase upgrade;

    /// <summary>Cuántas piezas hay puestas en la escena.</summary>
    public int PieceCount => pieces != null ? pieces.Length : 0;

    /// <summary>Cuántas se ven ahora mismo.</summary>
    public int VisibleCount { get; private set; }

    /// <summary>Si ya se ha comprado.</summary>
    public bool IsUnlocked => unlockable == null || unlockable.IsUnlocked;

    /// <summary>Ha cambiado el número de piezas visibles.</summary>
    public event Action OnVisibleCountChanged;

    private void Awake()
    {
        if (unlockable != null) unlockable.OnUnlocked += HandleUnlocked;
        if (upgrade != null) upgrade.OnLevelChanged += HandleLevelChanged;

        // Se calcula ya en Awake para que quien pregunte antes del primer
        // Start no se encuentre un cero que no es verdad.
        VisibleCount = Calculate();
    }

    private void Start() => Refresh();

    private void OnDestroy()
    {
        if (unlockable != null) unlockable.OnUnlocked -= HandleUnlocked;
        if (upgrade != null) upgrade.OnLevelChanged -= HandleLevelChanged;
    }

    private void HandleUnlocked(Unlockable _) => Refresh();
    private void HandleLevelChanged(int _) => Refresh();

    private void Refresh()
    {
        int previous = VisibleCount;
        VisibleCount = Calculate();

        ApplyVisibility();

        if (VisibleCount != previous) OnVisibleCountChanged?.Invoke();
    }

    private int Calculate()
    {
        if (!IsUnlocked) return 0;

        // Sin mejora asignada se asume que están todas: así la decoración
        // sigue sirviendo si alguien la usa sin sistema de niveles.
        if (upgrade == null || upgrade.UpgradeData == null) return PieceCount;

        return Mathf.Clamp(
            upgrade.UpgradeData.GetReachedStageCount(upgrade.CurrentLevel), 0, PieceCount);
    }

    /// <summary>
    /// La siguiente pieza que va a salir, pero solo cuando falta justo una
    /// mejora para que salga.
    ///
    /// Enseñarla desde mucho antes la convertiría en parte del decorado: se
    /// vería igual con 1 nivel que con 20 por delante y dejaría de significar
    /// nada. Apareciendo solo en el nivel anterior es un aviso de que la
    /// siguiente compra saca algo.
    ///
    /// Bloqueada es el otro caso: ahí lo que falta es comprarla, y el aviso va
    /// en el botón de desbloquear.
    /// </summary>
    public bool TryGetPreview(out GameObject sample, out Vector3 position)
    {
        sample = null;
        position = Vector3.zero;

        if (pieces == null) return false;

        int next = IsUnlocked ? VisibleCount : 0;
        if (next >= pieces.Length || pieces[next] == null) return false;

        if (IsUnlocked && !IsOnePieceAway()) return false;

        sample = pieces[next];
        position = pieces[next].transform.position;
        return true;
    }

    /// <summary>Si con una sola mejora más aparece la siguiente pieza.</summary>
    private bool IsOnePieceAway()
    {
        if (upgrade == null || upgrade.UpgradeData == null) return false;

        int threshold = upgrade.UpgradeData.GetNextStageThreshold(upgrade.CurrentLevel);
        return threshold >= 0 && upgrade.CurrentLevel == threshold - 1;
    }

    private void ApplyVisibility()
    {
        if (pieces == null) return;

        for (int i = 0; i < pieces.Length; i++)
        {
            if (pieces[i] == null) continue;

            bool visible = i < VisibleCount;
            Poof poof = piecePoofs != null && i < piecePoofs.Length ? piecePoofs[i] : null;

            // Mientras se monta la escena y se aplica la partida no se
            // anima: si no, todo lo que empieza escondido soltaría un puf.
            if (poof != null) poof.SetVisible(visible, animate: !BootPhase.IsBooting);
            else if (pieces[i].activeSelf != visible) pieces[i].SetActive(visible);
        }
    }
}
