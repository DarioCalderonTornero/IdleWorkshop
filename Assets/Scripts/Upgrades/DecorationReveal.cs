using System;
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
public class DecorationReveal : MonoBehaviour
{
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

    // El primer repaso monta la escena y va sin animación: si no, todo lo que
    // empieza escondido soltaría un puf en el arranque.
    private bool _settled;

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
        _settled = true;

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

    private void ApplyVisibility()
    {
        if (pieces == null) return;

        for (int i = 0; i < pieces.Length; i++)
        {
            if (pieces[i] == null) continue;

            bool visible = i < VisibleCount;
            Poof poof = piecePoofs != null && i < piecePoofs.Length ? piecePoofs[i] : null;

            if (poof != null) poof.SetVisible(visible, animate: _settled);
            else if (pieces[i].activeSelf != visible) pieces[i].SetActive(visible);
        }
    }
}
