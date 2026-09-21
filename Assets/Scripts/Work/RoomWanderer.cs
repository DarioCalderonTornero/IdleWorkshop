using System.Collections;
using UnityEngine;

/// <summary>
/// El jefe: sale de su habitación, se da una vuelta por el taller como quien
/// inspecciona, y vuelve a meterse un rato. No cobra ni toca nada; está por
/// ambiente.
///
/// No usa la red de navegación de los carritos. Podría, pero los bloqueadores
/// que la red tiene puestos sobre las salas de la izquierda lo dejarían
/// encerrado en la habitación, y tocarlos rompería las rutas de los carritos,
/// que sí dependen de ellos. En su lugar hace una ronda por puntos fijos, que
/// es lo que se quiere aquí: siempre el mismo paseo, comprobable y sin sorpresas.
///
/// La ronda se recorre de ida y de vuelta por los mismos tramos, así que basta
/// con comprobar que la ida está despejada.
/// </summary>
public class RoomWanderer : MonoBehaviour
{
    [Header("Partes")]
    [Tooltip("El muñeco: lo que se ve y se balancea al andar")]
    [SerializeField] private Transform body;

    [Tooltip("Efecto con el que aparece. Si es null, aparece de golpe")]
    [SerializeField] private Poof poof;

    [Header("Condiciones")]
    [Tooltip("Todo esto tiene que estar desbloqueado para que salga a pasear")]
    [SerializeField] private Unlockable[] requires;

    [Header("La ronda")]
    [Tooltip("Su sitio dentro de la habitación: de aquí sale y aquí vuelve")]
    [SerializeField] private Transform home;

    [Tooltip("Las paradas de la ida. La vuelta es la misma al revés")]
    [SerializeField] private Transform[] stops;

    [Header("Pies: lo que choca")]
    [Tooltip("Caja a la altura de los pies. Mientras toque algo sólido, ahí no pisa")]
    [SerializeField] private Vector2 feetSize = new(0.30f, 0.18f);

    [Tooltip("Dónde están los pies respecto a su centro")]
    [SerializeField] private Vector2 feetOffset = new(0f, -0.28f);

    [Tooltip("Las capas que no puede atravesar: muebles y decoraciones")]
    [SerializeField] private LayerMask solidLayers;

    [Header("Límites")]
    [Tooltip("Respecto a qué se mide el área. Normalmente la propia sala. " +
             "Sin esto el área se mediría en coordenadas de mundo y bastaría " +
             "mover el taller de sitio para dejarla descuadrada")]
    [SerializeField] private Transform areaReference;

    [Tooltip("Rectángulo del que no puede salir, medido desde areaReference. " +
             "Es la última red: lo normal es que le frenen los colliders")]
    [SerializeField] private Vector2 areaCenter;
    [SerializeField] private Vector2 areaSize = new(10.08f, 6.04f);

    [Header("Ritmo")]
    [SerializeField] private float moveSpeed = 1.15f;

    [Tooltip("Lo que se para en cada parada a mirar")]
    [SerializeField] private float lookAroundSeconds = 1.1f;

    [Tooltip("Lo que se queda en la habitación entre ronda y ronda")]
    [SerializeField] private float restSeconds = 8f;

    [Header("Andares")]
    [SerializeField] private float bobHeight = 0.05f;
    [SerializeField] private float bobSpeed = 3.2f;
    [SerializeField] private float leanDegrees = 5f;

    private Vector3 _bodyHome;
    private Coroutine _loop;

    private void Awake()
    {
        if (body != null) _bodyHome = body.localPosition;

        foreach (Unlockable gate in Gates())
            gate.OnUnlocked += HandleUnlocked;
    }

    private void Start()
    {
        if (home != null) transform.position = home.position;

        _routeIsSane = ValidateRoute();
        Refresh();
    }

    // ── Límites ──────────────────────────────────────────────────────

    private bool _routeIsSane = true;

    /// <summary>
    /// El área se mide respecto a <see cref="areaReference"/>, no en
    /// coordenadas de mundo. Es importante: el taller entero está movido de
    /// sitio en la escena, así que un rectángulo en coordenadas de mundo
    /// dejaba fuera media ronda y el jefe se quedaba plantado sin salir.
    /// </summary>
    private Vector3 ToArea(Vector3 world) =>
        areaReference != null ? areaReference.InverseTransformPoint(world) : world;

