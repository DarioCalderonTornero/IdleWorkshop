using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Un trabajador que no atiende a nadie: está a lo suyo en bucle —ordenando el
/// almacén, arreglando la habitación— y cobra cada vez que termina una tanda.
///
/// No se mueve del sitio ni toca objetos de clientes; solo el balanceo de estar
/// trabajando y el círculo de progreso, que aquí no acaba nunca: al llenarse
/// paga y vuelve a empezar.
///
/// Este componente se queda siempre activo y lo que enciende y apaga es su
/// muñeco. Si se desactivara él, su corrutina moriría y con ella el bucle, y
/// nadie quedaría escuchando el desbloqueo para volver a encenderlo.
/// </summary>
public class IdleWorker : MonoBehaviour, IUpgradePreview, IHiddenUntilBought
{
    /// <summary>El muñeco: no sale hasta comprar su sala.</summary>
    public IEnumerable<Transform> HiddenParts
    {
        get { if (body != null) yield return body; }
    }

    [Header("Partes")]
    [Tooltip("El muñeco: lo que se ve y se balancea. Se enciende al desbloquearse")]
    [SerializeField] private Transform body;

    [Tooltip("Efecto con el que aparece. Si es null, aparece de golpe")]
    [SerializeField] private Poof poof;

    [SerializeField] private RepairProgressUI progressUI;

    [Header("Condiciones")]
    [Tooltip("Todo esto tiene que estar desbloqueado para que trabaje: la sala, " +
             "y su propia compra si es un trabajador que se añade después")]
    [SerializeField] private Unlockable[] requires;

    [Header("Animación en bucle")]
    [Tooltip("Cuánto sube y baja")]
    [SerializeField] private float bobHeight = 0.07f;

    [Tooltip("Vaivenes por segundo")]
    [SerializeField] private float bobSpeed = 1.9f;

    [Tooltip("Cuánto se inclina a un lado y a otro")]
    [SerializeField] private float swayDegrees = 6f;

    private int _coinsPerCycle = 100;
    private float _cycleTime = 60f;

    private Vector3 _bodyHome;
    private Coroutine _loop;

    /// <summary>Si ahora mismo está trabajando.</summary>
    public bool IsWorking => _loop != null;

    /// <summary>Lo que cobrará la tanda actual, para poder enseñarlo en el panel.</summary>
    public int CoinsPerCycle => _coinsPerCycle;

    private void Awake()
    {
        if (body != null) _bodyHome = body.localPosition;

        foreach (Unlockable gate in Gates())
            gate.OnUnlocked += HandleUnlocked;
    }

    private void Start() => Refresh();

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

    /// <summary>
    /// Lo que paga y lo que tarda cada tanda. Lo llama su mejora al subir de
    /// nivel; el cambio entra en la tanda siguiente, no corta la que va.
    /// </summary>
    public void Configure(int coinsPerCycle, float cycleTime)
    {
        _coinsPerCycle = Mathf.Max(0, coinsPerCycle);
        _cycleTime = Mathf.Max(0.1f, cycleTime);
    }

    // ── Encendido y apagado ──────────────────────────────────────────

    private bool CanWork()
    {
        foreach (Unlockable gate in Gates())
            if (!gate.IsUnlocked) return false;

        return true;
    }

    private void Refresh()
    {
        bool active = CanWork();

        // Mientras se monta la escena y se aplica la partida no se anima: si
        // no, todos los trabajadores bloqueados soltarían un puf al arrancar.
        if (poof != null) poof.SetVisible(active, animate: !BootPhase.IsBooting);
        else if (body != null && body.gameObject.activeSelf != active) body.gameObject.SetActive(active);

        if (active && _loop == null)
        {
            _loop = StartCoroutine(WorkLoop());
        }
        else if (!active && _loop != null)
        {
            StopCoroutine(_loop);
            _loop = null;
            progressUI?.Hide();
            ResetBody();
        }
    }

    /// <summary>
    /// Mientras esté por comprar, se enseña dónde va a ponerse. Una vez está
    /// trabajando ya se ve él mismo y no hay nada que adelantar.
    /// </summary>
    public bool TryGetPreview(out GameObject sample, out Vector3 position)
    {
        sample = null;
        position = Vector3.zero;

        if (body == null || CanWork()) return false;

        sample = body.gameObject;
        position = body.position;
        return true;
    }

    // ── El bucle ─────────────────────────────────────────────────────

    private IEnumerator WorkLoop()
    {
        while (true)
        {
            float duration = _cycleTime;
            float elapsed = 0f;

            progressUI?.Show(0f);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                progressUI?.SetFill(elapsed / duration);
                Animate(elapsed);
                yield return null;
            }

            progressUI?.SetFill(1f);
            EconomyManager.Instance?.AddCoins(_coinsPerCycle, CoinSource.Production);

            // No se esconde el círculo entre tandas: el trabajo no para, y
            // verlo apagarse y encenderse cada vez daría sensación de parón.
        }
    }

    private void Animate(float time)
    {
        if (body == null) return;

        float phase = time * bobSpeed * Mathf.PI * 2f;

        body.localPosition = _bodyHome + new Vector3(0f, Mathf.Abs(Mathf.Sin(phase)) * bobHeight, 0f);
        body.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(phase * 0.5f) * swayDegrees);
    }

    private void ResetBody()
    {
        if (body == null) return;

        body.localPosition = _bodyHome;
        body.localRotation = Quaternion.identity;
    }
}
