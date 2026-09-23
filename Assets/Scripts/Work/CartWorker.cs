using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Trabajador que transporta encargos en carrito entre dos bolsas: los pasos
/// 4-5-6 (recepción → primera mesa) y 10-11-12 (última mesa → mesa de recogida)
/// de la hoja de diseño.
///
/// Trabaja por lotes: espera a que la bolsa de origen esté llena, va a por ella
/// y se lleva todo de una vez. Los encargos no se teletransportan — saltan uno
/// a uno de la bolsa al carrito, y del carrito a la bolsa de destino.
///
/// La ruta la calcula <see cref="WorkshopNavGraph"/> en el momento, desde donde
/// esté el carrito, así que se puede mover el carrito, una mesa o un punto de
/// atraque sin retocar ninguna ruta a mano.
/// </summary>
public class CartWorker : WorkerBase
{
    [Header("Ruta")]
    [Tooltip("De dónde carga. Cualquier componente que implemente IItemContainer (ItemStack, WorkTable...)")]
    [SerializeField] private MonoBehaviour sourceContainer;

    [Tooltip("Dónde descarga. Cualquier componente que implemente IItemContainer")]
    [SerializeField] private MonoBehaviour targetContainer;

    [Header("Carrito")]
    [SerializeField] private Cart cart;

    [Tooltip("Círculo de progreso que se llena mientras carga o descarga el lote")]
    [SerializeField] private RepairProgressUI progressUI;

    [Header("Cuándo sale")]
    [Tooltip("Esperar a que haya esperando tantos encargos como quepan en el carrito, " +
             "para no hacer el viaje medio vacío")]
    [SerializeField] private bool waitForFullCart = true;

    [Tooltip("Segundos como mucho esperando a juntar un carro lleno. 0 = esperar lo que " +
             "haga falta. Ponerlo a un valor > 0 evita que los encargos se queden ahí " +
             "para siempre si dejan de llegar clientes")]
    [SerializeField] private float maxWaitForFullCart = 0f;

    [Header("Tiempos")]
    [Tooltip("Duración del saltito de cada encargo al subir al carrito")]
    [SerializeField] private float loadTimePerItem = 0.3f;

    [Tooltip("Duración del saltito de cada encargo al bajar del carrito")]
    [SerializeField] private float unloadTimePerItem = 0.3f;

    [Tooltip("Altura del arco de esos saltitos")]
    [SerializeField] private float hopHeight = 0.4f;

    [Header("Movimiento")]
    [Tooltip("Lo que tarda en ponerse a velocidad de crucero, en unidades por segundo²")]
    [SerializeField] private float acceleration = 6f;

    [Tooltip("Lo que tarda en pararse. Empieza a frenar con antelación para llegar suave")]
    [SerializeField] private float deceleration = 7f;

    [Tooltip("Velocidad mínima, para que no se quede arrastrándose al arrancar")]
    [SerializeField] private float minSpeed = 0.4f;

    [Tooltip("Cuánto se redondean las esquinas de la ruta. 0 = esquinas en pico")]
    [Range(0f, 0.49f)]
    [SerializeField] private float cornerRounding = 0.3f;

    private IItemContainer _source;
    private IItemContainer _target;
    private readonly List<ItemOrder> _load = new();

    public int LoadCount => _load.Count;

    // ── Velocidad de carga y descarga ────────────────────────────────
    // Admite varias fuentes y las combina multiplicando, igual que
    // ServiceSpeed: así dos mejoras que toquen lo mismo no se borran.

    private readonly Dictionary<Object, float> _handlingSources = new();

    /// <summary>Lo que multiplica ahora mismo a los tiempos de carga y descarga.</summary>
    public float HandlingMultiplier { get; private set; } = 1f;

    /// <summary>Lo que tarda un encargo en subir al carrito, ya mejorado.</summary>
    public float LoadTimePerItem => loadTimePerItem * HandlingMultiplier;

    /// <summary>Lo que tarda un encargo en bajar del carrito, ya mejorado.</summary>
    public float UnloadTimePerItem => unloadTimePerItem * HandlingMultiplier;

    public void SetHandlingMultiplier(Object source, float multiplier)
    {
        if (source == null) return;

        _handlingSources[source] = Mathf.Clamp(multiplier, 0.01f, 10f);
        RecalculateHandling();
    }

    public void RemoveHandlingSource(Object source)
    {
        if (source == null) return;

        if (_handlingSources.Remove(source)) RecalculateHandling();
    }

