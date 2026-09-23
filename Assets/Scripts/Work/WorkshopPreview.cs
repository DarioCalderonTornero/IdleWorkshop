using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// El taller sin comprar, en gris: lo que el jugador ve al ir al puntito del
/// taller siguiente.
///
/// El taller de verdad sigue apagado. Encenderlo para enseñarlo arrancaría su
/// gestor de clientes, sus carritos y sus trabajadores, y lo apuntaría en el
/// guardado como si ya fuera del jugador. Así que esto es una copia muerta de
/// sus sprites, igual que el fantasma de las mejoras: se ve, pero no hace nada.
///
/// Enseña el taller tal como será al comprarlo: suelos, muebles, carritos y
/// mostradores. Deja fuera lo que se compra aparte dentro de él —decoraciones,
/// trabajadores de sala, el jefe—, que es lo que marca cada
/// <see cref="IHiddenUntilBought"/>.
/// </summary>
public static class WorkshopPreview
{
    /// <summary>
    /// Monta la copia en gris de <paramref name="workshop"/>. Va fuera de él, en
    /// la raíz de la escena: dentro estaría apagada como el resto del taller.
    /// </summary>
    public static GameObject Build(Workshop workshop, float darkness, float alpha)
    {
        if (workshop == null) return null;

        var hidden = new HashSet<Transform>();
        foreach (IHiddenUntilBought owner in workshop.GetComponentsInChildren<IHiddenUntilBought>(includeInactive: true))
            foreach (Transform part in owner.HiddenParts)
                if (part != null) hidden.Add(part);

        GameObject root = new($"{workshop.name} (sin comprar)");

        foreach (SpriteRenderer source in workshop.GetComponentsInChildren<SpriteRenderer>(includeInactive: true))
        {
            if (source.sprite == null) continue;
            if (IsUnder(source.transform, hidden)) continue;

            GameObject part = new(source.name);
            part.transform.SetParent(root.transform, worldPositionStays: false);
            part.transform.SetPositionAndRotation(source.transform.position, source.transform.rotation);
            part.transform.localScale = source.transform.lossyScale;

            SpriteRenderer copy = part.AddComponent<SpriteRenderer>();
            copy.sprite = source.sprite;
            copy.flipX = source.flipX;
            copy.flipY = source.flipY;
            copy.drawMode = source.drawMode;
            if (source.drawMode != SpriteDrawMode.Simple) copy.size = source.size;
            copy.sortingLayerID = source.sortingLayerID;
            copy.sortingOrder = source.sortingOrder;
            copy.color = Grey(source.color, darkness, alpha);
        }

        return root;
    }

    /// <summary>
    /// El mismo color pasado a gris, conservando lo claro u oscuro que era: así
    /// el suelo sigue distinguiéndose de las mesas aunque todo sea gris.
    /// </summary>
    private static Color Grey(Color color, float darkness, float alpha)
    {
        float luminance = 0.299f * color.r + 0.587f * color.g + 0.114f * color.b;
        float value = Mathf.Lerp(luminance, luminance * 0.35f, Mathf.Clamp01(darkness));

        return new Color(value, value, value, color.a * alpha);
    }

    private static bool IsUnder(Transform t, HashSet<Transform> roots)
    {
        for (; t != null; t = t.parent)
            if (roots.Contains(t)) return true;

        return false;
    }
}
