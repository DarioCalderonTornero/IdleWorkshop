/// <summary>
/// Algo que empieza bloqueado y se compra con monedas: una mesa del taller,
/// los asientos del hall...
///
/// Existe para que el panel de mejoras sepa tratar cualquier cosa bloqueada sin
/// conocer su tipo concreto.
/// </summary>
public interface IUnlockable
{
    bool IsUnlocked { get; }

    /// <summary>
    /// Si viene ya comprado de fábrica, como la primera mesa. Lo usa el
    /// progreso del taller para no contar como conseguido algo que el jugador
    /// nunca tuvo que pagar.
    /// </summary>
    bool StartsUnlocked { get; }

    /// <summary>Lo que cuesta desbloquearlo.</summary>
    double UnlockCost { get; }

    /// <summary>Si se cumplen los requisitos previos (niveles de otras cosas, etc.).</summary>
    bool MeetsRequirements();

    /// <summary>Texto de los requisitos que faltan, para mostrarlo en la UI.</summary>
    string GetMissingRequirementsText();

    void Unlock();
}
