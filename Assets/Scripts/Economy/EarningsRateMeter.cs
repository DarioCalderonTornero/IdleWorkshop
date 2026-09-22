using System;

/// <summary>
/// Lleva la cuenta de cuánto gana el jugador por segundo.
///
/// Hace falta porque los ingresos del juego no son un goteo, son picos: un
/// trabajador del almacén no da 13 monedas por segundo, da 1.200 de golpe cada
/// 90 segundos y cero el resto del tiempo. Medir en una ventana corta daría
/// cero casi siempre y un número disparatado justo en el instante del cobro, y
/// como la tasa se guarda al cerrar el juego, lo que el jugador se llevara por
/// estar fuera dependería del segundo exacto en que le dio a salir.
///
/// Por eso se mide sobre una ventana larga —varios ciclos completos del cobro
/// más lento— repartida en tramos que van rotando.
///
/// No usa nada de UnityEngine a propósito: así esta lógica, que decide cuánto
/// dinero se paga, se puede ejecutar y comprobar fuera del Editor.
/// </summary>
public class EarningsRateMeter
{
    private readonly double[] _buckets;
    private readonly float _bucketSeconds;
    private readonly float _minSampleSeconds;
    private readonly float _windowSeconds;

    private int _current;
    private float _bucketElapsed;

    /// <summary>
    /// Tiempo medido, tope la ventana. Es el divisor: mientras la partida sea
    /// más corta que la ventana se divide por lo jugado de verdad, que si no
    /// una sesión de un minuto leería la quinta parte de su producción real.
    /// </summary>
    private float _sampledSeconds;

    public EarningsRateMeter(float windowSeconds = 300f, int buckets = 60, float minSampleSeconds = 60f)
    {
        _windowSeconds = Math.Max(1f, windowSeconds);
        _buckets = new double[Math.Max(1, buckets)];
        _bucketSeconds = _windowSeconds / _buckets.Length;
        _minSampleSeconds = Math.Max(0.01f, minSampleSeconds);
    }

    /// <summary>
    /// Si se ha medido lo suficiente como para fiarse del resultado.
    ///
    /// Antes de eso la tasa sigue saliendo, pero amortiguada por el suelo del
    /// divisor; quien guarde la partida hará bien en conservar la tasa de la
    /// sesión anterior en vez de pisarla con esta.
    /// </summary>
    public bool HasEnoughSample => _sampledSeconds >= _minSampleSeconds;

    /// <summary>
    /// Monedas por segundo en la ventana.
    ///
    /// El suelo del divisor evita el caso de arrancar, cobrar 1.200 a los tres
    /// segundos y leer 400/s: sin él, eso por 24 horas serían millones por
    /// haber jugado un momento.
    /// </summary>
    public double CoinsPerSecond
    {
        get
        {
            double total = 0;
            foreach (double bucket in _buckets) total += bucket;

            return total / Math.Max(_sampledSeconds, _minSampleSeconds);
        }
    }

    /// <summary>Apunta unas monedas en el tramo que toca ahora mismo.</summary>
    public void Record(double amount)
    {
        if (amount <= 0) return;

        _buckets[_current] += amount;
    }

    /// <summary>
    /// Corre el reloj. Al agotarse un tramo se pasa al siguiente y se vacía, y
    /// así lo que sale por detrás de la ventana deja de contar.
    /// </summary>
    public void Advance(float deltaTime)
    {
        if (deltaTime <= 0f) return;

        _sampledSeconds = Math.Min(_sampledSeconds + deltaTime, _windowSeconds);
        _bucketElapsed += deltaTime;

        // While y no if: un frame muy largo —una pausa, una carga— puede
        // saltarse varios tramos de golpe, y todos tienen que vaciarse.
        while (_bucketElapsed >= _bucketSeconds)
        {
            _bucketElapsed -= _bucketSeconds;
            _current = (_current + 1) % _buckets.Length;
            _buckets[_current] = 0;
        }
    }
}