    private Vector3 FromArea(Vector3 local) =>
        areaReference != null ? areaReference.TransformPoint(local) : local;

    private Vector3 Clamp(Vector3 world)
    {
        Vector3 local = ToArea(world);
        Vector2 half = areaSize * 0.5f;

        local.x = Mathf.Clamp(local.x, areaCenter.x - half.x, areaCenter.x + half.x);
        local.y = Mathf.Clamp(local.y, areaCenter.y - half.y, areaCenter.y + half.y);

        return FromArea(local);
    }

    private bool Inside(Vector3 world)
    {
        Vector3 local = ToArea(world);
        Vector2 half = areaSize * 0.5f;

        return local.x >= areaCenter.x - half.x - 0.01f && local.x <= areaCenter.x + half.x + 0.01f
            && local.y >= areaCenter.y - half.y - 0.01f && local.y <= areaCenter.y + half.y + 0.01f;
    }

    /// <summary>
    /// Si en ese sitio los pies tocarían algo sólido. Se pregunta a los
    /// colliders de verdad, así que cualquier decoración que se añada más
    /// adelante con su collider le frena sin tocar este script.
    /// </summary>
    private bool Blocked(Vector3 world)
    {
        if (solidLayers.value == 0) return false;

        Vector2 feet = new Vector2(world.x, world.y) + feetOffset;
        return Physics2D.OverlapBox(feet, feetSize, 0f, solidLayers) != null;
    }

    /// <summary>
    /// Comprueba al arrancar que la ronda es sensata: que hay paradas y que
    /// todas caen dentro del área.
    ///
    /// Existe porque el fallo que tuvo esto no se veía en el código sino en la
    /// jerarquía —las paradas colgaban del propio jefe, así que se movían con
    /// él y nunca las alcanzaba— y desde dentro parecía que todo iba bien
    /// mientras se alejaba del mapa para siempre.
    /// </summary>
    private bool ValidateRoute()
    {
        if (home == null)
        {
            Debug.LogError($"[RoomWanderer] {name}: no tiene sitio al que volver.", this);
            return false;
        }

        if (StopCount == 0)
        {
            Debug.LogWarning($"[RoomWanderer] {name}: no tiene paradas, se queda en casa.", this);
            return false;
        }

        bool ok = true;

        // Ni el sitio ni las paradas pueden colgar del propio jefe: se moverían
        // con él y no llegaría nunca.
        foreach (Transform point in AllPoints())
        {
            if (point == null) continue;

            if (point.IsChildOf(transform))
            {
                Debug.LogError(
                    $"[RoomWanderer] {name}: '{point.name}' cuelga del propio jefe, así que " +
                    $"se movería con él y no llegaría nunca. Tiene que colgar de la sala.", this);
                ok = false;
            }

            if (!Inside(point.position))
            {
                Debug.LogError(
                    $"[RoomWanderer] {name}: '{point.name}' está fuera del área permitida.", this);
                ok = false;
            }
        }

        return ok;
    }

    private System.Collections.Generic.IEnumerable<Transform> AllPoints()
    {
        yield return home;

        if (stops == null) yield break;

        foreach (Transform stop in stops)
            yield return stop;
    }

    private void OnDestroy()
    {
        foreach (Unlockable gate in Gates())
            gate.OnUnlocked -= HandleUnlocked;
    }

    private System.Collections.Generic.IEnumerable<Unlockable> Gates()
    {
        if (requires == null) yield break;

        foreach (Unlockable gate in requires)
            if (gate != null) yield return gate;
    }

    private void HandleUnlocked(Unlockable _) => Refresh();

    // ── Encendido y apagado ──────────────────────────────────────────

    private bool CanWander()
    {
        foreach (Unlockable gate in Gates())
            if (!gate.IsUnlocked) return false;

        return true;
    }

    private void Refresh()
    {
        bool active = CanWander();

        // Mientras se monta la escena y se aplica la partida no se anima: si
        // no, todo lo que empieza bloqueado soltaría un puf al arrancar.
        if (poof != null) poof.SetVisible(active, animate: !BootPhase.IsBooting);
        else if (body != null && body.gameObject.activeSelf != active) body.gameObject.SetActive(active);

        // Con la ronda mal montada se queda quieto en casa. Es mejor un jefe
        // que no pasea que uno que se va del mapa.
        if (active && _routeIsSane && _loop == null)
        {
            _loop = StartCoroutine(Rounds());
        }
        else if (!active && _loop != null)
        {
            StopCoroutine(_loop);
            _loop = null;
            GoHome();
        }
    }

