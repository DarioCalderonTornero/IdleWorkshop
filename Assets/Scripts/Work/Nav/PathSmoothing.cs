using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Redondea las esquinas de una ruta para que no se note que va de punto en
/// punto. Cada esquina solo se recorta si el atajo resultante sigue libre, así
/// que suavizar nunca mete a nadie dentro de un mueble: en los pasillos justos
/// la esquina se queda como estaba.
/// </summary>
public static class PathSmoothing
{
    /// <summary>
    /// Devuelve la ruta con las esquinas recortadas.
    /// </summary>
    /// <param name="start">Desde dónde se empieza a recorrer.</param>
    /// <param name="path">Ruta original, sin incluir el punto de partida.</param>
    /// <param name="isClear">Prueba de visibilidad entre dos puntos.</param>
    /// <param name="cut">Fracción del tramo que se recorta en cada esquina (0 a 0.5).</param>
    /// <param name="passes">Cuántas veces se repite el recorte.</param>
    public static List<Vector3> Smooth(
        Vector3 start,
        IReadOnlyList<Vector3> path,
        System.Func<Vector3, Vector3, bool> isClear,
        float cut = 0.3f,
        int passes = 2)
    {
        var points = new List<Vector3> { start };
        points.AddRange(path);

        if (points.Count < 3 || isClear == null)
            return new List<Vector3>(path);

        cut = Mathf.Clamp(cut, 0.01f, 0.49f);

        for (int pass = 0; pass < passes; pass++)
            points = SmoothOnce(points, isClear, cut);

        // El primer punto es de dónde se sale, no un destino al que ir.
        points.RemoveAt(0);
        return points;
    }

    private static List<Vector3> SmoothOnce(
        List<Vector3> points, System.Func<Vector3, Vector3, bool> isClear, float cut)
    {
        var result = new List<Vector3> { points[0] };

        for (int i = 1; i < points.Count - 1; i++)
        {
            Vector3 previous = result[result.Count - 1];
            Vector3 corner = points[i];
            Vector3 next = points[i + 1];

            Vector3 entry = Vector3.Lerp(corner, previous, cut);
            Vector3 exit = Vector3.Lerp(corner, next, cut);

            // El atajo entry->exit se salta la esquina: solo vale si está libre.
            bool safe = isClear(previous, entry)
                     && isClear(entry, exit)
                     && isClear(exit, next);

            if (safe)
            {
                result.Add(entry);
                result.Add(exit);
            }
            else
            {
                result.Add(corner);
            }
        }

        result.Add(points[points.Count - 1]);
        return result;
    }
}