    private void RecalculateHandling()
    {
        float total = 1f;
        foreach (float m in _handlingSources.Values) total *= m;

        HandlingMultiplier = Mathf.Max(0.1f, total);
    }

    protected override void Awake()
    {
        base.Awake();

        _source = sourceContainer as IItemContainer;
        _target = targetContainer as IItemContainer;

        if (sourceContainer != null && _source == null)
            Debug.LogError($"[CartWorker] {name}: 'sourceContainer' no implementa IItemContainer.", this);

        if (targetContainer != null && _target == null)
            Debug.LogError($"[CartWorker] {name}: 'targetContainer' no implementa IItemContainer.", this);
    }

    private void Start()
    {
        if (idlePosition != null)
            transform.position = idlePosition.position;

        cart?.SetLoad(_load);

        if (_source == null || _target == null)
        {
            Debug.LogError($"[CartWorker] {name}: falta origen o destino, no arranca el transporte.", this);
            return;
        }

        StartCoroutine(TransportLoop());
    }

    // ── Bucle de transporte ──────────────────────────────────────────
    private IEnumerator TransportLoop()
    {
        while (true)
        {
            yield return WaitForCargo();

            yield return TravelTo(_source.AccessPointPos);
            yield return LoadRoutine();

            // El origen ha podido vaciarlo otro: no salir de vacío.
            if (_load.Count == 0)
            {
                yield return GoIdle();
                continue;
            }

            // Directo del origen al destino: pasar antes por el sitio de
            // reposo era un rodeo que no pintaba nada.
            yield return TravelTo(_target.AccessPointPos);
            yield return UnloadRoutine();
            yield return GoIdle();
        }
    }

    /// <summary>
    /// Cuántos se lleva por viaje. 0 si no hay carrito asignado, que aquí
    /// significa "sin tope": se llevaría lo que hubiera.
    /// </summary>
    private int CartCapacity => cart != null ? cart.Capacity : 0;

    /// <summary>
    /// Espera a tener motivo para salir: que haya juntados tantos encargos
    /// como quepan en el carrito, o al menos uno.
    ///
    /// Antes esperaba a que la bolsa de origen estuviese llena, pero las
    /// bolsas ya no tienen tope, así que el que marca el lote es el carrito.
    /// </summary>
    private IEnumerator WaitForCargo()
    {
        float waited = 0f;

        while (true)
        {
            if (_source.Count > 0)
            {
                bool cartWouldBeFull = CartCapacity <= 0 || _source.Count >= CartCapacity;
                if (!waitForFullCart || cartWouldBeFull) yield break;

                waited += Time.deltaTime;
                if (maxWaitForFullCart > 0f && waited >= maxWaitForFullCart) yield break;
            }
            else
            {
                waited = 0f;
            }

            yield return null;
        }
    }

    // ── Carga y descarga ─────────────────────────────────────────────

    private IEnumerator LoadRoutine()
    {
        // Sin carrito asignado no hay tope: se lleva lo que haya.
        int room = CartCapacity > 0 ? CartCapacity : _source.Count;
        int toMove = Mathf.Min(room, _source.Count);

        // El círculo dura exactamente lo que el trasiego: un saltito por objeto.
        Coroutine progress = StartProgress(toMove * LoadTimePerItem);

        while (_load.Count < room && _source.Count > 0)
        {
            if (!_source.TryDequeue(out ItemOrder order)) break;

            // Ya no está en el saco de origen: se dibuja aparte mientras salta.
            yield return HopItem(order, _source.ContentsPos, CartContentsPos, LoadTimePerItem);

            _load.Add(order);
            cart?.SetLoadWithPop(_load);
        }

        StopProgress(progress);
    }

    private IEnumerator UnloadRoutine()
    {
        Coroutine progress = StartProgress(_load.Count * UnloadTimePerItem);

        while (_load.Count > 0)
        {
            while (!_target.HasSpace)
                yield return null;

            ItemOrder order = _load[0];
            _load.RemoveAt(0);
            cart?.SetLoadWithPop(_load);

            yield return HopItem(order, CartContentsPos, _target.ContentsPos, UnloadTimePerItem);

            _target.TryEnqueue(order);
        }

        StopProgress(progress);
    }

    // ── Círculo de progreso ──────────────────────────────────────────

    private Coroutine StartProgress(float duration)
    {
        if (progressUI == null || duration <= 0f) return null;

        progressUI.Show(0f);
        return StartCoroutine(FillProgress(duration));
    }

