using UnityEngine;

/// <summary>
/// Los asientos donde los clientes esperan a que su objeto esté listo.
///
/// Quien no encuentra sitio se va del mapa y vuelve cuando le toca, así que
/// esto no limita cuántos encargos puede haber en marcha: solo cuántos clientes
/// se ven esperando a la vez.
/// </summary>
public class WaitingArea : MonoBehaviour
{
    [Tooltip("Un transform por asiento. El orden no importa")]
    [SerializeField] private Transform[] seats;

    private bool[] _taken;

    public int SeatCount => seats != null ? seats.Length : 0;

    /// <summary>Asientos ocupados ahora mismo. Solo para depurar.</summary>
    public int TakenCount
    {
        get
        {
            EnsureState();

            int count = 0;
            foreach (bool taken in _taken)
                if (taken) count++;

            return count;
        }
    }

    private void Awake() => EnsureState();

    /// <summary>Reserva el primer asiento libre. False si están todos ocupados.</summary>
    public bool TryClaimSeat(out int index, out Vector3 position)
    {
        EnsureState();

        for (int i = 0; i < _taken.Length; i++)
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

        for (int i = 0; i < seats.Length; i++)
        {
            if (seats[i] == null) continue;

            bool taken = _taken != null && i < _taken.Length && _taken[i];
            Gizmos.color = taken ? new Color(1f, 0.5f, 0.2f) : new Color(0.4f, 0.8f, 1f);
            Gizmos.DrawWireCube(seats[i].position, new Vector3(0.3f, 0.3f, 0f));
        }
    }
#endif
}
