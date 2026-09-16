using UnityEngine;

/// <summary>
/// El latido del fantasma: entra creciendo desde un poco más pequeño y luego
/// respira despacio.
///
/// Lo de respirar no es adorno: un dibujo quieto y semitransparente se
/// confunde con algo que ya está puesto y apagado. Moviéndose se lee como
/// "esto todavía no existe".
/// </summary>
public class GhostPulse : MonoBehaviour
{
    [SerializeField] private float appearDuration = 0.22f;
    [SerializeField] private float appearFrom = 0.82f;

    [Tooltip("Cuánto crece y encoge al respirar")]
    [SerializeField] private float breath = 0.045f;

    [Tooltip("Respiraciones por segundo")]
    [SerializeField] private float breathSpeed = 0.7f;

    private SpriteRenderer[] _renderers;
    private float[] _targetAlpha;
    private Vector3 _baseScale;
    private float _elapsed;

    private void Awake()
    {
        _baseScale = transform.localScale;
        _renderers = GetComponentsInChildren<SpriteRenderer>();

        _targetAlpha = new float[_renderers.Length];
        for (int i = 0; i < _renderers.Length; i++)
            _targetAlpha[i] = _renderers[i].color.a;

        Apply(0f);
    }

    private void Update()
    {
        _elapsed += Time.deltaTime;
        Apply(_elapsed);
    }

    private void Apply(float time)
    {
        float appear = appearDuration > 0f ? Mathf.Clamp01(time / appearDuration) : 1f;
        float eased = Mathf.SmoothStep(0f, 1f, appear);

        float pulse = 1f + Mathf.Sin(time * breathSpeed * Mathf.PI * 2f) * breath * eased;
        transform.localScale = _baseScale * Mathf.Lerp(appearFrom, 1f, eased) * pulse;

        for (int i = 0; i < _renderers.Length; i++)
        {
            if (_renderers[i] == null) continue;

            Color color = _renderers[i].color;
            color.a = _targetAlpha[i] * eased;
            _renderers[i].color = color;
        }
    }
}
