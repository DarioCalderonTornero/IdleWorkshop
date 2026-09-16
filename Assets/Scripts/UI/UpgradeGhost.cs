using UnityEngine;

/// <summary>
/// El "fantasma": una copia semitransparente de lo que va a aparecer, puesta
/// en su sitio, para que el jugador vea qué compra antes de comprarlo.
///
/// Se construye copiando los SpriteRenderer del original, no clonándolo con
/// Instantiate: el original arrastra colliders, corrutinas y componentes que
/// se pondrían a trabajar por su cuenta —un trabajador fantasma empezaría a
/// cobrar monedas— y limpiarlos después es más frágil que no traerlos.
/// </summary>
public static class UpgradeGhost
{
    /// <summary>
    /// Crea el fantasma de <paramref name="sample"/> en <paramref name="position"/>.
    /// Devuelve null si no hay nada que copiar.
    /// </summary>
    public static GameObject Build(GameObject sample, Vector3 position, Color tint, float alpha)
    {
        if (sample == null) return null;

        SpriteRenderer[] sources = sample.GetComponentsInChildren<SpriteRenderer>(includeInactive: true);
        if (sources.Length == 0) return null;

        GameObject root = new("FantasmaMejora");
        root.transform.position = position;

        Vector3 origin = sample.transform.position;

        foreach (SpriteRenderer source in sources)
        {
            if (source.sprite == null) continue;

            GameObject part = new(source.name);
            part.transform.SetParent(root.transform, worldPositionStays: false);

            // Se respeta la colocación relativa dentro del original, para que
            // un elemento de varias piezas no se apelotone en un punto.
            part.transform.position = position + (source.transform.position - origin);
            part.transform.rotation = source.transform.rotation;
            part.transform.localScale = source.transform.lossyScale;

            SpriteRenderer copy = part.AddComponent<SpriteRenderer>();
            copy.sprite = source.sprite;
            copy.flipX = source.flipX;
            copy.flipY = source.flipY;

            Color color = Color.Lerp(source.color, tint, 0.5f);
            color.a = alpha;
            copy.color = color;

            // Justo por debajo de los objetos, para que nada importante quede
            // tapado por algo que todavía no existe.
            copy.sortingOrder = SortingOrders.Sack;
        }

        root.AddComponent<GhostPulse>();
        return root;
    }
}
