using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Si un punto de la pantalla cae encima de la interfaz.
///
/// Hace falta porque el juego lee el input por su cuenta —el TapHandler decide
/// él mismo si un gesto fue toque o arrastre— y eso va por un camino distinto
/// al de los botones. Sin preguntar aquí, tocar un botón dispara las dos cosas:
/// el botón hace lo suyo y el mundo recibe el mismo toque por debajo.
///
/// Lanza el raycast a mano en vez de usar IsPointerOverGameObject porque ese
/// contesta con lo que el EventSystem procesó en su turno del frame, y las
/// respuestas a los eventos de input no caen necesariamente después. Dándole
/// nosotros la posición, el resultado es el mismo se llame desde donde se
/// llame.
/// </summary>
public static class UIPointer
{
    // Reutilizada entre llamadas: RaycastAll la vacía y la rellena, y así no
    // se genera basura en cada toque.
    private static readonly List<RaycastResult> _hits = new();

    /// <summary>
    /// True si en <paramref name="screenPos"/> hay algún elemento de interfaz
    /// que reciba toques.
    ///
    /// Solo cuenta la interfaz: si en la escena hubiera un raycaster de física,
    /// sus impactos del mundo saldrían en la misma lista y entonces esto diría
    /// que siempre se está sobre la UI.
    /// </summary>
    public static bool IsOver(Vector2 screenPos)
    {
        EventSystem events = EventSystem.current;
        if (events == null) return false;

        PointerEventData data = new(events) { position = screenPos };

        _hits.Clear();
        events.RaycastAll(data, _hits);

        for (int i = 0; i < _hits.Count; i++)
            if (_hits[i].module is GraphicRaycaster) return true;

        return false;
    }

    /// <summary>
    /// El componente de tipo T que hay bajo ese punto de pantalla, o null.
    ///
    /// Solo mira el impacto de más arriba: lo que esté tapado por un panel no
    /// recibe el toque, igual que en el sistema de eventos de Unity. Si mirara
    /// todos los impactos, un botón escondido detrás de un panel opaco se daría
    /// por pulsado.
    /// </summary>
    public static T TopmostUI<T>(Vector2 screenPos) where T : Component
    {
        EventSystem events = EventSystem.current;
        if (events == null) return null;

        PointerEventData data = new(events) { position = screenPos };

        _hits.Clear();
        events.RaycastAll(data, _hits);

        for (int i = 0; i < _hits.Count; i++)
        {
            if (_hits[i].module is not GraphicRaycaster) continue;

            // GetComponentInParent y no GetComponent: el raycast suele golpear
            // la imagen o el texto que hay dentro del botón, no el botón.
            return _hits[i].gameObject.GetComponentInParent<T>();
        }

        return null;
    }
}
