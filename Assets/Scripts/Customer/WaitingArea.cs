using UnityEngine;

/// <summary>
/// Los asientos donde los clientes esperan a que su objeto esté listo.
///
/// No aparecen todos de golpe: al comprarlos sale el primero y los demás van
/// saliendo según sube el nivel de la mejora. De eso se encarga el
/// <see cref="DecorationReveal"/>, que es el mismo componente que usan el resto
/// de decoraciones del taller; aquí solo se reparten los sitios.
///
/// Quien no encuentra sitio se va del mapa y vuelve cuando le toca, así que
/// esto no limita cuántos encargos puede haber en marcha: solo cuántos clientes
/// se ven esperando a la vez.
/// </summary>
public class WaitingArea : MonoBehaviour
{
    [Tooltip("Un transform por asiento, en el orden en que van apareciendo")]
    [SerializeField] private Transform[] seats;

    [Tooltip("Quién decide cuántos asientos se ven. Si es null, valen todos")]
    [SerializeField] private DecorationReveal reveal;

    private bool[] _taken;

    /// <summary>Cuántos asientos hay puestos en la escena.</summary>
    public int SeatCount => seats != null ? seats.Length : 0;

    /// <summary>
    /// Cuántos están disponibles ahora mismo. Sale del mismo sitio que las
    /// piezas que se ven, así que nunca se puede reservar un asiento invisible.
    /// </summary>
    public int AvailableSeats =>
        reveal != null ? Mathf.Min(reveal.VisibleCount, SeatCount) : SeatCount;

    /// <summary>Si ya se han comprado los asientos.</summary>
    public bool IsUnlocked => reveal == null || reveal.IsUnlocked;

    private void Awake() => EnsureState();

    // ── Reservar y soltar ────────────────────────────────────────────

    /// <summary>
    /// Reserva el primer asiento libre de los que hay disponibles. False si
    /// están todos ocupados, o si todavía no hay ninguno.
    /// </summary>
    public bool TryClaimSeat(out int index, out Vector3 position)
    {
        EnsureState();

        int available = AvailableSeats;

        for (int i = 0; i < available && i < _taken.Length; i++)
        {
            if (_taken[i] || seats[i] == null) continue;

            _taken[i] = true;
            index = i;
            position = seats[i].position;
            return true;
        }

        index = -1;
        position = Vector3.zero;
        return false;
    }

    public void ReleaseSeat(int index)
    {
        EnsureState();

        if (index < 0 || index >= _taken.Length) return;
        _taken[index] = false;
    }

    /// <summary>
    /// Por si algo pide un asiento antes del Awake, o si se cambian los
    /// asientos en el editor con el juego corriendo.
    /// </summary>
    private void EnsureState()
    {
        if (_taken == null || _taken.Length != SeatCount)
            _taken = new bool[SeatCount];
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (seats == null) return;

        int available = AvailableSeats;

        for (int i = 0; i < seats.Length; i++)
        {
            if (seats[i] == null) continue;

            bool isAvailable = i < available;
            bool taken = _taken != null && i < _taken.Length && _taken[i];

            Gizmos.color = !isAvailable ? new Color(0.4f, 0.4f, 0.4f, 0.4f)
                         : taken ? new Color(1f, 0.5f, 0.2f)
                         : new Color(0.4f, 0.8f, 1f);

            Gizmos.DrawWireCube(seats[i].position, new Vector3(0.3f, 0.3f, 0f));
        }
    }
#endif
}