    private void GoHome()
    {
        if (home != null) transform.position = home.position;

        if (body == null) return;
        body.localPosition = _bodyHome;
        body.localRotation = Quaternion.identity;
    }

    // ── La ronda ─────────────────────────────────────────────────────

    private IEnumerator Rounds()
    {
        while (true)
        {
            yield return new WaitForSeconds(restSeconds);

            for (int i = 0; i < StopCount; i++)
            {
                yield return WalkTo(stops[i].position);
                yield return new WaitForSeconds(lookAroundSeconds);
            }

            // De vuelta por donde vino, sin atajos: los tramos de la ida son
            // los únicos que se han comprobado.
            for (int i = StopCount - 2; i >= 0; i--)
                yield return WalkTo(stops[i].position);

            if (home != null) yield return WalkTo(home.position);
        }
    }

    private int StopCount => stops != null ? stops.Length : 0;

    private IEnumerator WalkTo(Vector3 target)
    {
        target = Clamp(target);

        float walked = 0f;

        // Tiempo de sobra para llegar. Si se pasa de aquí es que no está
        // llegando, y andar sin fin es justo lo que no puede volver a pasar.
        float budget = Vector3.Distance(transform.position, target) / Mathf.Max(0.01f, moveSpeed) + 3f;
        float spent = 0f;

        while (true)
        {
            Vector3 position = transform.position;
            Vector3 delta = target - position;
            float distance = delta.magnitude;

            if (distance < 0.02f) break;

            spent += Time.deltaTime;
            if (spent > budget)
            {
                Debug.LogError(
                    $"[RoomWanderer] {name}: no llega a {target}. Se vuelve a su sitio.", this);
                GoHome();
                yield break;
            }

            float step = Mathf.Min(moveSpeed * Time.deltaTime, distance);
            Vector3 direction = delta / distance;

            if (!TryStep(position, direction, step, out Vector3 moved))
            {
                // Con algo delante y sin poder rodearlo, no se queda empujando
                // la pared: se da por visitada esta parada y sigue la ronda.
                Debug.Log($"[RoomWanderer] {name}: algo bloquea el paso hacia {target}, sigue con la ronda.", this);
                yield break;
            }

            transform.position = moved;

            walked += step;
            Animate(walked);

            // Mira hacia donde anda.
            if (body != null && Mathf.Abs(delta.x) > 0.01f)
            {
                Vector3 scale = body.localScale;
                float size = Mathf.Abs(scale.x);
                body.localScale = new Vector3(delta.x < 0 ? -size : size, scale.y, scale.z);
            }

            yield return null;
        }

        transform.position = target;

        if (body == null) yield break;
        body.localPosition = _bodyHome;
        body.localRotation = Quaternion.identity;
    }

    /// <summary>
    /// Intenta dar un paso. Si de frente hay algo, prueba a deslizarse por un
    /// solo eje —que es como se bordea una mesa sin quedarse atascado contra
    /// la esquina— y solo se rinde si tampoco cabe por ninguno.
    /// </summary>
    private bool TryStep(Vector3 from, Vector3 direction, float step, out Vector3 moved)
    {
        foreach (Vector3 candidate in new[]
                 {
                     from + direction * step,
                     from + new Vector3(direction.x, 0f, 0f).normalized * step,
                     from + new Vector3(0f, direction.y, 0f).normalized * step,
                 })
        {
            if (float.IsNaN(candidate.x) || float.IsNaN(candidate.y)) continue;

            Vector3 clamped = Clamp(candidate);
            if (Blocked(clamped)) continue;

            moved = clamped;
            return true;
        }

        moved = from;
        return false;
    }

    /// <summary>
    /// El balanceo va por distancia recorrida y no por tiempo: así los pasos
    /// no se aceleran ni se frenan si se cambia la velocidad.
    /// </summary>
    private void Animate(float walked)
    {
        if (body == null) return;

        float phase = walked * bobSpeed * Mathf.PI;

        body.localPosition = _bodyHome + new Vector3(0f, Mathf.Abs(Mathf.Sin(phase)) * bobHeight, 0f);
        body.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(phase * 0.5f) * leanDegrees);
    }
}