    private IEnumerator FillProgress(float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            progressUI.SetFill(elapsed / duration);
            yield return null;
        }

        progressUI.SetFill(1f);
    }

    private void StopProgress(Coroutine progress)
    {
        if (progress != null) StopCoroutine(progress);
        progressUI?.Hide();
    }

    /// <summary>
    /// Dibuja el encargo en tránsito y lo hace saltar de un sitio a otro. La
    /// bolsa de origen ya no lo tiene y la de destino aún no, así que durante
    /// el salto este es el único que se ve.
    /// </summary>
    private IEnumerator HopItem(ItemOrder order, Vector3 from, Vector3 to, float duration)
    {
        GameObject itemGO = ItemVisual.Spawn(order.Definition);

        if (itemGO == null)
        {
            yield return new WaitForSeconds(duration);
            yield break;
        }

        itemGO.transform.position = from;
        yield return ItemHop.Move(itemGO.transform, from, to, duration, hopHeight);

        Destroy(itemGO);
    }

    private Vector3 CartContentsPos => cart != null ? cart.ContentsPos : transform.position;

    // ── Movimiento ───────────────────────────────────────────────────

    private IEnumerator GoIdle()
    {
        if (idlePosition != null)
            yield return TravelTo(idlePosition.position);
    }

    private WorkshopNavGraph _nav;

    /// <summary>
    /// La red de navegación de SU taller, buscada una vez. Con varios talleres
    /// la global sería la de cualquiera, y el carrito calcularía la ruta con
    /// los nodos de otro taller.
    /// </summary>
    private WorkshopNavGraph Nav
    {
        get
        {
            if (_nav == null) _nav = WorkshopNavGraph.For(this);
            return _nav;
        }
    }

    /// <summary>
    /// Va hasta el destino rodeando lo que haya, con las esquinas redondeadas y
    /// acelerando y frenando, en vez de a velocidad fija de punto en punto.
    /// </summary>
    private IEnumerator TravelTo(Vector3 destination)
    {
        WorkshopNavGraph nav = Nav;

        List<Vector3> path = nav != null
            ? nav.FindPath(transform.position, destination)
            : new List<Vector3> { destination };

        if (nav != null && cornerRounding > 0f)
            path = PathSmoothing.Smooth(transform.position, path, nav.IsClear, cornerRounding);

        yield return FollowPath(path);
    }

    private IEnumerator FollowPath(List<Vector3> path)
    {
        float speed = 0f;
        int index = 0;

        while (index < path.Count)
        {
            float remaining = RemainingDistance(path, index);

            // Frenar con antelación para llegar parado, y no antes: en los
            // puntos intermedios la distancia restante sigue siendo grande,
            // así que el carrito los cruza sin detenerse.
            float brakingSpeed = Mathf.Sqrt(2f * Mathf.Max(0.01f, deceleration) * remaining);

            speed = Mathf.Min(speed + acceleration * Time.deltaTime, _currentMoveSpeed);
            speed = Mathf.Max(Mathf.Min(speed, brakingSpeed), minSpeed);

            transform.position = Vector3.MoveTowards(
                transform.position, path[index], speed * Time.deltaTime);

            if (Vector3.Distance(transform.position, path[index]) < 0.02f)
                index++;

            yield return null;
        }

        if (path.Count > 0)
            transform.position = path[path.Count - 1];
    }

    private float RemainingDistance(List<Vector3> path, int index)
    {
        float total = Vector3.Distance(transform.position, path[index]);

        for (int i = index; i < path.Count - 1; i++)
            total += Vector3.Distance(path[i], path[i + 1]);

        return total;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        DrawDock(sourceContainer as IItemContainer, new Color(0.2f, 0.8f, 0.3f));
        DrawDock(targetContainer as IItemContainer, new Color(1f, 0.6f, 0.1f));
    }

    /// <summary>
    /// Solo el punto de atraque: la ruta ya no se guarda, la calcula la red de
    /// navegación en el momento. Para ver las conexiones, selecciona el
    /// WorkshopNavGraph.
    /// </summary>
    private void DrawDock(IItemContainer container, Color color)
    {
        if (container == null) return;

        Gizmos.color = color;
        Gizmos.DrawWireCube(container.AccessPointPos, new Vector3(0.25f, 0.25f, 0f));
        Gizmos.DrawLine(container.AccessPointPos, container.ContentsPos);
    }
#endif
}
