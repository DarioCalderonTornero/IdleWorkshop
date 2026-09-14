using System.Collections;
using UnityEngine;

/// <summary>
/// El saltito con el que los objetos se mueven solos por el taller: de la zona
/// de espera al punto de proceso, y de ahí a la mesa siguiente. Los
/// trabajadores no los llevan — se quedan en su puesto.
/// </summary>
public static class ItemHop
{
    /// <summary>
    /// Mueve el transform de <paramref name="from"/> a <paramref name="to"/>
    /// describiendo un arco. Si el objeto se destruye a mitad, corta sin ruido.
    /// </summary>
    public static IEnumerator Move(Transform target, Vector3 from, Vector3 to, float duration, float height)
    {
        if (target == null) yield break;

        if (duration <= 0f)
        {
            target.position = to;
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (target == null) yield break;

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            Vector3 position = Vector3.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t));
            position.y += Mathf.Sin(t * Mathf.PI) * height;

            target.position = position;
            yield return null;
        }

        if (target != null) target.position = to;
    }
}
