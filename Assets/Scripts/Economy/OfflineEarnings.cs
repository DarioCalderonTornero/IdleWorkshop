using System;
using System.Globalization;

/// <summary>
/// Lo que el jugador ha ganado mientras el juego estaba cerrado.
///
/// No simula el taller. Coge la tasa de monedas por segundo que se midió
/// mientras jugaba (ver <see cref="EarningsRateMeter"/>) y la multiplica por el
/// rato que ha estado fuera. Simular el pipeline —clientes, colas, carritos por
/// lotes, tiradas de estrella— sería caro, frágil ante cualquier cambio del
/// juego, y aun así una aproximación. Medir lo que de verdad produce su montaje
/// es más honesto y no hay que tocarlo cuando el juego crezca.
///
/// No usa nada de UnityEngine a propósito: decide cuánto dinero se regala, así
/// que tiene que poder ejecutarse y comprobarse fuera del Editor.
/// </summary>
public static class OfflineEarnings
{
    /// <summary>Lo que salió del cálculo, listo para abonar y para enseñar.</summary>
    public readonly struct Result
    {
        /// <summary>Lo que ha estado fuera de verdad, sin capar.</summary>
        public readonly TimeSpan TimeAway;

        /// <summary>Lo que se le paga, ya capado al máximo.</summary>
        public readonly TimeSpan PaidTime;

        public readonly double Coins;

        /// <summary>Si estuvo fuera más del máximo y se le ha recortado.</summary>
        public bool WasCapped => TimeAway > PaidTime;

        public bool HasEarnings => Coins > 0;

        public Result(TimeSpan timeAway, TimeSpan paidTime, double coins)
        {
            TimeAway = timeAway;
            PaidTime = paidTime;
            Coins = coins;
        }

        public static Result None => new(TimeSpan.Zero, TimeSpan.Zero, 0);
    }

    /// <summary>
    /// Cuánto le toca por haber estado fuera.
    /// </summary>
    /// <param name="lastSavedIso">Marca de tiempo del último guardado, en ISO 8601.</param>
    /// <param name="nowUtc">Ahora, en UTC.</param>
    /// <param name="coinsPerSecond">Tasa medida en la sesión anterior.</param>
    /// <param name="factor">Parte de la producción que se paga estando fuera.</param>
    /// <param name="maxHours">Tope de horas que se pagan por muy larga que sea la ausencia.</param>
    public static Result Calculate(string lastSavedIso, DateTime nowUtc,
                                   double coinsPerSecond, float factor, float maxHours)
    {
        if (!TryParseUtc(lastSavedIso, out DateTime lastSaved)) return Result.None;

        TimeSpan away = nowUtc - lastSaved;

        // Tiempo negativo: el reloj del aparato se ha movido hacia atrás, sea
        // por un cambio de zona horaria, un ajuste del sistema o alguien
        // trasteando. No se paga nada, pero tampoco se rompe nada.
        if (away <= TimeSpan.Zero) return Result.None;

        if (coinsPerSecond <= 0) return Result.None;
        if (factor <= 0f) return Result.None;

        // El tope no es solo de equilibrio: es lo único que limita lo que se
        // puede sacar adelantando el reloj del aparato. Sin servidor no hay
        // forma de saber cuánto tiempo pasó de verdad.
        TimeSpan cap = TimeSpan.FromHours(Math.Max(0f, maxHours));
        TimeSpan paid = away < cap ? away : cap;

        double coins = Math.Floor(coinsPerSecond * paid.TotalSeconds * factor);

        if (coins <= 0) return new Result(away, paid, 0);

        return new Result(away, paid, coins);
    }

    /// <summary>
    /// Lee la marca de tiempo del guardado.
    ///
    /// RoundtripKind porque se escribe con ToString("o"), que lleva la zona
    /// dentro: sin esto la fecha volvería como hora local y la resta daría el
    /// desfase horario del jugador como si hubiera estado fuera ese rato.
    /// </summary>
    private static bool TryParseUtc(string iso, out DateTime utc)
    {
        utc = default;

        if (string.IsNullOrEmpty(iso)) return false;

        if (!DateTime.TryParse(iso, CultureInfo.InvariantCulture,
                               DateTimeStyles.RoundtripKind, out DateTime parsed))
            return false;

        utc = parsed.ToUniversalTime();
        return true;
    }
}
