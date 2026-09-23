using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Los puntitos de abajo: uno por taller. El taller en el que estás es un
/// círculo blanco relleno; los demás, anillos grises vacíos. Pulsar uno te
/// lleva a ese taller, esté comprado o no — a los que no, vas a comprarlos.
///
/// Los círculos se dibujan por código en vez de salir de un asset: así no hay
/// una textura que se pueda perder o que haya que importar con unos ajustes
/// concretos para que se vea redonda.
///
/// Siguen a la cámara, no solo a los toques: si llegas a otro taller
/// arrastrando, el punto relleno se mueve igual.
/// </summary>
public class WorkshopDotsUI : MonoBehaviour
{
    [Tooltip("Donde se ponen los puntitos. Debería tener un HorizontalLayoutGroup")]
    [SerializeField] private RectTransform container;

    [SerializeField] private float dotSize = 34f;

    [Tooltip("Zona pulsable de cada punto. Más grande que el punto: un círculo " +
             "de pocos píxeles es difícil de acertar con el dedo")]
    [SerializeField] private float touchSize = 72f;

    [SerializeField] private Color currentColor = Color.white;
    [SerializeField] private Color otherColor = new(0.72f, 0.72f, 0.72f, 0.9f);

    private readonly List<Image> _dots = new();

    private void Start()
    {
        WorkStationUnlocker unlocker = WorkStationUnlocker.Instance;
        if (unlocker == null) return;

        Build(unlocker.Workshops.Count);

        unlocker.OnViewChanged += Refresh;
        unlocker.OnProgressionChanged += Refresh;

        Refresh();
    }

    private void OnDestroy()
    {
        WorkStationUnlocker unlocker = WorkStationUnlocker.Instance;
        if (unlocker == null) return;

        unlocker.OnViewChanged -= Refresh;
        unlocker.OnProgressionChanged -= Refresh;
    }

    private void Build(int count)
    {
        RectTransform parent = container != null ? container : (RectTransform)transform;

        // Con un solo taller no hay a dónde ir: los puntitos no aportan nada.
        gameObject.SetActive(count > 1);

        for (int i = 0; i < count; i++)
        {
            int index = i;

            // La zona pulsable, invisible pero más grande que el punto.
            GameObject hit = new($"Taller{i + 1}", typeof(RectTransform));
            hit.transform.SetParent(parent, worldPositionStays: false);
            ((RectTransform)hit.transform).sizeDelta = new Vector2(touchSize, touchSize);

            Image hitArea = hit.AddComponent<Image>();
            hitArea.color = new Color(0f, 0f, 0f, 0f);

            Button button = hit.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() => WorkStationUnlocker.Instance?.Visit(index));

            // El punto en sí.
            GameObject dot = new("Punto", typeof(RectTransform));
            dot.transform.SetParent(hit.transform, worldPositionStays: false);
            ((RectTransform)dot.transform).sizeDelta = new Vector2(dotSize, dotSize);

            Image image = dot.AddComponent<Image>();
            image.raycastTarget = false;
            _dots.Add(image);
        }
    }

    private void Refresh()
    {
        WorkStationUnlocker unlocker = WorkStationUnlocker.Instance;
        if (unlocker == null) return;

        for (int i = 0; i < _dots.Count; i++)
        {
            bool current = i == unlocker.ViewingIndex;

            _dots[i].sprite = current ? DotSprites.Disc : DotSprites.Ring;
            _dots[i].color = current ? currentColor : otherColor;
        }
    }
}

/// <summary>
/// Los dos círculos de los puntitos, dibujados una vez y compartidos.
/// </summary>
public static class DotSprites
{
    private const int Size = 64;

    /// <summary>Grosor del anillo, en proporción al radio.</summary>
    private const float RingThickness = 0.22f;

    private static Sprite _disc, _ring;

    public static Sprite Disc => _disc != null ? _disc : _disc = Make(filled: true);
    public static Sprite Ring => _ring != null ? _ring : _ring = Make(filled: false);

    private static Sprite Make(bool filled)
    {
        var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, mipChain: false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
        };

        float radius = Size * 0.5f;
        float inner = radius * (1f - RingThickness);
        var pixels = new Color32[Size * Size];

        for (int y = 0; y < Size; y++)
        for (int x = 0; x < Size; x++)
        {
            float dx = x + 0.5f - radius, dy = y + 0.5f - radius;
            float d = Mathf.Sqrt(dx * dx + dy * dy);

            // Borde suavizado de un píxel por fuera, y por dentro si es anillo.
            float alpha = Mathf.Clamp01(radius - d);
            if (!filled) alpha *= Mathf.Clamp01(d - inner);

            pixels[y * Size + x] = new Color32(255, 255, 255, (byte)(alpha * 255));
        }

        texture.SetPixels32(pixels);
        texture.Apply(updateMipmaps: false, makeNoLongerReadable: true);

        return Sprite.Create(texture, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), Size);
    }
}
